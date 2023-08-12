using BepInEx;
using HarmonyLib;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using BepInEx.Logging;
using System.Linq;
using ZeepkistClient;
using ZeepkistNetworking;
using System.Collections.Generic;

namespace PhotomodeStudio
{
    public class PSPlayer
    {
        public uint uid;
        public string username;
        public ZeepkistNetworkPlayer znp;
    }

    public class SocketMessage
    {
        public string messageType;
        public List<SocketKVPair> data;

        public SocketMessage(string messageType)
        {
            this.messageType = messageType;
            data = new List<SocketKVPair>();
        }
    }

    public class LayoutConfiguration
    {
        public Dictionary<string, SocketKVPair> layout { get; set; }
        public int rows { get; set; }
        public int columns { get; set; }
    }

    public class SocketKVPair
    {
        public int k;
        public string v;

        public SocketKVPair(int k, string v)
        {
            this.k = k;
            this.v = v;
        }
    }

    public static class PSManager
    {        
        //The bepinex logger, because we can't log through unity.
        private static ManualLogSource logger;
        //Are we currently in a lobby?
        public static bool inALobby = false;
        //The dictionary that contains all the players that are currently in the lobby.
        public static Dictionary<uint, PSPlayer> players = new Dictionary<uint, PSPlayer>();
        //The current layout configuration
        public static LayoutConfiguration layout = null;
        //This is true if we are currently in studio mode
        public static bool inStudioMode = false;

        //Save the logger and initialize the websocket.
        public static void Initialize(ManualLogSource log)
        {
            logger = log;
            PSSocketManager.Initialize(8081);
        }

        //Log a message to the console.
        public static void Log(string msg)
        {
            logger.LogInfo(msg);
        }

        public static void UsernameBeingSet(uint uid, string name)
        {
            //Check if the current player list contains a player with this UID.
            if(players.ContainsKey(uid))
            {
                players[uid].username = name;
            }

            //As we updated the player information, check if we should notify the socket as well.
            ShouldWeNotifySocket();
        }

        //Either we joined the lobby, a player has left or a player has joined. Refill the player dictionary with the current state.
        public static void PlayerUpdate()
        {
            //Clear the player dictionary.
            players.Clear();

            //Go over all players in the network dictionary.
            foreach (KeyValuePair<uint, ZeepkistNetworkPlayer> player in ZeepkistNetwork.Players)
            {
                //Create a new player object.
                PSPlayer psplayer = new PSPlayer()
                {
                    uid = player.Key,
                    username = player.Value.Username,
                    znp = player.Value
                };

                //If this is the local player we dont add it, cause we are the one in photomode so we are not racing.
                if (!player.Value.IsLocal)
                {
                    players.Add(player.Key, psplayer);
                }
            }

            //We got a player update so we must be in a lobby.
            inALobby = true;

            //Check if we should notify the socket right now.
            ShouldWeNotifySocket();
        }

        //Check if we should notify the socket right now. We only notify if all users in the list have a username assigned to them.
        public static void ShouldWeNotifySocket()
        {
            SocketMessage playerMessage = new SocketMessage("Playerlist");

            //There are no players, no update.
            if (players.Count == 0)
            {
                return;
            }

            foreach (KeyValuePair<uint, PSPlayer> player in players)
            {
                playerMessage.data.Add(new SocketKVPair((int)player.Key, player.Value.username));

                if (player.Value.username == "")
                {
                    //User is not assigned so we dont update.
                    return;
                }
            }

            //All usernames are assigned.
            PSSocketManager.SendMessage(playerMessage);
        }

        //Called when we disconnect from a lobby.
        public static void Disconnected()
        {
            inALobby = false;
            players.Clear();
            SocketMessage disconnectMessage = new SocketMessage("Disconnected");
            PSSocketManager.SendMessage(disconnectMessage);
        }

        //Called when we received a layout configuration
        public static void ReceivedLayoutConfiguration(LayoutConfiguration layoutConfiguration)
        {
            layout = layoutConfiguration;

            if(inStudioMode)
            {
                GenerateStudioView();
            }

            /*
            foreach(KeyValuePair<string, SocketKVPair> views in layoutConfiguration.layout)
            {
                Log(views.Key + "," + views.Value.k + "," + views.Value.v);
            }
            Log("Columns: " + layoutConfiguration.columns);
            Log("Rows: " + layoutConfiguration.rows);*/
        }

        //This function will generate the entire studio view.
        public static List<GameObject> activeStudioCameras = new List<GameObject>();
        
        public static void GenerateStudioView()
        {
            //First destroy all the current cameras.
            foreach(GameObject asc in activeStudioCameras)
            {
                if(asc != null)
                {
                    GameObject.Destroy(asc);
                }                
            }
            activeStudioCameras.Clear();

            //Calculate the size of the camera rects.
            Vector2 cameraRectSize = new Vector2(1f / layout.columns, 1f / layout.rows);

            //Go over each possible camera view location.

            for (int y = 0; y < layout.rows; y++)
            {
                for (int x = 0; x < layout.columns; x++)
                {
                    string viewID = (y * layout.columns + x).ToString();

                    if(layout.layout.ContainsKey(viewID))
                    {
                        SocketKVPair assignedPlayer = layout.layout[viewID];

                        //Try to get the player.
                        PSPlayer player = null;
                        if(players.ContainsKey((uint) assignedPlayer.k))
                        {
                            player = players[(uint)assignedPlayer.k];
                        }

                        if(player == null) { continue; }

                        if(player.znp == null) { continue; }

                        if(player.znp.Zeepkist == null) { continue; }

                        if(player.znp.Zeepkist.transform == null) { continue; }

                        //Player is found. Create a new camera object.
                        GameObject studioCameraObject = new GameObject("StudioCamera");
                        //Add a camera
                        Camera studioCam = studioCameraObject.AddComponent<Camera>();
                        //Set the camera rect
                        studioCam.rect = new Rect(x * cameraRectSize.x, (layout.rows - y - 1) * cameraRectSize.y, cameraRectSize.x, cameraRectSize.y);
                        //Attach the camera object to the player.
                        studioCameraObject.transform.parent = player.znp.Zeepkist.ghostModel.transform;
                        //Reposition the camera
                        studioCameraObject.transform.localPosition = new Vector3(0, 1, -4.5f);
                        //Remove any rotations
                        studioCameraObject.transform.localRotation = Quaternion.identity;
                        //Add the object to the camera list.
                        activeStudioCameras.Add(studioCameraObject);
                    }
                }
            }
        }
    }
}
