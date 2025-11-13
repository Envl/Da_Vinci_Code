import {
  Brick,
  BrickType,
  GameState,
  PlayerState,
  Player,
  Message,
  MessageType,
  JoinRoomData,
  PickBrickData,
  GuessBrickData,
  PlaceJokerData,
  OpenBrickData
} from './types';

export class GameRoom implements DurableObject {
  private state: DurableObjectState;
  private sessions: Set<WebSocket>;
  private roomName: string;
  private maxPlayers: number;
  private players: Map<string, Player>;
  private bricks: Map<string, Brick>;
  private unoccupiedBricks: string[];
  private currentPlayerId: string | null;
  private gameState: GameState;
  private turnOrder: string[];

  constructor(state: DurableObjectState, env: any) {
    this.state = state;
    this.sessions = new Set();
    this.roomName = '';
    this.maxPlayers = 4;
    this.players = new Map();
    this.bricks = new Map();
    this.unoccupiedBricks = [];
    this.currentPlayerId = null;
    this.gameState = GameState.WAITING;
    this.turnOrder = [];

    // Restore state from storage
    this.state.blockConcurrencyWhile(async () => {
      const stored = await this.state.storage.get('gameState');
      if (stored) {
        this.restoreState(stored as any);
      }
    });
  }

  async fetch(request: Request): Promise<Response> {
    const upgradeHeader = request.headers.get('Upgrade');
    if (upgradeHeader !== 'websocket') {
      return new Response('Expected WebSocket', { status: 426 });
    }

    const webSocketPair = new WebSocketPair();
    const [client, server] = Object.values(webSocketPair);

    this.handleSession(server);

    return new Response(null, {
      status: 101,
      webSocket: client,
    });
  }

  private handleSession(webSocket: WebSocket): void {
    this.sessions.add(webSocket);

    webSocket.accept();

    webSocket.addEventListener('message', async (event) => {
      try {
        const message: Message = JSON.parse(event.data as string);
        await this.handleMessage(webSocket, message);
      } catch (error) {
        this.sendError(webSocket, 'Invalid message format');
      }
    });

    webSocket.addEventListener('close', () => {
      this.sessions.delete(webSocket);
      // Handle player disconnect
      for (const [playerId, player] of this.players.entries()) {
        // Clean up disconnected player (could add reconnection logic)
      }
    });
  }

  private async handleMessage(ws: WebSocket, message: Message): Promise<void> {
    switch (message.type) {
      case MessageType.JOIN_ROOM:
        await this.handleJoinRoom(ws, message.data as JoinRoomData);
        break;
      case MessageType.LEAVE_ROOM:
        await this.handleLeaveRoom(message.playerId!);
        break;
      case MessageType.START_GAME:
        await this.handleStartGame(message.playerId!);
        break;
      case MessageType.PICK_BRICK:
        await this.handlePickBrick(message.playerId!, message.data as PickBrickData);
        break;
      case MessageType.PLACE_JOKER:
        await this.handlePlaceJoker(message.playerId!, message.data as PlaceJokerData);
        break;
      case MessageType.GUESS_BRICK:
        await this.handleGuessBrick(message.playerId!, message.data as GuessBrickData);
        break;
      case MessageType.OPEN_BRICK:
        await this.handleOpenBrick(message.playerId!, message.data as OpenBrickData);
        break;
      case MessageType.READY:
        await this.handleReady(message.playerId!);
        break;
    }
  }

  private async handleJoinRoom(ws: WebSocket, data: JoinRoomData): Promise<void> {
    if (this.players.size >= this.maxPlayers) {
      this.sendError(ws, 'Room is full');
      return;
    }

    if (!this.roomName) {
      this.roomName = data.roomName;
      this.maxPlayers = data.maxPlayers || 4;
      this.initializeBricks();
    }

    const playerId = crypto.randomUUID();
    const player: Player = {
      id: playerId,
      name: data.playerName,
      bricks: [],
      state: PlayerState.WAITING,
      isReady: false,
      isDone: false
    };

    this.players.set(playerId, player);
    await this.saveState();

    // Send player ID to the client
    this.send(ws, {
      type: MessageType.ROOM_STATE,
      playerId: playerId,
      data: this.getRoomState(playerId),
      timestamp: Date.now()
    });

    // Broadcast to all other players
    this.broadcast({
      type: MessageType.PLAYER_JOINED,
      data: { player },
      timestamp: Date.now()
    }, ws);
  }

