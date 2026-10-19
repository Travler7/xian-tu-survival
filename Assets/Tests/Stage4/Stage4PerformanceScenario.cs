#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Profiling;
using UnityEngine;
using RGame.Framework;
using RGame.RoguelikeKit;

public enum Stage4PerformanceScenario
{
    Ordinary80,
    HighHpReaction80,
    FeedbackWard80
}

/// <summary>
/// PlayMode-only sampler. Attach it to a dedicated performance scene and run
/// one scenario at a time; it never changes formal stage configuration.
/// </summary>
public sealed class Stage4PerformanceSampler : MonoBehaviour
{
    public Stage4PerformanceScenario Scenario;
    public int WarmupSeconds = 5;
    public int SampleSeconds = 30;
    public PoolRuntimeSO PoolRuntime;

    private ProfilerRecorder _gc;
    private ProfilerRecorder _mainThread;
    private readonly List<float> _frameMs = new();
    private readonly List<long> _gcBytes = new();
    private readonly List<long> _mainThreadNs = new();
    private int _reactionCount;
    private bool _previousRunInBackground;

    private void Awake()
    {
        // The sampler must keep advancing when the Editor/Game view loses
        // focus. This is a test-scene setting only and is restored on exit.
        _previousRunInBackground = Application.runInBackground;
        Application.runInBackground = true;
    }

    private void OnEnable() => ElementMarkController.AnyReactionTriggered += OnReaction;
    private void OnDisable() => ElementMarkController.AnyReactionTriggered -= OnReaction;

    private IEnumerator Start()
    {
        // Population setup is deliberately separate from the formal warmup.
        // This prevents the first 80 pooled enemy activations from polluting
        // the 5-second warmup and the 30-second measured window.
        var bootstrap = FindAnyObjectByType<Stage4PerformanceBootstrap>();
        while (bootstrap != null && !bootstrap.Ready)
            yield return null;

        _gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        yield return new WaitForSecondsRealtime(WarmupSeconds);
        _frameMs.Clear();
        _gcBytes.Clear();
        _mainThreadNs.Clear();
        _reactionCount = 0;
        // Pool metrics must describe the measured 30-second window only.
        // Dynamic reaction pools are created during warmup on their first
        // burst; retaining those counters would mix warmup allocation with
        // the formal sample and make the hit-rate result ambiguous.
        PoolRuntime?.ResetMetrics();
        float end = Time.realtimeSinceStartup + SampleSeconds;
        while (Time.realtimeSinceStartup < end)
        {
            _frameMs.Add(Time.unscaledDeltaTime * 1000f);
            if (_gc.Valid) _gcBytes.Add(_gc.LastValue);
            if (_mainThread.Valid) _mainThreadNs.Add(_mainThread.LastValue);
            yield return null;
        }
        WriteReport();
        _gc.Dispose();
        _mainThread.Dispose();
    }

    private void OnDestroy()
    {
        Application.runInBackground = _previousRunInBackground;
    }

    private void WriteReport()
    {
        _frameMs.Sort();
        var enemies = FindObjectsByType<BaseEnemy>().Length;
        var metrics = PoolRuntime != null ? PoolRuntime.GetMetrics("ReactionVfx") : default;
        var feedbackMetrics = PoolRuntime != null ? PoolRuntime.GetMetrics("ReactionFeedback") : default;
        var report = new Stage4PerformanceReport
        {
            Scenario = Scenario.ToString(),
            UnityVersion = Application.unityVersion,
            EnemyCountAtEnd = enemies,
            WarmupSeconds = WarmupSeconds,
            SampleSeconds = SampleSeconds,
            AverageFrameMs = _frameMs.Count == 0 ? 0f : _frameMs.Average(),
            P95FrameMs = Percentile(_frameMs, 0.95f),
            MinimumFps = _frameMs.Count == 0 ? 0f : 1000f / Mathf.Max(0.001f, _frameMs.Max()),
            GcAllocatedInFrameBytes = _gc.Valid ? _gcBytes.Sum() : -1,
            GcAllocatedAverageBytesPerFrame = _gc.Valid ? _gcBytes.DefaultIfEmpty().Average() : -1,
            GcAllocatedMaxBytesPerFrame = _gc.Valid ? _gcBytes.DefaultIfEmpty().Max() : -1,
            MainThreadNanoseconds = _mainThread.Valid ? (long)_mainThreadNs.DefaultIfEmpty().Average() : -1,
            MainThreadP95Nanoseconds = _mainThread.Valid ? Percentile(_mainThreadNs, 0.95f) : -1,
            ReactionCount = _reactionCount,
            ReactionVfxRequests = metrics.Requests + feedbackMetrics.Requests,
            ReactionVfxPoolHits = metrics.Hits + feedbackMetrics.Hits,
            ReactionVfxCreates = metrics.Creates + feedbackMetrics.Creates,
            ReactionVfxReturns = metrics.Returns + feedbackMetrics.Returns,
            PoolHitRate = CombinedHitRate(metrics, feedbackMetrics)
        };
        var path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "ProfilerCaptures", "Stage4", report.Scenario + "_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json"));
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        Debug.Log($"[Stage4Performance] {report.Scenario} wrote {path}");
    }

    private static float Percentile(List<float> values, float percentile)
    {
        if (values.Count == 0) return 0f;
        int index = Mathf.Clamp(Mathf.CeilToInt(values.Count * percentile) - 1, 0, values.Count - 1);
        return values[index];
    }

    private static long Percentile(List<long> values, float percentile)
    {
        if (values.Count == 0) return 0;
        var ordered = values.OrderBy(v => v).ToList();
        int index = Mathf.Clamp(Mathf.CeilToInt(ordered.Count * percentile) - 1, 0, ordered.Count - 1);
        return ordered[index];
    }

    private static float CombinedHitRate(PoolRuntimeMetrics a, PoolRuntimeMetrics b)
    {
        var requests = a.Requests + b.Requests;
        return requests <= 0 ? 1f : (float)(a.Hits + b.Hits) / requests;
    }

    private void OnReaction(ReactionResult _) => _reactionCount++;

    [Serializable]
    private sealed class Stage4PerformanceReport
    {
        public string Scenario;
        public string UnityVersion;
        public int EnemyCountAtEnd;
        public int WarmupSeconds;
        public int SampleSeconds;
        public float AverageFrameMs;
        public float P95FrameMs;
        public float MinimumFps;
        public long GcAllocatedInFrameBytes;
        public double GcAllocatedAverageBytesPerFrame;
        public long GcAllocatedMaxBytesPerFrame;
        public long MainThreadNanoseconds;
        public long MainThreadP95Nanoseconds;
        public int ReactionCount;
        public long ReactionVfxRequests;
        public long ReactionVfxPoolHits;
        public long ReactionVfxCreates;
        public long ReactionVfxReturns;
        public float PoolHitRate;
    }
}
#endif
