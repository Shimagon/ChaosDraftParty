using UnityEngine;

namespace ChaosDraftParty.Skills
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "ChaosDraftParty/SkillDefinition", order = 10)]
    public sealed class SkillDefinition : ScriptableObject
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
        [Tooltip("数値係数 (速度倍率、ダメージ等、効果毎の意味は ISkillEffect 実装側)")] public float magnitude = 1f;
        [Tooltip("範囲 (m、命中判定等で使用)")] public float radius = 0f;

        [Header("Presentation")]
        public Sprite icon;
        public GameObject vfxPrefab;
        public AudioClip sfxActivate;

        [Header("Effect (polymorphic)")]
        [Tooltip("ポリモーフィック効果実装。null の skill は CD / display のみ (M3 で実装予定の物含む)。")]
        [SerializeReference] private ISkillEffect _effect;

        /// <summary>ポリモーフィック効果 ref (M3 以降に逐次拡張)。null の場合は noop。</summary>
        public ISkillEffect Effect => _effect;
    }
}
