using Xesin.GameplayFramework;
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
        public static GUIContent inputActions = new GUIContent("Game Input Actions");
        public static GUIContent autoCreatePlayerOne = new GUIContent("Auto-create first player");
        public static GUIContent autoCreatePlayers = new GUIContent("Auto-create players");
        public static GUIContent assetNotPresent = new GUIContent("Settings asset is not present");
        public static GUIContent fix = new GUIContent("Fix");

        public static GUIStyle errorStyle = new GUIStyle() { normal = new GUIStyleState() { textColor = Color.red } };
    }

    private static SerializedProperty localPlayerPrefab;
    private static SerializedProperty inputActionAsset;
    private static SerializedProperty autocreatePlayerOne;
    private static SerializedProperty autocreatePlayersOnInput;

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
        EditorApplication.update += Init;
    }

    static void Init()
    {
        EditorApplication.update -= Init;
        if (GameplayGlobalSettings.Instance)
        {
            settings = new SerializedObject(GameplayGlobalSettings.Instance);
            localPlayerPrefab = settings.FindProperty(nameof(GameplayGlobalSettings.localPlayerPrefab));
            inputActionAsset = settings.FindProperty(nameof(GameplayGlobalSettings.gameInputActionAsset));
            autocreatePlayerOne = settings.FindProperty(nameof(GameplayGlobalSettings.autocreatePlayerOne));
            autocreatePlayersOnInput = settings.FindProperty(nameof(GameplayGlobalSettings.autocreatePlayersOnInput));
        }
    }

    static void OnGUI(string searchContext)
    {
        
        EditorGUI.indentLevel = 1;
        EditorGUILayout.Separator();

        if (!GameplayGlobalSettings.Instance)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.assetNotPresent, Styles.errorStyle);
            EditorGUILayout.EndHorizontal();
        }
        else
        {
            settings.Update();
            EditorGUILayout.PropertyField(localPlayerPrefab, Styles.playerPrefab);
            EditorGUILayout.PropertyField(inputActionAsset, Styles.inputActions);
            EditorGUILayout.PropertyField(autocreatePlayerOne, Styles.autoCreatePlayerOne);
            EditorGUILayout.PropertyField(autocreatePlayersOnInput, Styles.autoCreatePlayers);
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
