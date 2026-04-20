using UnityEngine;
using UnityEngine.SceneManagement;

namespace Somnia.UnityClient
{
    public static class SceneAutoBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureControllersForScenes()
        {
            var sceneName = SceneManager.GetActiveScene().name;

            if (sceneName == "Loading" && Object.FindFirstObjectByType<LoadingSceneController>() == null)
            {
                var go = new GameObject("[LoadingSceneController]");
                go.AddComponent<LoadingSceneController>();
            }

            if (sceneName == "Fail" && Object.FindFirstObjectByType<FailSceneController>() == null)
            {
                var go = new GameObject("[FailSceneController]");
                go.AddComponent<FailSceneController>();
            }
        }
    }
}