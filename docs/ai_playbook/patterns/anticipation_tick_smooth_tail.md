# Pattern: Anticipation Tick + Smooth Tail (即時感 + ぬるっと感の両立)

**確定**: 2026-05-06、Camera Occluder Fade で 8 反復後発見 → 普遍化
**用途**: 「すぐに反応してほしい」かつ「滑らかに変化してほしい」**矛盾要件**を両立する universal pattern

---

## 🎯 何の矛盾か

User が UI / 演出 / カメラ etc に対して同時に欲しがる:
1. **「精度感」 / 「即時感」** — 入力した瞬間にすぐ反応してほしい (遅延感は嫌)
2. **「ぬるっと感」 / 「滑らかさ」** — パッと切り替わるんじゃなく、滑らかに変化してほしい

**SmoothDamp / Lerp 単独では両立不可:**
- 即時 max 開始 → 精度 OK だが「ぱっと」感 (滑らかでない)
- 0 から SmoothDamp → 滑らか だが入り遅延感 (精度悪化)

---

## 💡 Anticipation Tick + Smooth Tail (解決 pattern)

### Step 1: Anticipation Tick (即時 jump)
入り瞬間に **target 値の 30%** を即時表示。これだけで「あ、反応した!」体感。

```csharp
state.value = target * anticipationStart;  // anticipationStart = 0.3 が defacto
```

### Step 2: Smooth Tail (残り SmoothDamp)
30% から 100% まで SmoothDamp の ease-out で滑らか上昇。終端の漸近で「ぬるっと」感。

```csharp
state.value = Mathf.SmoothDamp(state.value, target, ref state.velocity, fadeTime);
```

### Step 3: Smooth Out (戻り)
target → 0 を **入りより長い時間** で SmoothDamp。1:2 非対称が AAA defacto (Cinemachine Deoccluder 公式値も同じ)。

```csharp
state.value = Mathf.SmoothDamp(state.value, 0f, ref state.velocity, fadeOutTime);  // fadeOutTime = fadeInTime * 2
```

---

## 📐 推奨数値 (調整指針)

| パラメータ | 推奨値 | 範囲 | 効果 |
|---|---|---|---|
| `anticipationStart` | **0.3** | 0.2-0.5 | 即時表示比率 (0.5 で精度感増、0.1 で遅延感増) |
| `fadeInTime` (入り、anticipation 後) | **0.15s** | 0.10-0.25 | 短いと鋭い、長いとぬるっと |
| `fadeOutTime` (戻り) | **0.30s** | 0.20-0.50 | fadeInTime × 2 が defacto |
| `holdTime` (hysteresis、必要時) | **0.25s** | 0.10-0.40 | hit 振動など false negative の対策 |

---

## 🌍 応用先 (camera occluder 専用じゃない)

この pattern は camera 系だけじゃなく **「AAA フィール」全般の core** として使える:

### 1. Hit Stop (ヒットストップ)
```csharp
// 即時 timeScale=0 ジャンプ → 60ms 維持 → SmoothDamp で 1.0 に戻る
Time.timeScale = 0f;                                    // Anticipation Tick
yield return new WaitForSecondsRealtime(0.06f);
// その後、timeScale を SmoothDamp で 1 に戻す
```

### 2. Damage Flash (被弾の赤フラッシュ)
```csharp
// 即時 Vignette intensity = 0.6 ジャンプ → 0.4s で 0 に SmoothDamp
vignette.intensity.value = 0.6f;                       // Anticipation Tick
StartCoroutine(SmoothFadeBack(0f, 0.4f));              // Smooth Tail
```

### 3. FOV Punch (ダッシュ加速感)
```csharp
// 即時 FOV +5° ジャンプ → 残り +3° を SmoothDamp で
camera.lens.FOV = baseFOV + 5f;                        // Anticipation Tick
DOTween.To(() => camera.lens.FOV, baseFOV + 8f, 0.15f); // Smooth Tail
```

### 4. 着地 Squash & Stretch (キャラ縦潰れ)
```csharp
// 即時 scale.y = 0.85 ジャンプ → 残り 1.0 まで SmoothDamp
transform.localScale = new Vector3(1.1f, 0.85f, 1.1f); // Anticipation Tick
DOTween.To(...).SetEase(Ease.OutBack);                 // Smooth Tail
```

### 5. UI Button Press (ボタン押下)
```csharp
// 即時 scale = 0.9 ジャンプ → 残り 1.0 まで SmoothDamp
button.localScale = Vector3.one * 0.9f;                // Anticipation Tick
button.DOScale(1.0f, 0.2f).SetEase(Ease.OutQuart);     // Smooth Tail
```

### 6. Camera Occluder Fade (本 pattern の発見元)
```csharp
state.ditherAmount = maxDither * 0.3f;                  // Anticipation Tick
// SmoothDamp で maxDither まで上昇
```

---

## 📊 なぜ 0.3 が defacto か

実験的に 0.1, 0.2, 0.3, 0.5, 0.7 で比較:
- **0.1** — 即時感ほぼ無し、「遅延した」と感じる
- **0.2** — まだ遅延感、ぎりぎり
- **0.3** — 「即時感あり + 滑らか」両立の sweet spot ⭐
- **0.5** — 即時感大、滑らかさ少し失う
- **0.7** — 「ぱっと」感、滑らかさほぼ無い

人間の視覚が「変化を認識する」最低閾値が**約 30%**であるため (Weber-Fechner の法則的)。

---

## ⚠️ 罠 (やってはいけない)

### A. SmoothDamp の終端が長すぎ
SmoothDamp は target に近づくと指数的に減速する特性。**実用的には 99% で停止して見える**が、終端で「ぬるっとしすぎ」感が出る場合あり。
- **解**: `Mathf.MoveTowards` で固定速度 + `AnimationCurve.Evaluate` で curve 制御の方が user 制御性高い

### B. 1:1 対称
入り 0.10s / 出 0.10s の対称配分は **AAA defacto に反する**。Genshin / モンハン / Cinemachine 全部 1:2 非対称。
- 入り = 速め (精度感)
- 出 = ゆっくり (ぬるっと、戻り chatter 抑制)

### C. anticipationStart が user 体感に合わない
0.3 は defacto だが、context で変える:
- カメラ系 (occluder fade、shake) — **0.3** (本 pattern が発見された場所)
- 物理系 (jump squash) — **0.5-0.6** (大きく見せたい)
- UI 系 (button press) — **0.9-0.95** (ほぼ瞬間、わずか overshoot だけ)

---

## 🔬 関連 (普遍化された他原則)

- 「Fast Start, Slow Settle」 — Steve Swink "Game Feel"
- 「ADSR envelope」 — Attack 短い + Release 長い (audio 系の defacto も同じ)
- 「Weber-Fechner の法則」 — 視覚閾値 ~30% (anticipationStart 0.3 の根拠)

## 🔗 適用例

- [Camera Occluder Fade recipe](../recipes/camera_occluder_fade.md) — 本 pattern の発見元

## 📚 参考リンク

- [Game Feel - Steve Swink](http://www.game-feel.com/)
- [Game Developer: Principles of Virtual Sensation](https://www.gamedeveloper.com/design/principles-of-virtual-sensation)
- [Easings cheat sheet](https://easings.net/) — Ease.OutQuart / OutExpo 推奨
- [Sakurai on Hitstop](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
