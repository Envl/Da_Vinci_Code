// Full game flow test
const WebSocket = require('ws');

console.log('🎮 Testing Full Game Flow...\n');

let player1, player2;
let player1Id, player2Id;
let testsPassed = [];
let testsFailed = [];

function pass(test) {
  testsPassed.push(test);
  console.log('✅', test);
}

function fail(test, reason) {
  testsFailed.push({ test, reason });
  console.log('❌', test, '-', reason);
}

// Player 1
player1 = new WebSocket('ws://localhost:8787/room/game-test');

player1.on('open', () => {
  pass('Player 1 WebSocket connection');
  player1.send(JSON.stringify({
    type: 'JOIN_ROOM',
    data: {
      roomName: 'game-test',
      playerName: 'Alice',
      maxPlayers: 2
    },
    timestamp: Date.now()
  }));
});

player1.on('message', (data) => {
  const msg = JSON.parse(data.toString());

  switch (msg.type) {
    case 'ROOM_STATE':
      player1Id = msg.playerId;
      pass('Player 1 joined room (ID: ' + player1Id.substring(0, 8) + '...)');
      if (msg.data.unoccupiedBricks.length === 26) {
        pass('All 26 bricks initialized');
      }
      break;

    case 'PLAYER_JOINED':
      pass('Player 1 received PLAYER_JOINED notification');
      // Start game after both joined
      setTimeout(() => {
        console.log('\n🚀 Player 1 starting game...');
        player1.send(JSON.stringify({
          type: 'START_GAME',
          playerId: player1Id,
          timestamp: Date.now()
        }));
      }, 500);
      break;

    case 'GAME_STARTED':
      pass('Game started successfully');
      console.log('   Current player:', msg.data.currentPlayerId === player1Id ? 'Player 1' : 'Player 2');

      // If it's player 1's turn, pick a brick
      if (msg.data.currentPlayerId === player1Id) {
        setTimeout(() => {
          const brickId = msg.data.unoccupiedBricks[0].id;
          console.log('\n🎯 Player 1 picking brick:', brickId);
          player1.send(JSON.stringify({
            type: 'PICK_BRICK',
            playerId: player1Id,
            data: { brickId },
            timestamp: Date.now()
          }));
        }, 500);
      }
      break;

    case 'BRICK_PICKED':
      if (msg.data.playerId === player1Id) {
        pass('Player 1 picked brick successfully');
        console.log('   Position:', msg.data.position);
        console.log('   Is Joker:', msg.data.isJoker);
      } else {
        pass('Player 1 received Player 2 brick pick notification');
      }
      break;

    case 'ERROR':
      fail('Server sent error', msg.data.error);
      break;
  }
});

player1.on('error', (error) => {
  fail('Player 1 connection error', error.message);
});

// Player 2
setTimeout(() => {
  player2 = new WebSocket('ws://localhost:8787/room/game-test');

  player2.on('open', () => {
    pass('Player 2 WebSocket connection');
    player2.send(JSON.stringify({
      type: 'JOIN_ROOM',
      data: {
        roomName: 'game-test',
        playerName: 'Bob',
        maxPlayers: 2
      },
      timestamp: Date.now()
    }));
  });

  player2.on('message', (data) => {
    const msg = JSON.parse(data.toString());

    switch (msg.type) {
      case 'ROOM_STATE':
        player2Id = msg.playerId;
        pass('Player 2 joined room (ID: ' + player2Id.substring(0, 8) + '...)');
        if (msg.data.players.length === 2) {
          pass('Room has 2 players');
        }
        break;

      case 'GAME_STARTED':
        pass('Player 2 received GAME_STARTED');
        break;

      case 'BRICK_PICKED':
        if (msg.data.playerId === player1Id) {
          pass('Player 2 received Player 1 brick pick notification');
        }
        break;

      case 'ERROR':
        fail('Server sent error to Player 2', msg.data.error);
        break;
    }
  });

  player2.on('error', (error) => {
    fail('Player 2 connection error', error.message);
  });
}, 1000);

// Final report
setTimeout(() => {
  console.log('\n' + '='.repeat(50));
  console.log('📊 TEST RESULTS');
  console.log('='.repeat(50));
  console.log(`✅ Passed: ${testsPassed.length}`);
  testsPassed.forEach(test => console.log('   •', test));

  if (testsFailed.length > 0) {
    console.log(`\n❌ Failed: ${testsFailed.length}`);
    testsFailed.forEach(({ test, reason }) => console.log('   •', test, '-', reason));
  }

  console.log('\n' + '='.repeat(50));

  const successRate = (testsPassed.length / (testsPassed.length + testsFailed.length) * 100).toFixed(1);
  console.log(`Success Rate: ${successRate}%`);

  if (testsFailed.length === 0) {
    console.log('\n🎉 ALL TESTS PASSED! Server is fully operational!');
  } else {
    console.log('\n⚠️  Some tests failed. Check the errors above.');
  }

  // Cleanup
  if (player1) player1.close();
  if (player2) player2.close();

  setTimeout(() => process.exit(testsFailed.length === 0 ? 0 : 1), 500);
}, 5000);
