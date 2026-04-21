using UnityEngine;

namespace ChaosDraftParty.Core.Configs
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "ChaosDraftParty/GameConfig", order = 0)]
    public class GameConfig : ScriptableObject
    {
        [Header("Player Movement")]
        [Tooltip("基本移動速度 (m/s)")] public float playerBaseSpeed = 6f;
        [Tooltip("ジャンプ初速 (m/s)")] public float playerJumpSpeed = 7f;
        [Tooltip("重力倍率 (RuleCard low_gravity で 0.7 に変動)")] public float gravityMultiplier = 1f;

        [Header("Match / Round (Spec v9.5)")]
        [Tooltip("1マッチのラウンド数")] public int roundsPerMatch = 4;
        [Tooltip("1シーズンのマッチ数")] public int matchesPerSeason = 3;
        [Tooltip("1ラウンドの制限時間 (秒)")] public float roundDurationSec = 90f;
        [Tooltip("カウントダウン (秒)")] public float countdownSec = 3f;
        [Tooltip("ラウンド終了演出 (秒)")] public float roundEndingSec = 3f;
        [Tooltip("ドラフト制限時間 (秒)")] public float draftDurationSec = 5f;
        [Tooltip("マッチ結果表示 (秒)")] public float matchResultSec = 15f;
        [Tooltip("ウォームアップ (秒)")] public float warmupSec = 10f;
        [Tooltip("ルールカード表示 (秒)")] public float ruleCardRevealSec = 5f;

        [Header("Skill Stack Multipliers (凹関数)")]
        [Tooltip("1個目の倍率")] public float stackMul1 = 1.00f;
        [Tooltip("2個目重複の倍率")] public float stackMul2 = 1.20f;
        [Tooltip("3個目重複の倍率")] public float stackMul3 = 1.35f;
        [Tooltip("4個目以降の倍率 (頭打ち)")] public float stackMulCap = 1.45f;

        [Header("Network")]
        [Tooltip("Trigger 猶予 (ms、ホスト権威の落下判定など)")] public int triggerGraceMs = 200;

        public float GetStackMultiplier(int count)
        {
            if (count <= 1) return stackMul1;
            if (count == 2) return stackMul2;
            if (count == 3) return stackMul3;
            return stackMulCap;
        }

        public float GravityAcceleration => -9.81f * gravityMultiplier;
    }
}
