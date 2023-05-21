using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterMovement : PawnMovement
{
    public float maxWalkSpeed = 6f;
    public float maxAcceleration = 20.48f;
    public float groundFriction = 8f;
    public float brakingDecelerationWalking = 20.48f;
    public bool forceMaxAcceleration = false;
    public bool orientToMovement = false;
    public Vector3 rotationRate = new Vector3(0, 360, 0);

    private CharacterController characterController;

    protected override void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();
        Vector3 inputVector = ConsumeInputVector();

        ControlledCharacterMove(inputVector, Time.deltaTime);
    }

    /// <summary>
    /// Scales the input acceleration vector to a maximum acceleration value and clamps
    /// its magnitude to 1.0.
    /// </summary>
    /// <param name="Vector3">The input vector</param>
    /// <returns>
    /// a scaled input acceleration vector.
    /// </returns>
    protected virtual Vector3 ScaleInputAcceleration(Vector3 inputAcceleration)
    {
        return GetMaxAcceleration() * Vector3.ClampMagnitude(inputAcceleration, 1.0f);
    }


    /// <summary>
    /// Returns the maximum acceleration as a float value.
    /// </summary>
    /// <returns>
    /// Max acceleration value.
    /// </returns>
    public virtual float GetMaxAcceleration()
    {
        return maxAcceleration;
    }

    public override float GetMaxSpeed()
    {
        return maxWalkSpeed;
    }


    /// <summary>
    /// Performs the character movement based on input and external forces
    /// </summary>
    /// <param name="Vector3">The input vector in world space</param>
    /// <param name="deltaTime"></param>
    protected virtual void ControlledCharacterMove(Vector3 inputVector, float deltaTime)
    {
        acceleration = ScaleInputAcceleration(inputVector);

        PerformMovement(deltaTime);

        if (orientToMovement && velocity.sqrMagnitude > 0)
        {
            var currentRotation = transform.rotation.eulerAngles;
            var desiredRotation = Quaternion.LookRotation(inputVector, Vector3.up).eulerAngles;
            var deltaRotation = rotationRate.y * deltaTime;

            currentRotation.y = Mathf.MoveTowardsAngle(currentRotation.y, desiredRotation.y, deltaRotation);

            transform.rotation = Quaternion.Euler(currentRotation);
        }
    }

    /// <summary>
    /// Performs the actual movement based on the current movement mode.
    /// TODO: Add multiple movement modes
    /// </summary>
    /// <param name="deltaTime"></param>
    protected virtual void PerformMovement(float deltaTime)
    {
        PhysWalking(deltaTime);
    }

    /// <summary>
    /// Performs the character movement for the grounded state and walking movement mode
    /// </summary>
    /// <param name="deltaTime"></param>
    protected virtual void PhysWalking(float deltaTime)
    {
        Vector3 oldVelocity = velocity;
        Vector3 oldLocation = transform.position;

        acceleration.y = 0;
        CalcVelocity(deltaTime, groundFriction, brakingDecelerationWalking);
        Vector3 moveVelocity = velocity;

        MoveAlongFloor(moveVelocity, deltaTime);

    }

    protected virtual void MoveAlongFloor(Vector3 inVelocity, float deltaTime)
    {
        Vector3 delta = new Vector3(inVelocity.x, 0f, inVelocity.z);

        characterController.SimpleMove(delta);
    }

    /// <summary>
    /// Calculates the current velocity based on the acceleration, friction and brakingDeceleration
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="friction"></param>
    /// <param name="brakingDeceleration"></param>
    protected virtual void CalcVelocity(float deltaTime, float friction, float brakingDeceleration)
    {
        friction = Mathf.Max(0f, friction);
        float maxAccel = GetMaxAcceleration();
        float maxSpeed = GetMaxSpeed();

        bool zeroRequestedAcceleration = true;

        if (forceMaxAcceleration)
        {
            if (acceleration.sqrMagnitude > Mathf.Epsilon)
            {
                acceleration = acceleration.normalized * maxAccel;
            }
            else
            {
                acceleration = maxAccel * (velocity.sqrMagnitude < Mathf.Epsilon ? transform.forward : velocity.normalized);
            }
        }

        float maxInputSpeed = maxSpeed;

        bool zeroAcceleration = acceleration == Vector3.zero;
        bool velocityOverMax = IsExceedingMaxSpeed(maxSpeed);

        // Only apply braking if there is no acceleration, or we are over our max speed and need to slow down to it.
        if ((zeroAcceleration && zeroRequestedAcceleration) || velocityOverMax)
        {
            ApplyVelocityBraking(deltaTime, friction, brakingDeceleration);
        }
        else if (!zeroAcceleration)
        {
            // Friction affects our ability to change direction. This is only done for input acceleration, not path following.
            Vector3 accelerationDir = acceleration.normalized;
            float velocityMagniture = velocity.magnitude;
            velocity = velocity - (velocity - accelerationDir * velocityMagniture) * Mathf.Min(deltaTime * friction, 1f);
        }

        // Apply input acceleration
        if (!zeroAcceleration)
        {
            float newMaxInputSpeed = IsExceedingMaxSpeed(maxInputSpeed) ? velocity.magnitude : maxInputSpeed;
            velocity += acceleration * deltaTime;
            velocity = Vector3.ClampMagnitude(velocity, newMaxInputSpeed);
        }
    }


    /// <summary>
    /// Applies the braking to the velocity
    /// </summary>
    /// <param name="deltaTime"></param>
    /// <param name="friction"></param>
    /// <param name="brakingDeceleration"></param>
    protected void ApplyVelocityBraking(float deltaTime, float friction, float brakingDeceleration)
    {
        Vector3 oldVel = velocity;
        bool bZeroBraking = (brakingDeceleration == 0f);
        Vector3 RevAccel = (bZeroBraking ? Vector3.zero : (-brakingDeceleration * velocity.normalized));

        velocity = velocity + ((-friction) * velocity + RevAccel) * deltaTime;

        // Don't reverse direction
        if (Vector3.Dot(velocity, oldVel) <= 0f)
        {
            velocity = Vector3.zero;
            return;
        }
    }
}
