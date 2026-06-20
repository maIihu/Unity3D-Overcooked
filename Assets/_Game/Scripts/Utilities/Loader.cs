using System;
using UnityEngine.SceneManagement;
using _Game.Scripts.UI;
namespace GameCore
{
    public static class Loader
    {
        public enum Scene
        {
            MainMenuScene,
            GameScene,
            LoadingScene
        }

        public static Scene TargetScene { get; private set; }
        public static Action OnComplete { get; private set; }

        public static int GetSceneIndex(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.Contains(sceneName))
                {
                    return i;
                }
            }
            return -1;
        }

        public static async void Load(Scene targetScene, Action onComplete = null)
        {
            Loader.TargetScene = targetScene;
            Loader.OnComplete = onComplete;

            if (Network.FusionNetworkRunner.Instance != null && Network.FusionNetworkRunner.Instance.Runner != null)
            {
                // Active multiplayer session, should leave session and then load main menu
                if (targetScene == Scene.MainMenuScene)
                {
                    await Network.FusionNetworkRunner.Instance.LeaveSession();
                    TriggerLocalLoad();
                }
                else
                {
                    // For host to switch scene, use Runner
                    if (Network.FusionNetworkRunner.Instance.Runner.IsServer)
                    {
                        int sceneIndex = GetSceneIndex(targetScene.ToString());
                        await Network.FusionNetworkRunner.Instance.Runner.LoadScene(Fusion.SceneRef.FromIndex(sceneIndex));
                        OnComplete?.Invoke();
                        OnComplete = null;
                    }
                }
            }
            else
            {
                TriggerLocalLoad();
            }
        }

        private static void TriggerLocalLoad()
        {
            if (UIManager.Instance != null)
            {
                var loadingUI = UIManager.Instance.GetScreen<LoadingScreen>();
                if (loadingUI != null)
                {
                    UIManager.Instance.ShowScreen<LoadingScreen>();
                    loadingUI.TriggerLoad(TargetScene.ToString(), OnComplete);
                }
                else
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(TargetScene.ToString());
                }
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(TargetScene.ToString());
            }
        }
    }
}
