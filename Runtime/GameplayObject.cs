using UnityEngine;

namespace Xesin.GameplayFramework
{
    public abstract class GameplayObject : MonoBehaviour
    {
        public SceneObject Owner { get; private set; }
        

        public virtual void SetOwner(SceneObject obj)
        {
            Owner = obj;
        }
    }
}
