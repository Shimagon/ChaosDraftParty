namespace ChaosDraftParty.Skills
{
    /// <summary>
    /// 発動タイプ。Active=ボタン入力、Passive=常時、Trigger=条件発動 (HP0 等)。
    ///
    /// **歴史的経緯**: 既存 .asset では `activation: int` フィールド名で保存されており、
    /// enum 型名は <see cref="ActivationType"/> として SkillDefinition から参照される。
    /// 本 enum はタスク仕様 (M1) 命名 `SkillActionType` に揃えた alias 型として、
    /// 内部値 (Active=0 / Passive=1 / Trigger=2) を <see cref="ActivationType"/> と一致させる。
    /// **enum value 順を絶対に変えないこと** — 既存 SO の数値が壊れる。
    /// </summary>
    public enum ActivationType
    {
        Active,
        Passive,
        Trigger,
    }

    /// <summary>
    /// タスク仕様命名 alias。実体は <see cref="ActivationType"/> と同一の int 値を持つ。
    /// 新規 code から参照する際にどちらを使っても enum cast 可能。
    /// </summary>
    public enum SkillActionType
    {
        Active = ActivationType.Active,
        Passive = ActivationType.Passive,
        Trigger = ActivationType.Trigger,
    }
}
