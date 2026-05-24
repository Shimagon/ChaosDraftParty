using UnityEngine;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島ミニゲームのチューニング値を一括管理する ScriptableObject。
    ///
    /// 設計:
    /// - Manager の個別 SerializeField はそのまま fallback として残す (後方互換)
    /// - SO が assign されていれば SO 値が優先される
    /// - default 値は AAA defacto (Fall Guys Hex-a-gone) を参考
    /// </summary>
    [CreateAssetMenu(menuName = "ChaosDraftParty/Minigames/Shrinking Island Config")]
    public sealed class ShrinkingIslandConfig : ScriptableObject
    {
        [Header("Shrink Schedule")]
        [Tooltip("試合開始から最初の外周消失までの遅延 (秒)。AAA defacto: Fall Guys Hex-a-gone と同じく 5s で緊張感の前置き。")]
        [SerializeField] private float _initialDelay = 5f;

        [Tooltip("各 layer 消失の間隔 (秒)。Synthesizer 推奨 15→10s で Hex-a-gone defacto に寄せる。")]
        [SerializeField] private float _shrinkInterval = 10f;

        [Header("Tile Transition")]
        [Tooltip("Healthy → Warning (赤点滅) → Fading 開始までの期間 (秒)。AAA defacto: 1.0s 警告。")]
        [SerializeField] private float _warningDuration = 1f;

        [Tooltip("Fading 開始 (alpha 1→0) から SetActive(false) までの期間 (秒)。Hex-a-gone 1s に近い 0.5s で即消失感。")]
        [SerializeField] private float _fadeDuration = 0.5f;

        [Header("KO")]
        [Tooltip("Y がこれ未満で KO 判定 (PlayerController.IsAlive=false)。地面 Y=0 + 余裕 -5 推奨。")]
        [SerializeField] private float _koThresholdY = -5f;

        public float InitialDelay => _initialDelay;
        public float ShrinkInterval => _shrinkInterval;
        public float WarningDuration => _warningDuration;
        public float FadeDuration => _fadeDuration;
        public float KoThresholdY => _koThresholdY;
    }
}
