using UnityEngine;

namespace ChaosDraftParty.Skills
{
    public enum SkillCategory
    {
        Move,    // ダッシュ、ジャンプ、ロープ、ホバー
        Hit,     // パルス、ボム、盾、ミラージュ
        Sense,   // レーダー、復活
    }

    public enum ActivationType
    {
        Active,   // ボタン押下で発動、CD あり
        Passive,  // 常時発動
        Trigger,  // 条件で自動発動 (例: HP0 で Revive)
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "ChaosDraftParty/SkillDefinition", order = 10)]
    public class SkillDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("スキル固有 ID (例: dash, bomb, revive)")] public string id;
        [Tooltip("表示名")] public string displayName;
        [TextArea(2, 4), Tooltip("効果説明 (UI 表示用)")] public string description;

        [Header("Classification")]
        public SkillCategory category;
        public ActivationType activation;

        [Header("Timing")]
        [Tooltip("クールダウン (秒、Active のみ)")] public float cooldownSec = 5f;
        [Tooltip("効果持続時間 (秒、0 = 瞬間)")] public float durationSec = 0f;

        [Header("Balance")]
        [Tooltip("数値係数 (速度倍率、ダメージ等、効果毎の意味はサブクラス)")] public float magnitude = 1f;
        [Tooltip("範囲 (m、命中判定等で使用)")] public float radius = 0f;

        [Header("Presentation")]
        public Sprite icon;
        public GameObject vfxPrefab;
        public AudioClip sfxActivate;
    }
}
