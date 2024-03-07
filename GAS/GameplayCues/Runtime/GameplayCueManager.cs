using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;
using Xesin.GameplayFramework.Domain;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayCues
{
    public class GameplayCueManager : MonoSingleton<GameplayCueManager>
    {
        private Dictionary<KeyValuePair<Type, GameplayTag>, Queue<GameplayCueNotify_GameObject>> recycledQueue = new Dictionary<KeyValuePair<Type, GameplayTag>, Queue<GameplayCueNotify_GameObject>>();
        Dictionary<string, GameObject> loadedCues = new Dictionary<string, GameObject>();

        static readonly ProfilerMarker s_DispatchEventMarker = new ProfilerMarker("CueManager.DispatchEvent");
        static readonly ProfilerMarker s_CleanUpRecycleMarker = new ProfilerMarker("CueManager.RecycleCleanUp");

        private void Start()
        {
            SceneManager.sceneUnloaded -= OnSceneUnload;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }

        [ExecuteOnReload]
        private static void OnReload()
        {
            if (s_Instance)
            {
                foreach (var item in s_Instance.loadedCues)
                {
                    Addressables.Release(item.Value);
                }
            }
        }

        private void OnSceneUnload(Scene scene)
        {
            s_CleanUpRecycleMarker.Begin(this);
            List<GameplayCueNotify_GameObject> tmpList = new List<GameplayCueNotify_GameObject>(recycledQueue.Count);
            foreach (var cueType in recycledQueue)
            {
                while (cueType.Value.TryDequeue(out var recycledObject))
                {
                    if (recycledObject)
                        tmpList.Add(recycledObject);
                    else
                    {
                        ReleaseCue(recycledObject);
                    }
                }

            }

            for (int i = 0; i < tmpList.Count; i++)
            {
                PushToRecycleQueue(tmpList[i]);
            }

            s_CleanUpRecycleMarker.End();
        }

        private void ReleaseCue(GameplayCueNotify_GameObject cueObject)
        {
            if (loadedCues.ContainsKey(cueObject.TriggerTag.value))
            {
                loadedCues.Remove(cueObject.TriggerTag.value);
                Addressables.Release(cueObject.gameObject);
            }
        }

        private void PushToRecycleQueue(GameplayCueNotify_GameObject cueObject)
        {
            Type cueType = cueObject.GetType();
            KeyValuePair<Type, GameplayTag> keypair = new KeyValuePair<Type, GameplayTag>(cueType, cueObject.TriggerTag);

            if (!recycledQueue.ContainsKey(keypair))
            {
                recycledQueue.Add(keypair, new Queue<GameplayCueNotify_GameObject>());
            }
            cueObject.inRecycleQueue = true;
            recycledQueue[keypair].Enqueue(cueObject);
        }

        public void HandleGameplayCue(GameObject target, GameplayTag tag, GameplayCueEvent eventType, GameplayCueParameters gameplayCueParameters)
        {
            s_DispatchEventMarker.Begin(this);
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
#endif
            {
                if (string.IsNullOrEmpty(tag.value)) return;
                RouteGameplayCue(target, tag, eventType, gameplayCueParameters);
            }
            s_DispatchEventMarker.End();
        }

        private void RouteGameplayCue(GameObject target, GameplayTag tag, GameplayCueEvent eventType, GameplayCueParameters parameters)
        {
            if (!GameplayTagsContainer.Instance.IsValid(tag))
            {
                Debug.LogError("No valid tag was found for tag: " + tag.value);
                return;
            }

            tag = GameplayTagsContainer.Instance.ResolveTag(tag);
            parameters.cueTag = tag;

            if (!loadedCues.TryGetValue(tag.value, out var loadedCue) && eventType != GameplayCueEvent.Removed)
            {
                var handler = Addressables.LoadAssetAsync<GameObject>(tag.value);
                loadedCue = handler.WaitForCompletion();
                loadedCues.Add(tag.value, loadedCue);
            }

            if (loadedCue == null)
            {
                //Didn't even load it, so IsOverride should not apply.
                RouteGameplayCue(target, new GameplayTag(tag.parentTag), eventType, parameters);
                return;
            }

            if (loadedCue.TryGetComponent<GameplayCueNotify_Static>(out var staticCue))
            {
                if (staticCue.HandlesEvent(eventType))
                {
                    staticCue.HandleGameplayCue(target, eventType, parameters);

                    if (!staticCue.isOverride)
                    {
                        RouteGameplayCue(target, new GameplayTag(tag.parentTag), eventType, parameters);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(tag.parentTag)) return;

                    //Didn't even handle it, so IsOverride should not apply.
                    RouteGameplayCue(target, new GameplayTag(tag.parentTag), eventType, parameters);
                }

                return;
            }
            else if (loadedCue.TryGetComponent<GameplayCueNotify_GameObject>(out var instancedCue))
            {
                if (instancedCue.HandlesEvent(eventType))
                {
                    var spawnedCue = GetInstancedCueActor(target, instancedCue, parameters);

                    if (spawnedCue)
                    {
                        if (!spawnedCue.isOverride)
                        {
                            RouteGameplayCue(target, new GameplayTag(tag.parentTag), eventType, parameters);
                        }

                        spawnedCue.HandleGameplayCue(target, eventType, parameters);
                        return;
                    }
                }
                else
                {
                    //Didn't even handle it, so IsOverride should not apply.
                    RouteGameplayCue(target, new GameplayTag(tag.parentTag), eventType, parameters);
                }
            }
        }

        public T GetInstancedCueActor<T>(GameObject target, T prefab, GameplayCueParameters parameters) where T : GameplayCueNotify_GameObject
        {
            T existingCueOnObject = FindExistingCueOnActor<T>(target, parameters);
            if (existingCueOnObject)
            {
                existingCueOnObject.cueInstigator = parameters.instigator;
                existingCueOnObject.sourceObject = parameters.sourceObject;
                return existingCueOnObject;
            }

            T recycledCue = FindRecycledCue<T>(prefab.GetType(), prefab.TriggerTag);

            if (recycledCue)
            {
                Transform targetTransform = target.transform;
                recycledCue.inRecycleQueue = false;
                recycledCue.transform.SetParent(targetTransform);
                recycledCue.transform.SetPositionAndRotation(targetTransform.position, targetTransform.rotation);
                recycledCue.cueInstigator = parameters.instigator;
                recycledCue.sourceObject = parameters.sourceObject;

                return recycledCue;
            }

            T spawnedCue = Instantiate(prefab, target.transform);

            if (spawnedCue)
            {
                spawnedCue.cueInstigator = parameters.instigator;
                spawnedCue.sourceObject = parameters.sourceObject;
            }

            return spawnedCue;
        }

        private T FindRecycledCue<T>(Type type, GameplayTag tag) where T : GameplayCueNotify_GameObject
        {
            KeyValuePair<Type, GameplayTag> queueKey = new KeyValuePair<Type, GameplayTag>(type, tag);
            if (!recycledQueue.TryGetValue(queueKey, out var notifyQueue))
            {
                return null;
            }

            while (notifyQueue.Count > 0)
            {
                GameplayCueNotify_GameObject recycledCue = notifyQueue.Dequeue();

                if (recycledCue)
                {
                    return recycledCue as T;
                }
            }

            return null;
        }

        private T FindExistingCueOnActor<T>(GameObject targetActor, GameplayCueParameters Parameters) where T : GameplayCueNotify_GameObject
        {
            foreach (Transform child in targetActor.transform)
            {
                if (child && child.TryGetComponent<T>(out var foundCue))
                {
                    if (foundCue.IsPendingDestroy()) continue;
                    if (!foundCue.TriggerTag.MatchesTag(Parameters.cueTag, false)) continue;

                    bool instigatorMatches = !foundCue.uniqueInstancePerInstigator || foundCue.cueInstigator == Parameters.instigator;
                    bool sourceObjectMatches = !foundCue.uniqueInstancePerSourceObject || foundCue.sourceObject == Parameters.sourceObject;

                    if (instigatorMatches && sourceObjectMatches)
                    {
                        return foundCue;
                    }
                }
            }

            return null;
        }

        public void NotifyGameplayCueActorFinished(GameplayCueNotify_GameObject cue)
        {
            if (cue && cue.CanBeRecycled())
            {
                cue.Recycle();
                PushToRecycleQueue(cue);
            }
            else
            {
                ReleaseCue(cue);
            }
        }

    }
}
