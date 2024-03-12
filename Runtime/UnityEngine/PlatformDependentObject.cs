using UnityEngine;
using Xesin.GameplayCues;

namespace Xesin.GameplayFramework
{
    public class PlatformDependentObject : MonoBehaviour
    {
        [SerializeField] private GameplayTagList tagsToCheck;
        [SerializeField] private bool invertCondition;
        [SerializeField] private bool mustContainAllTags = false;
        [SerializeField] private bool performFullTagMatches = true;

        protected virtual void OnEnable()
        {
            var tagList = tagsToCheck.Tags;
            var platformTags = PlatformTags.Instance;
            bool containsAnyTag = mustContainAllTags && tagList.Count > 0;

            for (int i = 0; i < tagList.Count; i++)
            {
                if (mustContainAllTags)
                {
                    containsAnyTag &= platformTags.HasTagForCurrentPlatform(tagList[i], performFullTagMatches);
                }
                else
                {
                    containsAnyTag |= platformTags.HasTagForCurrentPlatform(tagList[i], performFullTagMatches);
                }
            }

            gameObject.SetActive(containsAnyTag != invertCondition);
        }
    }
}
