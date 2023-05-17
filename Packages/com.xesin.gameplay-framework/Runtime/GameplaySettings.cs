using UnityEngine;

namespace GameplayFramework
{
    [CreateAssetMenu(fileName = "GameplaySettings.asset", menuName = "Gameplay/Settings")]
    public class GameplaySettings : ScriptableObject
    {
        private static GameplaySettings _instance;
        public static GameplaySettings Instance
        {
            get
            {
                if(!_instance)
                {
                    _instance = Resources.Load<GameplaySettings>("GameplaySettings");
                }

                return _instance;
            }
        }
        public LocalPlayer localPlayerPrefab;
        public bool autocreatePlayersOnInput;
    }
}
