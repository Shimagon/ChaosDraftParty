using UnityEngine;

namespace ChaosDraftParty.Core.Season
{
    /// <summary>
    /// シーズン（3 試合）を通して発動する特殊ルール。Spec v9.5 で 3 種類。
    /// 通常は 3 試合目に発動して物語を作る (winner_takes_all 等)。
    /// </summary>
    [CreateAssetMenu(fileName = "SeasonRule_", menuName = "ChaosDraftParty/SeasonRuleDefinition", order = 40)]
    public class SeasonRuleDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ID (例: winner_takes_all)")] public string id;
        [Tooltip("画面表示名")] public string displayName;
        [TextArea(2, 5), Tooltip("効果説明")] public string description;

        [Header("Activation")]
        [Tooltip("何試合目に発動するか (0-indexed、spec v9.5 は 3 試合目=2)")] public int activationMatchIndex = 2;

        [Header("Effect")]
        [Tooltip("効果係数 (意味はカード毎: reverse_multiplier の上位倍率等)")] public float magnitude = 1f;
        [Tooltip("副係数 (reverse_multiplier の下位倍率等、必要な場合のみ)")] public float secondaryMagnitude = 1f;

        [Header("Presentation")]
        public Sprite icon;
        public Color accentColor = Color.yellow;
    }
}
