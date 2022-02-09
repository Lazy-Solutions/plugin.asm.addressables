#pragma warning disable IDE0062 // Make local function 'static'

using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine.Scripting;
#endif

[assembly: Preserve]
[assembly: AlwaysLinkAssembly]
namespace AdvancedSceneManager.Plugin._Addressables
{

    internal static class AddressablesSupport
    {

        [RuntimeInitializeOnLoadMethod]
        static void _OnLoad()
        {
            var addressableScenes = AddressMappings.current.mappings.Select(m => m.path).ToArray();
            SceneOpen.Refresh(addressableScenes);
            SceneClose.Refresh(addressableScenes);
        }

#if UNITY_EDITOR

        internal static AddressableAssetSettings settings { get; private set; }

        [InitializeOnLoadMethod]
        static void OnLoad()
        {

            EditorApplication.projectChanged -= OnLoad;
            EditorApplication.projectChanged += OnLoad;

            settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            if (settings != null)
            {

                BuildSettings.OnLoad();
                AddressMappings.OnLoad();
                UI.OnLoad();

                settings.OnModification -= OnModification;
                settings.OnModification += OnModification;

                void OnModification(AddressableAssetSettings s, AddressableAssetSettings.ModificationEvent e, object o) =>
                    Refresh(true);

                Refresh(!EditorApplication.isPlaying);

            }

        }

        internal static void Refresh(bool notifyBuildSettings = false)
        {

            var addressableScenes = settings.
                groups.
                SelectMany(g => g.entries).
                Where(e => e.MainAsset is SceneAsset).
                Select(s => s.AssetPath).
                ToArray();

            BuildSettings.Refresh(addressableScenes, notifyBuildSettings);
            SceneOpen.Refresh(addressableScenes);

        }

#endif

    }

}
