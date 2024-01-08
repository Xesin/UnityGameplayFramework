using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework.Input
{
    public class InputManager : MonoSingleton<InputManager>
    {
        private const int MAX_PLAYERS = 2;

        public UnityAction OnNewPlayer;

        private bool initialized = false;
        public bool Initialized => initialized;

        private CursorLockMode currentLockMode = CursorLockMode.None;
        private bool cursorVisibility = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void InitializeOnLoad()
        {
            Instance.Initialize();
        }


        private void Start()
        {
            if (Application.isPlaying)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Cursor.lockState = currentLockMode;
                Cursor.visible = cursorVisibility;
            }
            else
            {
                Debug.Log("Application lost focus");
            }
        }

        private void OnDestroy()
        {
#if !UNITY_SWITCH
            if (Application.isPlaying)
            {
                InputUser.onUnpairedDeviceUsed -= UnpairedDeviceUsed;
                --InputUser.listenForUnpairedDeviceActivity;
            }
#endif
        }

        internal void Initialize()
        {
            if (!initialized)
            {
#if !UNITY_SWITCH
                InputUser.onUnpairedDeviceUsed += UnpairedDeviceUsed;
                ++InputUser.listenForUnpairedDeviceActivity;
#endif
                LookForUnpairedDevices();

                Debug.Log("Input Manager initialized");

                initialized = true;
            }
        }

        private void LookForUnpairedDevices()
        {
            var playerOneDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name == "Mouse" || device.name == "Keyboard").ToArray();
            var otherDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name != "Mouse" && device.name != "Keyboard");

            Debug.Log("Found Devices: " + InputUser.GetUnpairedInputDevices().Count);

            if (otherDevices.Count() > 0)
            {
                playerOneDevices = playerOneDevices.Append(otherDevices.ElementAt(0)).ToArray();
            }

            if (playerOneDevices.Count() > 0)
            {
                var playerOneInput = CreatePlayerWithDevices(playerOneDevices);
            }

            if (GameplayGlobalSettings.Instance.autocreatePlayersOnInput)
            {
                var unpairedDevices = InputUser.GetUnpairedInputDevices().ToArray();

                for (int i = 0; i < unpairedDevices.Length; i++)
                {
                    if (playerOneDevices.Contains(unpairedDevices[i])) continue;

                    CreatePlayerWithDevices(unpairedDevices[i]);
                }
            }
        }

        private void UnpairedDeviceUsed(InputControl control, InputEventPtr eventPtr)
        {
            var device = control.device;
            int controllers = LocalPlayer.GetNumPlayers();
            if (!GameplayGlobalSettings.Instance.autocreatePlayersOnInput) return;

            for (int i = 0; i < controllers; i++)
            {
                LocalPlayer playerController = LocalPlayer.GetLocalPlayer(i);

                if (playerController.Devices.Contains(device)) return;
            }

            _ = CreatePlayerWithDevices(device);
        }

        public LocalPlayer CreatePlayer(InputDevice device)
        {
            var player = CreatePlayerWithDevices(device);
            GameMode.Instance.OnNewPlayerAdded(player);
            return player;
        }

        private LocalPlayer CreatePlayerWithDevices(params InputDevice[] devices)
        {
            Debug.Log("Creating player with " + devices.Length + " devices");
            if (LocalPlayer.GetNumPlayers() >= MAX_PLAYERS)
                return null;

            var player = PlayerInput.Instantiate(GameplayGlobalSettings.Instance.localPlayerPrefab.gameObject, -1, null, -1, devices).GetComponent<LocalPlayer>();

            player.SetDevices(devices);

            OnNewPlayer?.Invoke();

            return player;
        }

        public void SetCursorState(CursorLockMode lockMode, bool visibility)
        {
            currentLockMode = lockMode;
            cursorVisibility = visibility;
            Cursor.lockState = lockMode;
            Cursor.visible = visibility;
        }

    }
}
