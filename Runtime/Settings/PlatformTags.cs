using System;
using UnityEngine;
using Xesin.GameplayCues;
using Xesin.GameplayFramework.Utils;

namespace Xesin.GameplayFramework
{
    [Serializable]
    public struct PlatformTagsConfig
    {
        public string displayName;
        public RuntimePlatform platform;
        public GameplayTagList tagList;
    }

    public class PlatformTags : ScriptableSingleton<PlatformTags>
    {

        [SerializeField]
        private PlatformTagsConfig[] tagsConfig;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod()]
        private static void OnInitEditor()
        {
            _ = Instance;
        }
#endif

        public bool HasTagForCurrentPlatform(GameplayTag tag, bool fullMatch = true)
        {
            return HasTagForPlatform(Application.platform, tag, fullMatch);
        }

        public bool HasTagForPlatform(RuntimePlatform platform, GameplayTag tag, bool fullMatch = true)
        {
            if (tagsConfig == null) return false;

            bool containsTag = false;

            for (int i = 0; i < tagsConfig.Length; i++)
            {
                containsTag |= (tagsConfig[i].platform == platform
#if UNITY_EDITOR
                    || tagsConfig[i].platform == (TryConvertToRuntimePlatform(UnityEditor.EditorUserBuildSettings.activeBuildTarget) ?? Application.platform)
#endif
                    ) && tagsConfig[i].tagList.Contains(tag, fullMatch);

            }

            return containsTag;
        }

#if UNITY_EDITOR
        public static RuntimePlatform? TryConvertToRuntimePlatform(UnityEditor.BuildTarget buildTarget)
        {
            return buildTarget switch
            {
                UnityEditor.BuildTarget.Android => RuntimePlatform.Android,
                UnityEditor.BuildTarget.PS4 => RuntimePlatform.PS4,
                UnityEditor.BuildTarget.PS5 => RuntimePlatform.PS5,
                UnityEditor.BuildTarget.StandaloneLinux64 => RuntimePlatform.LinuxPlayer,
                UnityEditor.BuildTarget.StandaloneOSX => RuntimePlatform.OSXPlayer,
                UnityEditor.BuildTarget.StandaloneWindows => RuntimePlatform.WindowsPlayer,
                UnityEditor.BuildTarget.StandaloneWindows64 => RuntimePlatform.WindowsPlayer,
                UnityEditor.BuildTarget.Switch => RuntimePlatform.Switch,
                UnityEditor.BuildTarget.XboxOne => RuntimePlatform.XboxOne,
                UnityEditor.BuildTarget.iOS => RuntimePlatform.IPhonePlayer,
                UnityEditor.BuildTarget.tvOS => RuntimePlatform.tvOS,
                UnityEditor.BuildTarget.WebGL => RuntimePlatform.WebGLPlayer,
                UnityEditor.BuildTarget.GameCoreXboxSeries => RuntimePlatform.GameCoreXboxSeries,
                UnityEditor.BuildTarget.GameCoreXboxOne => RuntimePlatform.GameCoreXboxOne,
                UnityEditor.BuildTarget.Stadia => RuntimePlatform.Stadia,
                UnityEditor.BuildTarget.EmbeddedLinux => RuntimePlatform.EmbeddedLinuxArm64,
                UnityEditor.BuildTarget.QNX => RuntimePlatform.QNXArm64,
                _ => null,
            };
        }
#endif
    }
}
