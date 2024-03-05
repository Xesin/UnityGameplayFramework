#if UNITY_EDITOR
using System.IO;
using System.Linq;
#endif
using UnityEngine;

namespace Xesin.GameplayFramework.Utils
{

    /// <summary>
    /// Creates a ScriptableObject as a singleton.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    public abstract class ScriptableSingleton<T> : ScriptableObject where T : ScriptableSingleton<T>
    {
        public static string SettingsFolder => "Assets/Settings/";
        public static string AssetName => $"{typeof(T).Name}.asset";

        public static string AssetPath => SettingsFolder + AssetName;

        public static string ConfigName => "com.xesin.settings." + typeof(T).Name.ToLower();

        static T s_Instance;

        public static T Instance
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

        private static T GetOrCreateSettings()
        {
            var settings = GetInstanceDontCreateDefault();

            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (ReferenceEquals(settings, null))
            {
                Debug.LogWarning($"Could not find {typeof(T).Name}. Default will be used.");

                settings = CreateInstance<T>();
#if UNITY_EDITOR
                settings.OnScriptableCreated();
                if (!Directory.Exists(Path.GetDirectoryName(AssetPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
                }

                UnityEditor.AssetDatabase.CreateAsset(settings, AssetPath);

                settings.name = typeof(T).Name;
                var preloadedAssets = UnityEditor.PlayerSettings.GetPreloadedAssets();
                UnityEditor.EditorBuildSettings.AddConfigObject(ConfigName, settings, true);
                bool alreadyPreloaded = preloadedAssets.Any(val => val != null && val is T);
                for (int i = 0; i < preloadedAssets.Length; i++)
                {
                    if ((alreadyPreloaded && preloadedAssets[i] is T) || (!alreadyPreloaded && preloadedAssets[i] == null))
                    {
                        preloadedAssets[i] = settings;
                        UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets);
                        return settings;
                    }
                }
                UnityEditor.PlayerSettings.SetPreloadedAssets(preloadedAssets.Append(settings).ToArray());
#else
                settings.name = $"Default {typeof(T).Name}";
#endif
            }

            return settings;
        }

        public static T GetInstanceDontCreateDefault()
        {
            // Use ReferenceEquals so we dont get false positives when using MoQ
            if (!ReferenceEquals(s_Instance, null))
                return s_Instance;

            T settings;
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(ConfigName, out settings);
#else
            settings = FindObjectOfType<T>();
#endif
            return settings;
        }
#if UNITY_EDITOR
        public virtual void OnScriptableCreated()
        {

        }
#endif

        void OnEnable()
        {
            if (s_Instance == null)
            {
                s_Instance = (T)this;
            }
        }

    }
}