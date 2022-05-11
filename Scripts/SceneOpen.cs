using AdvancedSceneManager.Core;
using AdvancedSceneManager.Core.Actions;
using AdvancedSceneManager.Utility;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace plugin.asm.addressables
{

    internal static class SceneOpen
    {

        public static void Refresh(string[] addressableScenes)
        {

            SceneLoadAction.ClearOverrides();
            foreach (var scene in addressableScenes)
                SceneLoadAction.Override(scene, Load);

            SceneFinishLoadAction.ClearOverrides();
            foreach (var scene in addressableScenes)
                SceneFinishLoadAction.Override(scene, Activate);

        }

        public static Dictionary<string, AsyncOperationHandle<SceneInstance>> scenes = new Dictionary<string, AsyncOperationHandle<SceneInstance>>();

        static IEnumerator Load(SceneManagerBase _sceneManager, SceneLoadAction action)
        {

            if (action.scene == null)
            {
                action._Done();
                yield break;
            }

            var address = AddressMappings.current.Get(action.scene.path);
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError("Could not find address for scene: " + action.scene.path);
                yield break;
            }

            var async = Addressables.LoadSceneAsync(address, loadMode: LoadSceneMode.Additive, activateOnLoad: false);

            while (!async.IsDone)
            {
                yield return null;
                action.SetProgress(async.PercentComplete);
            }

            if (async.OperationException != null)
                throw async.OperationException;

            action.openScene = new OpenSceneInfo(action.scene, async.Result.Scene, _sceneManager, async) { isPreloadedOverride = true };
            scenes.Set(action.scene.path, async);

            action.SetPersistentFlag(action.openScene);
            action.AddScene(action.openScene, _sceneManager);

        }

        static IEnumerator Activate(SceneManagerBase _sceneManager, SceneFinishLoadAction action)
        {

            if (!action.openScene?.isOpen ?? false)
            {
                action._Done();
                yield break;
            }

            if (!typeof(AsyncOperationHandle<SceneInstance>).IsAssignableFrom(action.openScene.asyncOperation?.GetType()))
            {
                action._Done();
                yield break;
            }

            var async = ((AsyncOperationHandle<SceneInstance>)action.openScene.asyncOperation).Result.ActivateAsync();

            async.allowSceneActivation = true;
            while (!async.isDone)
            {
                yield return null;
                action.SetProgress(async.progress);
            }

            action.openScene.isPreloadedOverride = null;
            action.openScene.asyncOperation = null;

        }

    }

}
