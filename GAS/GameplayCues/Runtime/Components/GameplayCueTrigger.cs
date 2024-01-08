using System.Collections;
using UnityEngine;

namespace Xesin.GameplayCues
{
    public class GameplayCueTrigger : MonoBehaviour
    {
        [SerializeField]
        private GameplayTag cueTag;
        [SerializeField]
        private float repeatTime = 0f;
        [SerializeField]
        private float firstExecutionDelay = 0f;

        Coroutine startCoroutine;

        private void OnEnable()
        {
            
            if (firstExecutionDelay > 0f)
            {
                startCoroutine = StartCoroutine(DelayedStart());
            }
            else
            {
                StartCue();
            }
        }

        private void StartCue()
        {
            GameplayCueParameters gameplayCueParameters = new GameplayCueParameters();
            gameplayCueParameters.cueTag = cueTag;
            gameplayCueParameters.instigator = gameObject;
            gameplayCueParameters.sourceObject = gameObject;

            GameplayCueManager.Instance.HandleGameplayCue(gameObject, cueTag, GameplayCueEvent.OnActive, gameplayCueParameters);
            GameplayCueManager.Instance.HandleGameplayCue(gameObject, cueTag, GameplayCueEvent.WhileActive, gameplayCueParameters);

            if (repeatTime > 0f)
            {
                InvokeRepeating(nameof(Execute), 0, repeatTime);
            }

        }

        private IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(firstExecutionDelay);

            StartCue();

            startCoroutine = null;
        }

        private void OnDisable()
        {
            if (startCoroutine != null)
            {
                StopCoroutine(startCoroutine);
                startCoroutine = null;
                // Nothing started, so there's no need to remove the CUE object
                return;
            }

            GameplayCueParameters gameplayCueParameters = new GameplayCueParameters();
            gameplayCueParameters.cueTag = cueTag;
            gameplayCueParameters.instigator = gameObject;
            gameplayCueParameters.sourceObject = gameObject;

            GameplayCueManager.Instance.HandleGameplayCue(gameObject, cueTag, GameplayCueEvent.Removed, gameplayCueParameters);

            if (repeatTime > 0f)
            {
                CancelInvoke(nameof(Execute));
            }            
        }

        private void Execute()
        {
            GameplayCueParameters gameplayCueParameters = new GameplayCueParameters();
            gameplayCueParameters.cueTag = cueTag;
            gameplayCueParameters.instigator = gameObject;
            gameplayCueParameters.sourceObject = gameObject;

            GameplayCueManager.Instance.HandleGameplayCue(gameObject, cueTag, GameplayCueEvent.Executed, gameplayCueParameters);
        }
    }
}
