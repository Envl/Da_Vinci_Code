// ----------------------------------------------------------------------------
// <copyright file="Room.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   The Room class resembles the properties known about the room in which
//   a game/match happens.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Client.Photon.LoadBalancing
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using ExitGames.Client.Photon;

#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_5_0 || UNITY_5_1 || UNITY_6
    using Hashtable = ExitGames.Client.Photon.Hashtable;
#endif

    /// <summary>
    /// This class represents a room a client joins/joined.
    /// Mostly used through LoadBalancingClient.CurrentRoom, after joining any room.
    /// Contains a list of current players, their properties and those of this room, too.
    /// A room instance has a number of "well known" properties like IsOpen, MaxPlayers which can be changed.
    /// Your own, custom properties can be set via SetCustomProperties() while being in the room.
    /// </summary>
    /// <remarks>
    /// Typically, this class should be extended by a game-specific implementation with logic and extra features.
    /// </remarks>
    public class Room : RoomInfo
    {
        protected internal int PlayerTTL;
        protected internal int RoomTTL;

        /// <summary>
        /// A reference to the LoadbalancingClient which is currently keeping the connection and state.
        /// </summary>
        protected internal LoadBalancingClient LoadBalancingClient { get; set; }

        /// <summary>The name of a room. Unique identifier (per Loadbalancing group) for a room/match.</summary>
        /// <remarks>The name can't be changed once it's set. The setter is used to apply the name, if created by the server.</remarks>
        public new string Name
        {
            get
            {
                return this.name;
            }

            internal set
            {
                this.name = value;
            }
        }

        /// <summary>
        /// Defines if the room can be joined.
        /// This does not affect listing in a lobby but joining the room will fail if not open.
        /// If not open, the room is excluded from random matchmaking.
        /// Due to racing conditions, found matches might become closed while users are trying to join.
        /// Simply re-connect to master and find another.
        /// Use property "IsVisible" to not list the room.
        /// </summary>
        /// <remarks>
        /// As part of RoomInfo this can't be set.
        /// As part of a Room (which the player joined), the setter will update the server and all clients.
        /// </remarks>
        public new bool IsOpen
        {
            get
            {
                return this.isOpen;
            }

            set
            {
                if (!this.IsLocalClientInside)
                {
                    LoadBalancingClient.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
                }

                if (value != this.isOpen)
                {
                    LoadBalancingClient.OpSetPropertiesOfRoom(new Hashtable() { { GamePropertyKey.IsOpen, value } });
                }

                this.isOpen = value;
            }
        }

        /// <summary>
        /// Defines if the room is listed in its lobby.
        /// Rooms can be created invisible, or changed to invisible.
        /// To change if a room can be joined, use property: open.
        /// </summary>
        /// <remarks>
        /// As part of RoomInfo this can't be set.
        /// As part of a Room (which the player joined), the setter will update the server and all clients.
        /// </remarks>
        public new bool IsVisible
        {
            get
            {
                return this.isVisible;
            }

            set
            {
                if (!this.IsLocalClientInside)
                {
                    LoadBalancingClient.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
                }

                if (value != this.isVisible)
                {
                    LoadBalancingClient.OpSetPropertiesOfRoom(new Hashtable() { { GamePropertyKey.IsVisible, value } });
                }

                this.isVisible = value;
            }
        }

        /// <summary>
        /// Sets a limit of players to this room. This property is synced and shown in lobby, too.
        /// If the room is full (players count == maxplayers), joining this room will fail.
        /// </summary>
        /// <remarks>
        /// As part of RoomInfo this can't be set.
        /// As part of a Room (which the player joined), the setter will update the server and all clients.
        /// </remarks>
        public new byte MaxPlayers
        {
            get
            {
                return this.maxPlayers;
            }

            set
            {
                if (!this.IsLocalClientInside)
                {
                    LoadBalancingClient.DebugReturn(DebugLevel.WARNING, "Can't set room properties when not in that room.");
                }

                if (value != this.maxPlayers)
                {
                    LoadBalancingClient.OpSetPropertiesOfRoom(new Hashtable() { { GamePropertyKey.MaxPlayers, value } });
                }

                this.maxPlayers = value;
            }
        }

        /// <summary>Gets the count of players in this Room (using this.Players.Count).</summary>
        public new byte PlayerCount
        {
            get
            {
                if (this.Players == null)
                {
                    return 0;
                }

                return (byte)this.Players.Count;
            }
        }

        /// <summary>While inside a Room, this is the list of players who are also in that room.</summary>
        private Dictionary<int, Player> players = new Dictionary<int, Player>();

        /// <summary>While inside a Room, this is the list of players who are also in that room.</summary>
        public Dictionary<int, Player> Players
        {
            get
            {
                return players;
            }

            private set
            {
                players = value;
            }
        }

        /// <summary>
        /// The ID (actorID, actorNumber) of the player who's the master of this Room.
        /// Note: This changes when the current master leaves the room.
        /// </summary>
        public int MasterClientId { get; private set; }

        /// <summary>
        /// Gets a list of custom properties that are in the RoomInfo of the Lobby.
        /// This list is defined when creating the room and can't be changed afterwards. Compare: LoadBalancingClient.OpCreateRoom()
        /// </summary>
        /// <remarks>You could name properties that are not set from the beginning. Those will be synced with the lobby when added later on.</remarks>
        public string[] PropsListedInLobby
        {
            get
            {
                return this.propsListedInLobby;
            }

            private set
            {
                this.propsListedInLobby = value;
            }
        }

        /// <summary>Creates a Room with null for name and no properties.</summary>
        protected internal Room() : base(null, null)
        {
            // nothing set yet (until a room was assigned by master)
        }

        /// <summary>Creates a Room with given name and properties.</summary>
        protected internal Room(string roomName) : base(roomName, null)
        {
            // base sets name and (custom)properties. here we set "well known" properties
        }

        /// <summary>Creates a Room (representation) with given name and properties and the "listing options" as provided by parameters.</summary>
        /// <param name="roomName">Name of the room (can be null until it's actually created on server).</param>
        /// <param name="roomProperties">Custom room propertes (Hashtable with string-typed keys) of this room.</param>
        /// <param name="isVisible">Visible rooms show up in the lobby and can be joined randomly (a well-known room-property).</param>
        /// <param name="isOpen">Closed rooms can't be joined (a well-known room-property).</param>
        /// <param name="maxPlayers">The count of players that might join this room (a well-known room-property).</param>
        /// <param name="propsListedInLobby">List of custom properties that are used in the lobby (less is better).</param>
        [Obsolete]
        protected internal Room(string roomName, Hashtable roomProperties, bool isVisible, bool isOpen, byte maxPlayers, string[] propsListedInLobby)
            : base(roomName, roomProperties)
        {
            // base sets name and (custom)properties. here we set "well known" properties
            this.isVisible = isVisible;
            this.isOpen = isOpen;
            this.maxPlayers = maxPlayers;
            this.PropsListedInLobby = propsListedInLobby;
        }

        /// <summary>Creates a Room (representation) with given name and properties and the "listing options" as provided by parameters.</summary>
        /// <param name="roomName">Name of the room (can be null until it's actually created on server).</param>
        /// <param name="options">Room options.</param>
        protected internal Room(string roomName, RoomOptions options) : base(roomName, options.CustomRoomProperties)
        {
            // base sets name and (custom)properties. here we set "well known" properties
            this.isVisible = options.IsVisible;
            this.isOpen = options.IsOpen;
            this.maxPlayers = options.MaxPlayers;
            this.PropsListedInLobby = options.CustomRoomPropertiesForLobby;
            this.PlayerTTL = options.PlayerTtl;
            this.RoomTTL = options.EmptyRoomTtl;
        }

        /// <summary>
        /// Updates and synchronizes this Room's Custom Properties. Optionally, expectedProperties can be provided as condition.
        /// </summary>
        /// <remarks>
        /// Custom Properties are a set of string keys and arbitrary values which is synchronized
        /// for the players in a Room. They are available when the client enters the room, as
        /// they are in the response of OpJoin and OpCreate.
        ///
        /// Custom Properties either relate to the (current) Room or a Player (in that Room).
        ///
        /// Both classes locally cache the current key/values and make them available as
        /// property: CustomProperties. This is provided only to read them.
        /// You must use the method SetCustomProperties to set/modify them.
        ///
        /// Any client can set any Custom Properties anytime (when in a room).
        /// It's up to the game logic to organize how they are best used.
        ///
        /// You should call SetCustomProperties only with key/values that are new or changed. This reduces
        /// traffic and performance.
        ///
        /// Unless you define some expectedProperties, setting key/values is always permitted.
        /// In this case, the property-setting client will not receive the new values from the server but
        /// instead update its local cache in SetCustomProperties.
        ///
        /// If you define expectedProperties, the server will skip updates if the server property-cache
        /// does not contain all expectedProperties with the same values.
        /// In this case, the property-setting client will get an update from the server and update it's
        /// cached key/values at about the same time as everyone else.
        ///
        /// The benefit of using expectedProperties can be only one client successfully sets a key from
        /// one known value to another.
        /// As example: Store who owns an item in a Custom Property "ownedBy". It's 0 initally.
        /// When multiple players reach the item, they all attempt to change "ownedBy" from 0 to their
        /// actorNumber. If you use expectedProperties {"ownedBy", 0} as condition, the first player to
        /// take the item will have it (and the others fail to set the ownership).
        ///
        /// Properties get saved with the game state for Turnbased games (which use IsPersistent = true).
        /// </remarks>
        /// <param name="propertiesToSet">Hashtable of Custom Properties that changes.</param>
        /// <param name="expectedProperties">Provide some keys/values to use as condition for setting the new values. Client must be in room.</param>
        /// <param name="webForward">If true, this SetCustomProperties operation gets forwarded to your WebHooks. Client must be in room.</param>
        public virtual void SetCustomProperties(Hashtable propertiesToSet, Hashtable expectedProperties = null, bool webForward = false)
        {
            Hashtable customProps = propertiesToSet.StripToStringKeys() as Hashtable;

            // merge (and delete null-values), unless we use CAS (expected props)
            if (expectedProperties == null || expectedProperties.Count == 0)
            {
                this.CustomProperties.Merge(customProps);
                this.CustomProperties.StripKeysWithNullValues();
            }

            // send (sync) these new values if in room
            if (this.IsLocalClientInside)
            {
                this.LoadBalancingClient.loadBalancingPeer.OpSetPropertiesOfRoom(customProps, expectedProperties, webForward);
            }
        }

        /// <summary>
        /// Enables you to define the properties available in the lobby if not all properties are needed to pick a room.
        /// </summary>
        /// <remarks>
        /// Limit the amount of properties sent to users in the lobby as this improves speed and stability.
        /// </remarks>
        /// <param name="propsToListInLobby">An array of custom room property names to forward to the lobby.</param>
        public void SetPropertiesListedInLobby(string[] propsToListInLobby)
        {
            Hashtable customProps = new Hashtable();
            customProps[GamePropertyKey.PropsListedInLobby] = propsToListInLobby;

            bool sent = this.LoadBalancingClient.OpSetPropertiesOfRoom(customProps);
            if (sent)
            {
                this.propsListedInLobby = propsToListInLobby;
            }
        }

        /// <summary>
        /// Removes a player from this room's Players Dictionary.
        /// This is internally used by the LoadBalancing API. There is usually no need to remove players yourself.
        /// This is not a way to "kick" players.
        /// </summary>
        protected internal virtual void RemovePlayer(Player player)
        {
            this.Players.Remove(player.ID);
            player.RoomReference = null;

            if (player.ID == this.MasterClientId)
            {
                this.UpdateMasterClientId();
            }
        }

        /// <summary>
        /// Removes a player from this room's Players Dictionary.
        /// </summary>
        protected internal virtual void RemovePlayer(int id)
        {
            this.RemovePlayer(this.GetPlayer(id));
        }

        /// <summary>
        /// Internally used to mark a player as "offline" for async/turnbased games.
        /// </summary>
        /// <param name="id">The actorNumber (player.ID) of a player in this room.</param>
        protected internal virtual void MarkAsInactive(int id)
        {
            Player player = this.GetPlayer(id);
            if (player != null)
            {
                player.IsInactive = true;
            }
        }

        /// <summary>
        /// Picks a new master client and sets property MasterClientId accordingly.
        /// </summary>
        private void UpdateMasterClientId()
        {
            int lowestId = int.MaxValue;
            foreach (int id in this.Players.Keys)
            {
                if (id < lowestId)
                {
                    lowestId = id;
                }
            }

            // with 0 players, the lowest ID is 0 (and thus master is 0, too)
            if (this.players.Count == 0)
            {
                lowestId = 0;
            }

            this.MasterClientId = lowestId;
        }

        /// <summary>
        /// Checks if the player is in the room's list already and calls StorePlayer() if not.
        /// </summary>
        /// <param name="player">The new player - identified by ID.</param>
        /// <returns>False if the player could not be added (cause it was in the list already).</returns>
        public virtual bool AddPlayer(Player player)
        {
            if (!this.Players.ContainsKey(player.ID))
            {
                this.StorePlayer(player);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates a player reference in the Players dictionary (no matter if it existed before or not).
        /// </summary>
        /// <param name="player">The Player instance to insert into the room.</param>
        public virtual Player StorePlayer(Player player)
        {
            this.Players[player.ID] = player;
            player.RoomReference = this;

            // while initializing the room, the players are not guaranteed to be added in-order
            if (this.MasterClientId == 0 || player.ID < this.MasterClientId)
            {
                this.UpdateMasterClientId();
            }

            return player;
        }

        /// <summary>
        /// Tries to find the player with given actorNumber (a.k.a. ID).
        /// Only useful when in a Room, as IDs are only valid per Room.
        /// </summary>
        /// <param name="id">ID to look for.</param>
        /// <returns>The player with the ID or null.</returns>
        public virtual Player GetPlayer(int id)
        {
            Player result = null;
            this.Players.TryGetValue(id, out result);

            return result;
        }
    }
}