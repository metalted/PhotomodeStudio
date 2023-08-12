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
    public class PSService : WebSocketBehavior
    {
        protected override void OnOpen()
        {
            if (PSManager.inALobby)
            {
                PSManager.ShouldWeNotifySocket();
            }
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            PSSocketManager.ReceivedMessage(e.Data);
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log("WebSocket closed: " + e.Reason);
        }

        public void SendMessage(string msg)
        {
            Send(msg);
        }
    }
}
