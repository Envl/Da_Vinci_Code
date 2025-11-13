# Da Vinci Code - Godot + TypeScript Edition

A complete rebuild of the Da Vinci Code board game using Godot 4.x and TypeScript with Durable Objects for multiplayer communication.

## Overview

Da Vinci Code is a deduction board game where players try to guess their opponents' numbered tiles while protecting their own. This implementation features:

- **Godot 4.x** game engine with 3D graphics
- **TypeScript** for game logic and type safety
- **Cloudflare Durable Objects** for real-time multiplayer
- **WebSocket** communication between clients and server
- Support for 2-4 players

## Game Rules

1. Each player starts by picking 4 tiles from the table
2. Tiles are numbered 0-11 in black and white, plus 2 joker tiles
3. Players arrange their tiles in ascending order (black before white for same numbers)
4. On each turn:
   - Pick a new tile from the table
   - Place it in your hand in the correct order
   - Guess an opponent's tile
   - If correct, continue guessing; if wrong, reveal one of your tiles
5. Last player with unrevealed tiles wins!

## Project Structure

```
godot-davinci/
├── project.godot          # Godot project configuration
├── scenes/                # Game scenes
│   ├── main_menu.tscn    # Main menu UI
│   ├── game_scene.tscn   # Main game scene
│   └── brick.tscn        # Brick/tile prefab
├── scripts/               # GDScript files
│   ├── game_manager.gd   # Singleton for networking and state
│   ├── main_menu.gd      # Main menu logic
│   ├── game_scene.gd     # Game scene controller
│   └── brick.gd          # Brick/tile logic
├── assets/                # Game assets
│   ├── textures/
│   ├── materials/
│   └── fonts/
└── durable-objects/       # Multiplayer backend
    ├── src/
    │   ├── index.ts      # Worker entry point
    │   ├── GameRoom.ts   # Durable Object implementation
    │   └── types.ts      # TypeScript type definitions
    ├── package.json
    ├── tsconfig.json
    └── wrangler.toml
```

## Setup Instructions

### Prerequisites

- Godot 4.3 or later
- Node.js 18+ and npm
- Cloudflare account (for deploying Durable Objects)
- Wrangler CLI

### Backend Setup (Durable Objects)

1. Navigate to the durable-objects directory:
   ```bash
   cd godot-davinci/durable-objects
   ```

2. Install dependencies:
   ```bash
   npm install
   ```

3. Run typecheck to verify TypeScript:
   ```bash
   npm run typecheck
   ```

4. Start the development server:
   ```bash
   npm run dev
   ```

5. For production deployment:
   ```bash
   npm run deploy
   ```

### Frontend Setup (Godot)

1. Open Godot 4.x

2. Import the project:
   - Click "Import"
   - Navigate to `godot-davinci/project.godot`
   - Click "Import & Edit"

3. Configure autoload singleton:
   - Go to Project → Project Settings → Autoload
   - Add script: `res://scripts/game_manager.gd`
   - Name: `GameManager`
   - Enable Singleton

4. Run the project:
   - Press F5 or click the Play button
   - Enter server URL (default: `ws://localhost:8787/room/`)
   - Create or join a room

## TypeScript Architecture

### Type System

The project uses strict TypeScript with comprehensive type definitions:

```typescript
// Core game types
enum BrickType { BLACK = 0, WHITE = 1 }
enum GameState { WAITING, INITIAL_PICK, ADJUST, PLAYING, GAME_OVER }
enum PlayerState { WAITING, PICK_BRICK, PLACE_JOKER, GUESS_BRICK, OPEN_BRICK, WATCHING, DONE }

interface Brick {
  id: string;
  number: number;  // 0-11, or -1 for joker
  type: BrickType;
  isOpen: boolean;
  ownerId: string | null;
}

interface Player {
  id: string;
  name: string;
  bricks: string[];
  state: PlayerState;
  isReady: boolean;
  isDone: boolean;
}
```

### Durable Objects Communication

Durable Objects provide stateful, real-time game rooms:

- **Persistent state**: Game state survives server restarts
- **WebSocket support**: Real-time bidirectional communication
- **Automatic scaling**: Each room is an isolated Durable Object
- **Low latency**: Objects are automatically placed near users

Message flow:
```
Client → WebSocket → Durable Object → Process → Broadcast → All Clients
```

## API Reference

### Client Messages (Client → Server)

- `JOIN_ROOM`: Join or create a game room
- `LEAVE_ROOM`: Leave the current room
- `START_GAME`: Start the game (host only)
- `PICK_BRICK`: Pick a brick from the table
- `PLACE_JOKER`: Place a joker tile
- `GUESS_BRICK`: Guess an opponent's brick
- `OPEN_BRICK`: Open one of your bricks
- `READY`: Mark yourself as ready

### Server Messages (Server → Client)

- `ROOM_STATE`: Full room state update
- `PLAYER_JOINED`: A player joined the room
- `PLAYER_LEFT`: A player left the room
- `GAME_STARTED`: Game has started
- `BRICK_PICKED`: A brick was picked
- `BRICK_OPENED`: A brick was opened
- `JOKER_PLACED`: A joker was placed
- `TURN_CHANGED`: Turn changed to another player
- `GAME_OVER`: Game ended with a winner
- `ERROR`: Error message

## Development

### Running Typecheck

```bash
cd godot-davinci/durable-objects
npm run typecheck
```

This verifies all TypeScript code is type-safe before deployment.

### Local Development

1. Start the Durable Objects worker:
   ```bash
   cd godot-davinci/durable-objects
   npm run dev
   ```

2. Open Godot and run the project

3. Connect to `ws://localhost:8787/room/`

### Debugging

- **Godot**: Use built-in debugger (F7) and print statements
- **Durable Objects**: Check Wrangler logs in terminal
- **Network**: Use browser DevTools or Wireshark for WebSocket inspection

## Performance Considerations

- **Brick Physics**: Bricks use Godot's physics system for realistic scattering
- **Network Optimization**: Only essential state is synchronized
- **State Management**: Durable Objects persist state efficiently
- **Scalability**: Each room is isolated, supporting unlimited concurrent games

## Future Enhancements

- [ ] Add animations with Godot's AnimationPlayer
- [ ] Implement reconnection logic for disconnected players
- [ ] Add chat system
- [ ] Include sound effects and music
- [ ] Create AI opponents for single-player mode
- [ ] Add replay system
- [ ] Mobile touch controls
- [ ] Localization support

## License

This project is a educational rebuild of the Da Vinci Code board game.

## Credits

- Original Unity implementation: [Envl/Da_Vinci_Code](https://github.com/Envl/Da_Vinci_Code)
- Game design: Da Vinci Code board game
- Rebuilt with Godot 4.x and TypeScript
- Networking powered by Cloudflare Durable Objects

## Support

For issues or questions:
1. Check the documentation above
2. Review the TypeScript types in `src/types.ts`
3. Examine the game logic in `GameRoom.ts`
4. Test with `npm run typecheck`
