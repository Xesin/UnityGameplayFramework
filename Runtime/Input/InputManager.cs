using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
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
            if (GameplayGlobalSettings.Instance.autocreatePlayerOne)
            {
                var playerOneDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name == "Mouse" || device.name == "Keyboard").ToArray();
                var otherDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name != "Mouse" && device.name != "Keyboard");

                if (otherDevices.Count() > 0)
                {
                    playerOneDevices = playerOneDevices.Append(otherDevices.ElementAt(0)).ToArray();
                }

                var controlScheme = InputControlScheme.FindControlSchemeForDevices(
                    new ReadOnlyArray<InputDevice>(playerOneDevices, 0, playerOneDevices.Length), 
                    GameplayGlobalSettings.Instance.localPlayerPrefab.GetComponent<PlayerInput>().actions.controlSchemes,
                    allowUnsuccesfulMatch: true) ?? default;

                List<InputDevice> initialDevices = new List<InputDevice>();

                for (var i = 0; i < playerOneDevices.Length; ++i)
                {
                    var device = playerOneDevices[i];
                    if (controlScheme.SupportsDevice(device))
                        initialDevices.Add(device);
                }

                var playerOneInput = CreatePlayerWithDevices(initialDevices.ToArray(), playerOneDevices);
                
            }
        }

        private void UnpairedDeviceUsed(InputControl control, InputEventPtr eventPtr)
        {
            var device = control.device;
            int controllers = LocalPlayer.GetNumPlayers();

            for (int i = 0; i < controllers; i++)
            {
                LocalPlayer playerController = LocalPlayer.GetLocalPlayer(i);

                if (playerController.Devices.Contains(device))
                {
                    PerfomPairingWithDevice(device, playerController);
                    return;
                }
            }

            if (!GameplayGlobalSettings.Instance.autocreatePlayersOnInput)
            {
                LocalPlayer playerController = LocalPlayer.GetLocalPlayer(0);
                if(playerController)
                {
                    playerController.AddNewDevice(device);
                    PerfomPairingWithDevice(device, playerController);
                }
            }
            else
            {
                _ = CreatePlayer(device);
            }
        }

        private static void PerfomPairingWithDevice(InputDevice device, LocalPlayer playerController)
        {
            if (InputControlScheme.FindControlSchemeForDevices(playerController.Devices, playerController.PlayerInput.actions.controlSchemes,
                                    out var controlScheme, out var matchResult, mustIncludeDevice: device))
            {
                playerController.PlayerInput.SwitchCurrentControlScheme(matchResult.devices.ToArray());
            }
        }

        public LocalPlayer CreatePlayer(params InputDevice[] devices)
        {
            var player = CreatePlayerWithDevices(devices, devices);
            GameMode.Instance.OnNewPlayerAdded(player);
            return player;
        }

        private LocalPlayer CreatePlayerWithDevices(InputDevice[] initialDevices, params InputDevice[] devices)
        {
            Debug.Log("Creating player with " + devices.Length + " devices");
            if (LocalPlayer.GetNumPlayers() >= MAX_PLAYERS)
                return null;

            var player = PlayerInput.Instantiate(GameplayGlobalSettings.Instance.localPlayerPrefab.gameObject, -1, null, -1, initialDevices).GetComponent<LocalPlayer>();

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
