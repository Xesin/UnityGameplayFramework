namespace Xesin.GameplayFramework.AI
{
    public enum AILogicResuming
    {
        Continue,
        RestartedInstead
    }

    public class BrainComponent : GameplayObject
    {
        protected BlackboardComponent blackboardComponent;

        private bool doLogicResatartOnUnlock = false;

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
        public virtual AILogicResuming ResumeLogic()
        {
            if(doLogicResatartOnUnlock)
            {
                doLogicResatartOnUnlock = false;
                RestartLogic();
                return AILogicResuming.RestartedInstead;
            }

            return AILogicResuming.Continue;
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

        public void RequestLogicRestartOnUnlock()
        {
            doLogicResatartOnUnlock = true;
        }
    }
}
