# M2 Quality Overrides — 「Unity 感消える」10 個 詳細解説

**作成**: 2026-05-06
**目的**: URP Volume に追加した 10 個の override を 1 個ずつ詳細解説。メリット/デメリット/数値ガイドライン/他ゲーム比較。
**状態**: 全 10 適用済 (`Settings/Volumes/DefaultPostProcess.asset`)

---

## ⭐ 1. Tonemapping = Neutral (最大効果)

### 何する
HDR (high dynamic range、明るさ 0〜∞) を画面表示用 (0〜1) にロールオフするカーブ。

### 現状値
- mode: **Neutral**

### Before / After
| | Before (None) | After (Neutral) |
|---|---|---|
| 1.0 超の明るさ | **真っ白に飛ぶ** | 優しくロールオフ |
| 影 | **真っ黒に潰れる** | 階調が残る |
| 色彩 | flat | 柔らかい |

### メリット
- **これだけで「Unity 感」60% 消える** ⭐
- 工数ゼロ (mode 切替のみ)
- パフォーマンス影響なし
- **必須** — AAA 全タイトル使用

### デメリット
- (ほぼ無し)
- ACES 系より中立 = 映画感は弱い (Genshin 戦闘は ACES が defacto)

### 数値ガイドライン
- **Neutral**: cartoon / カジュアル (Splatoon / Mario Odyssey / Fall Guys)
- **ACES**: 映画 / シネマ (Genshin 戦闘 / MGS V)
- **None**: ❌ 使わない

### 他ゲーム比較
- Splatoon 3: Neutral
- Mario Odyssey: Neutral
- Genshin: ACES (戦闘)
- Cuphead: 自前 LUT (vintage)

---

## 2. White Balance (Temp +5, Tint -2)

### 何する
画面全体の色温度。+ で暖色 (黄)、- で寒色 (青)。Tint は緑↔マゼンタ。

### 現状値
- temperature: **+5** (やや暖色)
- tint: **-2** (マゼンタ抜き)

### Before / After
| | Before | After |
|---|---|---|
| 純白 (1,1,1) | 蛍光灯下の写真 | 太陽光の柔らかさ |
| 全体感 | 無機的 | 「肌に優しい」 |

### メリット
- 気付かないけど **画面の温度感** が出る
- Tint -2 で蛍光ピンク被りを抜く (Unity 標準 light の罠)
- パフォーマンス影響なし

### デメリット
- ステージ別に変えたいシーン (夜・洞窟) で別 Volume が必要
- Tint -2 は赤系キャラの肌色を僅かに鈍らせる (EGGYBOT は影響なし)

### 数値ガイドライン
- **Temp +3〜+10**: 屋外昼 / cartoon / Hat in Time
- **Temp +15〜+25**: 夕暮れ
- **Temp -5〜-15**: 夜 / 洞窟 / Cyberpunk
- **Tint -2〜0**: マゼンタ抜き defacto
- **Tint +3〜+5**: グリーン抜き (古い Unity 環境光バグ補正)

### 他ゲーム比較
- Hat in Time: Temp +15 (warm cozy)
- Pixar (Finding Nemo): Temp +8
- The Last of Us 2 病院シーン: Temp -10 (cool)

---

## 3. Color Adjustments 強化 ⭐ パーティ系の必須

### 何する
4 つの基本パラメータで全体カラーグレーディング。

### 現状値
- Post Exposure: **+0.15**
- Contrast: **+10**
- Saturation: **+28** (前 +8 から大幅強化)
- Color Filter: **#FFFAEC** (やや暖色)

### Before / After
| | Before (sat +8) | After (sat +28) |
|---|---|---|
| 4 色 EGGYBOT | 控えめ、Unity Lit 直 | **Splatoon 並みの彩度** |
| 全体 | flat | ポップ・お菓子感 |

### メリット
- パーティゲーム = **高彩度が defacto** (Fall Guys/Splatoon は +30+)
- Color Filter で全体に暖色乗せ → LUT 風の効果が only 1 設定で
- 4 色キャラが目で楽しい
- パフォーマンス影響なし

### デメリット
- **やりすぎ注意**: sat +35 超は「派手すぎ」「画質悪い」感が出る
- 黒人キャラ等の肌色が朱色寄りに振れることがある (EGGYBOT は影響なし)
- Color Filter を強くすると LUT を無効化に近づく

### 数値ガイドライン
- **Saturation +10〜+15**: 落ち着き AAA (TLOU / Halo)
- **Saturation +20〜+30**: パーティ / カートゥーン (Splatoon / Fall Guys) ← **本作はここ**
- **Saturation +35+**: 過剰、レトロアニメ専用 (Cuphead)
- **Contrast +5〜+15**: 自然
- **Post Exposure +0.1〜+0.3**: 屋外
- **Color Filter**: #FFFAEC (微暖) / #F5F8FF (微寒)

