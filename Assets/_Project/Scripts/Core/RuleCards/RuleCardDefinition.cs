using UnityEngine;

namespace ChaosDraftParty.Core.RuleCards
{
    /// <summary>
    /// ラウンド開始時に 1 枚ランダム発動する Rule Card。Spec v9.5 で 6 種類。
    /// magnitude の意味はカードごとに異なる (shrink_all=スケール、low_gravity=重力倍率、等)。
    /// </summary>
    [CreateAssetMenu(fileName = "RuleCard_", menuName = "ChaosDraftParty/RuleCardDefinition", order = 20)]
    public class RuleCardDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ID (例: shrink_all, low_gravity)")] public string id;
        [Tooltip("画面表示名")] public string displayName;
        [TextArea(2, 4), Tooltip("効果説明 (UI 表示)")] public string description;

        [Header("Effect")]
        [Tooltip("効果係数 (意味はカード毎: スケール/重力/速度/CD/視界/bounce 度)")] public float magnitude = 1f;
        [Tooltip("距離が関わる場合のみ (fog_of_war の視界 m 等)")] public float radius = 0f;
        [Tooltip("効果持続秒数 (0 = ラウンド全体)")] public float durationSec = 0f;

        [Header("Presentation")]
        public Sprite icon;
        public Color accentColor = Color.white;
    }
}
