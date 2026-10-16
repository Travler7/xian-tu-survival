using System;
using System.Collections.Generic;
using System.Reflection;
using RGame.CommonStat;
using RGame.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RGame.RoguelikeKit
{
    public class PowerUpAttributeUpgrade : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField] private CommonStatRuntimeSO mStatSO;
        [SerializeField] private string mAttributeName;
        [SerializeField] private string mDescription;
        
        [Space(10)] [Header("SaveInfo")]
        [SerializeField] private VoidEventChannelSO _saveGameChannel;
        [SerializeField] private VoidEventChannelSO mUpdateGoldUI;
        [SerializeField] private AttributeSO mAttributeSo;
        
        [Space(10)] [Header("LevelInfo")]
        [SerializeField] private int mMaxLevel;
        [SerializeField] private List<int> mRequireGold;
        [SerializeField] private List<int> mAttributeChangeValue;
        
        [Space(10)] [Header("LevelUIInfo")]
        [SerializeField] private Transform mLevelParent;
        [SerializeField] private ChildObjectController mLevelInfo;

        [Space(10)] [Header("ShowTip")] [SerializeField]
        private Image mBG;

        private Color mOriginColor;
        private List<ChildObjectController> mLevelInfos = new List<ChildObjectController>();
        private int mCurLevel;

        public UnityAction<string,string,int,UnityAction> SelectAction;
        
        private void Awake()
        {
            InitLevel();
            mOriginColor = mBG.color;
            ConfigureCardText();
        }

        private void Start()
        {
            var tempLevel = mAttributeSo.GetAttribute(mAttributeName);
            for (int i = 0; i < tempLevel; i++)
            {
                UpgradeInit();
            }
        }

        private void InitLevel()
        {
            foreach (Transform child in mLevelParent.transform)
            {
                Destroy(child.gameObject);
            }

            for (int i = 0; i < mMaxLevel; i++)
            {
                var levelInfo = Instantiate(mLevelInfo, mLevelParent);

                levelInfo.ShowAction += () => { levelInfo.BG.color = Color.black; };
                
                levelInfo.HideChildren();
                
                mLevelInfos.Add(levelInfo);
            }
        }
        
        private void UpgradeInit()
        {
            mStatSO.ModifyValue(mAttributeName, mAttributeChangeValue[mCurLevel]);
            mLevelInfos[mCurLevel].ShowChildren();
            
            mCurLevel++;
        }
        
        private void Upgrade()
        {
            if (mCurLevel == mMaxLevel) return;

            if (mAttributeSo.GoldCount < mRequireGold[mCurLevel]) return;
            
            mStatSO.ModifyValue(mAttributeName, mAttributeChangeValue[mCurLevel]);
            mLevelInfos[mCurLevel].ShowChildren();
            
            mCurLevel++;
            mAttributeSo.SetAttribute("GoldCount",mAttributeSo. GoldCount - mRequireGold[mCurLevel - 1]);
            mAttributeSo.SetAttribute(mAttributeName,mCurLevel);
            _saveGameChannel.RaiseEvent();
            mUpdateGoldUI.RaiseEvent();
            UpdateInfo();
        }
        
        public void OnPointerEnter(PointerEventData eventData)
        {
            mBG.color = new Color(0.82f, 0.66f, 0.28f, 0.96f);
        }
        
        public void OnPointerExit(PointerEventData eventData)
        {
            mBG.color = mOriginColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            UpdateInfo();
        }

        public void UpdateInfo()
        {
            if (mCurLevel == mMaxLevel) return;
            
            SelectAction?.Invoke(GetDisplayAttributeName(mAttributeName),mDescription,mRequireGold[mCurLevel],Upgrade);
        }

        private void ConfigureCardText()
        {
            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.text = GetDisplayAttributeName(mAttributeName);
                text.font = text.font != null ? text.font : TMP_Settings.defaultFontAsset;
                text.fontSize = 30f;
                text.fontStyle = FontStyles.Bold;
                text.color = new Color32(249, 241, 221, 255);
                text.outlineWidth = 0.16f;
                text.outlineColor = new Color32(28, 24, 20, 255);
            }
        }

        private static string GetDisplayAttributeName(string attributeName)
        {
            return attributeName switch
            {
                "Might" => "术威",
                "Armor" => "护体",
                "HPMax" => "道基",
                "Recovery" => "回元",
                "Cooldown" => "调息",
                "Area" => "法域",
                "SkillSpeed" => "施术速度",
                "Duration" => "持续真元",
                "Amount" => "分化",
                "MoveSpeed" => "身法",
                "Magnet" => "神识",
                "Growth" => "悟性",
                "Greed" => "聚财",
                "Curse" => "业障",
                _ => attributeName
            };
        }
    }
}
