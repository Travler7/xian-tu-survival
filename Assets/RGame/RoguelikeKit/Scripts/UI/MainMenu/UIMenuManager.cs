#region

using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    public class UIMenuManager : MonoBehaviour
    {
        [SerializeField] private UIMainMenu mMainMenuPanel;
        [SerializeField] private CharacterSelectPanel mCharacterSelectPanel;
        [SerializeField] private LevelSelectPanel mLevelSelectPanel;
        [SerializeField] private SettingPanel mSettingPanel;
        [SerializeField] private PowerUpPanel mPowerUpPanel;
        [SerializeField] private GameSceneSO mGameScene;
        [SerializeField] private LoadEventChannelSO mGameLoad;

        [SerializeField] private InputReader mInputReader;

        private void Start()
        {
            mInputReader.EnableMenuInput();
            CloseAllMenuPanels();
            mMainMenuPanel.gameObject.SetActive(true);
            SetMenuScreen();
        }

        private void OnDestroy()
        {
            mMainMenuPanel.SettingsButtonAction -= OpenSettingsScreen;
            mMainMenuPanel.NewGameButtonAction -= OpenCharacterSelectScreen;
            mMainMenuPanel.PowerUpButtonAction -= OpenPowerUpScreen;
        }

        private void SetMenuScreen()
        {
            mMainMenuPanel.SettingsButtonAction += OpenSettingsScreen;
            mMainMenuPanel.NewGameButtonAction += OpenCharacterSelectScreen;
            mMainMenuPanel.PowerUpButtonAction += OpenPowerUpScreen;
        }

        #region ChracterSelect

        private void OpenCharacterSelectScreen()
        {
            CloseAllMenuPanels();
            mCharacterSelectPanel.gameObject.SetActive(true);
            mCharacterSelectPanel.Confirm += OpenLevelSelectScreen;
            mCharacterSelectPanel.Closed += CloseCharacterSelectScreen;
        }

        private void CloseCharacterSelectScreen()
        {
            mCharacterSelectPanel.Confirm -= OpenLevelSelectScreen;
            mCharacterSelectPanel.Closed -= CloseCharacterSelectScreen;
            mCharacterSelectPanel.gameObject.SetActive(false);
            mMainMenuPanel.gameObject.SetActive(true);
        }

        #endregion

        #region LevelSelect

        private void OpenLevelSelectScreen()
        {
            CloseAllMenuPanels();
            mLevelSelectPanel.gameObject.SetActive(true);
            mLevelSelectPanel.Closed += CloseLevelSelectScreen;
            mLevelSelectPanel.Confirm += LoadGame;
        }

        private void CloseLevelSelectScreen()
        {
            mLevelSelectPanel.Closed -= CloseLevelSelectScreen;
            mLevelSelectPanel.Confirm -= LoadGame;
            mLevelSelectPanel.gameObject.SetActive(false);
            mMainMenuPanel.gameObject.SetActive(true);
        }

        private void LoadGame()
        {
            CloseAllMenuPanels();
            mGameLoad.RaiseEvent(mGameScene);
        }

        #endregion

        #region Setting

        private void OpenSettingsScreen()
        {
            CloseAllMenuPanels();
            mSettingPanel.gameObject.SetActive(true);
            mSettingPanel.Closed += CloseSettingsScreen;
        }

        private void CloseSettingsScreen()
        {
            mSettingPanel.Closed -= CloseSettingsScreen;
            mSettingPanel.gameObject.SetActive(false);
            mMainMenuPanel.gameObject.SetActive(true);
        }

        #endregion
        
        #region PowerUp

        private void OpenPowerUpScreen()
        {
            CloseAllMenuPanels();
            mPowerUpPanel.gameObject.SetActive(true);
            mPowerUpPanel.Closed += ClosePowerUpScreen;
        }

        private void ClosePowerUpScreen()
        {
            mPowerUpPanel.Closed -= ClosePowerUpScreen;
            mPowerUpPanel.gameObject.SetActive(false);
            mMainMenuPanel.gameObject.SetActive(true);
        }

        private void CloseAllMenuPanels()
        {
            mCharacterSelectPanel.Confirm -= OpenLevelSelectScreen;
            mCharacterSelectPanel.Closed -= CloseCharacterSelectScreen;
            mLevelSelectPanel.Closed -= CloseLevelSelectScreen;
            mLevelSelectPanel.Confirm -= LoadGame;
            mSettingPanel.Closed -= CloseSettingsScreen;
            mPowerUpPanel.Closed -= ClosePowerUpScreen;

            mMainMenuPanel.gameObject.SetActive(false);
            mCharacterSelectPanel.gameObject.SetActive(false);
            mLevelSelectPanel.gameObject.SetActive(false);
            mSettingPanel.gameObject.SetActive(false);
            mPowerUpPanel.gameObject.SetActive(false);
        }

        #endregion
    }
}
