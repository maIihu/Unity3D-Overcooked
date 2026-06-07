using System;
using UnityEngine.SceneManagement;

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
                        Network.FusionNetworkRunner.Instance.Runner.LoadScene(Fusion.SceneRef.FromIndex((int)targetScene));
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
                var loadingUI = UIManager.Instance.GetScreen<_Game.Scripts.UI.LoadingScreenUI>();
                if (loadingUI != null)
                {
                    UIManager.Instance.ShowScreen<_Game.Scripts.UI.LoadingScreenUI>();
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
