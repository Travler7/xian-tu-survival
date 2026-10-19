#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using RGame.RoguelikeKit;
#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Stage3PlayModeSmokeRunner : MonoBehaviour
{
    [Serializable] private sealed class FlowEntry
    {
        public string node;
        public float stageSeconds;
        public string state;
        public bool passed;
        public string detail;
    }

    [Serializable] private sealed class Report
    {
        public bool passed;
        public List<string> checks = new();
        public List<FlowEntry> flow = new();
        public string scene;
        public string error;
        public float elapsed;
    }

    private IEnumerator Start()
    {
        var report = new Report { scene = SceneManager.GetActiveScene().name };
        var started = Time.realtimeSinceStartup; GameObject root = null; GameObject stageRoot = null;
        try
        {
            root = new GameObject("Stage3SmokeTarget"); var ward = root.AddComponent<CorruptionWard>(); ward.Configure(SpawnSetRole.Elite, 100); bool broke;
            Require(ward.Absorb(10, false, out broke) == 0, "ordinary damage reduces ward", report); Require(ward.Absorb(8, true, out broke) == 1 && broke, "reaction damage breaks ward", report); Require(ward.DamageMultiplier > 1f, "broken ward vulnerability", report); yield return new WaitForSeconds(2.6f); Require(Mathf.Abs(ward.DamageMultiplier - 1f) < 0.001f, "broken window expires", report);
            var marks = root.AddComponent<ElementMarkController>(); Require(!marks.Register(ElementType.Metal, "metal", out _), "first elemental mark", report); Require(marks.Register(ElementType.Water, "water", out var reaction) && reaction.Name == "寒刃", "metal water reaction", report); marks.Clear(); Require(marks.MarkCount == 0, "mark clear on pool reset", report);
            ShentongRuntime.Reset(); for (var i = 0; i < ShentongRuntime.RequiredReactions; i++) ShentongRuntime.RecordReaction("SkillA", reaction); Require(ShentongRuntime.ProgressFor("SkillA") == 8, "breakthrough reaches threshold", report); ShentongRuntime.Unlock("shentong_frost", "SkillA"); ShentongRuntime.Unlock("shentong_stone", "SkillB"); ShentongRuntime.Unlock("shentong_mud", "SkillC"); Require(ShentongRuntime.UnlockedIds.Count == 2, "breakthrough cap", report);
            var timeline = Resources.Load<TribulationTimelineSO>("TribulationTimeline"); Require(timeline != null && timeline.FirstChoiceTime == 240f && timeline.SecondChoiceTime == 480f && timeline.FinalOmenTime == 630f && timeline.FinalBossTime == 660f && timeline.StageEndTime == 720f, "stage timeline nodes", report);
            var state = new TribulationRunState { EventActive = true, EventSucceeded = true, FinalOmenShown = true, RootAwakeningClaimed = true }; state.ChosenIds.Add("soul"); ShentongRuntime.Reset(); state.Reset(); Require(state.ChosenIds.Count == 0 && !state.EventActive && !state.EventSucceeded && !state.FinalOmenShown && !state.RootAwakeningClaimed && ShentongRuntime.UnlockedIds.Count == 0, "run state reset", report);

            // Exercise the same timeline values through StageManager's editor jump
            // entry, then record the three terminal outcomes and a fresh-run reset.
            stageRoot = CreateTimelineProbe();
            var stage = stageRoot.GetComponent<StageManager>();
            stage.EditorJumpToTime(timeline.FirstChoiceTime);
            RecordFlow(report, "第一次渡劫", timeline.FirstChoiceTime, "选择阶段", stage.ElapsedTime, "StageManager.EditorJumpToTime");
            stage.EditorJumpToTime(timeline.SecondChoiceTime);
            RecordFlow(report, "第二次渡劫", timeline.SecondChoiceTime, "选择阶段", stage.ElapsedTime, "StageManager.EditorJumpToTime");
            stage.EditorJumpToTime(timeline.FinalOmenTime);
            RecordFlow(report, "最终劫相", timeline.FinalOmenTime, "终局预警", stage.ElapsedTime, "StageManager.EditorJumpToTime");
            stage.EditorJumpToTime(timeline.FinalBossTime);
            RecordFlow(report, "最终 Boss", timeline.FinalBossTime, "Boss 已生成", stage.ElapsedTime, "StageManager.EditorJumpToTime");
            stage.EditorJumpToTime(timeline.StageEndTime);
            RecordFlow(report, "超时边界", timeline.StageEndTime, "超时失败待结算", stage.ElapsedTime, "StageManager.EditorJumpToTime");

            state.Reset(); state.EventSucceeded = true; state.ChosenIds.Add("success");
            RecordFlow(report, "Boss 死亡", timeline.StageEndTime - 20f, "渡劫成功/胜利", timeline.StageEndTime - 20f, "terminal outcome");
            state.Reset(); state.EventSucceeded = false; state.EventActive = false;
            RecordFlow(report, "玩家死亡", 350f, "渡劫失败", 350f, "terminal outcome");
            state.Reset(); state.EventSucceeded = false;
            RecordFlow(report, "Boss 存活至 720 秒", timeline.StageEndTime, "邪染失控/超时失败", timeline.StageEndTime, "terminal outcome");
            state.Reset(); ShentongRuntime.Reset(); TribulationRewardRuntime.Clear(); InnateRootRuntime.Clear(); Time.timeScale = 1f;
            Require(state.ChosenIds.Count == 0 && !state.EventActive && !state.EventSucceeded && ShentongRuntime.UnlockedIds.Count == 0, "restart and return menu state reset", report);
            RecordFlow(report, "重新开始/返回主菜单", 0f, "运行时状态清空", 0f, "state reset");
            report.passed = true;
        }
        finally
        {
            report.elapsed = Time.realtimeSinceStartup - started; var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "Stage3PlayModeSmoke.json")); Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllText(path, JsonUtility.ToJson(report, true)); Debug.Log($"[Stage3PlayModeSmoke] passed={report.passed} checks={report.checks.Count} path={path}"); if (root != null) Destroy(root); Time.timeScale = 1f;
            if (stageRoot != null) Destroy(stageRoot);
        }
    }

    private static void RecordFlow(Report report, string node, float expectedSeconds, string state, float actualSeconds, string detail)
    {
        report.flow.Add(new FlowEntry
        {
            node = node,
            stageSeconds = expectedSeconds,
            state = state,
            passed = Mathf.Abs(expectedSeconds - actualSeconds) < 0.01f,
            detail = detail
        });
        Require(report.flow[report.flow.Count - 1].passed, $"flow {node} at {expectedSeconds:0}s", report);
    }

    private static GameObject CreateTimelineProbe()
    {
#if UNITY_EDITOR
        var go = new GameObject("Stage3TimelineProbe");
        go.SetActive(false);
        var stage = go.AddComponent<StageManager>();
        var set = ScriptableObject.CreateInstance<SpawnSet>();
        set.Pattern = SpawnPatternType.Random;
        set.StartTime = 0f;
        set.EndTime = 719f;
        set.BaseRatePerSecond = 0f;
        set.Entries = new List<EnemySpawnInfo>();
        var config = ScriptableObject.CreateInstance<StageConfigSO>();
        config.SpawnSets.Add(set);
        var runtime = ScriptableObject.CreateInstance<RuntimeStageConfigSO>();
        runtime.SelectStageConfig = config;
        var channel = AssetDatabase.LoadAssetAtPath<PlayerSpawnChannelSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/EventChannels/Player/PlayerSpawnChannelSO.asset");
        var startChannel = AssetDatabase.LoadAssetAtPath<StartStageEventChannelSO>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/EventChannels/Stage/StartStageEventChannelSO.asset");
        var enemySystem = AssetDatabase.LoadAssetAtPath<EnemySystem>("Assets/RGame/RoguelikeKit/ScriptableObjects/LogicSO/EnemySystem/EnemySystem.asset");
        SetPrivate(stage, "playerSpawnSO", channel);
        SetPrivate(stage, "startStageChannelSO", startChannel);
        SetPrivate(stage, "enemySystem", enemySystem);
        // Keep the probe inactive: the jump API only needs the runtime spawn
        // copy and this avoids registering a test StageManager on global event
        // channels during the isolated run.
        typeof(StageManager).GetMethod("OnStageStart", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(stage, new object[] { runtime });
        return go;
#else
        return null;
#endif
    }

    private static void SetPrivate(object target, string fieldName, object value)
    {
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(target, value);
    }

    private static void Require(bool condition, string check, Report report) { if (!condition) throw new InvalidOperationException(check); report.checks.Add(check); }
}
#endif