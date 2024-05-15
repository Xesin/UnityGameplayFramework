using UnityEngine;

namespace Xesin.GameplayFramework
{
    public abstract class InputImageCollection : ScriptableObject
    {
        public abstract Sprite GetInputImage(string inputPath);
    }
}
