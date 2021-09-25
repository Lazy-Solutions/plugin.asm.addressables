using AdvancedSceneManager.Core;
using AdvancedSceneManager.Core.AsyncOperations;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace AdvancedSceneManager.Support._Addressables
{

    internal static class SceneClose
    {

        public static void Refresh(string[] addressableScenes)
        {
            SceneUnloadAction.ClearOverrides();
            foreach (var scene in addressableScenes)
                SceneUnloadAction.Override(scene, Close);
        }

        static IEnumerator Close(SceneManagerBase _sceneManager, SceneUnloadAction action)
        {

            var path = action.openScene.scene.path;
            if (SceneOpen.scenes.TryGetValue(path, out var handle))
            {

                var _async = Addressables.UnloadSceneAsync(handle);

                while (!(_async.IsDone))
                {
                    action.SetProgress(_async.PercentComplete);
                    yield return null;
                }

                action.UnsetPersistentFlag(action.openScene);
                action.Remove(action.openScene, _sceneManager);
                SceneOpen.scenes.Remove(path);

            }

        }

    }

}
