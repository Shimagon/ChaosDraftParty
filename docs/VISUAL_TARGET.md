# VISUAL_TARGET.md — ChaosDraftParty ビジュアル戦略

> 3 本の並列リサーチ（原神分析 / Fall Guys 分析 / URP スタック調査）を統合して、
> **5 ヶ月ソロ卒業制作で到達可能な最高品質のビジュアル**を数字付きで定義する。
> 実装は CLAUDE.md / TODO.md / meshy-unity-workflow skill と連動。

---

## 0. 結論（3行）

1. **原神 100% は絶対不可能**（miHoYo 200 人年 + 専用エンジン）。ソロ 5 ヶ月の現実ライン：**Fall Guys 75% + 原神の彩度 25% のハイブリッド**
2. 技術軸は「**Flat Kit Stylized Lit + 自作 Inverted Hull Outline + APV + URP Volume プリセット + Feel ジュース**」の 5 点盛り
3. 総投資 **$145**（Flat Kit $40 + Linework $50 + Beautify 3 $55） で卒業発表クオリティ到達

---

## 1. 目標スタイルの位置づけ

ChaosDraftParty のジャンルはパーティゲーム（TPS 引きカメラ、4 人同時、1 試合 8 分）。
原神のようなアニメ寄り cel shading は**ジャンル的にむしろマイナス**になる（serious 過ぎる）。

**採用スタイル：Fall Guys 寄り + 原神の「発色の良さ」混合**

| 成分 | Fall Guys から | 原神から |
|---|---|---|
| キャラ | jellybean 体型、非脅威性、色パレット豊富 | — |
| ライティング | Baked GI + 暖色 1 点 + 柔らかい影 | Ramp 駆動の彩度（ごく控えめに） |
| 質感 | Soft PBR（URP Lit 改 or Flat Kit Stylized Lit） | — |
| アウトライン | **採用しない**（Fall Guys は非トゥーン） | いや、ほんの少し入れる（カオス演出） |
| post | Bloom + Neutral tonemap + 彩度 +15 | ACES ではなく Neutral（原神は暗部沈むので不採用） |
| 動き | juice 絶対主義（Feel 導入） | MVP スロモー |
| 色 | ステージごと支配色ゾーニング | — |

**補足**：**本家 Fall Guys はトゥーンを使っていない**。原神研究の成果は「Ramp 作成の参考」「アウトライン演出の選択肢」として**部分流用**のみ。主軸は Fall Guys のソフト PBR。

---

## 2. 到達レベル定義

| レベル | 内容 | ソロ 5 ヶ月 | 判定 |
|---|---|---|---|
| 100% 原神 / Fall Guys | Face SDF、時間帯ramp、volumetric 雲、ragdoll 同期 | 不可能 | — |
| 90% | Flat Kit + Linework + Beautify 3 + APV + Feel + カラーゾーニング | $145 + 2 ヶ月集中 | **目標ライン** |
| 75% | Flat Kit + 自作アウトライン + URP 標準 Volume + Feel | $40 + 1.5 ヶ月 | 最低発表ライン |
| 60% | OSS シェーダ（NiloCat / Delt06） + URP 標準 | $0 + 1 ヶ月 | M3 中盤到達 |
| 40% | URP 標準 Lit + Bloom のみ | $0 + 1 週 | M2 時点 |
| 30% | URP 標準のまま | — | 現在地 |

**M6（卒業発表） = 90%** を目標、M5 時点で 80% に到達していれば発表品質 OK。

---

## 3. 月次実装ロードマップ

### M1（4月末、残り 1 週間）— 湿度仕込み（2 日で 30→45%）
**ROI 最大の作業**。見た目が一気に変わる。

- [ ] `Assets/_Project/Scenes/Boot.unity` に **Global Post-Process Volume** を置いて `DontDestroyOnLoad`
- [ ] Volume プリセット：
  - Bloom: Threshold 0.9 / Intensity 0.5 / Scatter 0.7
  - Color Adjustments: Post Exposure +0.2 / Contrast +10 / Saturation +15
  - Vignette: Intensity 0.25 / Smoothness 0.35
  - Tonemapping: **Neutral**（ACES は暗部が沈む、パーティゲーム不向き）
  - White Balance: Temperature +5（暖色寄り）
