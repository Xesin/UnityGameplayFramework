using System.IO;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace Xesin.GameplayFramework
{
    [CreateAssetMenu(fileName = "GameplaySettings.asset", menuName = "Gameplay/Settings")]
    public class GameplayGlobalSettings : ScriptableObject
    {
        internal const string ConfigName = "com.xaloc.gameplaycues.cueset";
        internal const string AssetPath = "Assets/Settings/GameplaySettings.asset";

        private static GameplayGlobalSettings instance;
        public static GameplayGlobalSettings Instance
        {
            get
            {
                // Use ReferenceEquals so we dont get false positives when using MoQ
                if (ReferenceEquals(instance, null))
                    instance = GetOrCreateSettings();
                return instance;
            }
            set => instance = value;
        }

        public LocalPlayer localPlayerPrefab;
        public InputActionAsset gameInputActionAsset;
        public bool autocreatePlayersOnInput;
        public bool autocreatePlayerOne = true;

        public static GameplayGlobalSettings GetInstanceDontCreateDefault()
        {
            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (!ReferenceEquals(instance, null))
                return instance;

            GameplayGlobalSettings settings;
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(ConfigName, out settings);
#else
            settings = FindObjectOfType<GameplayTagsContainer>();
#endif
            return settings;
        }

        static GameplayGlobalSettings GetOrCreateSettings()
        {
            var settings = GetInstanceDontCreateDefault();

            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (ReferenceEquals(settings, null))
            {
                Debug.LogWarning("Could not find GameplayGlobalSettings. Default will be used.");

                settings = CreateInstance<GameplayGlobalSettings>();
#if UNITY_EDITOR
                if (!Directory.Exists(Path.GetDirectoryName(AssetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                }

                string[] playerPrefabs = AssetDatabase.FindAssets("t:GameObject LocalPlayer");

                for (int i = 0; i < playerPrefabs.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(playerPrefabs[i]);

                    if (path.StartsWith("Packages/com.xesin.gameplay-framework"))
                    {
                        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LocalPlayer>();
                        settings.localPlayerPrefab = asset;
                        break;
                    }
                }
                settings.name = "GameplayGlobalSettings";

                AssetDatabase.CreateAsset(settings, AssetPath);

                var preloadedAssets = PlayerSettings.GetPreloadedAssets();
                PlayerSettings.SetPreloadedAssets(preloadedAssets.Append(settings).ToArray());
                EditorBuildSettings.AddConfigObject(ConfigName, settings, true);
#else
                settings.name = "GameplayGlobalSettings";
#endif
            }

            return settings;
        }
    }
}
