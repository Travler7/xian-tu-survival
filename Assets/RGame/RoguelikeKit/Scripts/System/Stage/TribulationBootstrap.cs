using UnityEngine;
using UnityEngine.SceneManagement;

namespace RGame.RoguelikeKit
{
    internal static class TribulationBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var stage = Object.FindFirstObjectByType<StageManager>();
            if (stage == null || stage.GetComponent<TribulationManager>() != null) return;
            stage.gameObject.AddComponent<TribulationManager>();
        }
    }
}
