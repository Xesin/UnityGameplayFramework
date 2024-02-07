namespace Xesin.GameplayFramework.AI
{
    public class BrainComponent : GameplayObject
    {
        protected BlackboardComponent blackboardComponent;

        protected virtual void Start()
        {
            if(Owner)
            {
                blackboardComponent = Owner.GetComponent<BlackboardComponent>();
                if(blackboardComponent)
                {
                    blackboardComponent.CacheBrainComponent(this);
                }
            }
        }

        public virtual void StartLogic() { }
        public virtual void RestartLogic() { }
        public virtual void StopLogic() { }

        public virtual void Cleanup() { }

        public virtual void PauseLogic() { }
        public virtual void ResumeLogic()
        {
        }

        public virtual bool IsRunning()
        {
            return false;
        }

        public virtual bool IsPaused()
        {
            return false;
        }

        public void CacheBlackboardComponent(BlackboardComponent blackboard)
        {
            blackboardComponent = blackboard;
        }

        public static BrainComponent FindBrainComponent(Controller controller)
        {
            return controller ? controller.GetComponent<BrainComponent>() : null;
        }

        public static BrainComponent FindBrainComponent(Pawn pawn)
        {
            if (pawn == null) return null;

            BrainComponent component = null;

            if (pawn.Controller)
            {
                component = FindBrainComponent(pawn.Controller);
            }

            if (pawn && component == null)
            {
                component = pawn.GetComponent<BrainComponent>();
            }

            return component;
        }
    }
}
