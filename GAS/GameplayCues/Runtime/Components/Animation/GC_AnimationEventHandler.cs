using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayCues.Animation
{
    [RequireComponent(typeof(Animator))]
    public class GC_AnimationEventHandler : MonoBehaviour
    {
        [SerializeField]
        private List<string> curvesToListen = new List<string>();
        [SerializeField]
        private GameObject gcTarget;

        private Animator animator;
        private Dictionary<int, List<string>> activeEvents = new Dictionary<int, List<string>>();
        private int[] curveParamHashes;

        public event Action<string, float> OnCurveUpdated;

        private void Start()
        {
            animator = GetComponent<Animator>();
            curveParamHashes = new int[curvesToListen.Count];

            for (int i = 0; i < curvesToListen.Count; i++)
            {
                curveParamHashes[i] = Animator.StringToHash(curvesToListen[i]);
            }
        }

        private void Update()
        {
            for (int i = 0; i < curveParamHashes.Length; i++)
            {
                var curveValue = animator.GetFloat(curveParamHashes[i]);
                OnCurveUpdated?.Invoke(curvesToListen[i], curveValue);
            }
        }

        /// <summary>
        /// Called from an <see cref="AnimationClip"/> event. It sends the <see cref="GameplayCueEvent.OnActive"/> and <see cref="GameplayCueEvent.WhileActive"/> 
        /// event for the <see cref="GameplayTag"/> specified inside the event. 
        /// </summary>
        /// <param name="evt"></param>
        public void StartGameplayCueLooping(AnimationEvent evt)
        {
            GameplayCueParamentersScriptable parameters = evt.objectReferenceParameter as GameplayCueParamentersScriptable;
            var stateInfo = evt.animatorStateInfo;

            if (!activeEvents.TryGetValue(stateInfo.fullPathHash, out List<string> events))
            {
                events = new List<string>();
                activeEvents.Add(stateInfo.fullPathHash, events);
            }

            GameplayCueParameters gameplayCueParameters = MakeGameplayCueParameters(evt.stringParameter, parameters);

            GameplayTag tag = gameplayCueParameters.cueTag;

            GameplayCueManager.Instance.HandleGameplayCue(gcTarget, tag, GameplayCueEvent.OnActive, gameplayCueParameters);
            GameplayCueManager.Instance.HandleGameplayCue(gcTarget, tag, GameplayCueEvent.WhileActive, gameplayCueParameters);

            if (!events.Contains(tag.value))
                events.Add(tag.value);
        }

        /// <summary>
        /// Called from an <see cref="AnimationClip"/> event. It sends the active event for the <see cref="GameplayTag"/> specified inside the event. 
        /// It will only call the OnActive event, so if the GC it's a <see cref="GameplayCueNotify_Loop"/> it will be active until a stop is sent manually.
        /// Make sure the GC is a <see cref="GameplayCueNotify_Burst"/>
        /// </summary>
        /// <param name="evt"></param>
        public void GameplayCueBurst(AnimationEvent evt)
        {
            GameplayCueParamentersScriptable parameters = evt.objectReferenceParameter as GameplayCueParamentersScriptable;

            GameplayCueParameters gameplayCueParameters = MakeGameplayCueParameters(evt.stringParameter, parameters);

            GameplayTag tag = gameplayCueParameters.cueTag;

            GameplayCueManager.Instance.HandleGameplayCue(gcTarget, tag, GameplayCueEvent.OnActive, gameplayCueParameters);
        }

        /// <summary>
        /// Called from an <see cref="AnimationClip"/> event. It sends the stop event for the <see cref="GameplayTag"/> specified inside the event
        /// </summary>
        /// <param name="evt"></param>
        public void StopGameplayCueLooping(AnimationEvent evt)
        {
            GameplayCueParamentersScriptable parameters = evt.objectReferenceParameter as GameplayCueParamentersScriptable;

            GameplayCueParameters gameplayCueParameters = MakeGameplayCueParameters(evt.stringParameter, parameters);

            var stateInfo = evt.animatorStateInfo;

            if (activeEvents.TryGetValue(stateInfo.fullPathHash, out List<string> events))
            {
                events.Remove(evt.stringParameter);
            }
            GameplayTag tag = gameplayCueParameters.cueTag;
            GameplayCueManager.Instance.HandleGameplayCue(gcTarget, tag, GameplayCueEvent.Removed, gameplayCueParameters);
        }

        /// <summary>
        /// Called from <see cref="GC_AnimStateCleanup.OnStateExit(Animator, AnimatorStateInfo, int)"/>. It tries to remove all Gameplay Cues spawned from
        /// the previous state when switching to a new one
        /// </summary>
        /// <param name="animatorStateInfo"></param>
        public void OnExitState(AnimatorStateInfo animatorStateInfo)
        {
            if (activeEvents.TryGetValue(animatorStateInfo.fullPathHash, out List<string> events))
            {
                GameplayCueParameters gameplayCueParameters = new GameplayCueParameters();
                gameplayCueParameters.sourceObject = gameObject;
                gameplayCueParameters.instigator = gcTarget;

                for (int i = 0; i < events.Count; i++)
                {
                    GameplayTag tag = GameplayTagsContainer.RequestGameplayTag(events[i]);
                    gameplayCueParameters.cueTag = tag;
                    GameplayCueManager.Instance.HandleGameplayCue(gcTarget, tag, GameplayCueEvent.Removed, gameplayCueParameters);
                }

                events.Clear();
            }
        }

        public void RemoveActiveEvents()
        {
            foreach (var state in activeEvents)
            {
                for (int i = 0; i < state.Value.Count; i++)
                {
                    GameplayCueParameters gameplayCueParameters = MakeGameplayCueParameters(state.Value[i], null);
                    GameplayCueManager.Instance.HandleGameplayCue(gcTarget, gameplayCueParameters.cueTag, GameplayCueEvent.Removed, gameplayCueParameters);
                }

                state.Value.Clear();
            }

            activeEvents.Clear();
        }

        private GameplayCueParameters MakeGameplayCueParameters(string cue, GameplayCueParamentersScriptable parameters)
        {
            GameplayCueParameters gameplayCueParameters = new GameplayCueParameters();
            gameplayCueParameters.sourceObject = gameObject;
            gameplayCueParameters.instigator = gcTarget;
            gameplayCueParameters.cueTag = GameplayTagsContainer.RequestGameplayTag(cue);

            if (parameters)
            {
                gameplayCueParameters.location = parameters.location;
                gameplayCueParameters.normalizedMagnitude = parameters.normalizedMagnitude;
                gameplayCueParameters.rawMagnitude = parameters.rawMagnitude;
            }

            return gameplayCueParameters;
        }
    }
}