- [ ] URP Asset: Soft Shadow ON / Cascade 2 / Shadow Distance 50m
- [ ] Directional Light: 暖色 `#FFE8C8` / Intensity 1.2
- [ ] Boot シーンに **Light Probe Group** 散布（試合エリアに 8-12 個）
- [ ] `manage_graphics` で Volume プロファイル SO として git 管理

### M2（5月）— スキル基盤 + Feel 導入で juice（45→60%）

- [ ] **Feel ($25-50) 購入** & 導入（Asset Store）
- [ ] MMFeedbacks テンプレ 5 種作成：着地 / スキル発動 / ヒット / ドラフト選択 / スコア加算
- [ ] Cinemachine 3 **Impulse Source** を爆弾・着地・被弾に
- [ ] Cinemachine **Noise Profile: Handheld** を常時微震 (Amplitude 0.1, Frequency 0.5)
- [ ] UI Toolkit の score popup に **PrimeTween**（無料）で Scale 1.0→1.2→1.0 ease OutBack 0.3s
- [ ] ドラフトカード選択時の tween も PrimeTween で

### M3（6月）— Toon Shader 導入で発色 UP（60→75%）

- [ ] **Flat Kit ($40) 購入** & 導入
- [ ] Synty POLYGON キャラの Material を **Stylized Lit** に一括差替（Script で自動化）
- [ ] Ramp テクスチャ 3 種作成（cool/warm/vivid）
- [ ] **Synty + Blendshape 豆化**（B プラン）：Blender で Synty humanoid に `torso_fat` / `head_small` blendshape 追加、Unity で `SkinnedMeshRenderer.SetBlendShapeWeight` で適用
- [ ] **自作 Inverted Hull Outline**：RenderObjects Feature + 反転法線マテリアル、頂点カラーで太さ制御（キャラ 4 体 × DrawCall +1 = ±0.3ms 予算内）
- [ ] **Meshy Pro ($20/月) を本格稼働**（既に契約済、M3 から credit 大量消費開始）
- [ ] ステージ小物 20 個バッチ生成（`meshy-unity-workflow` skill 参照、~600 credits）

### M4（7月）— アウトライン品質 UP + APV（75→82%）

- [ ] **Linework ($50) 購入**（URP Render Graph ネイティブ対応、Unity 6 最適化済）
- [ ] Fast / Soft / Wide 3 種から場面別に使い分け（UI 時は Wide、ゲーム中は Fast）
- [ ] **Adaptive Probe Volumes (APV)** 設定：試合エリアに Volume 配置、3 プロファイル（昼 / 夕 / 夜）作成
- [ ] ミニゲーム別に APV Scenario Blend で色温度切替（縮む島=夕、爆弾鬼=昼、しっぽ狩り=夜）

### M5（8月）— カラーゾーニング + Beautify 3（82→90%）

- [ ] **Beautify 3 ($55) 購入**（Bloom/Flare/DOF/AO/CA 全部入り、Unity Awards ノミネート）
- [ ] 4 ミニゲームそれぞれに支配色 1 + アクセント 1 決定、Volume override で切替：
  - **縮む島** → アクアブルー `#9BDCE5` / 白アクセント
  - **爆弾鬼** → サーモン `#FF9B7A` / 黄アクセント
  - **しっぽ狩りバトル** → ライム `#BFFF6E` / マゼンタアクセント
  - **しっぽ狩り逃走** → ラベンダー `#C8A2FF` / オレンジアクセント
- [ ] MVP スロモー実装：`Time.timeScale = 0.4` × 1.5s + Cinemachine DollyTrack で被写体周回
- [ ] Confetti VFX（VFX Graph、5000 particle、2 秒、勝利演出）
- [ ] **ドラフト画面のみ DOF** Volume override

### M6（9月）— 最終調整（90%維持 + 最適化）

- [ ] プラットフォーム切替テスト（Mac / Windows）
- [ ] GTX 1060 60fps 予算維持確認（16.6ms 予算、実測 +4.9ms でまだ余裕あるはず）
- [ ] **Bot 用 Blob Shadow**（下に暗い円板テクスチャ、Cascade 負荷削減）
- [ ] Steam ビルド最終クオリティ調整

---

## 4. 絶対やらない 5 つの穴

