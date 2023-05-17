using UnityEngine;
using UnityEngine.InputSystem;
using GameplayFramework;
using GameplayFramework.Input;
using UnityEditor.Rendering.LookDev;

public class FPSCharacter : Character
{
    public GameObject crosshair;
    public Camera fpsCamera;
    public GameObject weapon;


    Vector2 lasLookInput;
    Vector2 lastMoveInput;

    private void Start()
    {
        if (Controller)
            ViewportSubsystem.Instance.AddToScreen((PlayerController)Controller, Instantiate(crosshair));
    }

    protected override void Update()
    {
        if (!Controller) return;


        AddControllerPitchInput(lasLookInput.y);
        AddControllerYawInput(lasLookInput.x);
        AddMoveInput(transform.forward, lastMoveInput.y);
        AddMoveInput(transform.right, lastMoveInput.x);

        base.Update();
    }

    public override void Restart()
    {
        base.Restart();
        lasLookInput = Vector2.zero;
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        lastMoveInput = context.ReadValue<Vector2>();
    }
    private void OnLookContinuous(InputAction.CallbackContext context)
    {
        lasLookInput = context.ReadValue<Vector2>();
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
        return fpsCamera.transform.position;
    }

    public override void SetupPlayerInput(InputComponent inputComponent)
    {
        base.SetupPlayerInput(inputComponent);
        inputComponent.BindAction(this, "Move", OnMove, InputActionPhase.Performed, InputActionPhase.Canceled);
        inputComponent.BindAction(this, "Look", OnLook, InputActionPhase.Performed, InputActionPhase.Canceled);
        inputComponent.BindAction(this, "Fire", OnFire, InputActionPhase.Performed);
        inputComponent.BindAction(this, "LookContinuous", OnLookContinuous, InputActionPhase.Performed, InputActionPhase.Canceled);
    }
}
