using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Xesin.GameplayCues
{
    [CustomPropertyDrawer(typeof(GameplayTagList), true)]
    public class GameplayCueTagListDrawer : PropertyDrawer
    {
        public SerializedProperty tagListProperty;

        public GameplayTagList objectValue;
        public string newTagPropertyPath;

        private SerializedProperty currentProperty = null;

        private bool NeedsReload(SerializedProperty serializedProperty)
        {
            return tagListProperty == null || currentProperty == null || serializedProperty != currentProperty;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (NeedsReload(property))
            {
                RefreshTagListProperty(property);
            }

            return base.GetPropertyHeight(property, label) + EditorGUIUtility.singleLineHeight * objectValue.Tags.Count + 6;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (NeedsReload(property))
            {
                RefreshTagListProperty(property);
            }

            label.text = property.displayName;
            EditorGUI.BeginProperty(position, label, property);

            Rect assetDropDownRect = EditorGUI.PrefixLabel(position, label);
            Rect assetListBoxRect = new Rect(new Vector2(position.x, position.y + EditorGUIUtility.singleLineHeight + 3), new Vector2(position.width, position.height - EditorGUIUtility.singleLineHeight - 3)); ;
            Rect assetDropDownButtonRect = new Rect(assetDropDownRect.position, new Vector2(assetDropDownRect.width, EditorGUIUtility.singleLineHeight));

            if (EditorGUI.DropdownButton(assetDropDownButtonRect, new GUIContent(""), FocusType.Keyboard, EditorStyles.objectField))
            {
                newTagPropertyPath = property.propertyPath;
                PopupWindow.Show(assetDropDownButtonRect, new GameplayTagListSelectorPopup(this));
            }

            GUI.Box(assetListBoxRect, "");
            Rect startRect = new Rect(assetListBoxRect.position, new Vector2(assetListBoxRect.width, EditorGUIUtility.singleLineHeight));
            startRect.x += 10;
            startRect.width -= 10;
            for (int i = 0; i < objectValue.Tags.Count; i++)
            {
                EditorGUI.LabelField(startRect, objectValue.Tags[i].value);
                startRect.y += EditorGUIUtility.singleLineHeight;
            }

            EditorGUI.EndProperty();
        }

        internal void AddTag(string value)
        {
            tagListProperty.InsertArrayElementAtIndex(tagListProperty.arraySize);

            var newProperty = tagListProperty.GetArrayElementAtIndex(tagListProperty.arraySize - 1);
            var valueProperty = newProperty.FindPropertyRelative("value");
            var parentProperty = newProperty.FindPropertyRelative("parentTag");

            var tagValue = GameplayTagsContainer.RequestGameplayTag(value);

            valueProperty.stringValue = tagValue.value;
            parentProperty.stringValue = tagValue.parentTag;

            TriggerOnValidate(currentProperty);
            TriggerOnValidate(tagListProperty);
            TriggerOnValidate(newProperty);
            RefreshTagListProperty(currentProperty);
        }

        internal void RemoveTag(string value)
        {
            for (int i = 0; i < tagListProperty.arraySize; i++)
            {
                var arrayElement = tagListProperty.GetArrayElementAtIndex(i);
                var tagValue = arrayElement.FindPropertyRelative("value").stringValue;
                if (value == tagValue)
                {
                    tagListProperty.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            TriggerOnValidate(currentProperty);
            TriggerOnValidate(tagListProperty);
            RefreshTagListProperty(currentProperty);
        }

        private void RefreshTagListProperty(SerializedProperty property)
        {           
            objectValue = (GameplayTagList)property.boxedValue;
            tagListProperty = property.FindPropertyRelative("gameplayTags");
            currentProperty = property;
        }

        void TriggerOnValidate(SerializedProperty property)
        {
            if (property != null)
            {
                property.serializedObject.ApplyModifiedProperties();
                property.serializedObject.Update();

                // This is actually what triggers the OnValidate() method.
                // Since 'm_EditorAssetChanged' is of a recognized type and is a sub-property of AssetReference, both
                // are flagged as changed and OnValidate() is called.
                //property.FindPropertyRelative("m_EditorAssetChanged").boolValue = false;
            }
        }
    }

    class GameplayTagListSelectorPopup : PopupWindowContent
    {
        GameplayTagListTreeView m_Tree;
        TreeViewState m_TreeState;
        GameplayCueTagListDrawer m_Drawer = null;

        bool m_ShouldClose;

        void ForceClose()
        {
            m_ShouldClose = true;
        }

        string m_CurrentName = string.Empty;

        SearchField m_SearchField;

        public GameplayTagListSelectorPopup(GameplayCueTagListDrawer drawer)
        {
            m_Drawer = drawer;
            m_SearchField = new SearchField();
            m_ShouldClose = false;
        }

        public override void OnOpen()
        {
            m_SearchField.SetFocus();
            base.OnOpen();
        }

        public override void OnGUI(Rect rect)
        {
            int border = 4;
            int topPadding = 12;
            int searchHeight = 20;
            var searchRect = new Rect(border, topPadding, rect.width - border * 2, searchHeight);
            var remainTop = topPadding + searchHeight + border;
            var remainingRect = new Rect(border, topPadding + searchHeight + border, rect.width - border * 2, rect.height - remainTop - border);

            m_CurrentName = m_SearchField.OnGUI(searchRect, m_CurrentName);

            if (m_Tree == null)
            {
                if (m_TreeState == null)
                    m_TreeState = new TreeViewState();
                m_Tree = new GameplayTagListTreeView(m_TreeState, m_Drawer);
                m_Tree.Reload();
            }

            m_Tree.searchString = m_CurrentName;
            m_Tree.OnGUI(remainingRect);

            if (m_ShouldClose)
            {
                GUIUtility.hotControl = 0;
                editorWindow.Close();
            }
        }


        internal class GameplayTagListTreeView : TreeView
        {
            GameplayCueTagListDrawer m_Drawer;
            public GameplayTagListTreeView(TreeViewState state, GameplayCueTagListDrawer drawer)
                : base(state)
            {
                m_Drawer = drawer;
                showBorder = true;
                showAlternatingRowBackgrounds = true;
            }

            protected override bool CanMultiSelect(TreeViewItem item)
            {
                return false;
            }

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                if (string.IsNullOrEmpty(searchString))
                {
                    return base.BuildRows(root);
                }

                List<TreeViewItem> rows = new List<TreeViewItem>();

                foreach (var child in rootItem.children)
                {
                    RecursiveAdd(rows, child);
                }

                return rows;
            }

            private void AddAllChildren(List<TreeViewItem> treeViews, TreeViewItem root)
            {
                if (root.children == null) return;

                for (int i = 0; i < root.children.Count; i++)
                {
                    if (treeViews.Contains(root.children[i])) continue;

                    treeViews.Add(root.children[i]);
                    AddAllChildren(treeViews, root.children[i]);
                }
            }

            private void RecursiveAdd(List<TreeViewItem> treeViews, TreeViewItem viewItem)
            {
                if (viewItem.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!treeViews.Contains(viewItem))
                        treeViews.Add(viewItem);

                    AddAllChildren(treeViews, viewItem);
                }

                if (viewItem.children == null) return;

                for (int i = 0; i < viewItem.children.Count; i++)
                {
                    if (RecursiveSearch(treeViews, viewItem.children[i]))
                    {
                        if (treeViews.Contains(viewItem)) continue;
                        treeViews.Add(viewItem);
                    }
                }

                for (int i = 0; i < viewItem.children.Count; i++)
                {
                    RecursiveAdd(treeViews, viewItem.children[i]);
                }
            }

            private bool RecursiveSearch(List<TreeViewItem> treeViews, TreeViewItem viewItem)
            {
                if (viewItem.displayName.IndexOf(searchString, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }

                if (viewItem.children == null) return false;

                for (int i = 0; i < viewItem.children.Count; i++)
                {
                    if (RecursiveSearch(treeViews, viewItem.children[i]))
                    {
                        return true;
                    }
                }

                return false;
            }

            public override void OnGUI(Rect rect)
            {
                base.OnGUI(rect);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                Rect rowrect = args.rowRect;
                rowrect.x += (15 * (1 + args.item.depth));

                GameplayTagTreeViewItem item = args.item as GameplayTagTreeViewItem;

                EditorGUI.BeginChangeCheck();
                bool isChecked = m_Drawer.objectValue.Contains(item.node.ToGameplayTag());
                isChecked = EditorGUI.ToggleLeft(rowrect, args.item.displayName, isChecked);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isChecked)
                    {
                        m_Drawer.AddTag(item.node.ToGameplayTagString());
                    }
                    else if (item.node.parent != null)
                    {
                        m_Drawer.RemoveTag(item.node.ToGameplayTagString());
                    }
                    else
                    {
                        m_Drawer.RemoveTag(item.node.ToGameplayTagString());
                    }
                }
            }

            protected override bool CanChangeExpandedState(TreeViewItem item)
            {
                return item.hasChildren && string.IsNullOrEmpty(searchString);
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem(-1, -1);
                var tags = GameplayTagsContainer.Instance.NodeTree;

                void BuildItemTree(GameplayTagTreeViewItem parentNode, int depth)
                {
                    foreach (var entry in parentNode.node.children)
                    {
                        var child = new GameplayTagTreeViewItem(entry.ToGameplayTagString().GetHashCode(), depth, entry);
                        parentNode.AddChild(child);
                        BuildItemTree(child, depth + 1);
                    }
                }

                foreach (var entry in tags)
                {
                    var child = new GameplayTagTreeViewItem(entry.ToGameplayTagString().GetHashCode(), 0, entry);
                    root.AddChild(child);
                    BuildItemTree(child, 1);
                }


                return root;
            }
        }

        sealed class GameplayTagTreeViewItem : TreeViewItem
        {
            public Node node;

            public GameplayTagTreeViewItem(int id, int depth, Node tagValue)
                : base(id, depth, tagValue.id)
            {
                node = tagValue;
            }
        }
    }

}
