using UnityEngine;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework
{
    public class PlatformDependentObject : MonoBehaviour
    {
        [SerializeField] private GameplayTagList tagsToCheck;
        [SerializeField] private bool invertCondition;
        [SerializeField] private bool performFullMatch = true;

        protected virtual void OnEnable()
        {
            var tagList = tagsToCheck.Tags;
            var platformTags = PlatformTags.Instance;
            bool containsAnyTag = false;
            for (int i = 0; i < tagList.Count; i++)
            {
                if(platformTags.HasTagForCurrentPlatform(tagList[i], performFullMatch))
                {
                    containsAnyTag = true;
                    break;
                }
            }

            gameObject.SetActive(containsAnyTag != invertCondition);
        }
    }
}
