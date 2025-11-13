// Simpler synchronous test
const WebSocket = require('ws');

console.log('🎮 Testing Da Vinci Code WebSocket Server...\n');

const ws = new WebSocket('ws://localhost:8787/room/test-room');

ws.on('open', () => {
  console.log('✅ Connected to server');

  console.log('📤 Sending JOIN_ROOM...');
  ws.send(JSON.stringify({
    type: 'JOIN_ROOM',
    data: {
      roomName: 'test-room',
      playerName: 'TestPlayer',
      maxPlayers: 4
    },
    timestamp: Date.now()
  }));
});

ws.on('message', (data) => {
  const message = JSON.parse(data.toString());
  console.log('📨 Received:', message.type);
  console.log('   Data:', JSON.stringify(message.data || {}, null, 2).substring(0, 200));

  if (message.type === 'ROOM_STATE') {
    console.log('\n✅ Room joined successfully!');
    console.log('   Player ID:', message.playerId);
    console.log('   Room name:', message.data.roomName);
    console.log('   Max players:', message.data.maxPlayers);
    console.log('   Players count:', message.data.players.length);
    console.log('   Unoccupied bricks:', message.data.unoccupiedBricks.length);
    console.log('\n🎉 Server is working correctly!');

    setTimeout(() => {
      ws.close();
      process.exit(0);
    }, 1000);
  }
});

ws.on('error', (error) => {
  console.error('❌ Error:', error.message);
  process.exit(1);
});

ws.on('close', () => {
  console.log('🔌 Connection closed');
});

setTimeout(() => {
  console.log('⏱️ Timeout - test took too long');
  process.exit(1);
}, 10000);
