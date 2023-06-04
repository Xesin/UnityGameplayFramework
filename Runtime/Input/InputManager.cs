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
            var playerOneDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name == "Mouse" || device.name == "Keyboard").ToArray();
            var otherDevices = InputUser.GetUnpairedInputDevices().Where(device => device.name != "Mouse" && device.name != "Keyboard");

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
            int controllers = PlayerController.GetNumPlayerControllers();
            if (!GameplayGlobalSettings.Instance.autocreatePlayersOnInput) return;

            for (int i = 0; i < controllers; i++)
            {
                PlayerController playerController = PlayerController.GetPlayerController(i);

                InputComponent inputComponent = playerController.GetInputComponent();

                if (inputComponent.Devices.Contains(device)) return;
            }

            var player = CreatePlayerWithDevices(device);
        }

        public LocalPlayer CreatePlayer(InputDevice device)
        {
            var player = CreatePlayerWithDevices(device);
            GameMode.Instance.OnNewPlayerAdded(player);
            return player;
        }

        private LocalPlayer CreatePlayerWithDevices(params InputDevice[] devices)
        {
            if (LocalPlayer.GetNumPlayers() >= MAX_PLAYERS)
                return null;

            var player = PlayerInput.Instantiate(GameplayGlobalSettings.Instance.localPlayerPrefab.gameObject, -1, null, -1, devices).GetComponent<LocalPlayer>();

            player.SetDevices(devices);

            OnNewPlayer?.Invoke();

            return player;
        }

    }
}
