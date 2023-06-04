using UnityEngine;

namespace Xesin.GameplayFramework
{
    public class Controller : GameplayObject
    {
        protected Pawn possesedPawn;

        public Vector3 controlRotation;

        public virtual void Posses(Pawn pawn)
        {
            if (possesedPawn)
            {
                possesedPawn.Unpossesed();
            }

            possesedPawn = pawn;
            pawn.PossessedBy(this);
            pawn.Restart();
        }

        public virtual void Unposses()
        {
            var pawn = GetPawn();
            if (pawn)
            {
                possesedPawn = null;
                pawn.Restart();
            }
        }

        public Vector3 GetControlRotation()
        {
            return controlRotation;
        }

        public void SetControlRotation(Vector3 newRotation)
        {
            controlRotation = newRotation;
        }

        public Pawn GetPawn()
        {
            return possesedPawn;
        }
    }
}
