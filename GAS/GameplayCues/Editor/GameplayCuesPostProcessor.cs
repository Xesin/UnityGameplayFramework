using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Xesin.GameplayCues
{
    public class GameplayCuesPostProcessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            var cues = importedAssets.Where(assetPath => assetPath.EndsWith("prefab") && AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<GameplayCueNotify_GameObject>()).ToList();

            cues.ForEach(assetPath =>
            {
                var locale = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath).GetComponent<GameplayCueNotify_GameObject>();
                Debug.Assert(locale != null, "Failed to load cue asset.");
                GameplayCuesEditor.AddGameplayCue(locale);
            });
        }
    }
}
