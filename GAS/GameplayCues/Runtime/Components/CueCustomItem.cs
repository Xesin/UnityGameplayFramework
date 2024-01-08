using UnityEngine;

namespace Xesin.GameplayCues
{
    public class CueCustomItem : MonoBehaviour
    {
        [SerializeField]
        protected bool destroyOnStop = true;
        [SerializeField]
        protected float destroyDelay;
        [field: SerializeField]
        public float BurstDuration { get; private set; } = 2;



        public virtual void PlayEffect(GameplayCueNotify_SpawnContext spawnContext)
        {

        }

        public virtual void StopEffect()
        {
            if(destroyOnStop)
            {
                Destroy(gameObject, destroyDelay);
            }
        }
    }
}
