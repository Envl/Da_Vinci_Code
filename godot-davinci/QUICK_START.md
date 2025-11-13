# Quick Start Guide

## 1. Backend Setup (5 minutes)

```bash
cd godot-davinci/durable-objects
npm install
npm run typecheck  # Verify TypeScript
npm run dev        # Start local server on port 8787
```

## 2. Open Godot Project

1. Open Godot 4.x
2. Import project: `godot-davinci/project.godot`
3. Setup GameManager autoload:
   - Project → Project Settings → Autoload
   - Script: `res://scripts/game_manager.gd`
   - Name: `GameManager`
   - Enable: ✓ Singleton

## 3. Play!

1. Press F5 to run
2. Server URL: `ws://localhost:8787/room/`
3. Room Name: `test-room`
4. Player Name: Your name
5. Click "Create Room" or "Join Room"

## Testing Multiplayer

Open multiple Godot instances or browsers:
- First player: Create Room
- Other players: Join Room with same name
- First player: Click "Start Game"

## TypeScript Verification

Always run typecheck before deploying:
```bash
npm run typecheck
```

No output = success! ✓

## Common Issues

### "Connection refused"
- Make sure `npm run dev` is running
- Check port 8787 is not in use

### "Room is full"
- Max 4 players per room
- Use different room name

### GameManager not found
- Setup autoload in Project Settings
- Restart Godot

## Architecture Overview

```
┌─────────────┐          ┌──────────────────┐          ┌─────────────┐
│   Godot     │          │  Durable Object  │          │   Godot     │
│  Client 1   │◄────────►│    GameRoom      │◄────────►│  Client 2   │
│  (GDScript) │ WebSocket│  (TypeScript)    │ WebSocket│  (GDScript) │
└─────────────┘          └──────────────────┘          └─────────────┘
```

All game logic runs in TypeScript on the server. Clients send actions and receive state updates.

## Key Files

- `scripts/game_manager.gd` - WebSocket client & state
- `durable-objects/src/GameRoom.ts` - Game logic
- `durable-objects/src/types.ts` - Type definitions
- `scenes/game_scene.gd` - Main game UI

## Next Steps

1. Read `README.md` for full documentation
2. Explore `src/types.ts` to understand data structures
3. Modify `GameRoom.ts` to customize game rules
4. Run `npm run typecheck` to verify changes
5. Deploy with `npm run deploy` when ready
