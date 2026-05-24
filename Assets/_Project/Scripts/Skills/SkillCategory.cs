namespace ChaosDraftParty.Skills
{
    /// <summary>
    /// スキル分類 (CLAUDE.md スキル表)。
    /// Move = ダッシュ、ジャンプ、ロープ、ホバー。
    /// Hit  = パルス、ボム、盾、ミラージュ。
    /// Sense = レーダー、復活。
    ///
    /// 既存 .asset の `category: int` value 順を絶対に変えないこと
    /// (Move=0 / Hit=1 / Sense=2)。順序変更は全 SO 値破壊につながる。
    /// </summary>
    public enum SkillCategory
    {
        Move,
        Hit,
        Sense,
    }
}
