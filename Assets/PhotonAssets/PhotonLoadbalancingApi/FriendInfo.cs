// ----------------------------------------------------------------------------
// <copyright file="FriendInfo.cs" company="Exit Games GmbH">
//   Loadbalancing Framework for Photon - Copyright (C) 2013 Exit Games GmbH
// </copyright>
// <summary>
//   Collection of values related to a user / friend.
// </summary>
// <author>developer@photonengine.com</author>
// ----------------------------------------------------------------------------

namespace ExitGames.Client.Photon.LoadBalancing
{
#if UNITY_4_0 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7 || UNITY_5 || UNITY_5_0 || UNITY_5_1 || UNITY_6
    using Hashtable = ExitGames.Client.Photon.Hashtable;
#endif

    public class FriendInfo
    {
        public string Name { get; internal protected set; }
        public bool IsOnline { get; internal protected set; }
        public string Room { get; internal protected set; }
        public bool IsInRoom { get { return string.IsNullOrEmpty(this.Room); } }

        public override string ToString()
        {
            return string.Format("{0}\t({1})\tin room: '{2}'", this.Name, (this.IsOnline) ? "on": "off", this.Room );
        }
    }
}
