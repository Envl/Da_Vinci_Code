import { GameRoom } from './GameRoom';

export { GameRoom };

export default {
  async fetch(request: Request, env: any): Promise<Response> {
    const url = new URL(request.url);

    // CORS headers
    const corsHeaders = {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Methods': 'GET, POST, OPTIONS',
      'Access-Control-Allow-Headers': 'Content-Type',
    };

    if (request.method === 'OPTIONS') {
      return new Response(null, { headers: corsHeaders });
    }

    // Route to create or join a game room
    if (url.pathname.startsWith('/room/')) {
      const roomName = url.pathname.split('/')[2];

      if (!roomName) {
        return new Response('Room name required', { status: 400 });
      }

      // Get or create the Durable Object for this room
      const id = env.GAME_ROOM.idFromName(roomName);
      const stub = env.GAME_ROOM.get(id);

      return stub.fetch(request);
    }

    return new Response('Not found', { status: 404 });
  }
};