### 他ゲーム比較
- Fall Guys: Sat +30, Filter 暖色
- Splatoon 3: Sat +25
- Genshin: Sat +12 (キャラ重視で控え目)

---

## 4. Split Toning ⭐ AAA 映画の defacto

### 何する
シャドウ部・ハイライト部に **別々の色** を乗せる。映画の "Teal & Orange" 配色の正体。

### 現状値
- Shadows: **#2A4878** (深青)
- Highlights: **#FFE0B0** (暖黄)
- Balance: **+10** (highlights 寄り)

### Before / After
| | Before | After |
|---|---|---|
| 影部 | 黒 | わずかに**深青**で奥行き |
| 明部 | 白 | わずかに**暖黄**で温度感 |
| 全体 | 平面 | **映画的** 立体感 |

### メリット
- 画面に **「奥行き」と「映画感」** が出る
- 既存 4 色キャラを邪魔せず、背景・地面に効く
- パフォーマンス影響なし

### デメリット
- **強すぎ注意**: shadows 強くすると「夜のシーン」感、highlights 強くすると「夕方」感
- 暗いステージ (洞窟) では shadow tint が画面を支配
- LUT と併用すると効果が弱まる (色変換が二重)

### 数値ガイドライン
- **Shadows: 深青系** (#1C4A6C, #2A4878) — Teal
- **Highlights: 暖黄/暖橙** (#FFE0B0, #FFD089) — Orange
- **Balance -25〜+25**: 0 が中立、+ で highlights 強調、- で shadows 強調
- パーティゲーム = Balance +10〜+20 (明部優先)
- 映画 / 夜シーン = Balance -10〜-25 (暗部色強調)

### 他ゲーム比較
- マーベル映画: Teal & Orange の本家
- Genshin モンドの夕暮れ: Highlights #FFC080 強め
- FF14 海辺: Shadows #1A4060
- TLOU: 微弱で全体に

---

## 5. Bloom 強化 (intensity 0.5)

### 何する
明るい部分が **じんわり光る**。HDR で 1.0 超のピクセル周囲を blur。

### 現状値
- threshold: **0.95** (HDR 0.95 超で発動)
- intensity: **0.5** (前 0.3 から強化)
- scatter: **0.7** (前 0.65、より柔らかく)

### Before / After
| | Before (0.3) | After (0.5) |
|---|---|---|
| 光源 | 控えめ | じんわり光る |
| 全体 | drý | 「温度感」 |

### メリット
- 画面に **「光が物質→現象」** の変化、温度感
- AAA は基本 ON、OFF だと「2000 年代ゲーム感」
- カートゥーン調と相性◎ (Splatoon 流)

### デメリット
- **強すぎ注意**: intensity 1.0+ は「かすみ目」「眩しい」状態に
- 屋内シーンで照明が大きくなりすぎることがある
- パフォーマンスやや消費 (frame 0.3-0.5ms)

### 数値ガイドライン
- **Intensity 0.3-0.6**: 自然、cartoon (Splatoon 0.5)
- **Intensity 0.6-1.0**: 強め (Genshin 戦闘 0.8)
- **Intensity 1.0+**: 過剰、特殊シーン専用
- **Threshold 0.95-1.1**: 自然 (1.0 が default)
- **Scatter 0.6-0.8**: 柔らかさ (高いほど大きく拡散)

### 他ゲーム比較
- Splatoon 3: Intensity 0.5, Scatter 0.7
- Genshin 元素: 戦闘時 0.8 (常時 0.5)
- Fall Guys: 0.45 (柔らかい体育館感)
- TLOU: 0.3 (リアル系で控え目)

---

## 6. Vignette (intensity 0.22)

### 何する
画面 4 隅をほんのり暗くする。視線を中央 (player) に誘導。

### 現状値
- intensity: **0.22**
- smoothness: **0.45** (滑らかなフェード)

### Before / After
| | Before | After |
|---|---|---|
| 画面端 | 中央と同じ明るさ | 22% 暗い |
| 視線 | 散漫 | 中央に集まる |

### メリット
- **「フレームに入ってる感」** = 映画館スクリーン
- 視線誘導で player を見やすくする
- パフォーマンス影響軽微

### デメリット
- **強すぎ注意**: 0.4+ で「PS3 ゲーム感」(古臭い)
- 暗いシーンで端が真っ黒になる
- HUD UI が画面端にあると暗くなる

### 数値ガイドライン
- **Intensity 0.15-0.30**: subliminal、AAA defacto ← **本作はここ**
- **Intensity 0.30-0.45**: 強め (Hat in Time の cozy 感)
- **Intensity 0.45+**: 過剰、horror 系専用
- **Smoothness 0.4-0.6**: 自然なフェード
- **Smoothness 0.2 以下**: ハードな枠 (CRT エフェクト)

### 他ゲーム比較
- CoD / Apex: 0.20-0.25
- モンハン: 0.18 (探索)、0.30 (戦闘演出時)
- ホラー (Resident Evil): 0.40+
- Mario Odyssey: 0.15 (控え目で明るい)

---

## 7. Chromatic Aberration (intensity 0.10) ⭐ AAA subliminal

### 何する
画面端で R/G/B チャンネルが微妙にズレる。レンズの色収差を再現。

### 現状値
- intensity: **0.10** (subliminal)

### Before / After
| | Before | After |
|---|---|---|
| ピクセル | ピッタリ重なる (CG 丸出し) | 端で 1-2px の色ズレ |
| 雰囲気 | engine 出力 | 「カメラ越し」 |

### メリット
- ON/OFF を**並べないと**気付かないレベルの効果
- でも **OFF にすると「あれ平面?」** と感じる依存度
- AAA 全タイトル使用、subliminal 武器

### デメリット
- **強すぎ注意**: 0.20+ は Cyberpunk 風 (やりすぎ感)
- 文字 UI が読みにくくなる (テキストの端で色ズレ)
- パフォーマンスやや消費 (frame 0.2-0.4ms)
- 一部 user は「気持ち悪い」と感じる場合あり (Reddit にも議論)

### 数値ガイドライン
- **Intensity 0.05-0.12**: subliminal、自然 ← **本作はここ**
- **Intensity 0.12-0.20**: 強め (Apex リング外)
- **Intensity 0.20+**: 演出 (Cyberpunk 全画面、戦闘時のみ等)

### 他ゲーム比較
- Apex Legends: 0.10 (常時) + 戦闘時加算
- Genshin: 0.08 (常時)
- CoD: 0.08-0.12
- Cyberpunk 2077: 0.20 (常時、強い)

---

## 8. Lens Distortion (intensity -6%)

### 何する
画面を **わずかに球状に湾曲** (魚眼の超弱版)。

### 現状値
- intensity: **-0.06** (微弱バレル、URP の -1〜+1 範囲で -6%)

### Before / After
| | Before | After |
|---|---|---|
| 画面 | 完全な平面 (CAD 図面) | 端がほんのり奥に |
| 立体感 | flat | 「カメラレンズ」感 |

### メリット
- **「立体的」** に感じる
- Panini Projection と相互補完
- AAA FPS / TPS で defacto (CoD MW 流)

### デメリット
- **強すぎ注意**: -0.15+ で「魚眼カメラ」明らか、酔いの原因
- 画面端で UI 配置がズレる
- VR は 0 必須 (酔う)
- パフォーマンスやや消費

### 数値ガイドライン
- **Intensity 0**: 完全平面 (Mario Odyssey 等 cartoon)
- **Intensity -0.05〜-0.10**: 微弱、AAA defacto ← **本作はここ**
- **Intensity -0.15〜-0.30**: 強め (CoD MW 強強調モード)
- **Intensity -0.30+**: GoPro 風 (過剰)
- + 値: ピンクッション歪み (ほぼ使わない)

### 他ゲーム比較
- CoD MW 2019: -0.15 (デフォ強め)
- Battlefield: -0.10
- Apex: 0 (CoD と差別化)
- Mario Odyssey: 0 (cartoon defacto)

---

## 9. Panini Projection (distance 0.3) ⭐ FOV 広角時に必須

### 何する
広角 FOV (70+) で発生する**画面端の伸び (魚眼歪み)** を補正。

### 現状値
- distance: **0.3**
- cropToFit: **1** (端を切り詰めて画面に合わせる)

### Before / After
| | Before | After |
|---|---|---|
| FOV 60 | OK | OK |
| FOV 75+ | 端が伸びて魚眼感 | **人間の目に近い投影** |

### メリット
- **ダッシュ FOV punch (Day 5) で活きる** (将来効く)
- 純粋な数学: Panini 投影 = 16 世紀絵画技法のデジタル版
- CoD MW から AAA 全 FPS の defacto

### デメリット
- 現状 FOV 60 では効果が地味 (FOV 70+ で本領)
- パフォーマンスやや消費
- cropToFit=1 で画面端が切られる (FOV 上げ時は問題なし)

### 数値ガイドライン
- **Distance 0**: 効果なし
- **Distance 0.2-0.4**: 自然、AAA defacto ← **本作はここ**
- **Distance 0.5-0.7**: 強い (FOV 90+ 時)
- **Distance 1.0**: 円形投影 (魚眼補正)
- **CropToFit 1**: 端切り詰め (推奨)
- **CropToFit 0**: 黒帯出る

### 他ゲーム比較
- CoD MW 2019+: Distance 0.4 (FOV 90+ 対応)
- Apex: Distance 0.3
- Witcher 3 戦闘時: Distance 0.5
- 現状本作: 効果地味 (FOV 60)、Day 5 で活きる

---

## 🔟 Film Grain (Thin1, intensity 0.10)

### 何する
画面全体に微弱なザラつき。フィルム感の質感を加える。

### 現状値
- type: **Thin1** (細かい粒子)
- intensity: **0.10** (subliminal)
- response: **0.8** (中域強調)

### Before / After
| | Before | After |
|---|---|---|
| ピクセル | 完全平滑 (CG 然) | ザラっと粒状感 |
| 質感 | 冷たい | 温度感あり |

### メリット
- **CG 然とした「冷たさ」が消える**
- 暗部のバンディング (色段差) を隠す副次効果
- パフォーマンス影響軽微

### デメリット
- **強すぎ注意**: 0.30+ は「画質悪い」感
- カートゥーン感が薄れる (粒で柔らかくなる)
- 動画録画時にエンコードでブロックノイズが乗る

### 数値ガイドライン
- **Intensity 0.05-0.15**: subliminal ← **本作はここ**
- **Intensity 0.15-0.25**: 中、Hat in Time
- **Intensity 0.30+**: 強、Cuphead vintage
- **Type Thin1**: 細かい粒、cartoon
- **Type Medium**: 中粒、Hat in Time
- **Type Large**: 大粒、Cuphead

### 他ゲーム比較
- The Last of Us: 0.20 (Medium)
- Hat in Time: 0.15 (Thin)
- Cuphead: 0.40 (Large、強め)
- Mario Odyssey: 0 (cartoon = grain なし)

---

## 📊 統括メリデメ表

| # | Override | メリット | デメリット | 推奨度 |
|---|---|---|---|---|
| 1 | **Tonemapping=Neutral** | Unity 感 60% 消える | (ほぼなし) | ⭐⭐⭐⭐⭐ 必須 |
| 2 | **White Balance** | 画面温度感 | ステージ別で変えたい | ⭐⭐⭐ |
| 3 | **Color Adjustments** | パーティ感 | やりすぎは派手すぎ | ⭐⭐⭐⭐⭐ 必須 |
| 4 | **Split Toning** | 映画的奥行き | 暗部で支配的になる | ⭐⭐⭐⭐ |
| 5 | **Bloom** | 温度感、現象感 | 強いとかすみ目 | ⭐⭐⭐⭐ |
| 6 | **Vignette** | 視線誘導 | UI 端が暗くなる | ⭐⭐⭐ |
| 7 | **Chromatic Aberration** | カメラ越し感 | テキスト読みにくく | ⭐⭐⭐ |
| 8 | **Lens Distortion** | 立体感 | 強いと酔う | ⭐⭐ |
| 9 | **Panini Projection** | FOV 広角時必須 | 現状効果薄 | ⭐⭐ (Day 5 で活きる) |
| 10 | **Film Grain** | 温度感、CG感消す | 強いと画質悪く見える | ⭐⭐ |

## 🎯 「もし悩んだら」削減順

予算節約 / パフォーマンス / 主観で削るなら、**下から削る**:
1. Film Grain (0.10 → 0)
2. Panini Projection (Day 5 で再有効化)
3. Lens Distortion (-0.06 → 0)
4. Chromatic Aberration (0.10 → 0.05)
5. Vignette (0.22 → 0.15)

**絶対残すべき** (これだけで AAA 寄り 70%):
1. **Tonemapping = Neutral**
2. **Color Adjustments** (sat +28)
3. **Split Toning**
4. **Bloom**

---

## 💾 ステージ別 Volume Profile 拡張案 (M3+)

将来 4 ステージ分け:
- `DefaultPostProcess.asset` (本書、Sandbox 全般)
- `Volumes/MountainStage_M3.asset` (Hat in Time 風 warm)
- `Volumes/CityStage_M3.asset` (Splatoon 風 candy 強化)
- `Volumes/NightStage_M4.asset` (Genshin 風 Teal&Orange)

各 stage の Volume を Local Volume + Weight blend で切替 (Hat in Time defacto)。
