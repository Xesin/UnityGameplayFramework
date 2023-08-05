using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Xesin.GameplayCues
{
    /// <summary>
    /// Provides support for placing assets into different <see cref="AddressableAssetGroup"/>s.
    /// </summary>
    /// <example>
    /// This example places all English assets into a local group and all other languages into a remote group which could then be downloaded after the game is released.
    /// <code source="../../DocCodeSamples.Tests/GroupResolverExample.cs"/>
    /// </example>
    [System.Serializable]
    public class GroupResolver
    {
        [SerializeField] string m_SharedGroupName = "GameplayCues";
        [SerializeField] AddressableAssetGroup m_SharedGroup;
        [SerializeField] bool m_MarkEntriesReadOnly = true;

        /// <summary>
        /// The name to use when creating a new group.
        /// </summary>
        public string SharedGroupName { get => m_SharedGroupName; set => m_SharedGroupName = value; }

        /// <summary>
        /// The group to use for shared assets. If null then a new group will be created using <see cref="SharedGroupName"/>.
        /// </summary>
        public AddressableAssetGroup SharedGroup { get => m_SharedGroup; set => m_SharedGroup = value; }

        /// <summary>
        /// Should new Entries be marked as read only? This will prevent editing them in the Addressables window.
        /// </summary>
        public bool MarkEntriesReadOnly { get => m_MarkEntriesReadOnly; set => m_MarkEntriesReadOnly = value; }

        /// <summary>
        /// Creates a new default instance of <see cref="GroupResolver"/>.
        /// </summary>
        public GroupResolver() { }

        /// <summary>
        /// Creates a new instance which will store all assets into a single group.
        /// </summary>
        /// <param name="groupName">The name to use when creating a new group.</param>
        public GroupResolver(string groupName)
        {
            m_SharedGroupName = groupName;
        }

        /// <summary>
        /// Creates a new instance which will store all assets into a single group;
        /// </summary>
        /// <param name="group">The group to use.</param>
        public GroupResolver(AddressableAssetGroup group)
        {
            m_SharedGroup = group;
            m_SharedGroupName = group.Name;
        }

        /// <summary>
        /// Add an asset to an <see cref="AddressableAssetGroup"/>.
        /// The asset will be moved into a group which either matches <see cref="SharedGroup"/> or <see cref="SharedGroupName"/>.
        /// </summary>
        /// <param name="asset">The asset to be added to a group.</param>
        /// <param name="aaSettings">The Addressables setting that will be used if a new group should be added.</param>
        /// <param name="createUndo">Should an Undo record be created?</param>
        /// <returns>The asset entry for the added asset.</returns>
        public virtual AddressableAssetEntry AddToGroup(Object asset, AddressableAssetSettings aaSettings, string address, bool createUndo)
        {
            var group = SharedGroup ?? GetGroup(asset, aaSettings, createUndo);
            var guid = GetAssetGuid(asset);
            var assetEntry = aaSettings.FindAssetEntry(guid);

            if (assetEntry == null)
            {
                if (createUndo)
                    Undo.RecordObjects(new Object[] { aaSettings, group }, "Add to group");
                assetEntry = aaSettings.CreateOrMoveEntry(guid, group, MarkEntriesReadOnly);
                assetEntry.address = address;
            }
            else
            {
                // TODO: We may want to provide an option to leave assets that are in unknown groups here. We would need to figure out a way to know what is a known group and what is not.
                if (createUndo)
                    Undo.RecordObjects(new Object[] { aaSettings, group, assetEntry.parentGroup }, "Add to group");
                aaSettings.MoveEntry(assetEntry, group, MarkEntriesReadOnly);
                assetEntry.address = address;
            }

            return assetEntry;
        }

        /// <summary>
        /// Add an asset to an <see cref="AddressableAssetGroup"/>.
        /// The asset will be moved into a group which either matches <see cref="SharedGroup"/> or <see cref="SharedGroupName"/>.
        /// </summary>
        /// <param name="asset">The asset to be added to a group.</param>
        /// <param name="aaSettings">The Addressables setting that will be used if a new group should be added.</param>
        /// <param name="createUndo">Should an Undo record be created?</param>
        /// <returns>The asset entry for the added asset.</returns>
        public virtual AddressableAssetEntry RemoveFromGroup(Object asset, AddressableAssetSettings aaSettings)
        {
            var guid = GetAssetGuid(asset);
            var assetEntry = aaSettings.FindAssetEntry(guid);

            if (assetEntry != null)
            {
                aaSettings.RemoveAssetEntry(guid);
            }
            return assetEntry;
        }

        /// <summary>
        /// Returns the name that the Shared group is expected to have.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="aaSettings"></param>
        /// <returns></returns>
        protected virtual string GetExpectedSharedGroupName(Object asset, AddressableAssetSettings aaSettings)
        {
            return SharedGroup != null ? SharedGroup.Name : SharedGroupName;
        }

        /// <summary>
        /// Returns the Addressable group for the asset.
        /// </summary>
        /// <param name="asset">The asset that is to be added to an Addressable group.</param>
        /// <param name="aaSettings">The Addressable asset settings.</param>
        /// <param name="createUndo">Should an Undo record be created if changes are made?</param>
        /// <returns></returns>
        protected virtual AddressableAssetGroup GetGroup(Object asset, AddressableAssetSettings aaSettings, bool createUndo)
        {
            var groupName = GetExpectedSharedGroupName(asset, aaSettings);
            return FindOrCreateGroup(groupName, aaSettings, createUndo);
        }

        AddressableAssetGroup FindOrCreateGroup(string name, AddressableAssetSettings aaSettings, bool createUndo) => aaSettings.FindGroup(name) ?? CreateNewGroup(name, MarkEntriesReadOnly, aaSettings, createUndo);

        static AddressableAssetGroup CreateNewGroup(string name, bool readOnly, AddressableAssetSettings aaSettings, bool createUndo)
        {
            if (createUndo)
                Undo.RecordObject(aaSettings, "Create group");
            var group = aaSettings.CreateGroup(name, false, readOnly, true, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));
            var schema = group.GetSchema<BundledAssetGroupSchema>();

            // Don't use hash as it creates very long file names that can cause issues on Windows.
            schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;

            if (createUndo)
                Undo.RegisterCreatedObjectUndo(group, "Create group");
            return group;
        }

        AddressableAssetEntry GetAssetEntry(Object asset, AddressableAssetSettings aaSettings) => aaSettings.FindAssetEntry(GetAssetGuid(asset));

        static string GetAssetGuid(Object asset)
        {
            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long _))
                return guid;

            Debug.LogError("Failed to extract the asset Guid for " + asset.name, asset);
            return null;
        }
    }
}