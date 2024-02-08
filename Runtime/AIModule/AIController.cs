using System;
using System.Diagnostics;
using UnityEngine;

namespace Xesin.GameplayFramework.AI
{
    public class AIController : Controller
    {
        [SerializeField] private BlackboardComponent blackboard;
        [SerializeField] private BrainComponent brainComponent;

        protected virtual void Awake()
        {
            if(!brainComponent)
                brainComponent = GetComponent<BrainComponent>();
        }

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

        public virtual bool RunBehaviorTree(BehaviorTree btAsset)
        {
            if(!btAsset)
            {
                return false;
            }

            bool success = true;

            BlackboardComponent blackboardComp = blackboard;
            if(btAsset.blackboardAsset && (blackboard == null || !blackboardComp.IsCompatibleWith(btAsset.blackboardAsset)) )
            {
                success = UseBlackboard(btAsset.blackboardAsset, blackboardComp);
            }

            if(success)
            {
                BehaviorTreeComponent btComp = brainComponent as BehaviorTreeComponent;
                if(btComp == null)
                {
                    btComp = gameObject.AddComponent<BehaviorTreeComponent>();
                }

                brainComponent = btComp;

                btComp.StartTree(btAsset, TreeExecutionMode.Looped);
            }

            return success;
        }

        private bool UseBlackboard(BlackboardData blackboardAsset, BlackboardComponent blackboardComp)
        {
            if(blackboardAsset == null)
            {
                return false;
            }

            if(!TryGetComponent(out blackboard))
            { 
                blackboard = gameObject.AddComponent<BlackboardComponent>();

                InitializeBlackboard(blackboard, blackboardAsset);
            }
            else if(blackboard.GetBlackboardAsset() == null)
            {
                InitializeBlackboard(blackboard, blackboardAsset);
            }
            else if(blackboard.GetBlackboardAsset() != blackboardAsset)
            {
                InitializeBlackboard(blackboard, blackboardAsset);
            }

            return true;
        }

        private bool InitializeBlackboard(BlackboardComponent blackboardComp, BlackboardData blackboardAsset)
        {
            if(blackboardComp.InitializeBlackboard(blackboardAsset))
            {
                Blackbloard.Key selfKey = blackboardAsset.GetKeyID("Self");
                if(selfKey != Blackbloard.Key.invalid)
                {
                    blackboardComp.SetValue<UnityEngine.Object>(selfKey, GetPawn());
                }

                OnUsingBlackboard(blackboardComp, blackboardAsset);
                return true;
            }

            return false;
        }

        protected virtual void OnUsingBlackboard(BlackboardComponent blackboardComp, BlackboardData blackboardAsset)
        {
            
        }
    }
}