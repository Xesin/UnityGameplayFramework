using UnityEngine;

namespace GameplayFramework
{
    public abstract class GameplayObject : MonoBehaviour
    {
        public bool useAbsoluteRotation = false;

        public GameplayObject Owner { get; private set; }

        public virtual void SetOwner(GameplayObject obj)
        {
            Owner = obj;
        }
    }
}
