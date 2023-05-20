using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using static GameplayFramework.Input.InputManager;

namespace GameplayFramework.Input
{

    public class InputComponent : MonoBehaviour
    {
        private struct InputBind
        {
            public InputAction inputAction;
            public InputActionPhase actionPhase;
            public Action<InputAction.CallbackContext> action;
        }

        public InputDevice[] Devices { get; private set; } = new InputDevice[0];

        private PlayerInput playerInput;

        private Dictionary<object, List<InputBind>> binds = new Dictionary<object, List<InputBind>>();

        public void BindAction(object context, string actionName, Action<InputAction.CallbackContext> action, params InputActionPhase[] phases)
        {
            if (context == null)
            {
                Debug.LogError("Input context cannot be null");
                return;
            }

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

                actionsList.Add(new InputBind()
                {
                    inputAction = inputAction,
                    action = action,
                    actionPhase = phase
                });

                switch (phase)
                {
                    case InputActionPhase.Started:
                        inputAction.started += action;
                        break;
                    case InputActionPhase.Canceled:
                        inputAction.canceled += action;
                        break;
                    case InputActionPhase.Performed:
                        inputAction.performed += action;
                        break;
                }
            }
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

        public void SwtichActionMap(Maps newMap)
        {
            playerInput.SwitchCurrentActionMap(Enum.GetName(typeof(Maps), newMap));
        }

        public void DisableActionMap(Maps newMap)
        {
            playerInput.actions.FindActionMap(Enum.GetName(typeof(Maps), newMap)).Disable();
        }

        public void SetPlayerInput(PlayerInput playerInput)
        {

            if(this.playerInput != playerInput)
            {
                this.playerInput = playerInput;
                foreach (var bind in binds)
                {
                    var context = bind.Key;
                    foreach (var bindDefinition in bind.Value)
                    {
                        BindAction(context, bindDefinition.inputAction.name, bindDefinition.action, bindDefinition.actionPhase);
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