using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace GameplayFramework
{

    [RequireComponent(typeof(InputSystemUIInputModule), typeof(MultiplayerEventSystem), typeof(PlayerInput))]
    public class LocalPlayer : MonoBehaviour
    {
        private static List<LocalPlayer> localPlayers;
        public InputSystemUIInputModule UIInputModule { get; private set; }
        public MultiplayerEventSystem EventSystem { get; private set; }
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
            EventSystem = GetComponent<MultiplayerEventSystem>();
            PlayerInput = GetComponent<PlayerInput>();
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
            EventSystem.playerRoot = uiViewport.gameObject;
        }

        public void SetDevices(InputDevice[] devices)
        {
            Devices = devices;
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
    }
}