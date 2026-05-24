# 縮む島 v2 — Spelunky 級 polish 仕様

Date: 2026-05-24
Status: in implementation (M2-M3)

Synthesizer 統合判定 + Hex-a-gone 比較 agent の改善提案を全部取り込み、core ミニゲームとして Spelunky 級まで深掘りする仕様。

## 設計理念

- **見た目カオスでも勝つのは上手いやつ** (CDP コンセプト)
- **Hex-a-gone (Fall Guys defacto) を超える 3-axis hybrid**: 縮む島 + 押し出し相撲 + スキルドラフト統合
- **5 秒で理解、3 分で 1 試合、4 ラウンドで深まる**
- **Bot 混合を前提仕様化** (User 不在時の 1 人 playtest 可能、卒研評価期間中の保険)

## ステージ

### 基本構成
- **8×8 = 64 タイル**、各 2m × 0.3m × 2m
- world (0, 0, 0) 中心、X/Z 範囲 ±8m
- タイル prefab: BoxCollider + MeshRenderer (URP マテリアル)
- KillVolume: Y = -5、isTrigger BoxCollider、PlayerController.IsAlive で host 判定 → NetworkVariable 同期

### タイル variant 3 種 (M3 追加)
1. **Normal Tile** — デフォルト、shrink 順序で消える
2. **Bouncy Tile** — 落下時 1 回バウンドで救済 (Comeback 機構)、ラウンド開始時にランダム配置 (2-4 個)
3. **Spike Tile** — 踏むと一瞬で消える + push back (上級者用罠)、ランダム配置 (1-2 個)

## Shrink schedule

### 数値 (Synthesizer 推奨 + Hex-a-gone 寄せ)
- `initialDelay = 5s` (探索期間、緊張感の前置き)
- `shrinkInterval = 10s` (旧 15s → Hex-a-gone defacto に寄せる、緊張感 6.7% → 10%)
- `warningDuration = 1s` (タイル赤点滅、AAA defacto)
- `fadeDuration = 0.5s` (旧 1.0s → 即消失感、Hex-a-gone 1s に合わせる)

### 消失順序 (外周 layer 順)
| Layer | 範囲 | タイル数 | 累計時間 |
|---|---|---|---|
| 0 | 8×8 外周 | 28 | 5s + 0×10s = 5s |
| 1 | 6×6 外周 | 20 | 5s + 1×10s = 15s |
| 2 | 4×4 外周 | 12 | 5s + 2×10s = 25s |
| 3 | 2×2 全部 | 4 | 5s + 3×10s = 35s |
| **合計** | | **64** | **35s + 余韻 1.5s = 36.5s** |

ラウンド 90 秒のうち最初の ~37 秒で全消滅 → 残り 53 秒は「中央 0 タイル + 全員 KO 圏内」の決着フェーズ。

ただし最後に bouncy tile が 2-4 個残るとさらに救済 5-10 秒延命可。

## visual feedback (Synthesizer 必須項目)

### タイル状態 visual
- **Healthy**: 通常色 (白 or skin)
- **Warning** (1s): 赤に lerp 点滅、振動 (subtle scale 0.95-1.0)
- **Fading** (0.5s): alpha 1→0 で fade、collider OFF (踏抜け可)
- **Gone**: SetActive(false)

### SE
- **Warning 開始**: 「カチッ」(警告音、Kenney UI Audio pack)
- **Fade 完了**: 「ザザッ」(崩落音、Kenney Impact)
- **player KO**: 「ぴゅーぅぅ↓」(落下音、Mario defacto)

### Particle
- **Warning 中**: 赤い spark 粒 4-5 個 (subtle)
- **Fade 中**: ダスト粒 (Kenney whitePuff 流用)、Y+0.3 から下に降る

### Camera (KO 時)
- KO 直後 0.5s スロー (Time.timeScale = 0.5)
- 落下方向に slight camera tilt
- 復帰時に「KO」テキスト + 残時間 display

## スキル統合

### M2 で実装する skill 3 種
| skill | カテゴリ | 縮む島での使い方 |
|---|---|---|
| dash | Move | 消失するタイルから即脱出、push 強化 |
| jump_boost | Move | 大穴を飛び越える、push 上から |
| pulse | Hit | 前方の敵を縁ギリギリに押し出す |

