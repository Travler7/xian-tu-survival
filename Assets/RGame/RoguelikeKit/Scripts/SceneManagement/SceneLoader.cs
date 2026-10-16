#region

using System.Collections;
using RGame.Framework;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

#endregion

namespace RGame.RoguelikeKit
{
    /// <summary>
    /// Manages scene loading and unloading operations in the game.
    /// Handles transitions between gameplay and menu scenes with loading screens and fade effects.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        [Header("Scene References")] 
        [SerializeField] private GameSceneSO gameplaySceneSO;

        [Header("Input System")] 
        [SerializeField] private InputReader inputReader;

        [Header("Event Channels - Listen")] 
        [SerializeField] private LoadEventChannelSO loadLevelChannel;
        [SerializeField] private LoadEventChannelSO loadMenuChannel;

        [Header("Event Channels - Broadcast")] 
        [SerializeField] private BoolEventChannelSO toggleLoadingScreenChannel;
        [SerializeField] private FadeChannelSO fadeRequestChannel;
        [SerializeField] private VoidEventChannelSO sceneReadyChannel;

        private const float FadeDuration = 1.5f;

        /// <summary>
        /// Scene name of the gameplay manager scene loaded through <see cref="gameplaySceneSO"/>.
        /// </summary>
        private const string GameplayManagerSceneName = "GamePlay";

        private bool isLoading;
        private GameSceneSO currentLoadedScene;
        private GameSceneSO pendingSceneToLoad;
        
        private AsyncOperationHandle<SceneInstance> gameplayManagerLoadingHandle;
        private SceneInstance gameplayManagerSceneInstance;
        private AsyncOperationHandle<SceneInstance> currentSceneLoadingHandle;

        private void OnEnable()
        {
            loadLevelChannel.OnLoadingRequested += HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested += HandleMenuLoadRequest;
        }

        private void OnDisable()
        {
            loadLevelChannel.OnLoadingRequested -= HandleLevelLoadRequest;
            loadMenuChannel.OnLoadingRequested -= HandleMenuLoadRequest;
        }

        private void HandleLevelLoadRequest(GameSceneSO sceneToLoad, bool showLoadingScreen, bool fadeScreen)
        {
            if (isLoading) return;

            pendingSceneToLoad = sceneToLoad;
            isLoading = true;

            DiscardStrayGameplayManager();

            if (!IsGameplayManagerLoaded())
            {
                LoadGameplayManager();
            }
            else
            {
                StartCoroutine(UnloadCurrentSceneRoutine());
            }
        }

        private void HandleMenuLoadRequest(GameSceneSO menuToLoad, bool showLoadingScreen, bool fadeScreen)
        {
            if (isLoading) return;

            pendingSceneToLoad = menuToLoad;
            isLoading = true;

            if (IsGameplayManagerLoaded())
            {
                UnloadGameplayManager();
            }

            StartCoroutine(UnloadCurrentSceneRoutine());
        }

        /// <summary>
        /// Play mode can start with the gameplay manager scene already open: it is the scene that
        /// stays open while iterating on the HUD, and the editor restores the open scenes between
        /// sessions. That copy is already fully initialised - every manager and every UI panel has
        /// subscribed to its channels - so loading ours on top of it produces two identical HUD
        /// stacks at the same screen position. A popup then only closes the copy that received the
        /// click, while the other copy keeps rendering it, which looks exactly like a popup that
        /// refuses to close. Drop the stray copy so only one gameplay manager scene is ever live.
        /// </summary>
        private void DiscardStrayGameplayManager()
        {
            var stray = SceneManager.GetSceneByName(GameplayManagerSceneName);
            if (!stray.IsValid() || !stray.isLoaded) return;

            // Never touch the scene this loader owns; it is recycled through Addressables.
            if (gameplayManagerSceneInstance.Scene == stray) return;

            Debug.LogWarning(
                $"[SceneLoader] '{GameplayManagerSceneName}' was already open when play mode started. " +
                "Unloading that extra copy to avoid duplicated managers and HUD panels.");

            SceneManager.UnloadSceneAsync(stray);
        }

        private bool IsGameplayManagerLoaded()
        {
            return gameplayManagerSceneInstance.Scene != null && gameplayManagerSceneInstance.Scene.isLoaded;
        }

        private void LoadGameplayManager()
        {
            gameplayManagerLoadingHandle = gameplaySceneSO.sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            gameplayManagerLoadingHandle.Completed += OnGameplayManagerLoaded;
        }

        private void UnloadGameplayManager()
        {
            Addressables.UnloadSceneAsync(gameplayManagerLoadingHandle);
        }

        private void OnGameplayManagerLoaded(AsyncOperationHandle<SceneInstance> handle)
        {
            gameplayManagerSceneInstance = gameplayManagerLoadingHandle.Result;
            StartCoroutine(UnloadCurrentSceneRoutine());
        }

        private IEnumerator UnloadCurrentSceneRoutine()
        {
            inputReader.DisableAllInput();
            fadeRequestChannel.FadeOut(FadeDuration);

            yield return new WaitForSeconds(FadeDuration);

            toggleLoadingScreenChannel.RaiseEvent(true);

            if (currentLoadedScene != null && currentLoadedScene.sceneReference.OperationHandle.IsValid())
            {
                currentLoadedScene.sceneReference.UnLoadScene();
            }

            LoadPendingScene();
        }

        private void LoadPendingScene()
        {
            currentSceneLoadingHandle = pendingSceneToLoad.sceneReference.LoadSceneAsync(
                LoadSceneMode.Additive, 
                true, 
                0);
                
            currentSceneLoadingHandle.Completed += OnSceneLoaded;
        }

        private void OnSceneLoaded(AsyncOperationHandle<SceneInstance> handle)
        {
            currentLoadedScene = pendingSceneToLoad;
            SceneManager.SetActiveScene(handle.Result.Scene);
            LightProbes.TetrahedralizeAsync();

            isLoading = false;
            toggleLoadingScreenChannel.RaiseEvent(false);
            fadeRequestChannel.FadeIn(FadeDuration);
            sceneReadyChannel.RaiseEvent();
        }
    }
}
