using Xesin.GameplayFramework.Input;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [RequireComponent(typeof(InputComponent))]
    public class PlayerController : Controller
    {
        [SerializeField]
        private InputComponent inputComponent;
        private Vector3 rotationInput;

        protected LocalPlayer player;

        private static List<PlayerController> playerControllers = new List<PlayerController>();

        private void Awake()
        {
            inputComponent = GetComponent<InputComponent>();
            playerControllers.Add(this);
            InputManager.Instance.SetCursorState(GetDefaultCursorLockMode(), GetDefaultCursorVisibility());
        }

        protected virtual void LateUpdate()
        {
            UpdateRotation(Time.deltaTime);

            rotationInput = Vector3.zero;
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                Cursor.lockState = GetDefaultCursorLockMode();
                Cursor.visible = GetDefaultCursorVisibility();
            }
            else
            {
                Debug.Log("Application lost focus");
            }
        }

        private void OnDestroy()
        {
            if (possesedPawn)
                possesedPawn.Restart();

            possesedPawn = null;

            playerControllers.Remove(this);
        }


        private void UpdateRotation(float deltaTime)
        {
            // Calculate Delta to be applied on ViewRotation
            Vector3 DeltaRot = rotationInput;
            DeltaRot.x = -DeltaRot.x;
            Vector3 ViewRotation = GetControlRotation();

            ViewRotation = ViewRotation + DeltaRot * deltaTime;

            //ViewRotation.x = Mathf.Clamp(ViewRotation.x, 0, 89.9f);
            ViewRotation.y = ViewRotation.y % 360;
            ViewRotation.x = Mathf.Clamp(ViewRotation.x, -89.9f, 89.9f);

            SetControlRotation(ViewRotation);

            Pawn pawn = GetPawn();
            if (pawn)
            {
                pawn.FaceRotation(ViewRotation, deltaTime);
            }
        }

        public override void Posses(Pawn pawn)
        {
            SetControlRotation(pawn.transform.rotation.eulerAngles);
            base.Posses(pawn);
            Camera camera = pawn.GetComponentInChildren<Camera>();
            if (camera)
            {
                player.SetCurrentCamera(camera);
                camera.enabled = true;
            }
        }

        protected virtual CursorLockMode GetDefaultCursorLockMode()
        {
            return CursorLockMode.Locked;
        }

        protected virtual bool GetDefaultCursorVisibility()
        {
            return false;
        }


        public void SetPlayer(LocalPlayer player)
        {
            this.player = player;
            inputComponent.SetPlayerInput(player.PlayerInput);
        }

        public LocalPlayer GetPlayer()
        {
            return player;
        }

        public InputComponent GetInputComponent()
        {
            return inputComponent;
        }

        public int GetPlayerIndex()
        {
            return playerControllers.IndexOf(this);
        }

        public static PlayerController GetPlayerController(int index)
        {
            return index < playerControllers.Count ? playerControllers[index] : null;
        }

        public static int GetNumPlayerControllers()
        {
            return playerControllers.Count;
        }

        public void AddPitchInput(float value)
        {
            rotationInput.x += value;
        }

        public void AddYawInput(float value)
        {
            rotationInput.y += value;
        }

        public void AddRollInput(float value)
        {
            rotationInput.z += value;
        }

        public void SetInputUIOnly()
        {
            inputComponent.DisableInput();
            player.UIInputModule.enabled = true;
        }

        public void SetInputGameplayOnly()
        {
            inputComponent.ActivateInput();
            player.UIInputModule.enabled = false;
        }

        public void SetInputGameplayAndUI()
        {
            inputComponent.ActivateInput();
            player.UIInputModule.enabled = true;
        }
    }
}