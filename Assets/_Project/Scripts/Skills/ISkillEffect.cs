using UnityEngine;

namespace ChaosDraftParty.Skills
{
    /// <summary>
    /// スキル効果のサーバー権威実装インターフェイス。
    /// 各スキル（Dash, Bomb, Revive 等）はこれを実装し、SkillDefinition の `_effect`
    /// (SerializeReference) 経由で ScriptableObject に埋め込む。発動側はホスト権限で
    /// Apply を呼ぶ前提 (CLAUDE.md ネットワーク同期ポリシー準拠)。
    /// </summary>
    public interface ISkillEffect
    {
        /// <summary>
        /// スキルを発動する。呼び出し側はサーバー（ホスト）で実行すること。
        /// </summary>
        /// <param name="caster">発動キャラ GameObject (PlayerController が attach されている想定)</param>
        /// <param name="context">発動コンテキスト (stack count + multiplier 等)</param>
        void Apply(GameObject caster, SkillContext context);
    }

    /// <summary>
    /// スキル発動時のコンテキスト。stack count から凹関数倍率を算出して保持。
    /// 同じスキルを複数取った時の重複強化 (CLAUDE.md GetStackMultiplier 表) に対応。
    /// </summary>
    public readonly struct SkillContext
    {
        public readonly int stackCount;
        public readonly float stackMultiplier;
        public readonly ulong casterClientId;
        public readonly Vector3 casterPosition;
        public readonly Vector3 aimDirection;

        public SkillContext(int stack)
        {
            stackCount = Mathf.Max(1, stack);
            stackMultiplier = GetStackMultiplier(stackCount);
            casterClientId = 0;
            casterPosition = Vector3.zero;
            aimDirection = Vector3.forward;
        }

        public SkillContext(int stack, ulong clientId, Vector3 position, Vector3 direction)
        {
            stackCount = Mathf.Max(1, stack);
            stackMultiplier = GetStackMultiplier(stackCount);
            casterClientId = clientId;
            casterPosition = position;
            aimDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }

        /// <summary>
        /// 凹関数: 1=1.00 / 2=1.20 / 3=1.35 / 4+=1.45 (CLAUDE.md 重複強化表準拠)。
        /// 同スキル積上げ時の伸びを段々頭打ちさせる balance core。
        /// </summary>
        public static float GetStackMultiplier(int count)
        {
            if (count <= 1) return 1.0f;
            if (count == 2) return 1.20f;
            if (count == 3) return 1.35f;
            return 1.45f;
        }
    }
}
