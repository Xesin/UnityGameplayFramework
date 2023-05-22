using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace GameplayFramework.Input
{

    public class InputComponent : MonoBehaviour
    {
        private class InputBind
        {
            public InputAction inputAction;
            public InputActionPhase actionPhase;
            public Action<InputAction.CallbackContext> action;

            public InputBind(InputAction inputAction, InputActionPhase phase, Action<InputAction.CallbackContext> action)
            {
                this.inputAction = inputAction;
                actionPhase = phase;
                this.action = action;
            }
        }

        public InputDevice[] Devices { get; private set; } = new InputDevice[0];

        public event Action<string> OnActionMapSwitch;

        private PlayerInput playerInput;

        private Dictionary<object, List<InputBind>> binds = new Dictionary<object, List<InputBind>>();

        public void Bind(object context, string actionName, Action<InputAction.CallbackContext> action, params InputActionPhase[] phases)
        {
            if (context == null)
            {
                Debug.LogError("Input context cannot be null");
                return;
            }

            AddBinding(context, actionName, (inputAction, phase) => new InputBind(inputAction, phase, action), phases);
        }

        public void Bind2DAxis(object context, string actionName, Action<Vector2> action, params InputActionPhase[] phases)
        {
            Action<InputAction.CallbackContext> actionMapper = (context) =>
            {
                action?.Invoke(context.ReadValue<Vector2>());
            };

            AddBinding(context, actionName, (inputAction, phase) => new InputBind(inputAction, phase, actionMapper), phases);
        }

        public void BindAxis(object context, string actionName, Action<float> action, params InputActionPhase[] phases)
        {
            Action<InputAction.CallbackContext> actionMapper = (context) =>
            {
                action?.Invoke(context.ReadValue<float>());
            };

            AddBinding(context, actionName, (inputAction, phase) => new InputBind(inputAction, phase, actionMapper), phases);
        }

        public void BindAction(object context, string actionName, Action action, params InputActionPhase[] phases)
        {
            Action<InputAction.CallbackContext> actionMapper = (context) =>
            {
                action?.Invoke();
            };

            AddBinding(context, actionName, (inputAction, phase) => new InputBind(inputAction, phase, actionMapper), phases);
        }

        public InputAction GetAction(string name)
        {
            return playerInput.actions.FindAction(name);
        }

        public void ClearBinds(object context)
        {
            if (!binds.TryGetValue(context, out var actionsList))
            {
                Debug.LogWarning($"No binds found for context {context}");
                return;
            }

            for (int i = 0; i < actionsList.Count; i++)
            {
                InputBind inputBind = actionsList[i];

                switch (inputBind.actionPhase)
                {
                    case InputActionPhase.Started:
                        inputBind.inputAction.started -= inputBind.action;
                        break;
                    case InputActionPhase.Canceled:
                        inputBind.inputAction.canceled -= inputBind.action;
                        break;
                    case InputActionPhase.Performed:
                        inputBind.inputAction.performed -= inputBind.action;
                        break;
                }
            }
        }

        public void SetDevices(InputDevice[] devices)
        {
            Devices = devices;
        }

        public void AddDevice(InputDevice device)
        {
            var totalDevices = playerInput.devices.ToList();
            totalDevices.Add(device);
            playerInput.SwitchCurrentControlScheme(GetControlScheme(device.name, playerInput.actions), totalDevices.ToArray());
            SetDevices(totalDevices.ToArray());
        }

        public void DisableInput()
        {
            playerInput.DeactivateInput();
        }

        public void ActivateInput()
        {
            playerInput.ActivateInput();
        }

        public void SetPlayerInput(PlayerInput playerInput)
        {
            if (this.playerInput != playerInput)
            {
                this.playerInput = playerInput;
                foreach (var bind in binds)
                {
                    var context = bind.Key;
                    foreach (var bindDefinition in bind.Value)
                    {
                        Bind(context, bindDefinition.inputAction.name, bindDefinition.action, bindDefinition.actionPhase);
                        switch (bindDefinition.actionPhase)
                        {
                            case InputActionPhase.Started:
                                bindDefinition.inputAction.started -= bindDefinition.action;
                                break;
                            case InputActionPhase.Canceled:
                                bindDefinition.inputAction.canceled -= bindDefinition.action;
                                break;
                            case InputActionPhase.Performed:
                                bindDefinition.inputAction.performed -= bindDefinition.action;
                                break;
                        }
                    }
                }
            }

        }

        public void SetActionMap(string mapNameOrId)
        {
            playerInput.SwitchCurrentActionMap(mapNameOrId);
            OnActionMapSwitch?.Invoke(mapNameOrId);
        }

        private void AddBinding(object context, string actionName, Func<InputAction, InputActionPhase, InputBind> inputBindConstructor, params InputActionPhase[] phases)
        {
            InputAction inputAction = playerInput.actions.FindAction(actionName);
            if (inputAction == null)
            {
                Debug.LogError($"No input action with the name/id {actionName} found");
                return;
            }

            if (!binds.TryGetValue(context, out var actionsList))
            {
                actionsList = new List<InputBind>();
                binds.Add(context, actionsList);
            }

            for (int i = 0; i < phases.Length; i++)
            {
                var phase = phases[i];
                var inputBind = inputBindConstructor(inputAction, phase);
                actionsList.Add(inputBind);
                switch (phase)
                {
                    case InputActionPhase.Started:
                        inputAction.started += inputBind.action;
                        break;
                    case InputActionPhase.Canceled:
                        inputAction.canceled += inputBind.action;
                        break;
                    case InputActionPhase.Performed:
                        inputAction.performed += inputBind.action;
                        break;
                }
            }




        }

        public static string GetControlScheme(string type, InputActionAsset asset)
        {
            string group;

            type = type.ToLower();

            if (type.Contains("keyboard") || type.Contains("mouse"))
            {
                type = "Keyboard PC";
            }
            else if (type.Contains("ps4"))
            {
                type = "PS4";
            }
            else if (type.Contains("ps5"))
            {
                type = "PS5";
            }
            else if (type.Contains("switch"))
            {
                type = "Switch";
            }
            else if (type.Contains("gamepad") || type.Contains("xinput") || type.Contains("xbox"))
            {
                type = "GamePad PC";
            }
            else
            {
                type = string.Empty;
            }

            group = asset.controlSchemes.Where(x => x.name == type).Select(y => y.bindingGroup).DefaultIfEmpty(string.Empty).First();
            return group;
        }
    }
}