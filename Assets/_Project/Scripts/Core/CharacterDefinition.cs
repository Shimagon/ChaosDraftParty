using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// プレイヤーキャラクターの見た目定義。Spec v9.5 は 4 色 (赤/青/黄/緑)。
    /// 体型は Synty + Blendshape 豆化 (VISUAL_TARGET B プラン) で全員同一。
    /// </summary>
    [CreateAssetMenu(fileName = "Character_", menuName = "ChaosDraftParty/CharacterDefinition", order = 50)]
    public class CharacterDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ID (例: char_red, char_blue)")] public string id;
        [Tooltip("画面表示名")] public string displayName;

        [Header("Appearance")]
        [Tooltip("メインカラー (Player Material _BaseColor に適用)")] public Color primaryColor = Color.white;
        [Tooltip("サブカラー (しっぽトレイル、パーティクル等)")] public Color secondaryColor = Color.white;
        [Tooltip("Player prefab リファレンス (Synty humanoid + Blendshape 豆化)")] public GameObject prefab;

        [Header("Presentation")]
        public Sprite portraitSprite;
    }
}
