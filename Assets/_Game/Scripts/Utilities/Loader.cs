using _Game.Scripts.Utilities;

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

        public static async void Load(Scene targetScene)
        {
            if (Network.FusionNetworkRunner.Instance != null && Network.FusionNetworkRunner.Instance.Runner != null)
            {
                // Active multiplayer session, should leave session and then load main menu
                if (targetScene == Scene.MainMenuScene)
                {
                    await Network.FusionNetworkRunner.Instance.LeaveSession();
                    UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene.ToString());
                }
                else
                {
                    // For host to switch scene, use Runner
                    if (Network.FusionNetworkRunner.Instance.Runner.IsServer)
                    {
                        Network.FusionNetworkRunner.Instance.Runner.LoadScene(Fusion.SceneRef.FromIndex((int)targetScene));
                    }
                }
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene.ToString());
            }
        }
    }
}
