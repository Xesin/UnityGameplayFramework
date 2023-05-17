using UnityEngine;

namespace GameplayFramework
{
    public abstract class GameplayObject : MonoBehaviour
    {
        public GameplayObject Owner { get; private set; }

        public void SetOwner(GameplayObject obj)
        {
            Owner = obj;
        }
    }
}
