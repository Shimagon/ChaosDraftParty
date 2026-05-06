# M2 世界観適合 品質施策 優先順位ロードマップ

**作成**: 2026-05-06
**根拠**: 並列 agent 4 個調査 + Splatoon / Fall Guys / Mario Party / Hat in Time の defacto 比較
**コア原則**: **「打感は最大化、レンズ系は最小化」** = clean cartoon + 高彩度 + 強い juice + 低 lens artifact

---

## 🎯 本作の世界観 (再確認)

- **パーティゲーム** (Splatoon / Fall Guys / Mario Party 系)
- **clean cartoon** (Hat in Time / Cuphead の vintage ではなく、Mario Odyssey / Splatoon 流の clean)
- **4 色キャラ識別が gameplay critical** → 画面汚し NG
- **8 分試合 × 4 ラウンド × 90 秒** = 短期ハイテンション
- **カオス**: 派手・予測不能・笑える

## 重要な気づき
**Mediatonic (Fall Guys) / Nintendo (Splatoon, Mario) は意図的に lens artifact を避けてる**:
- Chromatic Aberration / Film Grain / Lens Distortion / Panini Projection = **不採用**
- Split Toning (Teal & Orange) = **Hollywood cliché、candy と逆方向**
- 常時 Vignette = **4 人画面で隅が暗いと不利平等性**

---

# 🌟 Tier S — 世界観 core 必須 (即実装、1 個ずつ進める)

| # | 施策 | 一行説明 | 工数 | 順序 |
|---|---|---|---|---|
| **S-1** | **Color Adjustments (saturation +28)** | Fall Guys candy 識別の根幹 | ✅完了 | - |
| **S-2** | **Tonemapping = Neutral** | Nintendo defacto、Unity 感 60% 消し | ✅完了 | - |
| **S-3** | **Bloom (intensity 0.5)** | Splatoon ink wet 感、Fall Guys candy 光沢 | ✅完了 | - |
| **S-4** | **White Balance (Temp +5, Tint -2)** | お祭り感の暖色寄せ | ✅完了 | - |
| **S-5** | **Coyote Time + Input Buffer (両方 0.1-0.15s)** | 縮む島の理不尽さ排除、Fall Guys defacto | 半日 | **次** |
| **S-6** | **Squash & Stretch (卵型 EGGYBOT)** | EGGYBOT形 = Splatoon イカ squash と完全一致、卒制差別化 | 1-2 日 | 次々 |
| **S-7** | **Footstep Dust Puff** | 「キャラが世界に居る」感、Genshin / モンハン defacto | 1 日 | その次 |

---

# ⭐ Tier A — 世界観合致 + 高効果 (M2 内で全部)

| # | 施策 | 一行説明 | 工数 |
|---|---|---|---|
| **A-1** | **Landing Dust Ring** | Fall Guys / Mario Odyssey 必須、落下 = パーティ核心 | 1 日 |
| **A-2** | **Dash Trail Renderer (4 色)** | Splatoon ink trail、4 色キャラ識別補強 | 半日 |
| **A-3** | **Dash FOV Punch (60→68°)** | Mario Kart / Sonic defacto、加速感の最も安い実装 | 半日 |
| **A-4** | **Look-Ahead** | TPS standard、酔い軽減にも寄与 | ✅完了 |
| **A-5** | **Jump 3 段 SE (踏切/[頂点]/着地)** | Mario / Splatoon 三段階 SE、テンポ支える | 1 時間 |
| **A-6** | **Kenney UI SE 5 種** | ドラフト 5 秒 × 16 回、SE 無しは破綻 | 30 分 |
| **A-7** | **BGM (Pixabay)** | パーティゲーは BGM 無音で死ぬ | 30 分 |

---

# ✨ Tier B — 合致だが二次 (M3 で OK)

| # | 施策 | 理由 |
|---|---|---|
| B-1 | Hit Stop (60-100ms) | 効くが、本作 pvp 打撃よりミニゲーム勝敗主軸。Splatoon もインク撃破に hit stop ほぼ無い |
| B-2 | Damage Number Popup | パーティ系は HP 数値見せない流派 (Fall Guys) もある、控えめ |
| B-3 | Cinemachine Impulse (hit shake) | bomb / pulse 命中時のみ |
| B-4 | Vignette Damage Feedback (赤 flash) | Splatoon の ink hit 系、ただし常時 vignette は別 |
| B-5 | AudioMixer 5 channel | 必須だが Tier S/A 鳴ってからで OK |
| B-6 | Speed Lines (radial blur) | Splatoon super jump 演出、ダッシュ時のみ短時間 |
| B-7 | Camera Roll on Strafe | 微調整領域、Mario Odyssey にもあるが微弱 |

---

# ❌ Tier C — 世界観 mismatch (削除 / 極小値、即対応)

