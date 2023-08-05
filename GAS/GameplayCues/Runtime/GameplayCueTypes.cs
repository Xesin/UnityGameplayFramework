using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayCues
{
    public enum GameplayCueEvent
    {
        /** Called when a GameplayCue with duration is first activated, this will only be called if the client witnessed the activation */
        OnActive,

        /** Called when a GameplayCue with duration is first seen as active, even if it wasn't actually just applied (Join in progress, etc) */
        WhileActive,

        /** Called when a GameplayCue is executed, this is used for instant effects or periodic ticks */
        Executed,

        /** Called when a GameplayCue with duration is removed */
        Removed
    }

    public enum GameplayCueNotify_AttachPolicy : byte
    {
        // Do not attach to the target actor.  The target may still be used to get location and other information.
        DoNotAttach,

        // Attach to the target actor if possible.
        AttachToTarget,
    };

    public struct GameplayCueParameters
    {
        public float normalizedMagnitude;
        public float rawMagniture;
        public string cueTag;
        public float destroyDelay;
        public Vector3 location;
        public GameObject instigator;
        public GameObject sourceObject;

    }

    [System.Serializable]
    public struct GameplayCueNotify_PlacementInfo
    {
        public string socketName;
        public GameplayCueNotify_AttachPolicy attachPolicy;


        public bool FindSpawnTransform(GameplayCueNotify_SpawnContext SpawnContext, out Transform OutTransform)
        {
            OutTransform = null;
            if (SpawnContext.targetActor)
            {
                if (string.IsNullOrEmpty(socketName))
                {
                    OutTransform = SpawnContext.targetActor.transform;
                }
                else
                {
                    OutTransform = RecursiveFindChild(SpawnContext.targetActor.transform, socketName);
                }
            }
            return OutTransform != null;
        }

        private Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }
                else
                {
                    Transform found = RecursiveFindChild(child, childName);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }
            return null;
        }
    }

    public struct GameplayCueNotify_SpawnContext
    {
        public GameObject targetActor;
        public GameplayCueParameters cueParameters;

        private GameplayCueNotify_PlacementInfo DefaultPlacementInfo;

        public GameplayCueNotify_SpawnContext(GameObject InTargetActor, GameplayCueParameters InCueParameters)
        {
            DefaultPlacementInfo = default;
            targetActor = InTargetActor;
            cueParameters = InCueParameters;
        }

        public void SetDefaultPlacementInfo(GameplayCueNotify_PlacementInfo placementInfo)
        {
            DefaultPlacementInfo = placementInfo;
        }

        public GameplayCueNotify_PlacementInfo GetPlacementInfo(bool useOverride, GameplayCueNotify_PlacementInfo overridePlacement)
        {
            return !useOverride ? DefaultPlacementInfo : overridePlacement;
        }
    }

    public struct GameplayCueNotify_SpawnResult
    {
        public List<ParticleSystem> FxSystemComponents;

        public GameplayCueNotify_SpawnResult(int i)
        {
            FxSystemComponents = new List<ParticleSystem>();
        }

        public void Reset()
        {
            FxSystemComponents.Clear();
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_ParticleInfo
    {
        public ParticleSystem particleSystem;
        public GameplayCueNotify_PlacementInfo placementInfoOverride;

        public bool overridePlacementInfo;

        public bool PlayParticleEffect(GameplayCueNotify_SpawnContext spawnContext, ref GameplayCueNotify_SpawnResult spawnResult, bool destroyAfterLifetime = false)
        {
            ParticleSystem spawnedFX = null;

            if (particleSystem != null)
            {
                var placementInfo = spawnContext.GetPlacementInfo(overridePlacementInfo, placementInfoOverride);
                if (!placementInfo.FindSpawnTransform(spawnContext, out var transform))
                {
                    return false;
                }

                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.AttachToTarget)
                {
                    spawnedFX = Object.Instantiate(particleSystem, transform);
                }
                else
                {
                    spawnedFX = Object.Instantiate(particleSystem, transform.position, Quaternion.identity);
                }

                if(spawnContext.cueParameters.location != Vector3.zero)
                {
                    spawnedFX.transform.localPosition = spawnContext.cueParameters.location;
                }

                var mainModule = spawnedFX.main;

                mainModule.stopAction = ParticleSystemStopAction.Destroy;

                if (destroyAfterLifetime)
                    Object.Destroy(spawnedFX.gameObject, spawnedFX.main.duration + spawnContext.cueParameters.destroyDelay);
            }

            spawnResult.FxSystemComponents.Add(spawnedFX);
            return spawnedFX != null;
        }
    }


    [System.Serializable]
    public struct GameplayCueNotify_BurstEffects
    {
        [SerializeField] private GameplayCueNotify_ParticleInfo[] burstParticles;

        public void ExecuteEffects(GameplayCueNotify_SpawnContext context, ref GameplayCueNotify_SpawnResult results)
        {
            for (int i = 0; i < burstParticles.Length; i++)
            {
                burstParticles[i].PlayParticleEffect(context, ref results, true);
            }
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_LoopEffects
    {
        [SerializeField] private GameplayCueNotify_ParticleInfo[] loopingParticles;

        public void StartEffects(GameplayCueNotify_SpawnContext context, ref GameplayCueNotify_SpawnResult results)
        {
            for (int i = 0; i < loopingParticles.Length; i++)
            {
                loopingParticles[i].PlayParticleEffect(context, ref results, false);
            }
        }

        public void StopEffects(ref GameplayCueNotify_SpawnResult results)
        {
            for (int i = 0; i < results.FxSystemComponents.Count; i++)
            {
                if (results.FxSystemComponents[i])
                    results.FxSystemComponents[i].Stop();
            }

            results.Reset();
        }
    }
}
