using System;
using System.Collections;
using RGame.Framework;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class GameEndPanel : MonoBehaviour
    {
        [SerializeField] private VoidEventChannelSO _openGameOverPanel;
        [SerializeField] private GameSceneSO _mainMenuScene;
        [SerializeField] private LoadEventChannelSO _sceneLoader;
        [SerializeField] private VoidEventChannelSO _clearAllBuffs;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private GlobalConfigSO _globalConfig;
        [SerializeField] private VoidEventChannelSO _saveGame;
        [SerializeField] private AttributeSO _attributeSO;
        [SerializeField] private PoolRuntimeSO poolRuntime;
        
        private void OnEnable()
        {
            _openGameOverPanel.RegisterListener(Show);
        }

        private void OnDisable()
        {
            _openGameOverPanel.UnregisterListener(Show);
        }

        private void Show()
        {
            // The run is over: drop every other pause owner so a panel left open behind the
            // result screen cannot keep the world frozen (or resume it) after this point.
            PauseCoordinator.ReleaseAll();
            PauseCoordinator.Acquire(this);

            poolRuntime.RecycleAll();
            _clearAllBuffs.RaiseEvent();
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;

            StartCoroutine(DelayInteractable());
        }
        
        public void Exit()
        {
            PauseCoordinator.Release(this);
            _attributeSO.SetAttribute("GoldCount",_attributeSO.GoldCount + _globalConfig.CurrentGetGold);
            _saveGame.RaiseEvent();
            _sceneLoader.RaiseEvent(_mainMenuScene);
        }

        private IEnumerator DelayInteractable()
        {
            // timeScale is zero here, so WaitForSeconds would never elapse; use unscaled time
            // so the guard actually delays the exit button by half a second.
            float until = Time.unscaledTime + 0.5f;
            while (Time.unscaledTime < until) yield return null;
            if (_canvasGroup == null) yield break;
            _canvasGroup.interactable = true;
        }
    }
}
