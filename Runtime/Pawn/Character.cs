using GameplayFramework;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class Character : Pawn
{

    [SerializeField] private float moveSpeed = 4;
    public bool orientToMovement = false;
    public Vector3 rotationRate = new Vector3(0, 360, 0);

    private CharacterController charController;
    private Vector3 movementInput;

    protected override void Awake()
    {
        base.Awake();
        movementInput = Vector2.zero;
        charController = GetComponent<CharacterController>();
    }

    protected virtual void Update()
    {
        charController.SimpleMove(movementInput * moveSpeed);

        if(orientToMovement && movementInput.sqrMagnitude > 0)
        {
            var currentRotation = transform.rotation.eulerAngles;
            var desiredRotation = Quaternion.LookRotation(movementInput, Vector3.up).eulerAngles;
            var deltaRotation = rotationRate.y * Time.deltaTime;

            currentRotation.y = Mathf.MoveTowardsAngle(currentRotation.y, desiredRotation.y, deltaRotation);

            transform.rotation = Quaternion.Euler(currentRotation);
        }
        movementInput = Vector3.zero;
    }

    public override void Restart()
    {
        base.Restart();
        movementInput = Vector3.zero;
    }

    public void AddMoveInput(Vector3 worldSpaceInput, float scale = 1)
    {
        movementInput += worldSpaceInput * scale;
    }
}
