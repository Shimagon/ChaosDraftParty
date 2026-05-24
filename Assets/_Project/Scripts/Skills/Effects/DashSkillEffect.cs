using System;
using UnityEngine;
using ChaosDraftParty.Core;

namespace ChaosDraftParty.Skills.Effects
{
    /// <summary>
    /// ダッシュスキル本実装 (Move/Active、CD 8s)。
    /// 2 秒間、caster の water speed multiplier を 1.8 × stackMultiplier に上書き。
    ///
    /// **クラス名注意**: scene 既存の <see cref="EggybotDashEffect"/> (FOV punch 等の
    /// camera 仕上げ用 MonoBehaviour) と衝突回避のため、本物 skill effect は
    /// `DashSkillEffect` を採用する。
    ///
    /// Network: caster.PlayerController.ApplyTemporarySpeedBoost はサーバー権限想定
    /// (Owner 権威の場合は ServerRpc 経由で再現)。本 effect 自体は Apply で
    /// 直接 method を呼ぶだけの薄い adapter。
    /// </summary>
    [Serializable]
    public sealed class DashSkillEffect : ISkillEffect
    {
        [Tooltip("速度倍率 (baseline)。stackMultiplier を最終的に乗じる。1.8 推奨。")]
        [SerializeField] private float _speedMultiplier = 1.8f;

        [Tooltip("持続時間 (秒)。2 推奨。")]
        [SerializeField] private float _durationSec = 2f;

        public void Apply(GameObject caster, SkillContext context)
        {
            if (caster == null) return;

            PlayerController controller = caster.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogWarning($"[DashSkillEffect] PlayerController が見つからない: {caster.name}");
                return;
            }

            float boost = _speedMultiplier * context.stackMultiplier;
            controller.ApplyTemporarySpeedBoost(boost, _durationSec);
        }
    }
}