> Splatoon / Fall Guys / Mario series 全部 **使ってない**。lens artifact = リアル / Cyberpunk 系。
> **既に追加してる 6 個を OFF or 削除**

| # | 施策 | 現状値 | 推奨アクション | 削減理由 |
|---|---|---|---|---|
| **C-1** | **Chromatic Aberration** | 0.10 | **OFF (0)** | Splatoon / Fall Guys 全部不採用、識別性悪化、Unity 臭の元凶、PC Gamer "actual worst" 批判 |
| **C-2** | **Film Grain** | 0.10 | **OFF (0)** | Cuphead は使うが clean cartoon は使わない、Mario / Splatoon ゼロ、candy 色の純度を汚す |
| **C-3** | **Lens Distortion** | -6% | **OFF (0)** | Mario Odyssey 不採用、8 分高テンポで頭が疲れる |
| **C-4** | **Panini Projection** | 0.3 | **OFF (0)** | 広角 FPS 用、3 人称 close cam の本作は不要 (Day 5 で FOV 70+ 採用するなら復活検討) |
| **C-5** | **Split Toning (Teal & Orange)** | active | **OFF** | Hollywood blockbuster cliché、candy 世界観と逆方向 |
| **C-6** | **Vignette (常時)** | 0.22 | **OFF or 0.10 以下** | 4 人画面で隅が暗いと不利平等性、ダメ受け演出時のみ赤 flash で代用 (Tier B-4) |

---

# 🌳 Tier D — M3+ 本格化 (今やらない)

| # | 施策 | 後回し理由 |
|---|---|---|
| D-1 | Toon Shader + Rim | EGGYBOT v16 SDF 完成済、独自ルート、M3 で全体統一 |
| D-2 | Stylized Outline (Sobel) | 全 prefab の URP Renderer Feature 調整、M3 後半 |
| D-3 | Cinemachine Noise (Handheld) | パーティゲーには handheld 不要 (Fall Guys / Splatoon 無し)、酔い対策で 0.10 残してるが**消しても OK** |
| D-4 | Toon Shader 全体統一 | M3 のステージ込み |
| D-5 | NetworkObject Pool (爆弾 / 弾) | M3 (爆弾鬼着手時) |

---

# 🛠️ 「一つずつ進める」 着手順 (推奨)

User 明示「**一つずつやって効果実感、全部やる**」に沿った段階表:

## Step 0: focus 問題確認 (必須、進む前に)
- **#69 Always Refresh ON** (user 手動 1 クリック)
- ▶ Play で dither / camera / mouse 全部効くか確認

## Step 1: Tier C 削減 (画面 clean 化、即体感、20 分)
1. **C-1 〜 C-5 を OFF** (Chromatic Aberration / Film Grain / Lens Distortion / Panini Projection / Split Toning)
2. **C-6 Vignette は 0.22 → 0.10 に弱化** (完全 OFF より枠感残し)
3. ▶ Play test → user「画面 clean になった」体感

## Step 2: Tier S 追加 (1 個ずつ、計 3-5 日)
1. **S-5 Coyote Time + Input Buffer** (半日)
2. **S-6 Squash & Stretch** (1-2 日、卵型最強相性)
3. **S-7 Footstep Dust** (1 日)

## Step 3: Tier A 追加 (1 個ずつ、計 3-4 日)
1. **A-1 Landing Dust Ring** (1 日)
2. **A-2 Dash Trail (4 色)** (半日)
3. **A-3 Dash FOV Punch** (半日)
4. **A-5 〜 A-7 Audio (BGM + SE)** (1 日、ただし音源 DL は user 手動)

## Step 4: Tier B 追加 (M3 開始時に判断)
- 現時点で着手しない、M3 のミニゲーム実装と並行

---

# 🔁 各 Step 完了時のルーティン

User 体感優先で「効果実感→値調整→次」を繰り返す:

```
1. AI が 1 候補を実装 (default 値)
2. ▶ Play test (user 体感)
3. 値調整 (Inspector で user が好み合わせ)
4. AI review loop 1 周
5. commit + push
6. 次の候補へ
```

これで「**何が変わったか分かりやすい**」を担保。

---

# 📊 既に「やった / やってない」現状マップ

| カテゴリ | 完了 | 未着手 |
|---|---|---|
| Tier S | S-1, S-2, S-3, S-4 | S-5, S-6, S-7 |
| Tier A | A-4 | A-1, A-2, A-3, A-5, A-6, A-7 |
| Tier B | (一部関連: dither / Camera Roll 0°) | 全部 |
| Tier C 削減 | (まだ追加した状態) | **次の最優先削減** |
| Tier D | - | 全部 (M3+) |

---

# 🎯 Next Action

User 確認待ち:
1. **Always Refresh ON 確認 + Play test** (#69)
2. 上記順序 OK か → OK なら **Step 1: Tier C 削減** から着手
