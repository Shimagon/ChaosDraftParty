using System;
using UnityEngine;
using ChaosDraftParty.Core;

namespace ChaosDraftParty.Skills.Effects
{
    /// <summary>
    /// 大ジャンプスキル本実装 (Move/Active、CD 6s)。
    /// 次回ジャンプ 1 回分の jump speed を 2.5 × stackMultiplier 倍に乗算する charge を付与。
    /// charges 消費は <see cref="PlayerController"/> 側で実施。
    /// </summary>
    [Serializable]
    public sealed class JumpBoostEffect : ISkillEffect
    {
        [Tooltip("ジャンプ初速倍率 (baseline)。stackMultiplier を最終的に乗じる。2.5 推奨。")]
        [SerializeField] private float _jumpMultiplier = 2.5f;

        [Tooltip("付与する強化ジャンプ回数。1 推奨 (CLAUDE.md 仕様準拠)。")]
        [SerializeField] private int _charges = 1;

        public void Apply(GameObject caster, SkillContext context)
        {
            if (caster == null) return;

            PlayerController controller = caster.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogWarning($"[JumpBoostEffect] PlayerController が見つからない: {caster.name}");
                return;
            }

            float mul = _jumpMultiplier * context.stackMultiplier;
            controller.ApplyTemporaryJumpMultiplier(mul, _charges);
        }
    }
}
