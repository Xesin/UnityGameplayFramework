using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Xesin.GameplayCues
{
    public class GameplayCuesEditor
    {
        private static GroupResolver groupResolver = new GroupResolver();

        [MenuItem("Assets/Create/Xaloc/GameplayCues/Looping Cue")]
        public static void CreateLoopingCue()
        {
            if(TryGetActiveFolderPath(out string path))
            {
                var newPrefab = new GameObject("GC_Looping", typeof(GameplayCueNotify_Loop));
                newPrefab.hideFlags = HideFlags.HideInHierarchy;
                var uniqueName = AssetDatabase.GenerateUniqueAssetPath(path + "/GC_NewCue.prefab");
                var savedPrfab = PrefabUtility.SaveAsPrefabAsset(newPrefab, uniqueName);

                Object.DestroyImmediate(newPrefab);

                EditorGUIUtility.PingObject(savedPrfab);

            }
        }

        [MenuItem("Assets/Create/Xaloc/GameplayCues/Burst Cue")]
        public static void CreateBurstCue()
        {
            if (TryGetActiveFolderPath(out string path))
            {
                var newPrefab = new GameObject("GC_Burst", typeof(GameplayCueNotify_Burst));
                newPrefab.hideFlags = HideFlags.HideInHierarchy;
                var uniqueName = AssetDatabase.GenerateUniqueAssetPath(path + "/GC_NewCueBurst.prefab");
                var savedPrfab = PrefabUtility.SaveAsPrefabAsset(newPrefab, uniqueName);

                Object.DestroyImmediate(newPrefab);

                EditorGUIUtility.PingObject(savedPrfab);
            }
        }

        // Define this function somewhere in your editor class to make a shortcut to said hidden function
        private static bool TryGetActiveFolderPath(out string path)
        {
            var _tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);

            object[] args = new object[] { null };
            bool found = (bool)_tryGetActiveFolderPath.Invoke(null, args);
            path = (string)args[0];

            return found;
        }

        public static void AddGameplayCue(GameplayCueNotify_GameObject cueObject)
        {
            AddressableAssetSettings settings = GetAddressableAssetSettings(false);

            if(string.IsNullOrEmpty(cueObject.TriggerTag.value))
            {
                Debug.LogWarning($"Cue object {cueObject.name} will not be added, the gameplay cue tag is null or empty");
                groupResolver.RemoveFromGroup(cueObject.gameObject, settings);
                return;
            }
            groupResolver.AddToGroup(cueObject.gameObject, settings, cueObject.TriggerTag.value, true);
        }

        internal static AddressableAssetSettings GetAddressableAssetSettings(bool create)
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(create);
            if (settings != null)
                return settings;

            // By default Addressables wont return the settings if updating or compiling. This causes issues for us, especially if we are trying to get the Locales.
            // We will just ignore this state and try to get the settings regardless.
            if (EditorApplication.isUpdating || EditorApplication.isCompiling)
            {
                // Legacy support
                if (EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName, out settings))
                {
                    return settings;
                }

                AddressableAssetSettingsDefaultObject so;
                if (EditorBuildSettings.TryGetConfigObject(AddressableAssetSettingsDefaultObject.kDefaultConfigObjectName, out so))
                {
                    // Extract the guid
                    var serializedObject = new SerializedObject(so);
                    var guid = serializedObject.FindProperty("m_AddressableAssetSettingsGuid")?.stringValue;
                    if (!string.IsNullOrEmpty(guid))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(guid);
                        return AssetDatabase.LoadAssetAtPath<AddressableAssetSettings>(path);
                    }
                }
            }
            return null;
        }
    }
}
