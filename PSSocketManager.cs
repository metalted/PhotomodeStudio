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
using Newtonsoft.Json;

namespace PhotomodeStudio
{
    public static class PSSocketManager
    {
        public static WebSocketServer wsServer;
        public static WebSocket connection;
        private static int socketPort = 0;
        public static bool init = false;

        public static void Initialize(int port)
        {
            if (init) { return; }

            socketPort = port;

            //Initialize the websocket.
            wsServer = new WebSocketServer(socketPort);
            wsServer.AddWebSocketService<PSService>("/");
            wsServer.Start();
            PSManager.Log("Started Websocket for Photomode Studio!");

            init = true;
        }

        public static void SendMessage(SocketMessage message)
        {
            if (wsServer.WebSocketServices.TryGetServiceHost("/", out var host))
            {
                var service = (PSService)host.Sessions.Sessions.FirstOrDefault();
                if (service != null)
                {
                    service.SendMessage(JsonConvert.SerializeObject(message));
                }
            }
        }

        public static void ReceivedMessage(string data)
        {
            if(data.Contains("layout"))
            {
                LayoutConfiguration layoutConfiguration = JsonConvert.DeserializeObject<LayoutConfiguration>(data);
                PSManager.ReceivedLayoutConfiguration(layoutConfiguration);
            }
        }
    }
}
