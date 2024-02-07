using UnityEngine;
using Xesin.GameplayFramework.Input;

namespace Xesin.GameplayFramework
{
    public enum AutoPossesValue
    {
        None = -2,
        AI = -1,
        Player1 = 0,
        Player2 = 1,
        Player3 = 2,
        Player4 = 3,
    }

    public class Pawn : SceneObject
    {
        public float BaseEyeHeight = 1.5f;

        public bool useControllerYaw;
        public bool useControllerPitch;
        public bool useControllerRoll;

        public AutoPossesValue autoPossesOnStart = AutoPossesValue.None;

        public Controller Controller => currentController;

        private PawnMovement movementComponent;
        private Controller currentController = null;
        private GameplayCamera pawnCamera = null;

        private Vector3 controlInputVector;
        private Vector3 lastControlInputVector;

        protected override void Awake()
        {
            base.Awake();
            movementComponent = GetComponent<PawnMovement>();
            var gameplayComponents = GetComponentsInChildren<GameplayObject>(true);

            for (int i = 0; i < gameplayComponents.Length; i++)
            {
                if (gameplayComponents[i] != this)
                {
                    gameplayComponents[i].SetOwner(this);
                }
            }
            controlInputVector = Vector2.zero;

            GameplayCamera camera = GetComponentInChildren<GameplayCamera>();
            if (camera)
            {
                pawnCamera = camera;
            }

            Restart();
        }

        protected virtual void Start()
        {
            if (autoPossesOnStart == AutoPossesValue.None) return;
            if(autoPossesOnStart == AutoPossesValue.AI)
            {

                return;
            }


        }

        protected virtual void OnDestroy()
        {
            Unpossesed();
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            PushRigidBodies(hit);
        }

        private void PushRigidBodies(ControllerColliderHit hit)
        {
            // make sure we hit a non kinematic rigidbody
            Rigidbody body = hit.collider.attachedRigidbody;
            if (body == null || body.isKinematic) return;

            // We dont want to push objects below us
            if (hit.moveDirection.y < -0.3f) return;

            // Calculate push direction from move direction, horizontal motion only
            Vector3 pushDir = new Vector3(hit.moveDirection.x, 0.0f, hit.moveDirection.z);

            // Apply the push and take strength into account
            body.AddForce(pushDir, ForceMode.Impulse);
        }

        public virtual void PossessedBy(Controller controller)
        {
            if (currentController == controller) return;

            currentController = controller;
            if (controller is PlayerController playerController)
            {
                SetupPlayerInput(playerController.GetInputComponent());
            }
            if (pawnCamera)
            {
                pawnCamera.enabled = true;
            }

            var gameplayComponents = GetComponentsInChildren<GameplayObject>(true);

            for (int i = 0; i < gameplayComponents.Length; i++)
            {
                if (gameplayComponents[i] != this)
                {
                    gameplayComponents[i].SetOwner(this);
                }
            }
        }

        public virtual void Restart()
        {
            RecalculateBaseEyeHeight();
            Internal_ConsumeInputVector();
            if (pawnCamera && !Controller)
            {
                pawnCamera.enabled = false;
            }
        }

        public virtual void Unpossesed()
        {
            Restart();
            if (!currentController) return;

            if (pawnCamera)
            {
                pawnCamera.enabled = false;
            }
            if (currentController is PlayerController playerController)
            {
                ClearPlayerInput(playerController.GetInputComponent());
            }
            currentController = null;
        }

        public virtual void SetupPlayerInput(InputComponent inputComponent)
        {

        }

        public virtual void ClearPlayerInput(InputComponent inputComponent)
        {
            inputComponent.ClearBinds(this);
        }

        public PawnMovement GetMovementComponent()
        {
            return movementComponent;
        }

        public void AddControllerPitchInput(float Val)
        {
            if (Val != 0 && Controller)
            {
                PlayerController PC = (PlayerController)Controller;
                PC.AddPitchInput(Val);
            }
        }

        public void AddControllerYawInput(float Val)
        {
            if (Val != 0 && Controller)
            {
                PlayerController PC = (PlayerController)Controller;
                PC.AddYawInput(Val);
            }
        }

        public void AddControllerRollInput(float Val)
        {
            if (Val != 0 && Controller)
            {
                PlayerController PC = (PlayerController)Controller;
                PC.AddRollInput(Val);
            }
        }

        public void FaceRotation(Vector3 rotation, float deltaTime)
        {
            if (useControllerPitch || useControllerRoll || useControllerYaw)
            {
                Vector3 currentRotation = transform.rotation.eulerAngles;
                if (!useControllerPitch)
                {
                    rotation.x = currentRotation.x;
                }

                if (!useControllerYaw)
                {
                    rotation.y = currentRotation.y;
                }

                if (!useControllerRoll)
                {
                    rotation.z = currentRotation.z;
                }

                transform.rotation = Quaternion.Euler(rotation);
            }
        }

        public Vector3 GetControlRotation()
        {
            return Controller?.GetControlRotation() ?? Vector3.zero;
        }

        public virtual void RecalculateBaseEyeHeight()
        {
            BaseEyeHeight = 1.5f;
        }


        public void GetEyesViewPoint(out Vector3 location, out Quaternion rotation)
        {
            location = GetPawnViewLocation();
            rotation = GetViewDirection();
        }

        public virtual Vector3 GetPawnViewLocation()
        {
            return transform.position + new Vector3(0f, BaseEyeHeight, 0f);
        }

        public virtual Quaternion GetViewDirection()
        {
            if (Controller)
            {
                Quaternion quaternion = Quaternion.Euler(Controller.GetControlRotation());
                return quaternion;
            }

            return transform.rotation;
        }

        internal Vector3 Internal_ConsumeInputVector()
        {
            lastControlInputVector = controlInputVector;
            controlInputVector = Vector3.zero;
            return lastControlInputVector;
        }

        public void AddMoveInput(Vector3 worldSpaceInput, float scale = 1)
        {
            controlInputVector += worldSpaceInput * scale;
        }

        public Vector3 GetVelocity()
        {
            if (!movementComponent) return Vector3.zero;
            return movementComponent.GetVelocity();
        }
    }
}