### M3-M5 で追加する 7 種
- grapple: 15m 先のタイルへ引き寄せ自己救出 (落下中も可)
- hover: 落下中 1.5s 滞空 → 救済 mode
- bomb: 投擲爆発、敵タイル除去
- shield: 3s 無敵 + 速度-30% (押し出し耐性)
- mirage: 敵の移動入力 2s 反転 (落下誘導)
- radar: 15m 内敵マーカー (terminal phase 必須)
- revive: HP0 で 1 度復活 (1 ラウンド 1 回)

### ドラフト重み付け (Synthesizer 推奨)
従来: 10 種から完全ランダム 3 提示 (combo pair 発生率 3-5%)
改善: **コンボ pair の片方が既に取得済なら、もう片方を pool 重み +30%** → combo pair 発生率 15-20% に上昇

実装: `SkillDraftSystem.SelectCandidates` で重み計算に combo pair table を参照 (M3 着手時)

## Bot 仕様 (Devil's 「正面化」採用)

### 最低スペック (M3 必須)
- NavMeshAgent で外周から内側へ移動 (タイル消失情報を読んで survival 行動)
- 敵が近づいたら回避 (5m 距離維持)
- skill: dash のみ自動発動 (CD 切れたら使う)
- 押し出しは「偶然」のみ (積極的に狙わない、初心者レベル)

### 強化版 (M5、時間あれば)
- skill 3 種使い分け
- 敵の落下狙い (push を意図する)
- 中盤以降の安全タイル予測

### Bot 数の Auto-fill
- 2-4 人未満で参加 → 4 人になるまで Bot 補填
- Bot は EGGYBOT skin の異なる color 版で視認性確保

## Comeback 機構 (Synthesizer 推奨)

### 最下位ボーナス
- ラウンド 2-3 で「累計 score 最下位」プレイヤー: 全 skill CD -20%
- ラウンド 4 で最下位: + Revive skill 自動付与
- これらは「カオスルール」の一部として visual 表示 (透明化しない)

## ラウンド構造との接続

| Phase | 時間 | 内容 |
|---|---|---|
| WarmUp | (試合最初のみ) 10s | scene load + 説明 |
| Countdown | 3s | 「3,2,1, GO!」 |
| Playing | 90s | shrink loop (この仕様書の本体) |
| Ending | 3s | KO 全員確定 + 順位演出 |
| Draft | 5s | 次ラウンド skill 選択 |
| Completed | (次へ) | |

各ラウンドで `ShrinkingIslandManager.ResetIsland()` で全 Tile を Healthy に戻し、coroutine 再起動。

## 数値定数 (ScriptableObject に集約)

```csharp
[CreateAssetMenu(menuName = "ChaosDraftParty/Minigames/ShrinkingIslandConfig")]
public sealed class ShrinkingIslandConfig : ScriptableObject
{
    [Header("Shrink")]
    public float initialDelay = 5f;
    public float shrinkInterval = 10f;
    public float warningDuration = 1f;
    public float fadeDuration = 0.5f;

    [Header("KO")]
    public float koThresholdY = -5f;
    public float koSlowMotionDuration = 0.5f;
    public float koSlowMotionScale = 0.5f;

    [Header("Variant")]
    public int bouncyTileCount = 3;
    public int spikeTileCount = 1;

    [Header("Comeback")]
    public float lowRankSkillCdReduction = 0.2f; // -20%
}
```

→ M3 で `Assets/_Project/ScriptableObjects/Configs/ShrinkingIslandConfig.asset` 作成

## 期限 (User judgement)

- **5/31 (M2 末)**: 8×8 タイル + shrink basic + 2 人 KO 動作 demo
- **6/15**: Bot + ドラフト重み付け + Comeback 機構 基本
- **6/30 中間 playtest**: 友人 2 人 + 教授に「面白いか」評価依頼 ← **判定 C plan B 発動分水嶺**
- **7/31 (M4 末)**: タイル variant 3 種 + tutorial 完成
- **8/31 (M5 末)**: 全 polish + balance 確定
- **9/30 (M6 末)**: Steam build + 卒業発表準備

## 関連
- `Assets/_Project/Scripts/Minigames/ShrinkingIsland/ShrinkingIslandManager.cs` (本日作成)
- `Assets/_Project/Scripts/Minigames/ShrinkingIsland/Tile.cs` (本日更新、後方互換維持)
- `Assets/_Project/Scenes/Sandbox_ShrinkingIsland_v1.unity` (本日作成予定)
- `docs/decisions/2026-05-24_scope_decision.md` (議論結果サマリ)
- `docs/handoffs/ShrinkingIsland_M1_setup_guide.md` (旧 setup guide、本仕様で上書き)
