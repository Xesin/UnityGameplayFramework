using System;
using UnityEditorInternal;
using UnityEngine;

namespace GameplayFramework
{
    public class GameplaySettingsReadOnlyBase : ScriptableObject
    {
        protected const string filePath = "ProjectSettings/GameFrameworkProjectSettings.asset";

        [SerializeField]
        protected string m_ProjectSettingFolderPath = "FrameworkDefaultResources";

        public static string projectSettingsFolderPath => instance.m_ProjectSettingFolderPath;

        protected static GameplaySettingsReadOnlyBase s_Instance;
        static GameplaySettingsReadOnlyBase instance => s_Instance ?? CreateOrLoad();

        protected GameplaySettingsReadOnlyBase()
        {
            s_Instance = this;
        }

        static GameplaySettingsReadOnlyBase CreateOrLoad()
        {
            InternalEditorUtility.LoadSerializedFileAndForget(filePath);

            if (s_Instance == null)
            {
                GameplaySettingsReadOnlyBase created = CreateInstance<GameplaySettingsReadOnlyBase>();
                created.hideFlags = HideFlags.HideAndDontSave;
            }

            return s_Instance;
        }
    }
}
