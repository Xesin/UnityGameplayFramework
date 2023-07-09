using System.Collections;
using UnityEngine;

namespace Xesin.GameplayFramework.Samples.HDRP
{
    public class SceneInit : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return FindObjectOfType<GameMode>().OnLevelReady();
        }
    }
}