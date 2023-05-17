using GameplayFramework.Input;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCharacter : Character
{
    public GameObject crosshair;
    public Camera thirdPersonCamera;
    public GameObject weapon;

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
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        lastMoveInput = context.ReadValue<Vector2>();
    }
    private void OnLookContinuous(InputAction.CallbackContext context)
    {
        lastLookInput = context.ReadValue<Vector2>();
    }

    private void OnLook(InputAction.CallbackContext context)
    {
        Vector2 axisValue = context.ReadValue<Vector2>();
        AddControllerPitchInput(axisValue.y);
        AddControllerYawInput(axisValue.x);
    }

    private void OnFire(InputAction.CallbackContext context)
    {
        Vector3 viewPosition;
        Quaternion viewDirection;

        GetEyesViewPoint(out viewPosition, out viewDirection);

        Debug.DrawLine(viewPosition, viewPosition + viewDirection * Vector3.forward * 10f, Color.red, 1f);
    }

    public override Vector3 GetPawnViewLocation()
    {
        return thirdPersonCamera.transform.position;
    }

    public override void SetupPlayerInput(InputComponent inputComponent)
    {
        base.SetupPlayerInput(inputComponent);
        inputComponent.BindAction(this, "Move", OnMove, InputActionPhase.Performed, InputActionPhase.Canceled);
        //inputComponent.BindAction(this, "Look", OnLook, InputActionPhase.Performed, InputActionPhase.Canceled);
        inputComponent.BindAction(this, "Fire", OnFire, InputActionPhase.Performed);
        inputComponent.BindAction(this, "LookContinuous", OnLookContinuous, InputActionPhase.Performed, InputActionPhase.Canceled);
    }
}
