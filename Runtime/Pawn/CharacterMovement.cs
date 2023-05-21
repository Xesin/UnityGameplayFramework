using System.Net;
using UnityEngine;
using static Codice.Client.Common.Threading.ThreadWaiter;

namespace GameplayFramework
{
    public enum MovementMode
    {
        Walking,
        Falling
    }

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

        [Header("Jumping")]
        public bool applyGravityWhileJumping = true;
        public float jumpYVelocity = 10f;
        public float gravityScale = 1;
        private CharacterController characterController;
        private Character characterOwner;

        protected MovementMode movementMode = MovementMode.Walking;

        protected override void Awake()
        {
            characterController = GetComponent<CharacterController>();
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            Vector3 inputVector = ConsumeInputVector();

            if (characterOwner)
            {
                ControlledCharacterMove(inputVector, Time.deltaTime);
            }
        }

        public override void SetOwner(GameplayObject obj)
        {
            base.SetOwner(obj);
            characterOwner = obj as Character;
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

        public virtual void SetMovementMode(MovementMode movementMode)
        {
            var prevMovementMode = movementMode;
            this.movementMode = movementMode;

            characterOwner.OnMovementModeChanged(prevMovementMode);
        }

        /// <summary>
        /// Performs the character movement based on input and external forces
        /// </summary>
        /// <param name="Vector3">The input vector in world space</param>
        /// <param name="deltaTime"></param>
        protected virtual void ControlledCharacterMove(Vector3 inputVector, float deltaTime)
        {
            characterOwner.CheckJumpInput(deltaTime);

            acceleration = ScaleInputAcceleration(inputVector);

            PerformMovement(deltaTime);
        }

        /// <summary>
        /// Performs the actual movement based on the current movement mode.
        /// TODO: Add multiple movement modes
        /// </summary>
        /// <param name="deltaTime"></param>
        protected virtual void PerformMovement(float deltaTime)
        {
            characterOwner.ClearJumpInput(deltaTime);

            StartNewPhysics(deltaTime);

            PhysicsRotation(deltaTime);

        }

        protected virtual void StartNewPhysics(float deltaTime)
        {
            switch (movementMode)
            {
                case MovementMode.Walking:
                    PhysWalking(deltaTime);
                    break;
                case MovementMode.Falling:
                    PhysFalling(deltaTime);
                    break;
                default:
                    break;
            }
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

            if(!characterController.isGrounded)
            {
                SetMovementMode(MovementMode.Falling);
            }

        }

        protected virtual void PhysFalling(float deltaTime)
        {

            CalcVelocity(deltaTime, 0.2f, 0.1f);

            Vector3 gravity = Physics.gravity * gravityScale;
            float gravityTime = deltaTime;

            bool endingJumpForce = false;

            if(characterOwner.jumpForceTimeRemaining > 0f)
            {
                float jumpForceTime = Mathf.Min(characterOwner.jumpForceTimeRemaining, deltaTime);

                gravityTime = applyGravityWhileJumping ? deltaTime : Mathf.Max(0, deltaTime - jumpForceTime);

                characterOwner.jumpForceTimeRemaining -= jumpForceTime;

                if (characterOwner.jumpForceTimeRemaining <= 0.0f)
                {
                    characterOwner.ResetJumpState();
                    endingJumpForce = true;
                }
            }

            // Apply gravity
            velocity = NewFallVelocity(velocity, gravity, gravityTime);

            // Compute change in position(using midpoint integration method).
            Vector3 Adjusted = 0.5f * velocity * deltaTime;

            // Special handling if ending the jump force where we didn't apply gravity during the jump.
            if (endingJumpForce && !!applyGravityWhileJumping)
            {
                // We had a portion of the time at constant speed then a portion with acceleration due to gravity.
                // Account for that here with a more correct change in position.
                float NonGravityTime = Mathf.Max(0, deltaTime - gravityTime);
                Adjusted = 0.5f * (velocity) * gravityTime;
            }

            characterController.Move(Adjusted);

            if(characterController.isGrounded)
            {
                SetMovementMode(MovementMode.Walking);
            }
        }

        protected virtual Vector3 NewFallVelocity(Vector3 initialVelocity, Vector3 gravity, float deltaTime)
        {

            Vector3 Result = initialVelocity;

	        if (deltaTime > 0f)
	        {
		        // Apply gravity.
		        Result += gravity * deltaTime;

                // Don't exceed terminal velocity.
                float TerminalLimit = Mathf.Abs(200f);
		        if (Result.sqrMagnitude > TerminalLimit * TerminalLimit)
		        {
			        Vector3 GravityDir = gravity.normalized;

                    Result = Vector3.ProjectOnPlane(Result, GravityDir) + GravityDir * TerminalLimit;        }
	        }

	        return Result;
        }

        protected virtual void PhysicsRotation(float deltaTime)
        {
            if (orientToMovement && acceleration.sqrMagnitude > 0)
            {
                var currentRotation = transform.rotation.eulerAngles;
                var desiredRotation = Quaternion.LookRotation(acceleration, Vector3.up).eulerAngles;
                var deltaRotation = rotationRate.y * deltaTime;

                currentRotation.y = Mathf.MoveTowardsAngle(currentRotation.y, desiredRotation.y, deltaRotation);

                transform.rotation = Quaternion.Euler(currentRotation);
            }
        }

        protected virtual void MoveAlongFloor(Vector3 inVelocity, float deltaTime)
        {
            Vector3 delta = new Vector3(inVelocity.x, -9.8f, inVelocity.z);

            characterController.Move(delta * deltaTime);
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

        public bool DoJump()
        {
            if (characterOwner && characterOwner.CanJump())
            {
                velocity.y = Mathf.Max(velocity.y, jumpYVelocity);
                SetMovementMode(MovementMode.Falling);
                return true;
            }

            return false;
        }

        public virtual bool IsJumpAllowed()
        {
            return true;
        }

        public bool CanAttempJump()
        {
            return IsJumpAllowed() && (IsMovingOnGround() || IsFalling());
        }

        public virtual bool IsMovingOnGround()
        {
            return movementMode == MovementMode.Walking;
        }

        public virtual bool IsFalling()
        {
            return movementMode == MovementMode.Falling;
        }
    }
}