| 罠 | 理由 |
|---|---|
| **Face SDF 顔陰影** | アーティストが顔SDFを手描きで週単位、ソロ不可能。NdotL + ramp で 95% の絵は作れる |
| **Volumetric Cloud** | URP 標準なし、raymarch は GPU 高負荷、ゲームプレイと無関係 |
| **時間帯 ramp swap** | パーティゲームは 1 ラウンド 90 秒、時間帯切替は意味なし。**ミニゲーム別固定** |
| **Ragdoll 同期** | Fall Guys 開発者自身が Unreal フォーラムで「悪夢」と発言。押し合いは**ローカル傾き**で代替 |
| **UTS2 採用** | Built-in RP 専用で URP 非対応、Unity 6 で完全に選定外 |
| **ragdoll 同期を卒業制作で追求** | HANDOFF の Must Not にも書いた、時間が溶ける |
| **キャラを Meshy で生成** | CLAUDE.md Must Not、rig ズレで M2 全体が溶ける |

---

## 5. 総投資プラン ($145)

| 月 | アセット | 価格 | 累計 |
|---|---|---|---|
| M2 | Feel | $25-50 | $25-50 |
| M3 | Flat Kit | $40 | $65-90 |
| M4 | Linework | $50 | $115-140 |
| M5 | Beautify 3 | $55 | $170-195 |
| — | PrimeTween | 無料 | — |
| 既存 | Synty POLYGON (既購入) | — | — |
| 既存 | Meshy Pro ($20/月 × 6) | $120 | **$290-315** |
| 既存 | Steamworks Direct (M6) | $100 | — |

**見栄え専用コスト: $145-195**（セール期狙うと $100 以下も）
**ChaosDraftParty 全体投資: ~$300**（Meshy 含む）

---

## 6. 推奨 OSS スタック（アセット買う前の土台）

有料アセット買えない場合の無料構成：

