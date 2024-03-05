#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using Xesin.GameplayFramework.Domain;

namespace Xesin.GameplayFramework.Utils
{

    /// <summary>
    /// Creates a singleton.
    /// </summary>
    /// <typeparam name="T">The singleton type.</typeparam>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        [ClearOnReload]
        protected static T s_Instance;

        /// <summary>
        /// Indicates whether or not there is an existing instance of the singleton.
        /// </summary>
        public static bool Exists => s_Instance != null;

        /// <summary>
        /// Stores the instance of the singleton.
        /// </summary>
        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = CreateNewSingleton();
                }
                return s_Instance;
            }
        }

        /// <summary>
        /// Retrieves the name of the object.
        /// </summary>
        /// <returns>Returns the name of the object.</returns>
        protected virtual string GetGameObjectName() => typeof(T).Name;

        static T CreateNewSingleton()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying) return null;
#endif
            var go = new GameObject();

            if (Application.isPlaying)
            {
                DontDestroyOnLoad(go);
                go.hideFlags = HideFlags.DontSave;
            }
            else
            {
                go.hideFlags = HideFlags.HideAndDontSave;
            }
            var instance = go.AddComponent<T>();
            go.name = instance.GetGameObjectName();
            return instance;
        }

        protected virtual void Awake()
        {
            if (s_Instance != null && s_Instance != this)
            {
                DestroyImmediate(gameObject);
                return;
            }
            s_Instance = this as T;
        }

        /// <summary>
        /// Destroys the singleton.
        /// </summary>
        public static void DestroySingleton()
        {
            if (Exists)
            {
                DestroyImmediate(Instance.gameObject);
                s_Instance = null;
            }
        }

#if UNITY_EDITOR
        void OnEnable()
        {
            EditorApplication.playModeStateChanged += PlayModeChanged;
        }

        void OnDisable()
        {
            EditorApplication.playModeStateChanged -= PlayModeChanged;
        }

        void PlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                if (Exists)
                {
                    DestroyImmediate(Instance.gameObject);
                    s_Instance = null;
                }
            }
        }

#endif
    }
}