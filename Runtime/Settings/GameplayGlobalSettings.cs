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
        private static GameplayGlobalSettings instance;
        public static GameplayGlobalSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameplayGlobalSettings>("GameplaySettings");
                }

                return instance;
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
        private bool autocreatePlayersOnInput;
        [SerializeField]
        private bool autocreatePlayerOne = true;
        public bool AutocreatePlayersOnInput
        {
            get => autocreatePlayersOnInput;
            set
            {
                autocreatePlayersOnInput = value;
            }
        }
        public bool AutocreatePlayerOne
        {
            get => autocreatePlayerOne;
            set
            {
                autocreatePlayerOne = value;
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