| 役割 | OSS | ライセンス | ★ |
|---|---|---|---|
| Toon Lit | [ColinLeung-NiloCat/UnityURPToonLitShaderExample](https://github.com/ColinLeung-NiloCat/UnityURPToonLitShaderExample) | MIT | HLSL 直書きで改造容易 |
| Outline | [Delt06/urp-toon-shader](https://github.com/Delt06/urp-toon-shader) | MIT | 454 |
| Genshin ramp | [NoiRC256/URPSimpleGenshinShaders](https://github.com/NoiRC256/URPSimpleGenshinShaders) | MIT | 顔 SDF 実装参考 |
| Synty × URP トゥーン | [eviltwo/PolygonURPShaderForSynty](https://github.com/eviltwo/PolygonURPShaderForSynty) | MIT | Synty マテリアル変換 |
| Tween | [KyryloKuzyk/PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) | MIT | DOTween より軽量 |

OSS のみで **60% ライン到達可能**、ただし M3 で Flat Kit に課金した方が 2 週間節約。

---

## 7. キャラ体型戦略 — Synty + Blendshape で豆化（B プラン）

| | A: Synty そのまま | **B: Synty + Blendshape 豆化** | C: Meshy 生成 | D: フル自作 |
|---|---|---|---|---|
| 工数 | 0.5d | **3d** | 5d | 14d |
| Mixamo 互換 | ○ | **○** | △ | △ |
| 4 色展開 | material swap | **material swap** | 再生成 × 4 | Substance |
| Fall Guys らしさ | 60% | **80%** | 75% | 90% |

**B を採用**。Blender で Synty humanoid に `torso_fat` / `head_small` / `limb_short` の 3 blendshape 追加、Unity で SkinnedMeshRenderer.SetBlendShapeWeight(0, 1.0f) で固定。**ChaosDraftParty はパーティゲームなので全員同一体型 OK**、プレイヤー識別は色のみ。

---

## 8. パフォーマンス予算（GTX 1060 / 60fps = 16.6ms）

| 機能 | +ms | 採用 |
|---|---|---|
| Flat Kit Stylized Lit | ±0 | ○ |
| Inverted Hull Outline (×4 キャラ) | +0.3 | ○ |
| Linework Wide Outline | +0.6 | ○ |
| Beautify 3 全部入り | +1.2 | ○ |
| SSAO | +0.8 | ○ |
| APV | +0.2 | ○ |
| **合計追加** | **+3.1** | 余裕 |

**Bot 用最適化**：Cast Shadow = Off + Blob Shadow 代替、Cascade 負荷削減。

---

## 9. ChaosDraftParty 的ビジュアルの特徴（これが独自性）

Fall Guys や原神には **ない** 独自演出：

- **ドラフト画面**：3 枚カードが手前にせり出し、PrimeTween で bouncy、DOF で背景ボケ
- **スキル重複強化**：凹関数倍率（1.0→1.20→1.35→1.45）に応じて VFX がランクアップ（銅→銀→金→虹）
- **RuleCard 発動演出**：画面全体が一瞬 color filter で染まる（`low_gravity` なら青紫、`high_speed` なら赤）
- **カオス度メーター**：HUD 右上、ラウンド進行で上昇、最大値でカメラ揺れ強化

これらは **Juice レイヤ（Feel + Cinemachine Impulse）で実装**、シェーダ難易度は低い。

---

## 10. 参考資料（Bookmark 推奨）

### 原神分析
- [Adrian Mendez — Genshin Impact Character Shader Breakdown [Unity URP]](https://adrianmendez.artstation.com/projects/wJZ4Gg) — **URP 前提完全解剖**
- [miHoYo — From mobile to high-end PC (YouTube)](https://www.youtube.com/watch?v=egHSE0dpWRw) — miHoYo 公式講演
- [Daniel Ilett — Cel-shading in Shader Graph and URP](https://danielilett.com/2020-03-21-tut5-1-urp-cel-shading/)
- [festivities/PrimoToon](https://github.com/festivities/PrimoToon) — Genshin 再現（Built-in）、アルゴリズム参考

### Fall Guys 分析
- [Mediatonic Fall Guys Art Blast — ArtStation](https://magazine.artstation.com/2020/10/mediatonic-games-fall-guys-art-blast/)
- [Fall Guys Level Design — Josh Hardy](https://www.joshhardy.co.uk/post/fall-guys-level-design-techniques)
- [Fall Guys Ragdoll Physics Replication — Unreal Forum](https://forums.unrealengine.com/t/fall-guys-ragdoll-physics-replication/475445)

### Unity 6 URP 公式
- [URP for advanced creators (Unity 6)](https://unity.com/resources/introduction-to-urp-advanced-creators-unity-6)
- [Shaders and VFX with URP](https://unity.com/resources/create-shaders-visual-effects-urp-unity-6)
- [Custom Renderer Features (Cyanilux)](https://www.cyanilux.com/tutorials/custom-renderer-features/)
- [Adaptive Probe Volumes (Unity Learn)](https://learn.unity.com/course/unity-6-new-rendering-features/tutorial/adaptive-probe-volumes?version=6.0)

### アセット購入先
- [Flat Kit — Asset Store](https://assetstore.unity.com/packages/vfx/shaders/flat-kit-toon-shading-and-water-143368)
- [Linework — Asset Store](https://assetstore.unity.com/packages/vfx/shaders/linework-outlines-and-edge-detection-294140)
- [Beautify 3 — Asset Store](https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/beautify-3-advanced-post-processing-233073)
- [Feel — Asset Store](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370)

---

## 11. Unity 臭 / AI 臭を消す戦略

ユーザーの要請（2026-04-22）：「Unity 臭 / AI 臭を極力感じさせたくない」
本節は生成物が「Unity Asset Store 詰め合わせ」にも「AI 生成品」にも見えないための**7 手 + 固有 signature**。

### Unity 臭の典型原因（撤去対象）

1. デフォルト Skybox（青空 Procedural Skybox）
2. URP Lit プラスチック質感
3. LiberationSans（TMP デフォルト）
4. グレー UGUI、9-slice 立体ボタン
5. OS 標準カーソル
6. Synty マテリアルそのまま
7. Unity スプラッシュ（Personal/学生は義務）
8. Cinemachine 初期値（damping 標準、noise なし）
9. グリッド配置（Snap で全部きっちり）
10. Standard Particle プリセット

### AI 臭の典型原因（撤去対象）

1. 対称性（Meshy の symmetry_mode: auto が強制対称）
2. 均一 weathering
3. Texture 反復パターン
4. 色パレット不統一（プロンプトごとバラバラ）
5. プロンプトバイアス（"stylized" "game asset" 多用）
6. PBR 過剰主張
7. ポリフロー汚（edge loop 不在）
8. 背景と小物の質感差

### 両方消す 7 手（$0 で効く）

**手1. カスタム LUT で画面全体を「自分の色」に**
- 8x8 LUT を Photoshop で 1 枚作成（warm shadows / cool highlights / 低彩度）
- URP Volume `Color Lookup` で全シーン適用
- **これだけで 9 割のゲームが「そのスタジオの色」に変わる**

**手2. Skybox を自前で**
- Meshy `text_to_image` 3cr で空 1 枚生成 → Procedural Skybox と差替
- または Gradient Skybox Shader Graph 自作（5 分）
- **デフォルト青空だけで Unity 臭の 30%**

**手3. Meshy 生成物の AI 臭消し（4 ステップ）**
```
① 同一プロンプト 3 回生成 → バリエから 1 つ厳選（対称性の偶然退避）
② symmetry_mode: "off" 明示（デフォルト auto は強制対称）
③ Unity 側で Decal 1-2 枚貼る（手書き落書き、擦れ、色差）→ 局所非対称
④ カスタム LUT 通す → project 色に統一
```
ポリフロー問題は手動リトポ非現実、カメラから見える面だけ気にすれば十分

**手4. フォントと UI の個性化**
- 英語フォント: **`Outfit`**（SIL OFL）or `Titan One`（無料）、LiberationSans 絶対使わない
- UI Toolkit: `border-radius: 12-16px` + drop-shadow + text color gradient
- カーソル: 16×16 pixel custom、`Cursor.SetCursor`

**手5. Cinemachine に独自の香り**
- 全 camera に Basic Multi Channel Perlin（Handheld Profile、Amplitude 0.2、Frequency 0.5）薄く常時
- Damping 標準 0.8 を **0.5（きびきび） or 1.2（手持ち風）** にずらす
- Blend カーブを **Animation Curve editor で手動 2 点足し**、Ease In-Out Cubic 避ける

**手6. ポスト処理で「空気」作成**
- Lens Flare 微弱（強光源全てに）
- Film Grain 0.15（AI 生成の滑らかさ隠し）
- Chromatic Aberration 0.1（縁が虹色、Unity 臭消し）
- 暖色 Vignette 0.25（画面端引き締め）

**手7. 手作業揺らぎ（ハンドメイド感）**
- オブジェクト配置 Snap OFF、position.y に ±0.01-0.03 ランダム
- rotation に ±2° 揺らぎ
- `execute_code` で一括 5 分

### ChaosDraftParty 独自 Signature（Unity/AI 臭より「このゲーム独自感」）

| 演出 | 発動条件 | 実装 |
|---|---|---|
| スキル発動 → 0.15s モーションブラー + CA +0.3 | 発動 instant | Volume override 瞬時切替 |
| RuleCard 発動 → 画面全体が color filter で 0.3s 染まる | RuleCard reveal | Color Lookup LUT 瞬時変更 |
| カオス度メーター MAX → カメラ揺れ + vignette 強化 | ラウンド終盤 | Cinemachine Impulse + Volume transition |
| ドラフト画面 → 背景 DOF + 前景スロー | ドラフト 5 秒 | `Time.timeScale = 0.7` + CinemachineDoF |
| MVP スロモー → アルファグロー輪郭 + フラッシュ | 試合終了 | カスタム Render Feature 1 回限り |

### 月次追加タスク（既存計画に織込み済）

- **M1 残週 +0.5 日**: Skybox Gradient 差替、カーソル 16×16、Outfit 英フォント
- **M3 +1 日**: カスタム LUT 作成、Meshy `symmetry_mode: off` 化、3 回生成ワークフロー確立
- **M4 +0.5 日**: Cinemachine Perlin 全 camera、Film Grain 0.15 + CA 0.1
- **M5 +1 日**: Signature 演出 5 種、Meshy 生成物への Decal 追加

**追加コスト $0**、工数 +3 日（全体計画内で吸収）

---

**初版**: 2026-04-22
**最終レビュー予定**: M2 着手時（2026-05-01）、Feel 導入時に更新
**§11 追記**: 2026-04-22 Unity 臭/AI 臭消し戦略、ユーザー明示要求
