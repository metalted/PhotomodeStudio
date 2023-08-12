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
    //Setup the plugin
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGuid = "com.metalted.zeepkist.photomodestudio";
        public const string pluginName = "Photomode Studio";
        public const string pluginVersion = "1.0";

        private void Awake()
        {
            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginGuid} is loaded!");

            PSManager.Initialize(Logger);
        }

        public void Update()
        {
            if(Input.GetKeyDown(KeyCode.Keypad8))
            {
                //Are we in a lobby?
                if(PSManager.inALobby)
                {
                    GameMaster master = PlayerManager.Instance.currentMaster;

                    //We are not in studio mode yet.
                    if (!PSManager.inStudioMode)
                    {
                        //We have to be in photomode to be able to switch.
                        if (master.flyingCamera.isPhotoMode)
                        {
                            PSManager.inStudioMode = true;

                            //Turn of the flying camera
                            master.flyingCamera.FlyingCamera.gameObject.SetActive(false);
                        }
                    }
                    //We are in studio mode.
                    else
                    {
                        //Check if we are in photomode, if not this is prob a new lobby.
                        if (master.flyingCamera.isPhotoMode)
                        {
                            //We can switch back.
                            PSManager.inStudioMode = false;
                            //Turn on the flying camera
                            master.flyingCamera.FlyingCamera.gameObject.SetActive(true);
                        }
                        else
                        {
                            //We arent in photomode, so only reset the flag.
                            PSManager.inStudioMode = false;
                        }
                    }
                }

                PSManager.Log("Studio mode is now: " + (PSManager.inStudioMode ? "ON" : "OFF"));
            }
        }
    }

    [HarmonyPatch(typeof(ZeepkistNetwork), "OnInitialState")]
    public class ZKN_OnInitialState
    {
        public static void Postfix()
        {
            PSManager.PlayerUpdate();
        }
    }

    [HarmonyPatch(typeof(ZeepkistNetwork), "OnPlayerConnected")]
    public class ZKN_OnPlayerConnected
    {
        public static void Postfix()
        {
            PSManager.PlayerUpdate();
        }
    }

    [HarmonyPatch(typeof(ZeepkistNetwork), "OnPlayerDisconnected")]
    public class ZKN_OnPlayerDisconnected
    {
        public static void Postfix()
        {
            PSManager.PlayerUpdate();
        }
    }

    [HarmonyPatch(typeof(ZeepkistNetwork), "Clear")]
    public class ZKN_OnClear
    {
        public static void Postfix()
        {
            PSManager.Disconnected();
        }
    }

    [HarmonyPatch(typeof(PlayerBase), "SetUsername")]
    public class ZKN_PlayerBase_SetUserName
    {
        public static void Postfix(ref string name, PlayerBase __instance)
        {
            PSManager.UsernameBeingSet(__instance.UID, name);            
        }
    }
}
