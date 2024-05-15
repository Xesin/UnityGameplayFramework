#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.InputSystem;

namespace Xesin.GameplayFramework
{
    [CreateAssetMenu(fileName = "GameplaySettings.asset", menuName = "Gameplay/Settings")]
    public class GameplayGlobalSettings : Utils.ScriptableSingleton<GameplayGlobalSettings>
    {
        public LocalPlayer localPlayerPrefab;
        public InputActionAsset gameInputActionAsset;
        public bool autocreatePlayersOnInput;
        public bool autocreatePlayerOne = true;

#if UNITY_EDITOR
        public override void OnScriptableCreated()
        {
            string[] playerPrefabs = AssetDatabase.FindAssets("t:GameObject LocalPlayer");

            for (int i = 0; i < playerPrefabs.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(playerPrefabs[i]);

                if (path.StartsWith("Packages/com.xesin.gameplay-framework"))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<LocalPlayer>();
                    localPlayerPrefab = asset;
                    break;
                }
            }
        }
#endif
    }
}
