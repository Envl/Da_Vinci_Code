extends Node

# Game Manager - Singleton for managing game state and networking

enum GameState {
	MENU,
	WAITING,
	INITIAL_PICK,
	ADJUST,
	PLAYING,
	GAME_OVER
}

enum PlayerState {
	WAITING,
	PICK_BRICK,
	PLACE_JOKER,
	GUESS_BRICK,
	OPEN_BRICK,
	WATCHING,
	DONE
}

var current_state: GameState = GameState.MENU
var player_state: PlayerState = PlayerState.WAITING

# Networking
var websocket: WebSocketPeer
var server_url: String = "ws://localhost:8787/room/"
var room_name: String = ""
var player_id: String = ""
var player_name: String = ""

# Game data
var players: Array = []
var my_bricks: Array = []
var unoccupied_bricks: Array = []
var current_player_id: String = ""
var max_players: int = 4

# Signals
signal connected_to_server
signal disconnected_from_server
signal room_state_updated
signal player_joined(player_data)
signal player_left(player_id)
signal game_started
signal brick_picked(player_id, brick_data)
signal brick_opened(player_id, brick_id)
signal turn_changed(player_id)
signal game_over(winner_id)
signal error_received(error_message)

func _ready() -> void:
	set_process(false)

func connect_to_room(p_room_name: String, p_player_name: String, p_server_url: String = "") -> void:
	room_name = p_room_name
	player_name = p_player_name

	if p_server_url != "":
		server_url = p_server_url

	websocket = WebSocketPeer.new()
	var url = server_url + room_name
	var error = websocket.connect_to_url(url)

	if error != OK:
		push_error("Failed to connect to server: " + str(error))
		emit_signal("error_received", "Failed to connect to server")
		return

	set_process(true)

func disconnect_from_room() -> void:
	if websocket:
		send_message({
			"type": "LEAVE_ROOM",
			"playerId": player_id,
			"timestamp": Time.get_unix_time_from_system()
		})
		websocket.close()
		websocket = null

	set_process(false)
	emit_signal("disconnected_from_server")

func _process(_delta: float) -> void:
	if not websocket:
		return

	websocket.poll()

	var state = websocket.get_ready_state()

	if state == WebSocketPeer.STATE_OPEN:
		while websocket.get_available_packet_count():
			var packet = websocket.get_packet()
			var json_string = packet.get_string_from_utf8()
			var json = JSON.new()
			var parse_result = json.parse(json_string)

			if parse_result == OK:
				var message = json.data
				handle_message(message)
			else:
				push_error("Failed to parse JSON: " + json_string)

	elif state == WebSocketPeer.STATE_CLOSED:
		var code = websocket.get_close_code()
		var reason = websocket.get_close_reason()
		push_error("WebSocket closed with code: %d, reason: %s" % [code, reason])
		set_process(false)
		emit_signal("disconnected_from_server")

func send_message(message: Dictionary) -> void:
	if not websocket or websocket.get_ready_state() != WebSocketPeer.STATE_OPEN:
		push_error("WebSocket is not connected")
		return

	var json_string = JSON.stringify(message)
	websocket.send_text(json_string)

func join_room() -> void:
	send_message({
		"type": "JOIN_ROOM",
		"data": {
			"roomName": room_name,
			"playerName": player_name,
			"maxPlayers": max_players
		},
		"timestamp": Time.get_unix_time_from_system()
	})

func start_game() -> void:
	send_message({
		"type": "START_GAME",
		"playerId": player_id,
		"timestamp": Time.get_unix_time_from_system()
	})

func pick_brick(brick_id: String) -> void:
	send_message({
		"type": "PICK_BRICK",
		"playerId": player_id,
		"data": {
			"brickId": brick_id
		},
		"timestamp": Time.get_unix_time_from_system()
	})

func place_joker(brick_id: String, position: int) -> void:
	send_message({
		"type": "PLACE_JOKER",
		"playerId": player_id,
		"data": {
			"brickId": brick_id,
			"position": position
		},
		"timestamp": Time.get_unix_time_from_system()
	})

func guess_brick(brick_id: String, guessed_number: int) -> void:
	send_message({
		"type": "GUESS_BRICK",
		"playerId": player_id,
		"data": {
			"brickId": brick_id,
			"guessedNumber": guessed_number
		},
		"timestamp": Time.get_unix_time_from_system()
	})

func open_brick(brick_id: String) -> void:
	send_message({
		"type": "OPEN_BRICK",
		"playerId": player_id,
		"data": {
			"brickId": brick_id
		},
		"timestamp": Time.get_unix_time_from_system()
	})

func ready() -> void:
	send_message({
		"type": "READY",
		"playerId": player_id,
		"timestamp": Time.get_unix_time_from_system()
	})

func handle_message(message: Dictionary) -> void:
	var msg_type = message.get("type", "")

	match msg_type:
		"ROOM_STATE":
			player_id = message.get("playerId", "")
			var data = message.get("data", {})
			players = data.get("players", [])
			unoccupied_bricks = data.get("unoccupiedBricks", [])
			current_player_id = data.get("currentPlayerId", "")

			# Extract my bricks
			for player in players:
				if player.get("id") == player_id:
					my_bricks = player.get("bricks", [])
					break

			emit_signal("room_state_updated")

		"PLAYER_JOINED":
			var player_data = message.get("data", {}).get("player", {})
			players.append(player_data)
			emit_signal("player_joined", player_data)

		"PLAYER_LEFT":
			var left_player_id = message.get("data", {}).get("playerId", "")
			players = players.filter(func(p): return p.get("id") != left_player_id)
			emit_signal("player_left", left_player_id)

		"GAME_STARTED":
			current_state = GameState.PLAYING
			var data = message.get("data", {})
			players = data.get("players", [])
			unoccupied_bricks = data.get("unoccupiedBricks", [])
			current_player_id = data.get("currentPlayerId", "")
			emit_signal("game_started")

		"BRICK_PICKED":
			var data = message.get("data", {})
			emit_signal("brick_picked", data.get("playerId"), data)

		"BRICK_OPENED":
			var data = message.get("data", {})
			emit_signal("brick_opened", data.get("playerId"), data.get("brickId"))

		"TURN_CHANGED":
			var data = message.get("data", {})
			current_player_id = data.get("currentPlayerId", "")
			emit_signal("turn_changed", current_player_id)

		"GAME_OVER":
			current_state = GameState.GAME_OVER
			var winner_id = message.get("data", {}).get("winnerId", "")
			emit_signal("game_over", winner_id)

		"ERROR":
			var error_msg = message.get("data", {}).get("error", "Unknown error")
			emit_signal("error_received", error_msg)
			push_error("Server error: " + error_msg)

func is_my_turn() -> bool:
	return current_player_id == player_id

func get_player_by_id(pid: String) -> Dictionary:
	for player in players:
		if player.get("id") == pid:
			return player
	return {}
