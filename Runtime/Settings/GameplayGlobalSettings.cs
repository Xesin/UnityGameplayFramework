using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [CreateAssetMenu(fileName = "GameplaySettings.asset", menuName = "Gameplay/Settings")]
    public class GameplayGlobalSettings : ScriptableObject
    {
        public static string assetPath = "Assets/Resources/GameplaySettings.asset";
        private static GameplayGlobalSettings s_Instance;
        public static GameplayGlobalSettings Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = Resources.Load<GameplayGlobalSettings>("GameplaySettings");
                }

                return s_Instance;
            }
        }

        [SerializeField]
        private LocalPlayer m_localPlayerPrefab;
        public LocalPlayer localPlayerPrefab
        {
            get => m_localPlayerPrefab;
            set
            {
                m_localPlayerPrefab = value;
            }
        }

        [SerializeField]
        private bool m_autocreatePlayersOnInput;
        public bool autocreatePlayersOnInput
        {
            get => m_autocreatePlayersOnInput;
            set
            {
                m_autocreatePlayersOnInput = value;
            }
        }

#if UNITY_EDITOR
        public static GameplayGlobalSettings CreateAsset()
        {
            var newInstance = CreateInstance<GameplayGlobalSettings>();

            if (!Directory.Exists(Path.GetDirectoryName(assetPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(assetPath));
            }

            string[] playerPrefabs = AssetDatabase.FindAssets("t:GameObject LocalPlayer");

            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(playerPrefabs[i]);

                if(path.StartsWith("Packages/com.xesin.gameplay-framework"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LocalPlayer>();
                    newInstance.localPlayerPrefab = asset;
                    break;
                }
            }

            AssetDatabase.CreateAsset(newInstance, assetPath);

            return newInstance;
        }
#endif
    }
}
