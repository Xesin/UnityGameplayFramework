using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Xesin.GameplayCues
{
    [CustomEditor(typeof(GameplayTagsContainer))]
    public class GameplayTagsContainerInspector: Editor
    {
        SerializedProperty addedTags;
        SerializedProperty renamedTags;

        private void OnEnable()
        {
            addedTags = serializedObject.FindProperty("addedTags");
            renamedTags = serializedObject.FindProperty("tagRedirects");

        }

        string newTag;
        bool alreadyExists;
        int selectedTag = 0;

        public override void OnInspectorGUI()
        {
            var availableTags = GameplayTagsContainer.Instance.addedTags != null ? GameplayTagsContainer.Instance.addedTags.Select(g => g.value).ToArray() : new string[0];

            serializedObject.Update();
            EditorGUI.BeginDisabledGroup(true);

            EditorGUILayout.PropertyField(addedTags);
            EditorGUILayout.PropertyField(renamedTags);

            EditorGUI.EndDisabledGroup();

            EditorGUI.BeginChangeCheck();
            newTag = EditorGUILayout.TextField(new GUIContent("New tag"), newTag);

            if(EditorGUI.EndChangeCheck())
            {
                alreadyExists = !string.IsNullOrEmpty(newTag) && GameplayTagsContainer.Instance.IsValid(GameplayTagsContainer.RequestGameplayTag(newTag));
            }

            if(alreadyExists) 
            {
                EditorGUILayout.HelpBox("GameplayTag already exists", MessageType.Error);
            }

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newTag) || alreadyExists);
            {
                if (GUILayout.Button("Create New"))
                {
                    GameplayTagsContainer.Instance.AddGameplayTag(newTag);
                    Repaint();
                }
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);
            selectedTag = EditorGUILayout.Popup(selectedTag, availableTags);

            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(newTag) || alreadyExists || selectedTag < 0);
            {
                if (GUILayout.Button("Rename Tag"))
                {
                    GameplayTagsContainer.Instance.RenameGameplayTag(availableTags[selectedTag], newTag);
                    Repaint();
                }
            }
            EditorGUI.EndDisabledGroup();

            if (GUILayout.Button("Delete Tag"))
            {
                GameplayTagsContainer.Instance.DeleteGameplayTag(GameplayTagsContainer.RequestGameplayTag(availableTags[selectedTag]));
                Repaint();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