  private async handleLeaveRoom(playerId: string): Promise<void> {
    this.players.delete(playerId);
    await this.saveState();

    this.broadcast({
      type: MessageType.PLAYER_LEFT,
      data: { playerId },
      timestamp: Date.now()
    });
  }

  private async handleStartGame(playerId: string): Promise<void> {
    if (this.players.size < 2) {
      return;
    }

    this.gameState = GameState.INITIAL_PICK;
    this.turnOrder = Array.from(this.players.keys());
    this.shuffleBricks();
    this.currentPlayerId = this.turnOrder[0];

    await this.saveState();

    this.broadcast({
      type: MessageType.GAME_STARTED,
      data: this.getRoomState(),
      timestamp: Date.now()
    });
  }

  private async handlePickBrick(playerId: string, data: PickBrickData): Promise<void> {
    const player = this.players.get(playerId);
    const brick = this.bricks.get(data.brickId);

    if (!player || !brick || brick.ownerId !== null) {
      return;
    }

    brick.ownerId = playerId;
    this.unoccupiedBricks = this.unoccupiedBricks.filter(id => id !== data.brickId);

    // Insert brick in sorted order
    const insertPos = this.findInsertPosition(player.bricks, brick);
    player.bricks.splice(insertPos, 0, data.brickId);

    // Check if it's a joker (-1)
    if (brick.number === -1) {
      player.state = PlayerState.PLACE_JOKER;
    } else {
      player.state = PlayerState.GUESS_BRICK;
    }

    await this.saveState();

    this.broadcast({
      type: MessageType.BRICK_PICKED,
      data: {
        playerId,
        brickId: data.brickId,
        position: insertPos,
        isJoker: brick.number === -1
      },
      timestamp: Date.now()
    });
  }

  private async handlePlaceJoker(playerId: string, data: PlaceJokerData): Promise<void> {
    const player = this.players.get(playerId);
    if (!player) return;

    // Remove and reinsert at specified position
    const brickIndex = player.bricks.indexOf(data.brickId);
    if (brickIndex !== -1) {
      player.bricks.splice(brickIndex, 1);
      player.bricks.splice(data.position, 0, data.brickId);
    }

    player.state = PlayerState.GUESS_BRICK;
    await this.saveState();

    this.broadcast({
      type: MessageType.JOKER_PLACED,
      data: { playerId, brickId: data.brickId, position: data.position },
      timestamp: Date.now()
    });
  }

  private async handleGuessBrick(playerId: string, data: GuessBrickData): Promise<void> {
    const brick = this.bricks.get(data.brickId);
    if (!brick || brick.ownerId === playerId) return;

    const guessCorrect = brick.number === data.guessedNumber;

    if (guessCorrect) {
      brick.isOpen = true;
      const owner = this.players.get(brick.ownerId!);

      // Check if owner has lost all bricks
      if (owner && this.checkAllBricksOpen(owner)) {
        owner.isDone = true;
        owner.state = PlayerState.DONE;
      }

      // Check for game over
      if (this.checkGameOver()) {
        this.gameState = GameState.GAME_OVER;
      }
    } else {
      // Wrong guess - player must reveal one of their bricks
      const player = this.players.get(playerId);
      if (player) {
        player.state = PlayerState.OPEN_BRICK;
      }
    }

    await this.saveState();

    this.broadcast({
      type: guessCorrect ? MessageType.BRICK_OPENED : MessageType.TURN_CHANGED,
      data: {
        playerId,
        brickId: data.brickId,
        guessedNumber: data.guessedNumber,
        correct: guessCorrect,
        brickNumber: guessCorrect ? brick.number : undefined
      },
      timestamp: Date.now()
    });

    if (!guessCorrect) {
      await this.nextTurn();
    }
  }

