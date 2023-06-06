using UnityEngine;

namespace Xesin.GameplayFramework
{
    public abstract class GameplayObject : MonoBehaviour
    {
        public GameplayObject Owner { get; private set; }
        

        public virtual void SetOwner(GameplayObject obj)
        {
            Owner = obj;
        }
    }
}
