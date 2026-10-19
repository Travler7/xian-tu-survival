using UnityEngine;

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(menuName = "RGame/RoguelikeKit/Tribulation/Timeline", fileName = "TribulationTimeline")]
    public sealed class TribulationTimelineSO : ScriptableObject
    {
        [Min(0)] public float FirstChoiceTime = 240f;
        [Min(0)] public float SecondChoiceTime = 480f;
        [Min(0)] public float FinalOmenTime = 630f;
        [Min(0)] public float FinalBossTime = 660f;
        [Min(0)] public float StageEndTime = 720f;
    }
}
