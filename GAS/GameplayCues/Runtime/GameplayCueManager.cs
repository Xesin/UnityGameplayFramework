using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.Util;
using UnityEngine.SceneManagement;
using Xesin.AddressablesExtensions;

namespace Xesin.GameplayCues
{
    public class GameplayCueManager : ComponentSingleton<GameplayCueManager>
    {
        private Dictionary<Type, Queue<GameplayCueNotify_GameObject>> recycledQueue = new Dictionary<Type, Queue<GameplayCueNotify_GameObject>>();


        private void Start()
        {
            SceneManager.sceneUnloaded -= OnSceneUnload;
            SceneManager.sceneUnloaded += OnSceneUnload;
        }

        private void OnDestroy()
        {
            SceneManager.sceneUnloaded -= OnSceneUnload;
        }

        private void OnSceneUnload(Scene scene)
        {
            List<GameplayCueNotify_GameObject> tmpList = new List<GameplayCueNotify_GameObject>(recycledQueue.Count);
            foreach (var cueType in recycledQueue)
            {
                while (cueType.Value.TryDequeue(out var recycledObject))
                {
                    if (recycledObject)
                        tmpList.Add(recycledObject);
                }

            }

            for (int i = 0; i < tmpList.Count; i++)
            {
                PushToRecycleQueue(tmpList[i]);
            }
        }

        private void PushToRecycleQueue(GameplayCueNotify_GameObject cueObject)
        {
            Type cueType = cueObject.GetType();
            if (!recycledQueue.ContainsKey(cueType))
            {
                recycledQueue.Add(cueType, new Queue<GameplayCueNotify_GameObject>());
            }
            cueObject.inRecycleQueue = true;
            recycledQueue[cueType].Enqueue(cueObject);
        }

        public void HandleGameplayCue(GameObject target, string tag, GameplayCueEvent eventType, GameplayCueParameters gameplayCueParameters)
        {
            RouteGameplayCue(target, tag, eventType, gameplayCueParameters);
        }

        public void RouteGameplayCue(GameObject target, string tag, GameplayCueEvent eventType, GameplayCueParameters parameters)
        {
            var handler = Addressables.LoadAssetAsync<GameObject>(tag);
            GameObject loadedCue = handler.WaitForCompletion();

            if (loadedCue == null)
            {
                Debug.LogError($"GameplayCue with tag {tag} cannot be loaded or do not exist");
                return;
            }

            if (loadedCue.TryGetComponent<GameplayCueNotify_Loop>(out var instancedCue))
            {
                if (instancedCue.HandlesEvent(eventType))
                {
                    var spawnedCue = GetInstancedCueActor(target, instancedCue, parameters);

                    if (spawnedCue)
                    {
                        handler.BindTo(spawnedCue.gameObject);
                        spawnedCue.HandleGameplayCue(target, eventType, parameters);
                        return;
                    }
                }
            }

            Addressables.ReleaseInstance(handler);
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

            T recycledCue = FindRecycledCue<T>();

            if(recycledCue)
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

        private T FindRecycledCue<T>() where T : GameplayCueNotify_GameObject
        {
            if(!recycledQueue.TryGetValue(typeof(T), out var notifyQueue))
            {
                return null;
            }

            while(notifyQueue.Count > 0)
            {
                GameplayCueNotify_GameObject recycledCue = notifyQueue.Dequeue();

                if(recycledCue)
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
            if (cue)
            {
                cue.Recycle();
                PushToRecycleQueue(cue);
            }
        }

    }
}
