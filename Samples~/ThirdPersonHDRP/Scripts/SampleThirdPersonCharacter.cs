using UnityEngine;
using UnityEngine.InputSystem;
using Xesin.GameplayFramework.Input;

namespace Xesin.GameplayFramework.Samples
{
    public class SampleThirdPersonCharacter : Character
    {
        public GameObject crosshair;
        public Camera thirdPersonCamera;

        Vector2 lastLookInput;
        Vector2 lastMoveInput;

        protected override void Update()
        {
            if (lastLookInput != Vector2.zero)
            {
                AddControllerPitchInput(lastLookInput.y);
                AddControllerYawInput(lastLookInput.x);
            }

            var rotVector = new Vector3(0, GetControlRotation().y, 0);
            var quaternion = Quaternion.Euler(rotVector);

            AddMoveInput(quaternion * Vector3.forward, lastMoveInput.y);
            AddMoveInput(quaternion * Vector3.right, lastMoveInput.x);

            base.Update();
        }

        public override void Restart()
        {
            base.Restart();
            lastLookInput = Vector2.zero;
            lastMoveInput = Vector2.zero;
        }

        private void OnMove(Vector2 value)
        {
            lastMoveInput = value;
        }

        private void OnFire()
        {
            Vector3 viewPosition;
            Quaternion viewDirection;

            GetEyesViewPoint(out viewPosition, out viewDirection);

            Debug.DrawLine(viewPosition, viewPosition + viewDirection * Vector3.forward * 10f, Color.red, 1f);
        }


        private void OnLookContinuous(Vector2 value)
        {
            lastLookInput = value;
        }

        private void OnLook(Vector2 value)
        {
            Vector2 axisValue = value;
            AddControllerPitchInput(axisValue.y);
            AddControllerYawInput(axisValue.x);
        }


        public override Vector3 GetPawnViewLocation()
        {
            return thirdPersonCamera.transform.position;
        }


        public override void SetupPlayerInput(InputComponent inputComponent)
        {
            base.SetupPlayerInput(inputComponent);
            inputComponent.Bind2DAxis(this, "Move", OnMove, InputActionPhase.Performed, InputActionPhase.Canceled);
            inputComponent.Bind2DAxis(this, "Look", OnLook, InputActionPhase.Performed, InputActionPhase.Canceled);
            inputComponent.Bind2DAxis(this, "LookContinuous", OnLookContinuous, InputActionPhase.Performed, InputActionPhase.Canceled);
            inputComponent.BindAction(this, "Fire", OnFire, InputActionPhase.Performed);
            inputComponent.BindAction(this, "Jump", Jump, InputActionPhase.Performed);
            inputComponent.BindAction(this, "Jump", StopJumping, InputActionPhase.Canceled);
        }
    }
}