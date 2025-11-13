// Game types for Da Vinci Code

export enum BrickType {
  BLACK = 0,
  WHITE = 1
}

export enum GameState {
  WAITING = "WAITING",
  INITIAL_PICK = "INITIAL_PICK",
  ADJUST = "ADJUST",
  PLAYING = "PLAYING",
  GAME_OVER = "GAME_OVER"
}

export enum PlayerState {
  WAITING = "WAITING",
  PICK_BRICK = "PICK_BRICK",
  PLACE_JOKER = "PLACE_JOKER",
  GUESS_BRICK = "GUESS_BRICK",
  OPEN_BRICK = "OPEN_BRICK",
  WATCHING = "WATCHING",
  DONE = "DONE"
}

export interface Brick {
  id: string;
  number: number; // 0-11, or -1 for joker
  type: BrickType;
  isOpen: boolean;
  ownerId: string | null;
}

export interface Player {
  id: string;
  name: string;
  bricks: string[]; // Array of brick IDs in order
  state: PlayerState;
  isReady: boolean;
  isDone: boolean;
}

export interface GameRoom {
  id: string;
  name: string;
  maxPlayers: number;
  players: Map<string, Player>;
  bricks: Map<string, Brick>;
  unoccupiedBricks: string[];
  currentPlayerId: string | null;
  gameState: GameState;
  turnOrder: string[];
  createdAt: number;
}

export enum MessageType {
  // Client -> Server
  JOIN_ROOM = "JOIN_ROOM",
  LEAVE_ROOM = "LEAVE_ROOM",
  START_GAME = "START_GAME",
  PICK_BRICK = "PICK_BRICK",
  PLACE_JOKER = "PLACE_JOKER",
  GUESS_BRICK = "GUESS_BRICK",
  OPEN_BRICK = "OPEN_BRICK",
  READY = "READY",

  // Server -> Client
  ROOM_STATE = "ROOM_STATE",
  PLAYER_JOINED = "PLAYER_JOINED",
  PLAYER_LEFT = "PLAYER_LEFT",
  GAME_STARTED = "GAME_STARTED",
  BRICK_PICKED = "BRICK_PICKED",
  BRICK_OPENED = "BRICK_OPENED",
  JOKER_PLACED = "JOKER_PLACED",
  TURN_CHANGED = "TURN_CHANGED",
  GAME_OVER = "GAME_OVER",
  ERROR = "ERROR"
}

export interface Message {
  type: MessageType;
  playerId?: string;
  data?: any;
  timestamp: number;
}

export interface JoinRoomData {
  roomName: string;
  playerName: string;
  maxPlayers?: number;
}

export interface PickBrickData {
  brickId: string;
  position?: number;
}

export interface GuessBrickData {
  brickId: string;
  guessedNumber: number;
}

export interface PlaceJokerData {
  brickId: string;
  position: number;
}

export interface OpenBrickData {
  brickId: string;
}
