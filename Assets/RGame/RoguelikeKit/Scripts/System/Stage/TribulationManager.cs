using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    /// <summary>Owns the per-run tribulation flow. StageManager remains authoritative for stage time and results.</summary>
    public sealed class TribulationManager : MonoBehaviour
    {
        [SerializeField] private StageManager _stage;
        [SerializeField] private SkillManager _skills;
        [SerializeField] private TribulationTimelineSO _timeline;
        [SerializeField] private TribulationPoolSO _tribulationPool;
        [SerializeField] private TribulationRewardPoolSO _rewardPool;
        [SerializeField] private TribulationOverlay _overlay;

        private const string TribulationTimelineResource = "TribulationTimeline";
        private const string TribulationPoolResource = "TribulationPool";
        private const string TribulationRewardPoolResource = "TribulationRewardPool";

        private readonly TribulationRunState _state = new();
        private readonly List<TribulationDefinition> _choices = new(3);
        private readonly List<TribulationRewardDefinition> _rewards = new(2);
        private readonly HashSet<string> _pendingBreakthroughSkills = new();
        private readonly Dictionary<string, ReactionResult> _lastSkillReactions = new();
        private TribulationDefinition _active;
        private float _eventEndTime;
        private float _nextChoiceTime;
        private bool _choosing;
        private bool _choosingReward;
        private bool _choosingBreakthrough;
        private string _queuedBreakthroughSkill;
        private readonly List<ShentongBreakthroughDefinition> _breakthroughChoices = new(3);
        private BreakthroughProgressView _breakthroughView;
        private bool _ownsPause;
        private bool _runStarted;
        private PlayerHit _playerHit;
        private float _nextPressureSpawn;

        public TribulationRunState State => _state;

        /// <summary>Overlay accessor that transparently rebuilds a destroyed overlay.</summary>
        private TribulationOverlay Overlay
        {
            get
            {
                if (_overlay == null) EnsureOverlay();
                return _overlay;
            }
        }

        private void Awake()
        {
            if (_stage == null) _stage = GetComponent<StageManager>();
            if (_skills == null) _skills = FindFirstObjectByType<SkillManager>();
            EnsureOverlay();
            EnsureDefaults();
        }

        /// <summary>
        /// Resolves the overlay (and its HUD readout), recreating them when the previously cached
        /// instance was destroyed by a scene unload. Without this a rebuilt manager would throw on
        /// a dead reference right after pausing the game.
        /// </summary>
        private void EnsureOverlay()
        {
            // Unity's == null overload is true for destroyed objects, which is the case we care about.
            if (_overlay == null) _overlay = FindAnyObjectByType<TribulationOverlay>(FindObjectsInactive.Include);
            if (_overlay == null) _overlay = new GameObject("TribulationOverlay").AddComponent<TribulationOverlay>();
            if (_breakthroughView == null)
            {
                _breakthroughView = _overlay.EnsureBreakthroughProgressView();
                _breakthroughView.Hide();
            }
        }

        /// <summary>
        /// Resolves the tribulation content from <c>Resources/</c>. These assets are part of the
        /// project and covered by the acceptance tests, so a missing one is a broken build, not a
        /// case to paper over: a hard-coded copy used to live here and silently drifted away from
        /// the assets, which meant the game could run against a second, unreviewed set of numbers.
        /// </summary>
        private void EnsureDefaults()
        {
            if (_timeline == null) _timeline = Resources.Load<TribulationTimelineSO>(TribulationTimelineResource);
            if (_tribulationPool == null) _tribulationPool = Resources.Load<TribulationPoolSO>(TribulationPoolResource);
            if (_rewardPool == null) _rewardPool = Resources.Load<TribulationRewardPoolSO>(TribulationRewardPoolResource);

            if (_timeline == null) Debug.LogError($"[{nameof(TribulationManager)}] Missing Resources/{TribulationTimelineResource}. Tribulation timing is disabled.", this);
            if (_tribulationPool == null || _tribulationPool.Tribulations.Count == 0)
                Debug.LogError($"[{nameof(TribulationManager)}] Missing or empty Resources/{TribulationPoolResource}. Tribulation choices are disabled.", this);
            if (_rewardPool == null || _rewardPool.Rewards.Count == 0)
                Debug.LogError($"[{nameof(TribulationManager)}] Missing or empty Resources/{TribulationRewardPoolResource}. Tribulation rewards are disabled.", this);
        }

        private void OnEnable()
        {
            if (_stage != null) _stage.EnemyKilled += OnEnemyKilled;
            if (_stage != null) _stage.TribulationEnemyKilled += OnTribulationEnemyKilled;
            if (_stage != null) _stage.StageStarted += BeginRun;
            if (_stage != null) _stage.StageEnded += EndRun;
            ElementMarkController.AnyReactionTriggered += OnReaction;
            ElementMarkController.AnyReactionTriggeredWithSource += OnReactionWithSource;
            ShentongRuntime.BreakthroughReady += QueueBreakthrough;
        }

        private void OnDisable()
        {
            if (_stage != null) _stage.EnemyKilled -= OnEnemyKilled;
            if (_stage != null) _stage.TribulationEnemyKilled -= OnTribulationEnemyKilled;
            if (_stage != null) _stage.StageStarted -= BeginRun;
            if (_stage != null) _stage.StageEnded -= EndRun;
            ElementMarkController.AnyReactionTriggered -= OnReaction;
            ElementMarkController.AnyReactionTriggeredWithSource -= OnReactionWithSource;
            ShentongRuntime.BreakthroughReady -= QueueBreakthrough;
            if (_playerHit != null) _playerHit.OnDie -= EndRun;
            ReleasePause();
        }

        public void BeginRun()
        {
            _state.Reset();
            TribulationRewardRuntime.Clear();
            _choices.Clear();
            _rewards.Clear();
            _pendingBreakthroughSkills.Clear();
            _breakthroughChoices.Clear();
            _lastSkillReactions.Clear();
            _active = null;
            _runStarted = true;
            EnsureOverlay();
            _breakthroughView.Hide();
            _progress = 0;
            // Update() bails out while the timeline is missing, so this value only has to be sane.
            _nextChoiceTime = _timeline != null ? _timeline.FirstChoiceTime : float.MaxValue;
            InnateRootRuntime.ClearAwakening();
            ShentongRuntime.Reset();
            ElementResonanceRuntime.Reset();
            if (_playerHit != null) _playerHit.OnDie -= EndRun;
            _playerHit = FindFirstObjectByType<PlayerHit>();
            if (_playerHit != null) _playerHit.OnDie += EndRun;
        }

        private void EndRun()
        {
            _runStarted = false;
            _choosing = false;
            _choosingReward = false;
            _choosingBreakthrough = false;
            _state.EventActive = false;
            _active = null;
            _progress = 0;
            if (_stage != null) _stage.SetTribulationSpawnMultiplier(1f);
            if (_stage != null) _stage.ClearTribulationEnemyTracking();
            TribulationRewardRuntime.Clear();
            InnateRootRuntime.ClearAwakening();
            ShentongRuntime.Reset();
            ElementResonanceRuntime.Reset();
            _pendingBreakthroughSkills.Clear();
            _breakthroughChoices.Clear();
            _lastSkillReactions.Clear();
            if (_breakthroughView != null) _breakthroughView.Hide();
            if (_overlay != null) _overlay.Hide();
            ReleasePause();
        }

        private void Update()
        {
            if (!_runStarted || _stage == null || _timeline == null) return;
            if (_breakthroughView == null) EnsureOverlay();
            float now = _stage.ElapsedTime;
            _breakthroughView.Refresh(_skills != null ? _skills.GetActiveSkillSnapshot() : null);
            if (!_state.FinalOmenShown && now >= _timeline.FinalOmenTime)
            {
                _state.FinalOmenShown = true;
                Debug.Log("最终劫相已至。终局将近。", this);
                Overlay.ShowToast("最终劫相将至", 5f);
            }
            if (_state.EventActive && _active != null)
            {
                if (Time.time >= _eventEndTime)
                {
                    CompleteEvent(_active.Goal == TribulationGoal.Survive ||
                        _active.Goal == TribulationGoal.KillMarked ||
                        _progress >= _active.TargetCount);
                    return;
                }
                if (Time.time >= _nextPressureSpawn)
                {
                    _stage.SpawnTribulationSet(_active.EventSpawnSet);
                    _nextPressureSpawn += 5f;
                }
                if (!_choosing && !_choosingReward && !_choosingBreakthrough)
                {
                    Overlay.ShowEvent($"{_active.DisplayName}\n{_active.DifficultyEffect}\n剩余 {Mathf.Max(0, _eventEndTime - Time.time):0}s  进度 {_progress}/{_active.TargetCount}");
                }
                return;
            }
            if (_choosing || _choosingReward) return;
            if (!_state.FinalOmenShown && Time.timeScale > 0f && !PauseCoordinator.IsPaused && !_choosingBreakthrough && _pendingBreakthroughSkills.Count > 0)
            {
                OpenBreakthrough();
                return;
            }
            if (_state.ChosenIds.Count < 2 && now >= _nextChoiceTime) OpenChoices();
        }

        private void OpenChoices()
        {
            if (_tribulationPool == null) return;
            var usedPressures = new HashSet<TribulationPressure>(_state.ChosenPressures);
            var available = TribulationRules.GetAvailableChoices(_tribulationPool.Tribulations, _state.ChosenIds, usedPressures)
                .OrderBy(_ => Random.value).Take(3).ToList();
            if (available.Count < 3) return;
            _choices.Clear();
            _choices.AddRange(available);
            AcquirePause();
            _choosing = true;
            Overlay.ShowChoices(_choices, SelectTribulation);
        }

        private void SelectTribulation(TribulationDefinition definition)
        {
            if (!_choosing || definition == null) return;
            _choosing = false;
            Overlay.Hide();
            _state.ChosenIds.Add(definition.Id);
            _state.ChosenPressures.Add(definition.Pressure);
            _active = definition;
            _state.EventActive = true;
            _state.EventSucceeded = false;
            _progress = 0;
            _eventEndTime = Time.time + definition.Duration;
            _nextPressureSpawn = Time.time + 5f;
            _stage.SetTribulationSpawnMultiplier(PressureMultiplier(definition.Pressure));
            ReleasePause();
            _stage.SpawnTribulationSet(definition.EventSpawnSet, definition.SpawnMultiplier);
        }

        private void OnEnemyKilled(BaseEnemy enemy)
        {
        }

        private void OnTribulationEnemyKilled(BaseEnemy enemy)
        {
            if (!_state.EventActive || _active == null || enemy == null) return;
            if (_active.Goal == TribulationGoal.KillAny || _active.Goal == TribulationGoal.KillRanged ||
                _active.Goal == TribulationGoal.KillMarked || _active.Goal == TribulationGoal.KillElites) _progress++;
            CheckGoal();
        }

        private int _progress;
        private void OnReaction(ReactionResult reaction)
        {
            if (!_state.EventActive || _active == null || _active.Goal != TribulationGoal.TriggerReactions) return;
            _progress++;
            CheckGoal();
        }

        private void QueueBreakthrough(string skillKey)
        {
            if (!_runStarted || string.IsNullOrWhiteSpace(skillKey) || ShentongRuntime.UnlockedIds.Count >= ShentongRuntime.MaxBreakthroughs) return;
            _pendingBreakthroughSkills.Add(skillKey);
        }

        private void OnReactionWithSource(ReactionResult reaction, string sourceKey)
        {
            if (!_runStarted || string.IsNullOrWhiteSpace(sourceKey) || _skills == null) return;
            string skillKey = sourceKey.Split(':')[0];
            var skill = _skills.GetActiveSkillSnapshot().FirstOrDefault(s => s != null && s.Key == skillKey && s.SkillType == SkillType.AttackSkill);
            if (skill == null || (skill.PrimaryElement != reaction.First && skill.PrimaryElement != reaction.Second &&
                                  skill.SecondaryElement != reaction.First && skill.SecondaryElement != reaction.Second)) return;
            _lastSkillReactions[skillKey] = reaction;
        }

        private void OpenBreakthrough()
        {
            _queuedBreakthroughSkill = _pendingBreakthroughSkills.FirstOrDefault(skill =>
                _lastSkillReactions.ContainsKey(skill) && ShentongRuntime.TryGetReadyReaction(skill, out _) &&
                _skills.GetActiveSkillSnapshot().Any(s => s != null && s.Key == skill));
            if (string.IsNullOrEmpty(_queuedBreakthroughSkill)) { _pendingBreakthroughSkills.Clear(); return; }

            var candidates = ShentongResolver.ForReaction(_lastSkillReactions[_queuedBreakthroughSkill], ShentongRuntime.UnlockedIds).Take(3).ToList();
            // The overlay only prints the definition name/description, so carry the owning skill
            // into the card text: a lone "断生·枯荣" card otherwise reads like a mystery.
            var skill = _skills.GetActiveSkillSnapshot().FirstOrDefault(s => s != null && s.Key == _queuedBreakthroughSkill);

            // No candidate means there is nothing to choose: drop the request and stay unpaused.
            if (candidates.Count == 0)
            {
                _pendingBreakthroughSkills.Remove(_queuedBreakthroughSkill);
                _queuedBreakthroughSkill = null;
                return;
            }

            _breakthroughChoices.Clear();
            _breakthroughChoices.AddRange(candidates);
            AcquirePause();
            _choosingBreakthrough = true;
            Overlay.ShowBreakthroughs(_breakthroughChoices, SelectBreakthrough, skill != null ? skill.DisplayName : null);
        }

        private void SelectBreakthrough(ShentongBreakthroughDefinition definition)
        {
            if (!_choosingBreakthrough || definition == null) return;
            ShentongRuntime.Unlock(definition.Id, _queuedBreakthroughSkill);
            _pendingBreakthroughSkills.Remove(_queuedBreakthroughSkill);
            _queuedBreakthroughSkill = null;
            _breakthroughChoices.Clear();
            _choosingBreakthrough = false;
            Overlay.Hide();
            ReleasePause();
        }

        private void CheckGoal()
        {
            if (_active == null || _progress < _active.TargetCount) return;
            CompleteEvent(true);
        }

        private void CompleteEvent(bool success)
        {
            _state.EventActive = false;
            _state.EventSucceeded = success;
            _stage.SetTribulationSpawnMultiplier(1f);
            if (_overlay != null) _overlay.Hide();
            if (!success) Overlay.ShowToast("渡劫失败，机缘消散", 2.5f);
            _progress = 0;
            _active = null;
            _stage.ClearTribulationEnemyTracking();
            if (success) OpenRewards();
            else AdvanceChoiceTime();
        }

        private void OpenRewards()
        {
            if (_rewardPool == null || _skills == null) { AdvanceChoiceTime(); return; }
            var activeKeys = new HashSet<string>(_skills.GetActiveSkillSnapshot().Where(s => s.SkillType == SkillType.AttackSkill).Select(s => s.Key));
            var candidates = TribulationRules.GetEligibleRewards(_rewardPool.Rewards, _state.RewardIds, activeKeys, _state.RootAwakeningClaimed)
                .OrderBy(_ => Random.value).Take(2).ToList();
            if (candidates.Count == 0) { AdvanceChoiceTime(); return; }
            _rewards.Clear();
            _rewards.AddRange(candidates);
            AcquirePause();
            _choosingReward = true;
            Overlay.ShowRewards(_rewards, SelectReward);
        }

        private void SelectReward(TribulationRewardDefinition reward)
        {
            if (!_choosingReward || reward == null) return;
            if (reward.Kind == TribulationRewardKind.SkillMutation)
            {
                if (!_skills.ApplyTribulationMutation(reward)) return;
                TribulationRewardRuntime.Apply(reward.Id);
            }
            else
            {
                InnateRootRuntime.Awaken(reward.Root);
                _state.RootAwakeningClaimed = true;
            }
            _state.RewardIds.Add(reward.Id);
            _choosingReward = false;
            Overlay.Hide();
            ReleasePause();
            AdvanceChoiceTime();
        }

        private void AdvanceChoiceTime()
        {
            // Without a timeline no further choice can be scheduled.
            _nextChoiceTime = _timeline != null && _state.ChosenIds.Count == 1
                ? _timeline.SecondChoiceTime
                : float.MaxValue;
        }

        private void AcquirePause()
        {
            // The coordinator counts owners, so a level-up card picked mid-tribulation no
            // longer resumes the world underneath an open tribulation panel.
            PauseCoordinator.Acquire(this);
            _ownsPause = true;
        }

        private void ReleasePause()
        {
            if (!_ownsPause) return;
            PauseCoordinator.Release(this);
            _ownsPause = false;
        }

        private static float PressureMultiplier(TribulationPressure pressure) => pressure switch
        {
            TribulationPressure.Spawn => 1.75f,
            TribulationPressure.Ranged => 1.35f,
            TribulationPressure.Control => 1.3f,
            TribulationPressure.Elite => 1.2f,
            TribulationPressure.Element => 1.4f,
            _ => 1.5f
        };

        private static string PressureName(TribulationPressure pressure) => pressure switch
        {
            TribulationPressure.Spawn => "刷怪压力",
            TribulationPressure.Ranged => "远程压力",
            TribulationPressure.Control => "控制压力",
            TribulationPressure.Elite => "精英压力",
            TribulationPressure.Element => "元素压力",
            _ => pressure.ToString()
        };
    }
}