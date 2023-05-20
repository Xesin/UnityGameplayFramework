using GameplayFramework;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

class GameplayFrameworkSettingsPanelProvider
{
    private static SerializedObject settings;

    class Styles
    {
        public static GUIContent playerPrefab = new GUIContent("Player prefab");
        public static GUIContent autoCreatePlayers = new GUIContent("Auto-create players");
        public static GUIContent assetNotPresent = new GUIContent("Settings asset is not present");
        public static GUIContent fix = new GUIContent("Fix");

        public static GUIStyle errorStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } };
    }

    [SettingsProvider]
    public static SettingsProvider CreateSettingsProvider()
    {
        

        return new SettingsProvider("Project/GameplayFramework", SettingsScope.Project)
        {
            label = "GameplayFramework",
            activateHandler = OnActivate,
            guiHandler = OnGUI,
            titleBarGuiHandler = OnTitleBarGUI
        };
    }

    static void OnActivate(string searchContext, VisualElement root)
    {
        if(GameplayGlobalSettings.Instance)
            settings = new SerializedObject(GameplayGlobalSettings.Instance);
    }

    static void OnGUI(string searchContext)
    {
        
        EditorGUI.indentLevel = 1;
        EditorGUILayout.Separator();

        if (!GameplayGlobalSettings.Instance)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.assetNotPresent, Styles.errorStyle);
            if (GUILayout.Button("Fix"))
            {
                GameplayGlobalSettings.CreateAsset();
            }
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            settings.Update();
            EditorGUILayout.PropertyField(settings.FindProperty("m_localPlayerPrefab"), Styles.playerPrefab);
            EditorGUILayout.PropertyField(settings.FindProperty("m_autocreatePlayersOnInput"), Styles.autoCreatePlayers);
            if (settings.hasModifiedProperties)
            {
                settings.ApplyModifiedProperties();
            }
        }
    }

    static void OnTitleBarGUI()
    {
    }
}
