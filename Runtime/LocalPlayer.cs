using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Xesin.GameplayFramework
{

    [RequireComponent(typeof(PlayerInput))]
    public class LocalPlayer : MonoBehaviour
    {
        private static List<LocalPlayer> localPlayers;
        public InputSystemUIInputModule UIInputModule { get; private set; }
        public EventSystem EventSystem { get; set; }
        public PlayerInput PlayerInput { get; private set; }
        public UIViewport uiViewport { get; private set; }
        public InputDevice[] Devices { get; private set; } = new InputDevice[0];

        private PlayerController currentControledPlayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void OnLoadInit()
        {
            localPlayers = new List<LocalPlayer>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            localPlayers.Add(this);
            UIInputModule = GetComponent<InputSystemUIInputModule>();
            PlayerInput = GetComponent<PlayerInput>();

            EventSystem = EventSystem.current;
        }


        private void OnDestroy()
        {
            if (currentControledPlayer)
                Destroy(currentControledPlayer.gameObject);
        }

        public void SetPlayerController(PlayerController playerController)
        {
            currentControledPlayer = playerController;
        }

        public void SetPlayerViewport(UIViewport uiViewport)
        {
            this.uiViewport = uiViewport;
            if(EventSystem is MultiplayerEventSystem multiplayerEventSystem)
                multiplayerEventSystem.playerRoot = uiViewport.gameObject;
        }

        public void SetDevices(InputDevice[] devices)
        {
            Devices = devices;
        }

        public void AddNewDevice(InputDevice device)
        {
            InputDevice[] devices = Devices;
            Array.Resize(ref devices, Devices.Length + 1);
            Devices = devices;
            devices[^1] = device;
        }

        public void UnpairDevices()
        {
            Devices = new InputDevice[0];
            PlayerInput.user.UnpairDevices();
        }

        public int GetPlayerIndex()
        {
            return localPlayers.IndexOf(this);
        }

        public static LocalPlayer GetLocalPlayer(int index)
        {
            return index < localPlayers.Count ? localPlayers[index] : null;
        }

        public static int GetNumPlayers()
        {
            return localPlayers.Count;
        }

        public void SetCurrentCamera(Camera camera)
        {
            uiViewport.SetOutputCamera(camera);
        }

        public Pawn GetControlledPawn()
        {
            if(currentControledPlayer)
            {
                return currentControledPlayer.GetPawn();
            }

            return null;
        }
    }
}