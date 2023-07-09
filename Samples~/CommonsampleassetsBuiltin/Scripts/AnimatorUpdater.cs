using UnityEngine;

namespace Xesin.GameplayFramework.Samples.BuiltIn
{
    public class AnimatorUpdater : GameplayObject
    {
        [SerializeField]
        private Animator targetAnimator;
        private Pawn PawnOwner;

        public override void SetOwner(GameplayObject obj)
        {
            base.SetOwner(obj);
            PawnOwner = obj as Pawn;
        }

        private void LateUpdate()
        {
            if (PawnOwner && targetAnimator)
            {
                targetAnimator.SetFloat("Speed", PawnOwner.GetVelocity().magnitude);

                var charMovement = PawnOwner.GetMovementComponent() as CharacterMovement;
                var character = PawnOwner as Character;

                targetAnimator.SetBool("Grounded", charMovement.IsMovingOnGround());
                targetAnimator.SetBool("FreeFall", charMovement.IsFalling() && !character.wasJumping);
                targetAnimator.SetBool("Jump", character.wasJumping);
            }
        }
    }
}