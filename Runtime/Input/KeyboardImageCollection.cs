using System.Collections.Generic;
using UnityEngine;

namespace Xesin.GameplayFramework
{
    [CreateAssetMenu(menuName = "Gameplay/Input/Keyboard Images Collection", fileName = "KeyboardImagesCollection.asset")]
    public class KeyboardImageCollection : InputImageCollection
    {
        [Tooltip("Image names needs no folow the {inputPath}_Key naming")]
        [SerializeField] private Sprite[] keyImages;
        [SerializeField] private Sprite unrecognized;

        private Dictionary<string, Sprite> cachedResults;

        private void OnEnable()
        {
            cachedResults ??= new Dictionary<string, Sprite>();
            cachedResults.Clear();
        }

        public override Sprite GetInputImage(string inputPath)
        {

            for (int i = 0; i < keyImages.Length; i++)
            {
                if(cachedResults.TryGetValue(inputPath, out var cachedValue))
                {
                    return cachedValue;
                }

                if (keyImages[i].name.StartsWith(inputPath + "_Key", System.StringComparison.OrdinalIgnoreCase))
                {
                    cachedResults.Add(inputPath, keyImages[i]);
                    return keyImages[i];
                }
            }

            return unrecognized;
        }
    }
}
