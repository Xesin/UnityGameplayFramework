using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Xesin.GameplayCues
{
    public class Node
    {
        public string id = string.Empty;
        public Node parent;
        public List<Node> children;

        public override bool Equals(object obj)
        {
            if (obj is string otherID)
            {
                return id.Equals(otherID);
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(id);
        }

        public GameplayTag ToGameplayTag()
        {
            return new GameplayTag(ToGameplayTagString());
        }

        public string ToGameplayTagString()
        {
            Node currentNode = parent;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(id);

            while (currentNode != null)
            {
                stringBuilder.Insert(0, currentNode.id + ".");
                currentNode = currentNode.parent;
            }

            return stringBuilder.ToString();
        }
    }


    public class GameplayTagsContainer : ScriptableObject, ISerializationCallbackReceiver
    {
        internal const string ConfigName = "com.xaloc.gameplaycues.cueset";

        public List<GameplayTag> addedTags = new();
        public List<GameplayTagRedirect> tagRedirects = new();

        private HashSet<GameplayTag> addedTags_set = new HashSet<GameplayTag>();
        private HashSet<GameplayTagRedirect> tagRedirects_set = new HashSet<GameplayTagRedirect>();

        private List<Node> nodeTree = new List<Node>();
        public IReadOnlyList<Node> NodeTree => nodeTree;

        static GameplayTagsContainer s_Instance;

        public static GameplayTagsContainer Instance
        {
            get
            {
                // Use ReferenceEquals so we dont get false positives when using MoQ
                if (ReferenceEquals(s_Instance, null))
                    s_Instance = GetOrCreateSettings();
                return s_Instance;
            }
            set => s_Instance = value;
        }

        /// <summary>
        /// Used to retrieve a <see cref="GameplayTag"/> for the specified string tag. It tries to resolve the renaming rules before returning the final tag.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns>The tag that is already converted if a redirect rule was applied</returns>
        public static GameplayTag RequestGameplayTag(string tag)
        {
            GameplayTag result = Instance.ResolveTag(new GameplayTag(tag));

            if (!Instance.IsValid(result))
            {
                if (Application.isPlaying)
                    Debug.LogWarning("Requested invalid tag: " + tag + " is missing");
            }

            return result;
        }

#if UNITY_EDITOR

        /// <summary>
        /// EDITOR ONLY. Adds a new <see cref="GameplayTag"/> to the valid tags using the specified string.
        /// </summary>
        /// <param name="value"></param>
        public void AddGameplayTag(string value)
        {
            AddGameplayTag(new GameplayTag(value));
        }

        /// <summary>
        /// EDITOR ONLY. Renames a <see cref="GameplayTag"/> to the specified tag string. This adds a redirect rule so old tags can be converted to the old one
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        public void RenameGameplayTag(string oldValue, string newValue)
        {
            var oldTag = new GameplayTag(oldValue);
            var newTag = new GameplayTag(newValue);
            AddGameplayTag(newTag);
            DeleteGameplayTag(oldTag);

            var redirect = new GameplayTagRedirect();
            redirect.originalTagValue = oldValue;
            redirect.redirectectValue = newValue;

            tagRedirects.Add(redirect);
            tagRedirects_set.Add(redirect);

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }

        /// <summary>
        /// EDITOR ONLY. Adds a new <see cref="GameplayTag"/> to the valid tags.
        /// </summary>
        /// <param name="value"></param>
        public void AddGameplayTag(GameplayTag value)
        {
            if (addedTags_set.Contains(value)) return;
            addedTags_set.Add(value);
            addedTags.Append(value);

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            BuildNodeTree();

            if (!string.IsNullOrEmpty(value.ParentTag))
            {
                AddGameplayTag(new GameplayTag(value.ParentTag));
            }
        }

        /// <summary>
        /// EDITOR ONLY. Deletes the specified <see cref="GameplayTag"/>.
        /// </summary>
        /// <param name="value"></param>
        public void DeleteGameplayTag(GameplayTag value)
        {
            if (!addedTags_set.Contains(value)) return;
            addedTags_set.Remove(value);
            addedTags.Remove(value);

            for (int i = addedTags.Count - 1; i >= 0; i--)
            {
                if (i >= addedTags.Count) break;

                if (addedTags[i].MatchesTag(value))
                {
                    addedTags_set.Remove(addedTags[i]);
                    addedTags.Remove(addedTags[i]);
                }
            }

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            BuildNodeTree();
        }
#endif

        void OnEnable()
        {
            Debug.LogWarning("Tag container Initialized");
            BuildNodeTree();
            Instance = this;
        }

        /// <summary>
        /// Used to check if a <see cref="GameplayTag"/> is in the system.
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns></returns>
        public bool IsValid(GameplayTag gameplayTag)
        {
            var tag = ResolveTag(gameplayTag);

            return addedTags_set.Contains(tag);
        }

        /// <summary>
        /// Method that checks the redirect rules and applies them recursively.
        /// </summary>
        /// <param name="gameplayTag"></param>
        /// <returns>A <see cref="GameplayTag"/> with all redirects applied</returns>
        public GameplayTag ResolveTag(GameplayTag gameplayTag)
        {
            foreach (var redirector in tagRedirects_set)
            {
                if (redirector.originalTagValue.GetHashCode() == gameplayTag.value.GetHashCode())
                {
                    gameplayTag.value = redirector.redirectectValue;
                    gameplayTag = ResolveTag(gameplayTag);
                    break;
                }
            }

            return gameplayTag;
        }

        /// <summary>
        /// Part of the singleton pattern
        /// </summary>
        /// <returns></returns>
        public static GameplayTagsContainer GetInstanceDontCreateDefault()
        {
            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (!ReferenceEquals(s_Instance, null))
                return s_Instance;

            GameplayTagsContainer settings;
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(ConfigName, out settings);
#else
            settings = FindObjectOfType<GameplayTagsContainer>();
#endif
            return settings;
        }

        static GameplayTagsContainer GetOrCreateSettings()
        {
            var settings = GetInstanceDontCreateDefault();

            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (ReferenceEquals(settings, null))
            {
                Debug.LogWarning("Could not find GameplayTagsContainer. Default will be used.");

                settings = CreateInstance<GameplayTagsContainer>();
#if UNITY_EDITOR
                UnityEditor.AssetDatabase.CreateAsset(settings, "Assets/GameplayTags.asset");

                settings.name = "GameplayTags";
                var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets();
                UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.Append(settings).ToArray());
                UnityEditor.EditorBuildSettings.AddConfigObject(ConfigName, settings, true);
#else
                settings.name = "Default GameplayTags";
#endif
            }

            return settings;
        }


        private void BuildNodeTree()
        {
            nodeTree.Clear();
            foreach (var tag in addedTags)
            {
                var tagMembers = tag.value.Split(".");
                Node currentNode = null;
                if (!nodeTree.Any(n => n.Equals(tagMembers[0])))
                {
                    currentNode = new Node() { id = tagMembers[0], children = new List<Node>() };
                    nodeTree.Add(currentNode);
                }
                else
                {
                    currentNode = nodeTree.First(n => n.Equals(tagMembers[0]));
                }

                for (int i = 1; i < tagMembers.Length; i++)
                {
                    if (currentNode.children.Any(n => n.Equals(tagMembers[i])))
                    {
                        currentNode = currentNode.children.First(n => n.Equals(tagMembers[i]));
                        continue;
                    }

                    var newNode = new Node() { id = tagMembers[i], children = new List<Node>() };
                    newNode.parent = currentNode;
                    currentNode.children.Add(newNode);

                    currentNode = newNode;
                }
            }
        }

        public void OnBeforeSerialize()
        {
            addedTags = addedTags_set.ToList();
            tagRedirects = tagRedirects_set.ToList();
        }

        public void OnAfterDeserialize()
        {

            for (int i = 0; i < addedTags.Count; i++)
            {
                addedTags_set.Add(addedTags[i]);
            }

            for (int i = 0; i < tagRedirects.Count; i++)
            {
                tagRedirects_set.Add(tagRedirects[i]);
            }

            BuildNodeTree();
        }
    }
}