  private async handleOpenBrick(playerId: string, data: OpenBrickData): Promise<void> {
    const brick = this.bricks.get(data.brickId);
    const player = this.players.get(playerId);

    if (!brick || !player || brick.ownerId !== playerId) return;

    brick.isOpen = true;

    if (this.checkAllBricksOpen(player)) {
      player.isDone = true;
      player.state = PlayerState.DONE;
    }

    await this.saveState();

    this.broadcast({
      type: MessageType.BRICK_OPENED,
      data: { playerId, brickId: data.brickId },
      timestamp: Date.now()
    });

    await this.nextTurn();

    if (this.checkGameOver()) {
      this.gameState = GameState.GAME_OVER;
      this.broadcast({
        type: MessageType.GAME_OVER,
        data: { winnerId: this.getWinner() },
        timestamp: Date.now()
      });
    }
  }

  private async handleReady(playerId: string): Promise<void> {
    const player = this.players.get(playerId);
    if (!player) return;

    player.isReady = true;
    await this.saveState();

    // Check if all players are ready
    const allReady = Array.from(this.players.values()).every(p => p.isReady);
    if (allReady && this.gameState === GameState.ADJUST) {
      this.gameState = GameState.PLAYING;
      this.broadcast({
        type: MessageType.GAME_STARTED,
        data: this.getRoomState(),
        timestamp: Date.now()
      });
    }
  }

  private initializeBricks(): void {
    // Create 26 bricks: 0-11 in black, 0-11 in white, 2 jokers
    let brickId = 0;

    // Black bricks (0-11)
    for (let i = 0; i <= 11; i++) {
      const id = `brick_${brickId++}`;
      this.bricks.set(id, {
        id,
        number: i,
        type: BrickType.BLACK,
        isOpen: false,
        ownerId: null
      });
      this.unoccupiedBricks.push(id);
    }

    // Black joker
    const blackJokerId = `brick_${brickId++}`;
    this.bricks.set(blackJokerId, {
      id: blackJokerId,
      number: -1,
      type: BrickType.BLACK,
      isOpen: false,
      ownerId: null
    });
    this.unoccupiedBricks.push(blackJokerId);

    // White bricks (0-11)
    for (let i = 0; i <= 11; i++) {
      const id = `brick_${brickId++}`;
      this.bricks.set(id, {
        id,
        number: i,
        type: BrickType.WHITE,
        isOpen: false,
        ownerId: null
      });
      this.unoccupiedBricks.push(id);
    }

    // White joker
    const whiteJokerId = `brick_${brickId++}`;
    this.bricks.set(whiteJokerId, {
      id: whiteJokerId,
      number: -1,
      type: BrickType.WHITE,
      isOpen: false,
      ownerId: null
    });
    this.unoccupiedBricks.push(whiteJokerId);
  }

  private shuffleBricks(): void {
    for (let i = this.unoccupiedBricks.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [this.unoccupiedBricks[i], this.unoccupiedBricks[j]] =
        [this.unoccupiedBricks[j], this.unoccupiedBricks[i]];
    }
  }

  private findInsertPosition(brickIds: string[], newBrick: Brick): number {
    let position = 0;

    for (const brickId of brickIds) {
      const brick = this.bricks.get(brickId);
      if (!brick) continue;

      // Jokers are handled separately
      if (newBrick.number === -1) continue;
      if (brick.number === -1) {
        position++;
        continue;
      }

      // Higher numbers come first
      if (newBrick.number > brick.number) {
        position++;
      } else if (newBrick.number === brick.number) {
        // Same number: black comes before white
        if (newBrick.type > brick.type) {
          position++;
        }
      }
    }

    return position;
  }

