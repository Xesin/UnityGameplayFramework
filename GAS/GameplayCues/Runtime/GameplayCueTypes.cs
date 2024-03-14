using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Pool;

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

        DoNotAttachFollowRotation
    };

    public enum GameplayCueNotify_AttachRule : byte
    {
        KeepWorld,

        SnapToTarget
    };

    [System.Serializable]
    public struct GameplayTag
    {
        public string value;
        public string parentTag;

        [System.NonSerialized]
        public string originalTagValue;

        internal GameplayTag(string initValue)
        {
            value = initValue;
            parentTag = GetParentTagValue(initValue);
            originalTagValue = initValue;
        }

        public static string GetParentTagValue(string value)
        {
            var nodes = value.Split(".");
            StringBuilder stringBuilder = new StringBuilder();

            if (nodes.Length > 1)
            {
                return string.Join(".", nodes, 0, nodes.Length - 1);
            }
            else
            {
                return string.Empty;
            }
        }

        public GameplayTag ParentTag()
        {
            return GameplayTagsContainer.RequestGameplayTag(parentTag);
        }

        public bool MatchesTag(GameplayTag gameplayTag, bool partially = true)
        {
            if (string.IsNullOrEmpty(value)) return false;
            if (string.IsNullOrEmpty(gameplayTag.value)) return false;

            if (partially)
            {
                string[] members = value.Split('.');
                string[] otherMembers = gameplayTag.value.Split('.');

                for (int i = 0; i < otherMembers.Length; i++)
                {
                    if (members.Length <= i) return false;
                    if (members[i] != otherMembers[i]) return false;
                }

                return true;
            }
            else
                return gameplayTag.GetHashCode() == GetHashCode();
        }

        public bool HasValue()
        {
            return !string.IsNullOrEmpty(value);
        }

        public override int GetHashCode()
        {
            return value.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is not GameplayTag tag) return false;
            return tag.value.GetHashCode() == value.GetHashCode();
        }

        public static implicit operator GameplayTag(string stringValue)
        {
            return GameplayTagsContainer.RequestGameplayTag(stringValue);
        }

        public static implicit operator string(GameplayTag tagValue)
        {
            return tagValue.value;
        }
    }

    [System.Serializable]
    public struct GameplayTagList
    {
        [SerializeField]
        private List<GameplayTag> gameplayTags;

        public IReadOnlyList<GameplayTag> Tags => gameplayTags;

        public GameplayTagList(params GameplayTag[] initialValues)
        {
            gameplayTags = new List<GameplayTag>();
            if (initialValues != null)
                gameplayTags.AddRange(initialValues);
        }

        public GameplayTagList(IReadOnlyList<GameplayTag> initialValues)
        {
            gameplayTags = new List<GameplayTag>();
            if (initialValues != null)
                gameplayTags.AddRange(initialValues);
        }

        public bool Contains(GameplayTag tag, bool fullMatch = true)
        {
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                if (gameplayTags[i].MatchesTag(tag, !fullMatch)) return true;
            }

            return false;
        }

        public bool Contains(GameplayTag[] tags, bool fullMatch = true)
        {
            int numMatches = 0;
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                for (int j = 0; j < tags.Length; j++)
                {
                    if (gameplayTags[i].MatchesTag(tags[j], !fullMatch))
                        numMatches++;

                    if (numMatches == tags.Length)
                        return true;
                }
            }

            return numMatches == tags.Length;
        }

        public bool Contains(IReadOnlyList<GameplayTag> tags, bool fullMatch = true)
        {
            int numMatches = 0;
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                for (int j = 0; j < tags.Count; j++)
                {
                    if (gameplayTags[i].MatchesTag(tags[j], !fullMatch))
                        numMatches++;

                    if (numMatches == tags.Count)
                        return true;
                }
            }

            return numMatches == tags.Count;
        }

        public bool ContainsAny(GameplayTag[] tags, bool fullMatch = true)
        {
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                for (int j = 0; j < tags.Length; j++)
                {
                    if (gameplayTags[i].MatchesTag(tags[j], !fullMatch))
                        return true;
                }
            }

            return false;
        }

        public bool ContainsAny(IReadOnlyList<GameplayTag> tags, bool fullMatch = true)
        {
            for (int i = 0; i < gameplayTags.Count; i++)
            {
                for (int j = 0; j < tags.Count; j++)
                {
                    if (gameplayTags[i].MatchesTag(tags[j], !fullMatch))
                        return true;
                }
            }

            return false;
        }

        public void AddTag(GameplayTag tag)
        {
            gameplayTags.Add(tag);
        }

        public void AddTags(params GameplayTag[] tags)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                gameplayTags.Add(tags[i]);
            }
        }

        public void AddTags(IReadOnlyList<GameplayTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                gameplayTags.Add(tags[i]);
            }
        }

        public void AddTags(IList<GameplayTag> tags)
        {
            for (int i = 0; i < tags.Count; i++)
            {
                gameplayTags.Add(tags[i]);
            }
        }

        public void AddTags(GameplayTagList tags)
        {
            for (int i = 0; i < tags.Tags.Count; i++)
            {
                gameplayTags.Add(tags.Tags[i]);
            }
        }

        public void RemoveSingleTag(GameplayTag tag)
        {
            for (int i = gameplayTags.Count - 1; i >= 0; i--)
            {
                if (gameplayTags[i].MatchesTag(tag, partially: false))
                {
                    gameplayTags.RemoveAt(i);
                    break;
                }
            }
        }

        public void RemoveAllTags(GameplayTag tag)
        {
            for (int i = gameplayTags.Count - 1; i >= 0; i--)
            {
                if (gameplayTags[i].MatchesTag(tag, partially: false))
                    gameplayTags.RemoveAt(i);
            }
        }
    }


    [System.Serializable]
    public struct GameplayTagRedirect
    {
        public string originalTagValue;
        public string redirectectValue;
    }


    public struct GameplayCueParameters
    {
        public float normalizedMagnitude;
        public float rawMagnitude;
        public GameplayTag cueTag;
        public Vector3 location;
        public GameObject instigator;
        public GameObject sourceObject;

        /// <summary>
        /// Creates a new <see cref="GameplayCueParameters"/> with all initialized
        /// </summary>
        /// <param name="instigator">"Who" will be executing the event.</param>
        /// <param name="gameplayTag">The tag of the event</param>
        /// <returns></returns>
        public static GameplayCueParameters MakeParams(GameObject instigator, GameplayTag gameplayTag)
        {
            return new GameplayCueParameters()
            {
                normalizedMagnitude = 1,
                rawMagnitude = 1,
                cueTag = gameplayTag,
                location = Vector3.zero,
                instigator = instigator,
                sourceObject = instigator
            };
        }

        /// <summary>
        /// Creates a new <see cref="GameplayCueParameters"/> with all initialized
        /// </summary>
        /// <param name="instigator">"Who" will be executing the event.</param>
        /// <param name="location">Location offset, this will added to the placement info offset</param>
        /// <param name="gameplayTag">The tag of the event</param>
        /// <returns></returns>
        public static GameplayCueParameters MakeParams(GameObject instigator, Vector3 location, GameplayTag gameplayTag)
        {
            return new GameplayCueParameters()
            {
                normalizedMagnitude = 1,
                rawMagnitude = 1,
                cueTag = gameplayTag,
                location = location,
                instigator = instigator,
                sourceObject = instigator
            };
        }

        /// <summary>
        /// Creates a new <see cref="GameplayCueParameters"/> with all initialized
        /// </summary>
        /// <param name="instigator">"Who" will be executing the event.</param>
        /// <param name="sourceObject">The object where we will apply the GC</param>
        /// <param name="gameplayTag">The tag of the event</param>
        /// <returns></returns>
        public static GameplayCueParameters MakeParams(GameObject instigator, GameObject sourceObject, GameplayTag gameplayTag)
        {
            return new GameplayCueParameters()
            {
                normalizedMagnitude = 1,
                rawMagnitude = 1,
                cueTag = gameplayTag,
                location = Vector3.zero,
                instigator = instigator,
                sourceObject = sourceObject
            };
        }

        /// <summary>
        /// Creates a new <see cref="GameplayCueParameters"/> with all initialized
        /// </summary>
        /// <param name="instigator">"Who" will be executing the event.</param>
        /// <param name="sourceObject">The object where we will apply the GC</param>
        /// <param name="location">Location offset, this will added to the placement info offset</param>
        /// <param name="gameplayTag">The tag of the event</param>
        /// <returns></returns>
        public static GameplayCueParameters MakeParams(GameObject instigator, GameObject sourceObject, Vector3 location, GameplayTag gameplayTag)
        {
            return new GameplayCueParameters()
            {
                normalizedMagnitude = 1,
                rawMagnitude = 1,
                cueTag = gameplayTag,
                location = location,
                instigator = instigator,
                sourceObject = sourceObject
            };
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_PlacementInfo
    {
        public string socketName;
        public GameplayCueNotify_AttachPolicy attachPolicy;
        public Vector3 PositionOffset;
        public Vector3 RotationOverride;
        public Vector3 ScaleOverride;

        public static GameplayCueNotify_PlacementInfo Default => new GameplayCueNotify_PlacementInfo(Vector3.zero, Vector3.zero, Vector3.one);

        GameplayCueNotify_PlacementInfo(Vector3 positionOverride, Vector3 rotationOverride, Vector3 scaleOverride)
        {
            RotationOverride = rotationOverride;
            ScaleOverride = scaleOverride;
            attachPolicy = GameplayCueNotify_AttachPolicy.DoNotAttach;
            socketName = string.Empty;
            PositionOffset = positionOverride;
        }

        /// <summary>
        /// Finds the transform where the effect will be spawned
        /// </summary>
        /// <param name="SpawnContext"></param>
        /// <param name="OutTransform"></param>
        /// <returns>true if finds a valid transform</returns>
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
                    if (OutTransform == null)
                        OutTransform = SpawnContext.targetActor.transform;
                }
            }
            return OutTransform != null;
        }

        private Transform RecursiveFindChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.gameObject.activeInHierarchy && child.name == childName)
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
        public List<GameplayCueNotify_ParticleSpawnResult> FxSystemComponents;
        public List<GameplayCueNotify_AudioSpawnResult> AudioSystemComponents;
        public List<GameplayCueNotify_CustomItemSpawnResult> CustomItems;

        public GameplayCueNotify_SpawnResult(int i)
        {
            FxSystemComponents = new List<GameplayCueNotify_ParticleSpawnResult>();
            AudioSystemComponents = new List<GameplayCueNotify_AudioSpawnResult>();
            CustomItems = new List<GameplayCueNotify_CustomItemSpawnResult>();
        }

        public void Reset()
        {
            for (int i = 0; i < FxSystemComponents.Count; i++)
            {
                FxSystemComponents[i].Release();
            }

            for (int i = 0; i < AudioSystemComponents.Count; i++)
            {
                AudioSystemComponents[i].Release();
            }

            for (int i = 0; i < CustomItems.Count; i++)
            {
                CustomItems[i].Release();
            }

            FxSystemComponents.Clear();
            AudioSystemComponents.Clear();
            CustomItems.Clear();
        }
    }

    public struct GameplayCueNotify_ParticleSpawnResult
    {
        public ParticleSystem FxSystemComponent;
        public ParticleSystemStopBehavior stopBehavior;
        public ObjectPool<ParticleSystem> objectPool;
        public bool handlesRelease;

        public GameplayCueNotify_ParticleSpawnResult(ParticleSystem item, ObjectPool<ParticleSystem> inPool, bool handlesRelease, ParticleSystemStopBehavior stopBehavior)
        {
            FxSystemComponent = item;
            objectPool = inPool;
            this.handlesRelease = handlesRelease;
            this.stopBehavior = stopBehavior;
        }

        public void Release()
        {
            if (!handlesRelease) return;
            if (objectPool != null)
            {
                objectPool.Release(FxSystemComponent);
            }
            else if (FxSystemComponent)
            {
                var mainModule = FxSystemComponent.main;
                mainModule.stopAction = ParticleSystemStopAction.Destroy;
                FxSystemComponent.Stop(true, stopBehavior);
            }
        }
    }

    public struct GameplayCueNotify_AudioSpawnResult
    {
        public AudioSource AudioSystemComponent;
        public ObjectPool<AudioSource> objectPool;
        public bool handlesRelease;

        public GameplayCueNotify_AudioSpawnResult(AudioSource item, ObjectPool<AudioSource> inPool, bool handlesRelease)
        {
            AudioSystemComponent = item;
            objectPool = inPool;
            this.handlesRelease = handlesRelease;
        }

        public void Release()
        {
            if (!handlesRelease) return;
            if (objectPool != null)
            {
                objectPool.Release(AudioSystemComponent);
            }
            else if (AudioSystemComponent)
            {
                Object.Destroy(AudioSystemComponent.gameObject, 1.0f);
            }
        }
    }

    public struct GameplayCueNotify_CustomItemSpawnResult
    {
        public CueCustomItem CustomItem;
        public ObjectPool<CueCustomItem> objectPool;
        public bool handlesRelease;

        public GameplayCueNotify_CustomItemSpawnResult(CueCustomItem item, ObjectPool<CueCustomItem> inPool, bool handlesRelease)
        {
            CustomItem = item;
            objectPool = inPool;
            this.handlesRelease = handlesRelease;
        }

        public void Release()
        {
            if (!handlesRelease) return;
            if (objectPool != null)
            {
                objectPool.Release(CustomItem);
            }
            else if (CustomItem)
            {
                CustomItem.StopEffect();
            }
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_ParticleInfo
    {
        public ParticleSystem particleSystem;
        public ParticleSystemStopBehavior stopBehavior;
        public GameplayCueNotify_PlacementInfo placementInfoOverride;

        public bool overridePlacementInfo;

        public ObjectPool<ParticleSystem> objectPool;
        public bool usePool;

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

                spawnedFX = Instantiate();
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.AttachToTarget)
                {
                    spawnedFX.transform.SetParent(transform, false);
                }
                else
                {
                    spawnedFX.transform.position = transform.position;
                }

                Transform spawnedTransform = spawnedFX.transform; // Caching transform so no extra calls to the C++ side

                spawnedTransform.localPosition += spawnContext.cueParameters.location + placementInfo.PositionOffset;
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.DoNotAttachFollowRotation)
                {
                    spawnedTransform.rotation *= transform.rotation;
                }
                spawnedTransform.localRotation *= Quaternion.Euler(placementInfo.RotationOverride);
                spawnedTransform.localScale = placementInfo.ScaleOverride * spawnContext.cueParameters.rawMagnitude;

                spawnedFX.Play(true);
                if (destroyAfterLifetime)
                    GameplayCueManager.Instance.StartCoroutine(DelayedDestroy(spawnedFX.main.duration, spawnedFX, objectPool));
            }

            spawnResult.FxSystemComponents.Add(new GameplayCueNotify_ParticleSpawnResult(spawnedFX, objectPool, !destroyAfterLifetime, stopBehavior));

            return spawnedFX != null;
        }

        public void CleanUp()
        {
            if (objectPool != null)
            {
                objectPool.Dispose();
                objectPool = null;
            }
        }

        private ParticleSystem Instantiate()
        {
            if (usePool)
            {
                if (objectPool == null)
                {
                    objectPool = new ObjectPool<ParticleSystem>(InstantiateNewParticle, OnGetInstance, OnReleaseInstance, OnDestroyItem);
                }

                return objectPool.Get();
            }
            return InstantiateNewParticle();
        }

        private ParticleSystem InstantiateNewParticle()
        {
            return Object.Instantiate(particleSystem);
        }

        private void OnReleaseInstance(ParticleSystem particleSystem)
        {
            if (particleSystem.main.stopAction == ParticleSystemStopAction.Destroy) // prevent destroying
            {
                var mainModule = particleSystem.main;
                mainModule.stopAction = ParticleSystemStopAction.Disable;
            }
            particleSystem.Stop(true, stopBehavior);
        }

        private void OnGetInstance(ParticleSystem particleSystem)
        {
            particleSystem.gameObject.SetActive(true);
        }

        private void OnDestroyItem(ParticleSystem particleSystem)
        {
            if (particleSystem)
                Object.Destroy(particleSystem.gameObject);
        }

        private IEnumerator DelayedDestroy(float lifeTime, ParticleSystem particleSystem, ObjectPool<ParticleSystem> objectPool)
        {
            yield return new WaitForSeconds(lifeTime);
            if (particleSystem)
            {
                if (objectPool != null)
                    objectPool.Release(particleSystem);
                else
                {
                    var mainModule = particleSystem.main;
                    mainModule.stopAction = ParticleSystemStopAction.Destroy;
                    particleSystem.Stop(true, stopBehavior);
                }
            }
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_SoundInfo
    {
        public AudioSource sound;

        public GameplayCueNotify_PlacementInfo placementInfoOverride;

        public bool overridePlacementInfo;

        public ObjectPool<AudioSource> objectPool;
        public bool usePool;

        public bool PlaySound(GameplayCueNotify_SpawnContext spawnContext, ref GameplayCueNotify_SpawnResult spawnResult, bool destroyAfterLifetime = false)
        {
            AudioSource spawnedFX = null;

            if (sound != null && sound.clip != null)
            {
                var placementInfo = spawnContext.GetPlacementInfo(overridePlacementInfo, placementInfoOverride);
                if (!placementInfo.FindSpawnTransform(spawnContext, out var transform))
                {
                    return false;
                }

                sound.playOnAwake = false;

                spawnedFX = Instantiate();
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.AttachToTarget)
                {
                    spawnedFX.transform.SetParent(transform, false);
                }
                else
                {
                    spawnedFX.transform.position = transform.position;
                }

                Transform spawnedTransform = spawnedFX.transform; // Caching transform so no extra calls to the C++ side

                spawnedTransform.localPosition += spawnContext.cueParameters.location + placementInfo.PositionOffset;
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.DoNotAttachFollowRotation)
                {
                    spawnedTransform.rotation *= transform.rotation;
                }
                spawnedTransform.localRotation *= Quaternion.Euler(placementInfo.RotationOverride);
                spawnedTransform.localScale = placementInfo.ScaleOverride;
                spawnedFX.volume = spawnContext.cueParameters.normalizedMagnitude;
                spawnedFX.Play();

                if (destroyAfterLifetime)
                    GameplayCueManager.Instance.StartCoroutine(DelayedDestroy(spawnedFX.clip.length, spawnedFX, objectPool));
            }


            spawnResult.AudioSystemComponents.Add(new GameplayCueNotify_AudioSpawnResult(spawnedFX, objectPool, !destroyAfterLifetime));

            return spawnedFX != null;
        }

        public void CleanUp()
        {
            if (objectPool != null)
            {
                objectPool.Dispose();
                objectPool = null;
            }
        }

        private IEnumerator DelayedDestroy(float lifeTime, AudioSource audioSource, ObjectPool<AudioSource> objectPool)
        {
            yield return new WaitForSeconds(lifeTime);
            if (audioSource)
            {
                if (objectPool != null)
                    objectPool.Release(audioSource);
                else
                    Object.Destroy(audioSource.gameObject);
            }
        }

        private AudioSource Instantiate()
        {
            if (usePool)
            {
                if (objectPool == null)
                {
                    objectPool = new ObjectPool<AudioSource>(InstantiateNewParticle, OnGetInstance, OnReleaseInstance, OnDestroyItem);
                }

                return objectPool.Get();
            }
            return InstantiateNewParticle();
        }

        private AudioSource InstantiateNewParticle()
        {
            return Object.Instantiate(sound);
        }

        private void OnReleaseInstance(AudioSource audioSource)
        {
            audioSource.Stop();
        }

        private void OnGetInstance(AudioSource audioSource)
        {
            audioSource.gameObject.SetActive(true);
        }

        private void OnDestroyItem(AudioSource audioSource)
        {
            if (audioSource)
                Object.Destroy(audioSource.gameObject);
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_CustomItemInfo
    {
        public CueCustomItem customItem;
        public GameplayCueNotify_PlacementInfo placementInfoOverride;

        public bool overridePlacementInfo;

        public ObjectPool<CueCustomItem> objectPool;
        public bool usePool;

        public bool PlayEffect(GameplayCueNotify_SpawnContext spawnContext, ref GameplayCueNotify_SpawnResult spawnResult, bool destroyAfterLifetime = false)
        {
            CueCustomItem spawnedFX = null;

            if (customItem != null)
            {
                var placementInfo = spawnContext.GetPlacementInfo(overridePlacementInfo, placementInfoOverride);
                if (!placementInfo.FindSpawnTransform(spawnContext, out var transform))
                {
                    return false;
                }

                spawnedFX = Instantiate();
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.AttachToTarget)
                {
                    spawnedFX.transform.SetParent(transform, false);
                }
                else
                {
                    spawnedFX.transform.position = transform.position;
                }

                Transform spawnedTransform = spawnedFX.transform; // Caching transform so no extra calls to the C++ side

                spawnedTransform.localPosition += spawnContext.cueParameters.location + placementInfo.PositionOffset;
                if (placementInfo.attachPolicy == GameplayCueNotify_AttachPolicy.DoNotAttachFollowRotation)
                {
                    spawnedTransform.rotation *= transform.rotation;
                }
                spawnedTransform.localRotation *= Quaternion.Euler(placementInfo.RotationOverride);
                spawnedTransform.localScale = placementInfo.ScaleOverride * spawnContext.cueParameters.rawMagnitude;

                spawnedFX.PlayEffect(spawnContext);

                if (destroyAfterLifetime)
                    GameplayCueManager.Instance.StartCoroutine(DelayedDestroy(spawnedFX.BurstDuration, spawnedFX, objectPool));
            }

            spawnResult.CustomItems.Add(new GameplayCueNotify_CustomItemSpawnResult(spawnedFX, objectPool, !destroyAfterLifetime));

            return spawnedFX != null;
        }

        public void CleanUp()
        {
            if (objectPool != null)
            {
                objectPool.Dispose();
                objectPool = null;
            }
        }

        private IEnumerator DelayedDestroy(float lifeTime, CueCustomItem customItem, ObjectPool<CueCustomItem> objectPool)
        {
            yield return new WaitForSeconds(lifeTime);
            if (customItem)
            {
                if (objectPool != null)
                    objectPool.Release(customItem);
                else
                    Object.Destroy(customItem.gameObject);
            }
        }

        private CueCustomItem Instantiate()
        {
            if (usePool)
            {
                if (objectPool == null)
                {
                    objectPool = new ObjectPool<CueCustomItem>(InstantiateNewParticle, OnGetInstance, OnReleaseInstance, OnDestroyItem);
                }

                return objectPool.Get();
            }
            return InstantiateNewParticle();
        }

        private CueCustomItem InstantiateNewParticle()
        {
            return Object.Instantiate(customItem);
        }

        private void OnReleaseInstance(CueCustomItem item)
        {
            item.StopEffect();
            item.gameObject.SetActive(false);
        }

        private void OnGetInstance(CueCustomItem item)
        {
            item.gameObject.SetActive(true);
        }

        private void OnDestroyItem(CueCustomItem item)
        {
            if (item)
                Object.Destroy(item.gameObject);
        }
    }


    [System.Serializable]
    public struct GameplayCueNotify_BurstEffects
    {
        [SerializeField] private GameplayCueNotify_ParticleInfo[] burstParticles;
        [SerializeField] private GameplayCueNotify_SoundInfo[] burstSounds;
        [SerializeField] private GameplayCueNotify_CustomItemInfo[] customItems;

        public void ExecuteEffects(GameplayCueNotify_SpawnContext context, ref GameplayCueNotify_SpawnResult results)
        {
            for (int i = 0; i < burstParticles.Length; i++)
            {
                burstParticles[i].PlayParticleEffect(context, ref results, true);
            }

            for (int i = 0; i < burstSounds.Length; i++)
            {
                burstSounds[i].PlaySound(context, ref results, true);
            }

            for (int i = 0; i < customItems.Length; i++)
            {
                customItems[i].PlayEffect(context, ref results, true);
            }
        }

        public void CleanUp()
        {
            for (int i = 0; i < burstParticles.Length; i++)
            {
                burstParticles[i].CleanUp();
            }

            for (int i = 0; i < burstSounds.Length; i++)
            {
                burstSounds[i].CleanUp();
            }

            for (int i = 0; i < customItems.Length; i++)
            {
                customItems[i].CleanUp();
            }
        }
    }

    [System.Serializable]
    public struct GameplayCueNotify_LoopEffects
    {
        [SerializeField] private GameplayCueNotify_ParticleInfo[] loopingParticles;
        [SerializeField] private GameplayCueNotify_SoundInfo[] loopingSounds;
        [SerializeField] private GameplayCueNotify_CustomItemInfo[] loopingCustomItems;

        public void StartEffects(GameplayCueNotify_SpawnContext context, ref GameplayCueNotify_SpawnResult results)
        {
            for (int i = 0; i < loopingParticles.Length; i++)
            {
                loopingParticles[i].PlayParticleEffect(context, ref results, false);
            }

            for (int i = 0; i < loopingSounds.Length; i++)
            {
                loopingSounds[i].PlaySound(context, ref results, false);
            }

            for (int i = 0; i < loopingCustomItems.Length; i++)
            {
                loopingCustomItems[i].PlayEffect(context, ref results, false);
            }
        }

        public void StopEffects(ref GameplayCueNotify_SpawnResult results)
        {
            results.Reset();
        }

        public void CleanUp()
        {
            for (int i = 0; i < loopingParticles.Length; i++)
            {
                loopingParticles[i].CleanUp();
            }

            for (int i = 0; i < loopingSounds.Length; i++)
            {
                loopingSounds[i].CleanUp();
            }

            for (int i = 0; i < loopingCustomItems.Length; i++)
            {
                loopingCustomItems[i].CleanUp();
            }
        }
    }
}
