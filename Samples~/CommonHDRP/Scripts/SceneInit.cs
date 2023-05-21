using GameplayFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneInit : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return FindObjectOfType<GameMode>().OnLevelReady();
    }
}
