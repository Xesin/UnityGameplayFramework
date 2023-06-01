using System;
using UnityEditor.PackageManager;
using UnityEngine;

namespace GameplayFramework
{
    public enum MovementMode
    {
        None,
        Walking,
        Falling
    }

    public struct FindFloorResult
    {
        public bool blockingHit;

        // True when sweep test fails to find a walkable floor
        public bool lineTrace;

        public float floorDistance;

        public RaycastHit hitResult;
    }

    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovement : PawnMovement
    {
        public float maxAcceleration = 20.48f;
        public bool forceMaxAcceleration = false;
        public bool orientToMovement = false;
        public Vector3 rotationRate = new Vector3(0, 360, 0);

        [Header("Walking")]
        public float maxWalkSpeed = 6f;
        public float groundFriction = 8f;
        public float brakingDecelerationWalking = 20.48f;

        [Header("Falling")]
        public float fallingLateralFriction = 0f;
        public float brakingDecelerationFalling = 0f;
        public float airControl = 0.05f;
        public float airControlBoostMultiplier = 2f;
        public float airControlBoostVelocityThreshold = 25f;

        [Header("Jumping")]
        public bool applyGravityWhileJumping = true;
        public float jumpYVelocity = 10f;
        public float gravityScale = 1;

        private CharacterController characterController;
        protected RootMotionSource rootMotionComponent;

        protected MovementMode movementMode = MovementMode.Walking;
        protected RootMotionParams rootMotionParams;
        protected Vector3 animRootMotionVelocity;
        protected FindFloorResult currentFloor;
        protected bool forceFloorCheck;

        protected override void Awake()
        {
            characterController = GetComponent<CharacterController>();
            rootMotionComponent = GetComponentInChildren<RootMotionSource>();
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
            if (!HasValidData()) return;

            //forceFloorCheck |= IsMovingOnGround();

            if (rootMotionComponent)
            {
                RootMotionParams rootMotion = rootMotionComponent.ConsumeRootMotion();
                if (rootMotion.HasRootMotion)
                {
                    rootMotionParams.Accumulate(rootMotion);
                }
            }

            if (rootMotionParams.HasRootMotion)
            {
                animRootMotionVelocity = CalcAnimRootMotionVelocity(rootMotionParams.translation, deltaTime, velocity);
            }

            characterOwner.ClearJumpInput(deltaTime);

            StartNewPhysics(deltaTime);

            if(!rootMotionParams.HasRootMotion)
                PhysicsRotation(deltaTime);

            // Root motion has been used, clear it
            rootMotionParams.Clear();
        }

        private void ApplyRootMotionToVelocity()
        {
            if(rootMotionParams.HasRootMotion)
            {
                velocity = ConstrainAnimRootMotionVelocity(animRootMotionVelocity, velocity);
            }
        }

        private Vector3 ConstrainAnimRootMotionVelocity(Vector3 rootMotionVelocity, Vector3 currentVelocity)
        {
            Vector3 result = rootMotionVelocity;
            if(IsFalling())
            {
                result.y = currentVelocity.y;
            }

            return result;
        }

        private Vector3 CalcAnimRootMotionVelocity(Vector3 translation, float deltaTime, Vector3 velocity)
        {
            if (deltaTime > 0)
            {
                return translation / deltaTime;
            }
            else
            {
                return velocity;
            }
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
            if(!characterOwner || (!characterOwner.Controller && !rootMotionParams.HasRootMotion))
            {
                acceleration = Vector3.zero;
                velocity = Vector3.zero;
            }

            FindFloorResult oldFloor = currentFloor;

            acceleration.y = 0;

            if(!rootMotionParams.HasRootMotion)
            {
                CalcVelocity(deltaTime, groundFriction, GetMaxBrakingDeceleration());
            }

            ApplyRootMotionToVelocity();

            Vector3 moveVelocity = velocity;

            MoveAlongFloor(moveVelocity, deltaTime);

            if (!characterController.isGrounded)
            {
                SetMovementMode(MovementMode.Falling);
            }

            FindFloor(out currentFloor);
        }

        protected virtual void PhysFalling(float deltaTime)
        {
            Vector3 oldVelocityWithRootMotion = velocity;
            Vector3 oldVelocity = velocity;
            Vector3 fallAcceleration = GetFallingLateralAcceleration(deltaTime);
            fallAcceleration.y = 0;
            if (!rootMotionParams.HasRootMotion)
            {
                Vector3 restoreAccel = acceleration;
                acceleration = fallAcceleration;
                velocity.y = 0;
                CalcVelocity(deltaTime, fallingLateralFriction, GetMaxBrakingDeceleration());
                acceleration = restoreAccel;
                velocity.y = oldVelocity.y;
            }

            Vector3 gravity = Physics.gravity * gravityScale;
            float gravityTime = deltaTime;

            bool endingJumpForce = false;

            if (characterOwner.jumpForceTimeRemaining > 0f)
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
            ApplyRootMotionToVelocity();

            // Compute change in position(using midpoint integration method).
            Vector3 Adjusted = 0.5f * (oldVelocityWithRootMotion + velocity) * deltaTime;

            // Special handling if ending the jump force where we didn't apply gravity during the jump.
            if (endingJumpForce && !applyGravityWhileJumping)
            {
                // We had a portion of the time at constant speed then a portion with acceleration due to gravity.
                // Account for that here with a more correct change in position.
                float NonGravityTime = Mathf.Max(0, deltaTime - gravityTime);
                Adjusted = (oldVelocityWithRootMotion * NonGravityTime) + (0.5f * (velocity) * gravityTime);
            }

            characterController.Move(Adjusted);
            //characterController.SimpleMove(new Vector3(Adjusted.x, 0, Adjusted.z));
            if (characterController.isGrounded)
            {
                SetMovementMode(MovementMode.Walking);
            }
        }

        public virtual Vector3 GetFallingLateralAcceleration(float deltaTime)
        {
            Vector3 fallAcceleration = new Vector3(acceleration.x, 0, acceleration.z);

            if(!rootMotionParams.HasRootMotion && fallAcceleration.sqrMagnitude > 0)
            {
                fallAcceleration = GetAirControl(deltaTime, airControl, fallAcceleration);
                fallAcceleration = Vector3.ClampMagnitude(fallAcceleration, GetMaxAcceleration());
            }

            return fallAcceleration;
        }

        private Vector3 GetAirControl(float deltaTime, float airControl, Vector3 fallAcceleration)
        {
            if (airControl != 0f)
            {
                airControl = BoostAirControl(deltaTime, airControl, fallAcceleration);
            }

            return airControl * fallAcceleration;
        }

        private float BoostAirControl(float deltaTime, float airControl, Vector3 fallAcceleration)
        {
            // Allow a burst of initial acceleration
            Vector2 velocity2D = new Vector2(velocity.x, velocity.z);
            if (airControlBoostMultiplier > 0f && velocity2D.sqrMagnitude < airControlBoostVelocityThreshold)
            {
                airControl = Mathf.Min(1f, airControlBoostMultiplier * airControl);
            }

            return airControl;
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

                    Result = Vector3.ProjectOnPlane(Result, GravityDir) + GravityDir * TerminalLimit;
                }
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
            Vector3 delta = new Vector3(inVelocity.x, -0.5f, inVelocity.z);
            
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

        protected virtual float GetMaxBrakingDeceleration()
        {
            switch (movementMode)
            {
                case MovementMode.Walking:
                    return brakingDecelerationWalking;
                case MovementMode.Falling:
                    return brakingDecelerationFalling;
                case MovementMode.None:
                default:
                    return 0f;
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

        public virtual void FindFloor(out FindFloorResult floorResult)
        {
            floorResult = default;

            if (characterController.isGrounded)
            {
                // Set the downward direction for the sweep test
                Vector3 sweepDirection = Vector3.down;

                Vector3 sweepStart = characterController.transform.position + characterController.center - Vector3.up * (characterController.height * 0.5f - characterController.radius);
                float sweepDistance = characterController.height * 0.5f + characterController.skinWidth;

                // Perform the sweep test
                if (Physics.CapsuleCast(sweepStart, sweepStart, characterController.radius, sweepDirection, out var hitInfo, sweepDistance))
                {
                    floorResult.hitResult = hitInfo;
                    floorResult.blockingHit = true;
                    floorResult.floorDistance = hitInfo.distance;

                    GameObject groundedObject = hitInfo.collider.gameObject;
                    // Do something with the grounded object
                    Debug.Log("Grounded on: " + groundedObject.name);
                }
                else
                {
                    floorResult.lineTrace = true;
                }
            }

        }
    }
}
