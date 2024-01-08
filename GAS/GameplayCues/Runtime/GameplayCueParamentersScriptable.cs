using UnityEngine;

namespace Xesin.GameplayCues
{
    [CreateAssetMenu(menuName = "Xaloc/GameplayCues/Parameters", fileName = "CueParameter.asset")]
    public class GameplayCueParamentersScriptable : ScriptableObject
    {
        public Vector3 location;
        public float normalizedMagnitude;
        public float rawMagnitude;
    }
}
