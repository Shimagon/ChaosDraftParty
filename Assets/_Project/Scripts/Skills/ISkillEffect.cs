namespace ChaosDraftParty.Skills
{
    /// <summary>
    /// スキル効果のサーバー権威実装インターフェイス。
    /// 各スキル（Dash, Bomb, Revive 等）はこれを実装し、SkillRegistry に登録される。
    /// </summary>
    public interface ISkillEffect
    {
        /// <summary>
        /// スキルを発動する。呼び出し側はサーバー（ホスト）で実行すること。
        /// </summary>
        /// <param name="ctx">発動コンテキスト（発動者、対象位置、スタック数など）</param>
        void Apply(SkillContext ctx);
    }

    /// <summary>
    /// スキル発動時の入力。スタック数で凹関数倍率を使う。
    /// </summary>
    public struct SkillContext
    {
        public ulong casterClientId;
        public UnityEngine.Vector3 casterPosition;
        public UnityEngine.Vector3 aimDirection;
        public int stackCount;       // 1..4+ （凹関数で倍率決定）
        public float deltaTime;
    }
}
