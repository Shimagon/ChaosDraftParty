using System;
using UnityEngine;

namespace ChaosDraftParty.Skills.Effects
{
    /// <summary>
    /// パルス弾スキル stub (Hit/Active、CD 5s)。
    ///
    /// TODO (M3): 前方吹き飛ばし弾を ServerRpc で生成。
    ///   - 弾速 = magnitude × stackMultiplier
    ///   - 半径 / 寿命 SO 化
    ///   - 命中時 OverlapSphereNonAlloc → ノックバック impulse
    ///   - VFX trigger は ClientRpc 1 発、physics はホスト権威
    ///
    /// 現状は log のみ (compile / wiring 検証用)。
    /// </summary>
    [Serializable]
    public sealed class PulseEffect : ISkillEffect
    {
        [Tooltip("吹き飛ばし baseline 強度。M3 実装時に弾速 / impulse へ展開。")]
        [SerializeField] private float _baselineImpulse = 8f;

        public void Apply(GameObject caster, SkillContext context)
        {
            // M3 で本実装。現状は wiring 確認のみ。
            float magnitude = _baselineImpulse * context.stackMultiplier;
            Debug.Log($"[PulseEffect] (stub) caster={(caster != null ? caster.name : "null")} dir={context.aimDirection} mag={magnitude}");
        }
    }
}
