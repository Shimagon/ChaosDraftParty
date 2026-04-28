---
name: ChaosDraftParty ビジュアル戦略（Fall Guys 75% + 原神 25%）
description: 3 本の並列リサーチで確定した到達目標ライン、月次ロードマップ、$145 投資プラン、絶対やらない 5 つの穴。詳細は docs/VISUAL_TARGET.md 参照
type: project
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## 結論

**Fall Guys 75% + 原神の彩度 25% ハイブリッド**を M6（2026-09）で達成目標。

**Why:** 原神 100% はソロ 5 ヶ月で絶対不可能（miHoYo 200 人年級、Face SDF だけで週単位の工数）。Fall Guys は party game ジャンルで実在する到達可能な目標。原神の「発色の良さ」「アニメ的彩度」は Volume + Ramp で部分採用。

**How to apply:** Claude が shader / post-process / lighting / VFX の選択をする時、常に「Fall Guys 主 + 原神従」の方針。Face SDF / volumetric cloud / 時間帯 ramp swap / UTS2 / ragdoll 同期は禁則。

## 月次マイルストーン（M1-M6）

| 月 | 到達% | 主タスク | 投資 |
|---|---|---|---|
| **M1 (4月)** 残1週 | 30→45% | URP Volume プリセット + Soft Shadow + Directional Light 暖色化 + Light Probe | $0 |
| **M2 (5月)** | 45→60% | **Feel ($25-50)** で juice 統合、Cinemachine Impulse、PrimeTween UI | $50 |
| **M3 (6月)** | 60→75% | **Flat Kit ($40)** + 自作 Inverted Hull + Synty Blendshape 豆化 + Meshy Pro 本格稼働 | +$40 |
| **M4 (7月)** | 75→82% | **Linework ($50)** + APV 時間帯 3 プロファイル | +$50 |
| **M5 (8月)** | 82→90% | **Beautify 3 ($55)** + カラーゾーニング（4 ミニゲーム別支配色）+ MVP スロモー | +$55 |
| **M6 (9月)** | 90% 維持 | 最適化、Bot Blob Shadow、Steam ビルド | $0 |

**合計投資: $145（見栄え）+ $120（Meshy 6ヶ月）+ $100（Steam Direct）= $365**

## 採用スタック

- Shader: Flat Kit Stylized Lit ($40) + NiloCat OSS で改造
- Outline: 自作 Inverted Hull (M3) → Linework $50 (M4)
- Post: URP Volume 標準 → Beautify 3 $55 (M5)
- Lighting: APV Unity 6 標準 + 時間帯 3 プロファイル
- Juice: Feel $25-50 + PrimeTween 無料 + Cinemachine Impulse
- VFX: VFX Graph + Shuriken + Six Way Shader Graph
- Water: Flat Kit 同梱 water
- Tween: PrimeTween (DOTween より軽量)

## キャラ戦略: Synty + Blendshape 豆化（B プラン）

Blender で Synty humanoid に `torso_fat` / `head_small` / `limb_short` blendshape 追加、Unity で固定適用。Fall Guys らしさ 80%、Mixamo 互換維持、4 色展開は material color swap。工数 3 日。

## 絶対やらない 5 つの穴

1. **Face SDF 顔陰影** — アーティスト手作業週単位、ソロ不可能
2. **Volumetric Cloud** — GPU 高負荷 + ゲームプレイ無関係
3. **時間帯 ramp swap** — 1 ラウンド 90 秒で切替意味なし、ミニゲーム別固定に
4. **Ragdoll 同期** — Fall Guys 開発者も「悪夢」と発言、押し合いローカル傾きで代替
5. **UTS2** — Built-in RP 専用、URP 非対応
6. **キャラを Meshy 生成** — CLAUDE.md Must Not、Synty + Mixamo 固定

## パフォーマンス予算

GTX 1060 / 60fps = 16.6ms、追加 +3.1ms 想定（余裕）。Bot は Cast Shadow=Off + Blob Shadow。Forward+ で 8 Light まで。

## 独自演出（ChaosDraftParty だけのもの）

- ドラフト画面：3 枚カード bouncy + DOF 背景ボケ
- スキル重複 VFX ランクアップ（凹関数 1.0→1.20→1.35→1.45 に対応、銅→銀→金→虹）
- RuleCard 発動で画面 color filter 染め
- カオス度メーター（HUD 右上、ラウンド進行で上昇、最大値でカメラ揺れ）

## 本体ドキュメント

`docs/VISUAL_TARGET.md`（git 管理、両マシン共有、実装タスクチェックリスト込み）
