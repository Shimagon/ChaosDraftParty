---
name: EGGYBOT キャラクターデザイン（2026-04-22 確定）
description: ChaosDraftParty のマスコット/プレイヤーキャラ。ガチャガチャカプセル型、2 形態変形、ピンク+クリアプラスチック。仕様書 /キャラクター/ 以下に全情報
type: project
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## キャラクター名
**EGGYBOT**（エッグィボット / ガチャガチャカプセル型マスコット）

## 配置
`/Users/taisei/Documents/Dev/ChaosDraftParty/キャラクター/` 配下:
- `3D生成AI用キャラクタープロンプト.md` — 日英両方の完全仕様書
- `Gemini_Generated_Image_*.png` × 3 — Gemini 生成 3D レンダ風イメージ（Front/Side/Back/Action view + コミカルイラスト）
- `IMG_2323.JPG`, `IMG_2324.JPG` — ユーザー手書きラフスケッチ（2 形態 + 上面俯瞰）

## コアコンセプト
- **ガチャガチャのカプセルトイ** をモチーフ
- **2 形態変形ギミック**:
  - A（通常）: 2 球重ねの雪だるま、頭 pink 球 + 胴 pink+clear 球
  - B（収納）: 頭が胴に沈んで単一カプセル化、透明カバー越しに顔透ける
- 配色: **ピンク (不透明) + クリア (透明)**、完全ツルツルな硬質プラスチック（毛皮・布無し）

## パーツ構造

| パーツ | 形状 | 材質 |
|---|---|---|
| 頭 | 完全球、髪凹凸なし | 不透明ピンク光沢プラ |
| 胴体 | 頭より一回り大きい球、上半透明・下半ピンク | 半透明+不透明の 2 体構造 |
| パーティングライン | 胴体の赤道やや上に水平 1 周する溝 | カプセル上下の分割線 |
| 顔 | 黒小円 2 の目 + U 字スマイル | 頭表面に貼付 |
| 腕 | 胴下半（pink 部）左右に短ずんぐり nub | 指なし、不透明ピンク |
| 脚 | 胴真下に 2 本の短 nub、接地部平 | 足首・足指なし、自立可 |

## Meshy 適合性分析

ユーザー洞察 2026-04-22: 「メッシーは無機物みたいなのに強いのかも？」→ **正解**。EGGYBOT は 100% 工業製品的意匠なので Meshy の得意分野。

| 要素 | Meshy 適合 | 対処 |
|---|---|---|
| 光沢硬質プラ質感 | ◎ Meshy の本領発揮、PBR refine で自動 | meshy-6 + enable_pbr=true |
| ピンクの単色 | ◎ 問題なし | 色指示を prompt に明記 |
| 球・カプセルの幾何 | ◎ 原始形状、topology 安定 | target_polycount 10-15k で十分 |
| **透明/クリア部** | ✕ Meshy は transparency を opaque として出力する | **別メッシュ分離** or Unity 側で別マテ +  _Surface Transparent 手動設定 |
| パーティングライン溝 | △ prompt で明記、出なければ Blender で追加 | "visible horizontal groove around upper body" |
| 腕/脚の短 nub | ○ 単純形状、リグ可能 | meshy_rig で Humanoid は過剰、Generic rig 推奨 |
| 顔のドット目＋U口 | ○ テクスチャ or geometry 両対応 | refine で顔プリントも付く |
| **2 形態変形** | ✕ Meshy は blend shape 出力しない | **Unity 側で 2 meshes + BlendShape or Animator で切替** |

## 実装候補アプローチ（Plan で選定待ち）

### A. 1 ショット生成（透明無視）
meshy-6 preview + refine で A 形態のみ生成 (30 credits)。クリア部分は後で Unity の URP Lit マテリアル Transparent + _Smoothness 0.95 で半透明化。リグは後付け。

### B. 2 メッシュ分離生成
(1) 不透明ピンク部（頭+脚+腕+胴下）1 メッシュ  
(2) 透明カプセル上半分 1 メッシュ  
それぞれ generate → 合成 → 材質分離。60-80 credits だが透明がきれい。

### C. Blender MCP で事前モデリング → Meshy は使わない
ベース形状（球複数）は Blender で組む方が早い（sphere primitive 重ね、ブール加減）。pink part を一体化、clear capsule を別オブジェ。Claude が Blender MCP で直接組める。0 credits。

**俺の推奨: C → A のハイブリッド**。
- 土台ジオメトリは Blender MCP で正確に（キャラ仕様が明確なので）
- ディテール（目・口の凹み、パーティングライン溝）も Blender で
- Meshy で PBR テクスチャ refine 風のリアル質感だけ欲しければ、仕上がり後に retexture API に渡す

### リグ方針
- Full Humanoid は過剰（足首・膝関節無いキャラ）
- **Generic rig の 6 ボーン構成**: root / body / head / arm_L / arm_R / legs（脚 2 本一体でもイイ）
- 変形（A⇄B）は Animator の BlendShape Trigger or 2 prefab 切替で実装
- Mixamo アニメ retarget は難しい（足首が無いため）→ Unity 手書きアニメ推奨

## タスク接続

- Task #19: Blender MCP 導入検討 → EGGYBOT 制作の要
- Task #17: マスコット walking アニメ改善 → EGGYBOT のアニメ設計と統合
- Character SO: 既に 4 色 (red/blue/yellow/green) 作成済 (`Assets/_Project/ScriptableObjects/Characters/`)、EGGYBOT で色替えパターン展開

## 参照順
1. この memory
2. `/キャラクター/3D生成AI用キャラクタープロンプト.md`（一次情報、英語プロンプトそのまま使える）
3. Gemini_Generated_Image_*.png（完成イメージの目視確認）
