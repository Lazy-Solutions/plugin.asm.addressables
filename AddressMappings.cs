﻿#pragma warning disable IDE0062 // Make local function 'static'

using AdvancedSceneManager.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace AdvancedSceneManager.Support._Addressables
{

    [Serializable]
    public class AddressMapping
    {

        public string address;
        public string path;

        public static implicit operator AddressMapping((string path, string address) mapping) =>
            new AddressMapping() { path = mapping.path, address = mapping.address };

    }

    internal class AddressMappings : ScriptableObject
    {

        public List<AddressMapping> mappings = new List<AddressMapping>();

        public string Get(string path) =>
            GetMapping(path)?.address;

        public AddressMapping GetMapping(string path) =>
            mappings.FirstOrDefault(m => m.path == path);

        #region Singleton

        const string AssetPath = "Assets/Settings/Resources/AdvancedSceneManager/AddressMappings.asset";
        const string ResourcesPath = "AdvancedSceneManager/AddressMappings";

        internal static AddressMappings current =>
            ScriptableObjectUtility.GetSingleton<AddressMappings>(AssetPath, ResourcesPath);

        #endregion


#if UNITY_EDITOR

        public static void OnLoad()
        {

            AddressablesSupport.settings.OnModification += OnModification;

            foreach (var entry in AddressablesSupport.settings.groups.SelectMany(g => g.entries))
                if (entry.MainAsset is SceneAsset && !current.mappings.Any(m => m.path == entry.AssetPath))
                    OnModification(null, AddressableAssetSettings.ModificationEvent.EntryCreated, entry);

            foreach (var entry in current.mappings.ToArray())
                if (!AddressablesSupport.settings.groups.Any(g => g.entries.Any(e => e.AssetPath == entry.path)))
                    current.mappings.Remove(entry);

        }

        static void OnModification(AddressableAssetSettings settings, AddressableAssetSettings.ModificationEvent e1, object obj)
        {

            if (obj is List<AddressableAssetEntry> entries)
                foreach (var entry in entries)
                    Set(e1, entry);
            else if (obj is AddressableAssetEntry entry)
                Set(e1, entry);

            void Set(AddressableAssetSettings.ModificationEvent e, AddressableAssetEntry entry)
            {

                switch (e)
                {
                    case AddressableAssetSettings.ModificationEvent.EntryCreated:
                    case AddressableAssetSettings.ModificationEvent.EntryModified:
                    case AddressableAssetSettings.ModificationEvent.EntryAdded:
                    case AddressableAssetSettings.ModificationEvent.EntryMoved:

                        current.Set((entry.AssetPath, entry.address));
                        AssetDatabase.SaveAssets();
                        break;

                    case AddressableAssetSettings.ModificationEvent.EntryRemoved:

                        current.Remove(path: entry.AssetPath);
                        AssetDatabase.SaveAssets();
                        break;

                    default:
                        break;
                }

            }

        }

        public void Set(AddressMapping mapping)
        {
            Unset(mapping);
            if (!(string.IsNullOrWhiteSpace(mapping?.path) || string.IsNullOrWhiteSpace(mapping?.address)))
            {
                mappings.Add(mapping);
                EditorUtility.SetDirty(this);
            }
        }

        public void Unset(AddressMapping mapping)
        {
            if (GetMapping(mapping.path) is AddressMapping m)
                mappings.Remove(m);
            EditorUtility.SetDirty(this);
        }

        public void Remove(string path = null, string address = null)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                mappings.RemoveAll(m => m.path == path);
                EditorUtility.SetDirty(this);
            }
            else if (!string.IsNullOrWhiteSpace(name))
            {
                mappings.RemoveAll(m => m.address == address);
                EditorUtility.SetDirty(this);
            }
        }

#endif

    }

}
