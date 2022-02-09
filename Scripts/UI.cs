#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using AdvancedSceneManager.Editor;
using AdvancedSceneManager.Models;
using AdvancedSceneManager.Utility;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static AdvancedSceneManager.Plugin._Addressables.AddressablesSupport;
using Scene = AdvancedSceneManager.Models.Scene;

namespace AdvancedSceneManager.Plugin._Addressables
{

    internal static class UI
    {

        public static bool showButtons
        {
            get => EditorPrefs.GetBool("AdvancedSceneManager.Addressables.ShowButtons", true);
            set => EditorPrefs.SetBool("AdvancedSceneManager.Addressables.ShowButtons", value);
        }

        internal static void OnLoad()
        {
            SceneManagerWindow.OnGUIEvent -= OnGUI;
            SceneManagerWindow.OnGUIEvent += OnGUI;
            ScenesTab.AddExtraButton(GetCollectionAddressablesButton);
            ScenesTab.AddExtraButton(GetSceneAddressablesButton);
            SettingsTab.Settings.Add(() =>
                new Toggle("Display addressable buttons:").
                Setup(
                    valueChanged: e => showButtons = e.newValue,
                    defaultValue: showButtons,
                    tooltip: "Enables or disables addressable buttons in scenes tab (does not disable functionality, saved in EditorPrefs)"),
                    header: SettingsTab.Settings.DefaultHeaders.Appearance);
        }

        static Vector2 mousePos;
        static void OnGUI() =>
            mousePos = Event.current.mousePosition;

        static bool IsEnabled(string path)
        {
            var group = settings ? settings.groups?.FirstOrDefault(g => g.entries.Any(e => e.AssetPath == path)) : null;
            var entry = group ? group.entries?.FirstOrDefault(e => e.AssetPath == path) : null;
            return entry != null;
        }

        static bool IsEnabled(string[] paths)
        {
            if (!paths.Any())
                return false;
            var entries = settings.groups.SelectMany(g => g.entries?.Where(e => paths.Contains(e.AssetPath)));
            return paths.All(path => entries.Any(e => e.AssetPath == path));
        }

        static AddressableAssetGroup GetGroup()
        {
            var name = Profile.current ? Profile.current.name : "ASM";
            var g = settings.FindGroup(name);
            return g ? g : settings.CreateGroup(name, setAsDefaultGroup: false, readOnly: false, postEvent: false, schemasToCopy: null);
        }

        static VisualElement GetCollectionAddressablesButton(SceneCollection collection)
        {

            if (!showButtons || !collection || collection.scenes == null)
                return null;

            var paths = collection.scenes.Where(s => s).Select(s => s.path).ToArray();
            var button = Button(collection, "Addressable", 82, IsEnabled(paths));

            button.RegisterValueChangedCallback(value =>
            {

                if (value.newValue)
                {

                    var pathsToAdd = paths.Where(p => !IsEnabled(p));
                    var group = GetGroup();

                    foreach (var path in pathsToAdd)
                        settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(path), group, postEvent: false);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, null, postEvent: true, settingsModified: true);

                }
                else
                {
                    foreach (var group in settings.groups)
                        foreach (var entry in group.entries.ToArray())
                            if (paths.Contains(entry.AssetPath))
                                group.RemoveAssetEntry(entry, postEvent: false);

                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, null, postEvent: true, settingsModified: true);

                }

                RefreshButtons();

            });

            return button;

        }

        static VisualElement GetSceneAddressablesButton(Scene scene)
        {

            if (!showButtons || !scene)
                return null;

            var button = Button(scene, "Addr.", 56, IsEnabled(scene.path));

            button.RegisterValueChangedCallback(value =>
            {

                if (value.newValue)
                {

                    var group = GetGroup();
                    var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(scene.path), group, postEvent: false);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryCreated, entry, postEvent: true, settingsModified: true);

                }
                else
                {

                    var group = settings.groups.FirstOrDefault(g => g.entries.Any(e => e.AssetPath == scene.path));
                    if (group)
                    {
                        var entry = group.entries?.FirstOrDefault(e => e?.AssetPath == scene.path);
                        group.RemoveAssetEntry(entry, postEvent: false);
                        settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entry, postEvent: true, settingsModified: true);
                    }

                }

                RefreshButtons();

            });

            return button;

        }

        static readonly Color hoverBackground = new Color(0, 0, 0, 0.3f);

        static Color checkedColor =>
            SceneManagerWindow.IsDarkMode
            ? darkCheckedColor
            : lightCheckedColor;

        static Color uncheckedColor =>
            SceneManagerWindow.IsDarkMode
            ? darkUncheckedColor
            : lightUncheckedColor;

        static readonly Color darkCheckedColor = new Color32(85, 246, 98, 255);
        static readonly Color darkUncheckedColor = Color.white;

        static readonly Color lightCheckedColor = new Color32(0, 150, 8, 255);
        static readonly Color lightUncheckedColor = Color.black;

        static readonly Dictionary<ISceneObject, ToolbarToggle> buttons = new Dictionary<ISceneObject, ToolbarToggle>();
        static ToolbarToggle Button(ISceneObject obj, string text, float width, bool value)
        {

            var button = new ToolbarToggle();
            button.style.alignSelf = Align.Center;
            button.style.marginLeft = 2;
            button.style.SetBorderWidth(0);
            button.style.width = width;
            button.text = text;

            button.AddToClassList("StandardButton");
            button.AddToClassList("no-checkedBackground");
            button.style.backgroundColor = Color.clear;
            button.SetValueWithoutNotify(value);

            RefreshButton(button, value);
            button.RegisterValueChangedCallback(e => RefreshButton(button, e.newValue));

            button.RegisterCallback<MouseEnterEvent>(e => { button.style.backgroundColor = hoverBackground; });
            button.RegisterCallback<MouseLeaveEvent>(e => { button.style.backgroundColor = Color.clear; });

            button.RegisterCallback<GeometryChangedEvent>(e =>
            {

                var pos = mousePos;
                if (button.worldBound.Contains(pos))
                    button.style.backgroundColor = new Color(0, 0, 0, 0.3f);

            });

            buttons.Set(obj, button);

            return button;

        }

        static void RefreshButtons()
        {
            foreach (var button in buttons)
            {
                if (button.Key is SceneCollection collection)
                    RefreshButton(collection);
                else if (button.Key is Scene scene)
                    RefreshButton(scene);
            }
        }

        static void RefreshButton(SceneCollection collection) =>
            RefreshButton(buttons.GetValue(collection), IsEnabled(collection.scenes.Where(s => s).Select(s => s.path).ToArray()));

        static void RefreshButton(Scene scene) =>
            RefreshButton(buttons.GetValue(scene), scene && IsEnabled(scene.path));

        static void RefreshButton(ToolbarToggle button, bool value)
        {

            button.style.opacity = value ? 1 : 0.4f;

            button.Q<Label>().style.color = value ? checkedColor : uncheckedColor;
            button.SetValueWithoutNotify(value);
            button.tooltip = value ? "Remove from addressables" : "Add to addressables";

        }

    }

}
#endif
