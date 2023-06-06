using System;
using UnityEngine;

namespace Xesin.GameplayFramework
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

        public bool walkableFloor;

        public float floorDistance;

        public RaycastHit hitResult;

        public bool IsWalkableFloor()
        {
            return blockingHit && walkableFloor;
        }

        public void Clear()
        {
            hitResult = default;
            walkableFloor = false;
            lineTrace = false;
            blockingHit = false;
            floorDistance = Mathf.Infinity;
        }
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
        protected RootMotionParams rootMotionParams = default;
        protected Vector3 animRootMotionVelocity = Vector3.zero;
        protected Vector3 decayingFormerBaseVelocity = Vector3.zero;
        protected Vector3 accumulatedMovement = Vector3.zero;

        [NonSerialized]
        public FindFloorResult currentFloor = default;
        protected bool forceFloorCheck = false;

        protected Quaternion oldBaseRotation;
        protected Vector3 oldBaseLocation;

        protected override void Awake()
        {
            characterController = GetComponent<CharacterController>();
            rootMotionComponent = GetComponentInChildren<RootMotionSource>();
        }

        private void Start()
        {
            SetMovementMode(MovementMode.Walking);
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
            if (movementMode == this.movementMode) return;

            var prevMovementMode = movementMode;
            this.movementMode = movementMode;

            OnMovementModeChange(prevMovementMode);
            characterOwner.OnMovementModeChanged(prevMovementMode);
        }

        protected virtual void OnMovementModeChange(MovementMode previousMovementMode)
        {
            if (!HasValidData()) return;

            if (movementMode == MovementMode.Walking)
            {
                // Walking uses only XY velocity, and must be on a walkable floor, with a Base.
                velocity.y = 0;

                // make sure we update our new floor/base on initial entry of the walking physics
                FindFloor(out currentFloor);
                //AdjustFloorHeight();
                SetBaseFromFloor(currentFloor);
            }
            else
            {
                currentFloor.Clear();

                if(movementMode == MovementMode.Falling)
                {
                    decayingFormerBaseVelocity = GetImpartedMovementBaseVelocity();
                    velocity += decayingFormerBaseVelocity;

                    decayingFormerBaseVelocity = Vector3.zero;
                }
                
                SetBase(null);
            }
        }

        private Vector3 GetImpartedMovementBaseVelocity()
        {
            Vector3 result = Vector3.zero;

            if(characterOwner)
            {
                Transform movementBase = characterOwner.GetMovementBase();
                if(movementBase && !movementBase.gameObject.isStatic)
                {
                    Vector3 baseVelocity = Vector3.zero;
                    if(movementBase.TryGetComponent<Rigidbody>(out var rigidbody) && !rigidbody.isKinematic)
                    {
                        baseVelocity = rigidbody.velocity;

                        Vector3 characterBasePosition = transform.position + characterController.center - ( new Vector3(0, characterController.height * 0.5f, 0));
                        Vector3 baseTangentialVelocity = GetMovementBaseTangentialVelocity(rigidbody, characterBasePosition);

                        baseVelocity += baseTangentialVelocity;
                    }
                    else if(movementBase.TryGetComponent<SceneObject>(out var component))
                    {
                        baseVelocity += component.Velocity;
                    }

                    result = baseVelocity;
                }
            }

            return result;
        }

        private Vector3 GetMovementBaseTangentialVelocity(Rigidbody movementBase, Vector3 worldLocation)
        {
            Vector3 baseAngleVelocity = movementBase.angularVelocity;
            Vector3 baseLocation = movementBase.position;
            Quaternion baseRotation = movementBase.rotation;

            Vector3 radialDistanceToBase = worldLocation - baseLocation;
            Vector3 tangentialVelocity = Vector3.Cross(baseAngleVelocity, radialDistanceToBase);

            return tangentialVelocity;
        }

        private void SetBaseFromFloor(FindFloorResult floor)
        {
            if(floor.IsWalkableFloor())
            {
                SetBase(floor.hitResult.collider.transform);
            }
            else
            {
                SetBase(null);
            }
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

            UpdateBasedMovement(deltaTime);            
            SaveBaseLocation();

            characterOwner.ClearJumpInput(deltaTime);

            StartNewPhysics(deltaTime);

            if (!rootMotionParams.HasRootMotion)
                PhysicsRotation(deltaTime);

            characterController.Move(accumulatedMovement);
            accumulatedMovement = Vector3.zero;
            // Root motion has been used, clear it
            rootMotionParams.Clear();
        }

        private void ApplyRootMotionToVelocity()
        {
            if (rootMotionParams.HasRootMotion)
            {
                velocity = ConstrainAnimRootMotionVelocity(animRootMotionVelocity, velocity);
                if (IsFalling())
                {
                    velocity += new Vector3(decayingFormerBaseVelocity.x, 0, decayingFormerBaseVelocity.z);
                }
            }
        }

        private Vector3 ConstrainAnimRootMotionVelocity(Vector3 rootMotionVelocity, Vector3 currentVelocity)
        {
            Vector3 result = rootMotionVelocity;
            if (IsFalling())
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
            if (!characterOwner || (!characterOwner.Controller && !rootMotionParams.HasRootMotion))
            {
                acceleration = Vector3.zero;
                velocity = Vector3.zero;
            }

            FindFloorResult oldFloor = currentFloor;
            Vector3 oldLocation = transform.position;

            acceleration.y = 0;

            if (!rootMotionParams.HasRootMotion)
            {
                CalcVelocity(deltaTime, groundFriction, GetMaxBrakingDeceleration());
            }

            ApplyRootMotionToVelocity();

            Vector3 moveVelocity = velocity;
            Vector3 delta = deltaTime * moveVelocity;

            MoveAlongFloor(moveVelocity, deltaTime);

            //if (!characterController.isGrounded)
            //{
            //    SetMovementMode(MovementMode.Falling);
            //}

            FindFloor(out currentFloor);

            if (currentFloor.IsWalkableFloor())
            {
                SetBase(currentFloor.hitResult.collider.transform);
            }
            else
            {

                CheckFall(delta, oldLocation, deltaTime, true);
            }
        }

        protected virtual bool CheckFall(Vector3 delta, Vector3 oldLocation, float deltaTime, bool mustJump)
        {
            if (!HasValidData()) return false;

            if (mustJump)
            {
                if (IsMovingOnGround())
                {
                    StartFalling(deltaTime, delta, oldLocation);
                }
                return true;
            }

            return false;
        }

        protected virtual void StartFalling(float deltaTime, Vector3 delta, Vector3 oldLocation)
        {
            float desiredDist = delta.magnitude;
            Vector3 distance = (transform.position - oldLocation);
            float actualDist = new Vector2(distance.x, distance.z).magnitude;

            if (IsMovingOnGround())
            {
                SetMovementMode(MovementMode.Falling);
            }

            StartNewPhysics(deltaTime);
        }

        protected virtual void PhysFalling(float deltaTime)
        {
            Vector3 oldVelocity = velocity;
            Vector3 fallAcceleration = GetFallingLateralAcceleration(deltaTime);
            fallAcceleration.y = 0;

            // Apply input
            if (!rootMotionParams.HasRootMotion)
            {
                // store the original acceleration to restore it later
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
            Vector3 Adjusted = 0.5f * (oldVelocity + velocity) * deltaTime;

            // Special handling if ending the jump force where we didn't apply gravity during the jump.
            if (endingJumpForce && !applyGravityWhileJumping)
            {
                // We had a portion of the time at constant speed then a portion with acceleration due to gravity.
                // Account for that here with a more correct change in position.
                float NonGravityTime = Mathf.Max(0, deltaTime - gravityTime);
                Adjusted = (oldVelocity * NonGravityTime) + (0.5f * (velocity) * gravityTime);
            }

            accumulatedMovement += Adjusted;

            if(characterController.collisionFlags.HasFlag(CollisionFlags.Below) && !characterOwner.wasJumping) 
            {
                FindFloor(out currentFloor);

                if (currentFloor.IsWalkableFloor())
                {
                    SetMovementMode(MovementMode.Walking);
                    StartNewPhysics(deltaTime);
                }
                // Prevents sliding if we are too close to the edge
                else
                {
                    FindFloor(out var edgeFloor, 1.0f);
                    if(edgeFloor.blockingHit && !edgeFloor.lineTrace)
                    {
                        characterController.Move(edgeFloor.hitResult.normal * 1.5f * deltaTime); 
                    }
                }
            }            
        }

        public virtual Vector3 GetFallingLateralAcceleration(float deltaTime)
        {
            Vector3 fallAcceleration = new Vector3(acceleration.x, 0, acceleration.z);

            if (!rootMotionParams.HasRootMotion && fallAcceleration.sqrMagnitude > 0)
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
            if (!currentFloor.IsWalkableFloor())
            {
                return;
            }

            Vector3 delta = new Vector3(inVelocity.x, 0, inVelocity.z) * deltaTime;
            Vector3 rampVector = ComputeGroundMovementDelta(delta, currentFloor.hitResult, currentFloor.lineTrace);
            accumulatedMovement += rampVector;

            FindFloor(out var floor);
            if (floor.IsWalkableFloor())
                accumulatedMovement += new Vector3(0, currentFloor.floorDistance, 0);
        }

        public virtual void FindFloor(out FindFloorResult floorResult, float radiusScale = 0.5f)
        {
            floorResult = default;

            // Set the downward direction for the sweep test
            Vector3 sweepDirection = Vector3.down;
            Vector3 characterActualPosition = accumulatedMovement + characterController.transform.position;
            float height = (characterController.height * 0.5f);
            Vector3 sweepStart = characterActualPosition + characterController.center - Vector3.up * (height - characterController.radius);
            float sweepDistance = Mathf.Max(0.024f, characterController.stepOffset + characterController.skinWidth + 0.05f);
            float sweepRadius = Mathf.Max(0.02f, characterController.radius * radiusScale);
            // Perform the sweep test
            if (Physics.SphereCast(sweepStart, sweepRadius, sweepDirection, out var hitInfo, sweepDistance))
            {
                floorResult.hitResult = hitInfo;
                floorResult.blockingHit = true;
                floorResult.floorDistance = hitInfo.point.y - (characterActualPosition.y - (characterController.height * 0.5f + characterController.skinWidth));
                floorResult.walkableFloor = true;

                return;
            }
            else if(Physics.Raycast(sweepStart, sweepDirection, out hitInfo, sweepDistance))
            {
                floorResult.hitResult = hitInfo;
                floorResult.blockingHit = true;
                floorResult.floorDistance = hitInfo.point.y - (characterActualPosition.y - (characterController.height * 0.5f + characterController.skinWidth));
                floorResult.walkableFloor = true;
                floorResult.lineTrace = true;

                return;
            }

            floorResult.blockingHit = false;
            floorResult.walkableFloor = false;
            floorResult.floorDistance = Mathf.Infinity;
            floorResult.lineTrace = false;
        }

        public virtual bool IsWalkable(RaycastHit hit)
        {
            if (hit.collider == null)
                return false;

            // Don't walk over vertical surfaces
            if (hit.normal.y < 1.0E-4f)
            {
                return false;
            }

            if (Vector3.Angle(Vector3.up, hit.normal) > characterController.slopeLimit)
            {
                return false;
            }

            return true;
        }

        protected virtual void UpdateBasedMovement(float deltaTime)
        {
            if (!HasValidData()) return;

            Transform movementBase = characterOwner.GetMovementBase();
            
            if(!movementBase)
            {
                SetBase(null);
                return;
            }

            Quaternion deltaRotation = Quaternion.identity;
            Vector3 deltaPosition;

            Quaternion newBaseRotation = movementBase.rotation;
            Vector3 newBaseLocation = movementBase.position;

            bool rotationChanged = Quaternion.Angle(oldBaseRotation, newBaseRotation) > 1.0E-8f;

            if (rotationChanged)
            {
                deltaRotation = newBaseRotation * Quaternion.Inverse(oldBaseRotation);
            }

            if(rotationChanged || oldBaseLocation != newBaseLocation)
            {
                var oldLocalToWorldMatrix = Matrix4x4.TRS(oldBaseLocation, oldBaseRotation, Vector3.one);
                var newLocalToWorldMatrix = Matrix4x4.TRS(newBaseLocation, newBaseRotation, Vector3.one);

                Quaternion finalQuaternion = transform.rotation;

                if(rotationChanged)
                {

                }

                float halfHeight = characterController.height / 2f;
                float radius = characterController.radius;

                Vector3 baseOffset = new Vector3(0, halfHeight, 0);
                Vector3 offsetPosition = transform.position - baseOffset;
                Vector4 localBasePos = oldLocalToWorldMatrix.inverse * new Vector4(offsetPosition.x, offsetPosition.y, offsetPosition.z, 1);
                Vector4 localToWorlPos = newLocalToWorldMatrix * localBasePos;
                Vector3 newWorldPosition = new Vector3(localToWorlPos.x, localToWorlPos.y, localToWorlPos.z) + baseOffset;

                deltaPosition = newWorldPosition - transform.position;

                accumulatedMovement += deltaPosition;
                transform.rotation = finalQuaternion * deltaRotation;
            }
        }

        private void SetBase(Transform movementBase)
        {
            if(characterOwner)
            {
                characterOwner.SetBase(movementBase);
            }
        }

        protected virtual Vector3 ComputeGroundMovementDelta(Vector3 delta, RaycastHit rampHit, bool hitFromLineTrace)
        {

            Vector3 FloorNormal = rampHit.normal;
            //Vector3 ContactNormal = rampHit.Normal;

            if (FloorNormal.y < (1.0f - 1.0E-4f) && FloorNormal.y > 1.0E-4f && !hitFromLineTrace && IsWalkable(rampHit))
            {
                // Compute a vector that moves parallel to the surface, by projecting the horizontal movement direction onto the ramp.
                float FloorDotDelta = Vector3.Dot(FloorNormal, delta);
                Vector3 RampMovement = new Vector3(delta.x, -FloorDotDelta / FloorNormal.y, delta.z);

                return RampMovement;
            }

            return delta;
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

        public virtual void SaveBaseLocation()
        {
            if (!HasValidData()) return;

            Transform movementBase = characterOwner.GetMovementBase();

            if(movementBase)
            {
                oldBaseLocation = movementBase.transform.position;
                oldBaseRotation = movementBase.transform.rotation;
            }
        }
    }
}
