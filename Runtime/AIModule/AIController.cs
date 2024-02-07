using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public class AIController : Controller
    {

        protected virtual void LateUpdate()
        {
            UpdateRotation(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (possesedPawn)
            {
                possesedPawn.Restart();
            }

            possesedPawn = null;
        }

        private void UpdateRotation(float deltaTime)
        {
            Vector3 ViewRotation = GetControlRotation();

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
        }

        protected virtual CursorLockMode GetDefaultCursorLockMode()
        {
            return CursorLockMode.Locked;
        }

        protected virtual bool GetDefaultCursorVisibility()
        {
            return false;
        }
    }
}