  private checkAllBricksOpen(player: Player): boolean {
    return player.bricks.every(brickId => {
      const brick = this.bricks.get(brickId);
      return brick?.isOpen || false;
    });
  }

  private checkGameOver(): boolean {
    const activePlayers = Array.from(this.players.values()).filter(p => !p.isDone);
    return activePlayers.length === 1;
  }

  private getWinner(): string | null {
    const activePlayers = Array.from(this.players.values()).filter(p => !p.isDone);
    return activePlayers.length === 1 ? activePlayers[0].id : null;
  }

  private async nextTurn(): Promise<void> {
    const currentIndex = this.turnOrder.indexOf(this.currentPlayerId!);
    let nextIndex = (currentIndex + 1) % this.turnOrder.length;

    // Skip players who are done
    let attempts = 0;
    while (attempts < this.turnOrder.length) {
      const nextPlayerId = this.turnOrder[nextIndex];
      const nextPlayer = this.players.get(nextPlayerId);

      if (nextPlayer && !nextPlayer.isDone) {
        this.currentPlayerId = nextPlayerId;
        nextPlayer.state = this.unoccupiedBricks.length > 0
          ? PlayerState.PICK_BRICK
          : PlayerState.GUESS_BRICK;
        break;
      }

      nextIndex = (nextIndex + 1) % this.turnOrder.length;
      attempts++;
    }

    await this.saveState();

    this.broadcast({
      type: MessageType.TURN_CHANGED,
      data: { currentPlayerId: this.currentPlayerId },
      timestamp: Date.now()
    });
  }

  private getRoomState(forPlayerId?: string): any {
    return {
      roomName: this.roomName,
      maxPlayers: this.maxPlayers,
      players: Array.from(this.players.values()).map(p => ({
        ...p,
        // Don't reveal brick numbers to other players
        bricks: forPlayerId === p.id
          ? p.bricks.map(id => {
              const brick = this.bricks.get(id);
              return brick ? { ...brick } : null;
            }).filter(Boolean)
          : p.bricks.map(id => {
              const brick = this.bricks.get(id);
              return brick ? {
                id: brick.id,
                type: brick.type,
                isOpen: brick.isOpen,
                number: brick.isOpen ? brick.number : undefined
              } : null;
            }).filter(Boolean)
      })),
      unoccupiedBricks: this.unoccupiedBricks.map(id => {
        const brick = this.bricks.get(id);
        return brick ? { id: brick.id, type: brick.type } : null;
      }).filter(Boolean),
      currentPlayerId: this.currentPlayerId,
      gameState: this.gameState
    };
  }

  private broadcast(message: Message, exclude?: WebSocket): void {
    const data = JSON.stringify(message);
    for (const session of this.sessions) {
      if (session !== exclude) {
        session.send(data);
      }
    }
  }

  private send(ws: WebSocket, message: Message): void {
    ws.send(JSON.stringify(message));
  }

  private sendError(ws: WebSocket, error: string): void {
    this.send(ws, {
      type: MessageType.ERROR,
      data: { error },
      timestamp: Date.now()
    });
  }

  private async saveState(): Promise<void> {
    await this.state.storage.put('gameState', {
      roomName: this.roomName,
      maxPlayers: this.maxPlayers,
      players: Array.from(this.players.entries()),
      bricks: Array.from(this.bricks.entries()),
      unoccupiedBricks: this.unoccupiedBricks,
      currentPlayerId: this.currentPlayerId,
      gameState: this.gameState,
      turnOrder: this.turnOrder
    });
  }

  private restoreState(stored: any): void {
    this.roomName = stored.roomName || '';
    this.maxPlayers = stored.maxPlayers || 4;
    this.players = new Map(stored.players || []);
    this.bricks = new Map(stored.bricks || []);
    this.unoccupiedBricks = stored.unoccupiedBricks || [];
    this.currentPlayerId = stored.currentPlayerId || null;
    this.gameState = stored.gameState || GameState.WAITING;
    this.turnOrder = stored.turnOrder || [];
  }
}
