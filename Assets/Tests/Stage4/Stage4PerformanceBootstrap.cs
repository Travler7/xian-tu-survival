#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using RGame.Framework;
using RGame.RoguelikeKit;
using UnityEngine;

/// <summary>
/// Creates a fixed 80-enemy performance population without touching a formal
/// StageConfig or SpawnSet. The enemies still come from the production pool.
/// </summary>
public sealed class Stage4PerformanceBootstrap : MonoBehaviour
{
    public Stage4PerformanceScenario Scenario;
    public PoolRuntimeSO PoolRuntime;
    public string EnemyPoolKey = "Ghost";
    public int EnemyCount = 80;

    private readonly List<BaseEnemy> _enemies = new();
    private bool _ready;
    private Coroutine _reactionRoutine;

    public bool Ready => _ready;

    private IEnumerator Start()
    {
        if (PoolRuntime == null)
        {
            Debug.LogError("[Stage4Performance] PoolRuntime is not assigned.", this);
            yield break;
        }

        // A ScriptableObject asset can retain its runtime dictionaries while
        // an isolated Editor scene is reloaded. Start each sample from a clean
        // test-only pool state so the population and hit metrics are repeatable.
        PoolRuntime.ResetRuntimeForTests();
        var root = new GameObject("Stage4PerformancePoolRoot");
        PoolRuntime.InitializeForTests(root.transform, EnemyPoolKey, "DamagePopup");

        // The measured scenes keep all 80 reaction presentations resident.
        // Prewarm these test-only pools before activating enemies so the sample
        // window contains no VFX/text Instantiate calls.
        ReactionFeedback.Preload(PoolRuntime, EnemyCount);
        ReactionVfx.Preload(PoolRuntime, "ReactionVfx", EnemyCount);

        for (var i = 0; i < Mathf.Max(1, EnemyCount); i++)
        {
            var instance = PoolRuntime.Request(EnemyPoolKey);
            if (instance == null) continue;
            instance.transform.position = PositionFor(i, EnemyCount);
            var enemy = instance.GetComponent<BaseEnemy>();
            if (enemy != null) _enemies.Add(enemy);

            // Give the Editor a frame between small batches. This keeps the
            // harness responsive while Unity runs the pooled prefab's first
            // frame initialization for an 80-enemy population.
            if ((i + 1) % 4 == 0)
                yield return null;
        }

        // BaseEnemy and EnemyHit initialise their runtime stats on the first
        // active frame. Waiting here also makes ward setup deterministic.
        yield return null;
        if (Scenario == Stage4PerformanceScenario.FeedbackWard80)
        {
            foreach (var enemy in _enemies)
                enemy?.MyHit?.ConfigureCorruptionWard(SpawnSetRole.Elite);
        }

        yield return null;
        _ready = true;
        if (Scenario != Stage4PerformanceScenario.Ordinary80)
            _reactionRoutine = StartCoroutine(ReactionLoop());
    }

    private IEnumerator ReactionLoop()
    {
        // Keep one mark visible briefly, then complete the pair. The interval
        // respects the production reaction cooldown while still creating a
        // stable high-frequency workload for the 30-second sample.
        while (_ready)
        {
            foreach (var enemy in _enemies)
                enemy?.MyHit?.ElementMarks?.Register(ElementType.Metal, "Stage4:metal", out _);

            yield return new WaitForSecondsRealtime(0.2f);

            foreach (var enemy in _enemies)
            {
                if (enemy == null || enemy.MyHit == null || enemy.MyHit.ElementMarks == null) continue;
                if (!enemy.MyHit.ElementMarks.Register(ElementType.Water, "Stage4:water", out var reaction)) continue;
                enemy.MyHit.ElementMarks.NotifyReaction(reaction, "Stage4Performance");
                ReactionFeedback.Show(PoolRuntime, enemy.transform.position, reaction.Name, reaction.PopupColor);
                ReactionVfx.Show(PoolRuntime, enemy.transform.position, reaction.VfxPoolKey, reaction.PopupColor, reaction.AreaRadius);
            }

            yield return new WaitForSecondsRealtime(1.9f);
        }
    }

    private void OnDestroy()
    {
        if (_reactionRoutine != null) StopCoroutine(_reactionRoutine);
        if (PoolRuntime != null)
        {
            PoolRuntime.RecycleAll();
            PoolRuntime.ResetRuntimeForTests();
        }
    }

    private static Vector3 PositionFor(int index, int count)
    {
        const float radius = 7.5f;
        var angle = index * Mathf.PI * 2f / Mathf.Max(1, count);
        return new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
    }
}
#endif