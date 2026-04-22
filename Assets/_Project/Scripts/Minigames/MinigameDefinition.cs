using UnityEngine;

namespace ChaosDraftParty.Minigames
{
    public enum MinigameVictoryCondition
    {
        /// <summary>最後の 1 人生存で勝利 (縮む島・しっぽ狩り逃走)</summary>
        LastSurvivor,
        /// <summary>時間終了時スコア最多 (しっぽ狩りバトル・玉入れ)</summary>
        HighScore,
        /// <summary>最後に爆弾を持っていた人が敗北 (爆弾鬼)</summary>
        HotPotato,
    }

    /// <summary>
    /// 1 ラウンドで実行するミニゲームのメタデータ。Spec v9.5 準拠、コア 4 種 + ストレッチ 1 種。
    /// Scene を Additive ロードして使う想定。
    /// </summary>
    [CreateAssetMenu(fileName = "Minigame_", menuName = "ChaosDraftParty/MinigameDefinition", order = 30)]
    public class MinigameDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("ID (例: shrinking_island, bomb_tag)")] public string id;
        [Tooltip("画面表示名")] public string displayName;
        [TextArea(2, 5), Tooltip("ルール説明 (ドラフト画面等で表示)")] public string description;

        [Header("Rules")]
        public MinigameVictoryCondition victoryCondition;
        [Tooltip("ラウンド制限時間 (秒、spec v9.5 は 90s 固定)")] public float durationSeconds = 90f;
        [Tooltip("カウントダウン (秒、spec v9.5 は 3s)")] public float countdownSeconds = 3f;
        [Tooltip("終了演出時間 (秒、spec v9.5 は 3s)")] public float endingSeconds = 3f;
        [Tooltip("最小プレイヤー数")] public int minPlayers = 2;
        [Tooltip("最大プレイヤー数")] public int maxPlayers = 4;

        [Header("Scene / Loading")]
        [Tooltip("Additive ロードするシーン名 (例: ShrinkingIslandProto)")] public string sceneName;

        [Header("Requirements")]
        [Tooltip("NPC 配達員等を必要とするか (bomb_tag で NavMesh 要)")] public bool requiresNPC = false;
        [Tooltip("物理同期を必要とするか (bomb_tag の爆弾のみ true 推奨)")] public bool requiresPhysicsSync = false;

        [Header("Presentation")]
        public Sprite icon;
        public Color themeColor = Color.white;
    }
}
