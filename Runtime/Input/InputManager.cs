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
            if (Application.isPlaying)
            {
                InputUser.onUnpairedDeviceUsed -= UnpairedDeviceUsed;
                --InputUser.listenForUnpairedDeviceActivity;
            }
        }

        internal void Initialize()
        {
            if (!initialized)
            {
                InputUser.onUnpairedDeviceUsed += UnpairedDeviceUsed;
                ++InputUser.listenForUnpairedDeviceActivity;
                LookForUnpairedDevices();

                Debug.Log("Input Manager initialized");

                initialized = true;
            }
        }

        private void LookForUnpairedDevices()
        {
            if (GameplayGlobalSettings.Instance.autocreatePlayerOne)
            {
                Debug.Log("CREATING PLAYER ONE");
                var playerOneDevices = GetPlayerOneDevicesCandidates();

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

        public InputDevice[] GetPlayerOneDevicesCandidates()
        {
            var unpairedDevices = InputUser.GetUnpairedInputDevices();
            var playerOneDevices = unpairedDevices.Where(device => device.name == "Mouse" || device.name == "Keyboard").ToArray();
            var otherDevices = unpairedDevices.Where(device => device.name != "Mouse" && device.name != "Keyboard");

            if (otherDevices.Count() > 0)
            {
                var device = otherDevices.ElementAt(0);
                var tmpDevices = playerOneDevices.Append(device);

                if (device.name.Contains("npad", System.StringComparison.OrdinalIgnoreCase) && otherDevices.Count() > 1)
                {
                    tmpDevices = playerOneDevices.Append(otherDevices.ElementAt(1));
                }
                playerOneDevices = tmpDevices.ToArray();
            }

            return playerOneDevices;
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

        public static void PerfomPairingWithDevice(InputDevice device, LocalPlayer playerController)
        {
            if (InputControlScheme.FindControlSchemeForDevices(playerController.Devices, playerController.PlayerInput.actions.controlSchemes,
                                    out var controlScheme, out var matchResult, mustIncludeDevice: device))
            {
                playerController.PlayerInput.SwitchCurrentControlScheme(matchResult.devices.ToArray());
            }
            matchResult.Dispose();
        }

        public static void PerfomPairingWithDevice(InputDevice[] devices, LocalPlayer playerController)
        {
            for (int i = 0; i < devices.Length; i++)
            {
                PerfomPairingWithDevice(devices[i], playerController);
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
