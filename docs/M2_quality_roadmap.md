# ChaosDraftParty M2-M5 クオリティ向上ロードマップ

**作成**: 2026-05-06 朝
**目的**: AAA / 名作インディの defacto 実装手法を ChaosDraftParty に取り入れ、卒業制作の品質を 1 段階引き上げる
**根拠**: 並列 agent 3 個 (Visual / Game Feel / Netcode) × 各 10 候補 = 30 候補からコスパ順に整理
**メソッド**: AI review loop (unity-reviewer + code-reviser) を実装ごとに必ず通す

---

## 🌟 M2 即導入 4 点セット (合計 3〜4 日、Skill 3 種実装と並行)

Skill 全 10 種が AAA フィールを持つ基盤になる。SkillBase に共通 hook で組む。

| # | defacto | 軸 | 効果 | 工数 | 出典 |
|---|---|---|---|---|---|
| 1 | **Hit Stop (Hitlag)** | Game Feel | 命中時 `Time.timeScale=0.05f` × 60-100ms。打感が AAA 級 | 半日 | Smash / モンハン / Hi-Fi Rush / Hades |
| 2 | **Cinemachine Impulse + FOV Punch** | Visual+Feel | ダッシュ +8° FOV、ヒット 6D shake | 半日 | Vlambeer / Doom / Genshin |
| 3 | **Coyote Time + Input Buffer** | Game Feel | 0.1-0.15s 空中ジャンプ補正 + 入力先行受付 | 半日 | Celeste / Mario / Hat in Time |
| 4 | **Damage Number Popup** | Visual | TextMeshPro + DOTween punch、crit は色変更 | 1 日 | Genshin / Hades / Fall Guys |

### 実装ガイド (M2 着手時に SkillBase + PlayerController に同時投入)

```csharp
// SkillBase に共通 hook
abstract class SkillBase {
    protected void OnHit(Vector3 hitPos, float damage) {
        TimeStopUtil.HitStop(0.06f);                    // #1 ローカル演出のみ
        ImpulseSource.GenerateImpulse(strength);        // #2
        DamagePopup.Show(hitPos, damage);               // #4
    }
}
// PlayerController に
float lastGroundedTime, lastJumpPressedTime;            // #3 Coyote/Buffer
DOTween.To(() => lens.FOV, +8f, ...).OnComplete(...);   // #2 FOV Punch on dash
```

---

## ⭐ M2 並行 Netcode 4 候補 (合計 2-3 日、低工数)

| # | defacto | 工数 | 効果 |
|---|---|---|---|
| 5 | **Lobby Code 6 桁 + Steam invite フォールバック** | 1 日 | 集合の手間 = 4 人カジュアル最大の障壁解消。Steamworks.NET の SetRichPresence("connect", code) |
| 6 | **Loading Screen 4 段ステート (Boss Room パターン)** | 半日 | 接続失敗の理由 (Relay token 切れ / version mismatch / lobby full / NAT / timeout) を明示。`NetworkManager.OnTransportFailure` + `DisconnectReason` |
| 7 | **NetworkTransform Interpolation tick latency 調整 + Snap Threshold** | 半日 | default 40ms jitter → 滑らか、帯域半分。tick latency=2、Half Float Precision ON、Quaternion Compression ON |
| 8 | **MPPM 4 ウィンドウ移行** (ParrelSync 卒業) | 半日 | ParrelSync は重い (clone 全体)、MPPM は domain 共有で高速。4 人試合の動作確認が秒で回る |

---

## 🎨 M3 で質的ジャンプ (合計 1 週間)

| # | defacto | 工数 | 効果 |
|---|---|---|---|
| 9 | **Toon Shader + Rim Light + 眼鏡光 (Specular Eye Highlight)** | 中 (3日) | URP Shader Graph で 3-step ramp + Fresnel rim + MatCap eye。Genshin NPR / Splatoon |
| 10 | **Damage Flash + Camera Punch** | 低 (半日) | URP Shader Graph で `_FlashAmount` を 80ms 白化 + 6D shake 0.15s。Splatoon / Doom / Returnal |
| 11 | **Look-Ahead Camera + Speed Lines** | 中 (2日) | Cinemachine `TrackedObjectOffset` を velocity 方向 Lerp 0.3s。Dash 中 URP Renderer Feature で radial blur。Splatoon / Mario Odyssey |
| 12 | **Squash & Stretch (Anticipation + Landing)** | 中 (3日) | EGGYBOT bowl localScale を Jump 前=(1.1,0.9)、Land=(0.85,1.15)→(1,1) DOTween Punch。卵型と最強相性 |
| 13 | **Bot 自動操作 (NavMesh ランダム移動 + 確率テーブル skill)** | 中 (3日) | 「3 人集まらない」既知リスク対策。Fall Guys / Stumble Guys defacto |
| 14 | **NetworkObject Pooling (爆弾 / パルス弾)** | 中 (2日) | spawn RPC 帯域削減 (毎ラウンド 50+ spawn)。Boss Room から流用可 |
| 15 | **Client-Side Prediction (移動のみ) + Reconciliation** | 中 (3日) | 縮む島 / しっぽ狩りで 50ms 遅れ解消。スキル発動アニメは即時再生 (latency masking)。TCleard ライブラリ |

---

## 💥 M4 で chaos 強化 (合計 1 週間)

| # | defacto | 工数 | 効果 |
|---|---|---|---|
| 16 | **Hit Spark VFX + Impact Decal** | 中 (2-3日) | VFX Graph で星型 sprite + radial、URP Decal Projector pool。モンハン / Hi-Fi Rush / スマブラ |
| 17 | **Stylized Outline (Sobel)** | 中 (2日) | URP `FullScreenPassRendererFeature` + Depth+Normal Sobel、距離フェード必須。Splatoon / Hat in Time / ARMS |
| 18 | **Anticipation SE (Pre-cast cue 80-150ms 前)** | 低 (半日) | Skill 発動の 80-150ms 前に低音 whoosh、本体 SE と分離。プレイヤー間の読み合い創出。Overwatch / Genshin |
| 19 | **Knockback Recovery + Partial Ragdoll** | 高 (週単位) | RagdollBlender で 0.4s 部分ラグドール → Animator blend 戻し。Fall Guys "ぐにゃぐにゃ" の本体 |
| 20 | **Respawn Cushion / Super Jump-In** | 中 (3日) | リスポーン点に EGGYBOT 落下 → Squash 1.4x → Bonk SE → Pose、0.6s 演出。Splatoon スーパージャンプ |
| 21 | **Vignette Damage Feedback** | 低 (半日) | Volume の Vignette intensity を被弾時 DOTween (赤 0.6 → 0.0 を 0.4s)。CoD / Apex / Fall Guys |
| 22 | **Speed Hack 検知 (server min/max delta)** | 低 (1日) | NGO 2.x の `OnAuthorityPushTransformState` でサーバー側 `Vector3.Distance` 検証 (10-15% tolerance) |

---

## 🏆 M5 仕上げ (合計 1 週間)

| # | defacto | 工数 | 効果 |
|---|---|---|---|
| 23 | **Win/Lose Screen Flash + Slow-Mo MVP Cam** | 中 (3日) | `Time.timeScale=0.3f` + Cinemachine 切替 + 全画面 flash。Splatoon リザルト / Fall Guys |
| 24 | **Highlight Reel (5 秒 ring buffer 自動録画)** | 高 (週単位) | 60Hz × 5s × 4人 × (pos+rot+anim) ≈ 50KB。MVP プレイヤー視点で再生。卒業発表で「映える」最大要因 |
| 25 | **Disconnect Graceful Handling (15s reconnect window)** | 中 (3日) | `NetworkManager.OnClientDisconnectCallback` + `DisconnectReason` ScriptableObject に文言マップ。Among Us / Lethal Company |

---

## 📌 落選 (採用見送り推奨)

- **Lag Compensation hitscan rewind** — パルス弾は projectile で十分、hitscan 不要
- **Area-of-Interest / Relevancy** — 4人なら過剰、16人時に検討
- **Lobby chat (独自実装)** — Discord 前提で OK、工数 > 価値
- **Deterministic replay** — 物理が爆弾だけなので決定論不要、#24 highlight reel で十分

---

## ⚠️ NGO 文脈での注意点

- **Hit Stop は ローカル演出のみ**: `Time.timeScale` はサーバー側で触らない (NetworkVariable 同期破壊)
- **Ragdoll は見た目のみ**: 位置同期は continue (CLAUDE.md の同期ポリシー準拠)
- **Camera Impulse は Owner クライアントのみ**: 他人視点では発火しない

---

## 📅 マイルストーン統合タイムライン

| マイルストーン | 主要実装 | 候補 # |
|---|---|---|
| **M2 (5月)** | Skill 3 種 + 4 ラウンド + Draft UI + ★4 点セット + Netcode 4 候補 | 1, 2, 3, 4, 5, 6, 7, 8 |
| **M3 (6月)** | しっぽ狩りバトル + 爆弾鬼 + Skill 7 種 + 質的ジャンプ | 9, 10, 11, 12, 13, 14, 15 |
| **M4 (7月)** | しっぽ狩り逃走 + Skill 10 種 + chaos 強化 + ルールカード 4 種 | 16, 17, 18, 19, 20, 21, 22 |
| **M5 (8月)** | シーズンメタ + 仕上げ + 卒業発表用ハイライト | 23, 24, 25 |
| **M6 (9月)** | QA + 最適化 + Steam ビルド | (新規実装なし、polish only) |

---

## 📚 参考リンク (調査時 Web 検索結果)

- [Vlambeer / Game Juice](https://www.gamedeveloper.com/design/squeezing-more-juice-out-of-your-game-design-)
- [Game Feel - Steve Swink](http://www.game-feel.com/)
- [Sakurai on Hitstop](https://sourcegaming.info/2015/11/11/thoughts-on-hitstop-sakurais-famitsu-column-vol-490-1/)
- [Cinemachine Impulse docs](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/setup-apply-noise.html)
- [Boss Room (NGO 2.5)](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/manual/samples/bossroom/getting-started-boss-room.html)
- [NetworkTransform interpolation](https://docs.unity3d.com/Packages/com.unity.netcode.gameobjects@2.5/manual/components/helper/networktransform.html)
- [Mirror Cheats & Anticheats](https://mirror-networking.gitbook.io/docs/security/cheating)
- [Multiplayer Play Mode docs](https://docs-multiplayer.unity3d.com/mppm/1.0.0/troubleshoot/debug-mppm/)
- [Coyote Time + Jump Buffer Unity tutorial](https://www.ketra-games.com/2021/08/coyote-time-and-jump-buffering.html)
- [PredictionReconciliationNetwork (TCleard)](https://github.com/TCleard/PredictionReconciliationNetwork)

---

**承認後、実装は M2 着手と同時に SkillBase 構造を組んでから埋め込む方式が最もコスパ良い。**

---

# 🌙 追補 (2026-05-06): 「常時感じる底上げ」系 13 候補

User 明示要望: 「**1 系やりたい (dither みたいな全体クオリティ系)。普通の動きでもクオリティを上げたい。カメラの設定 / ダッシュ / エフェクト / 音、何からしよう**」

並列 agent 3 個 (常時 visual / 移動 VFX / audio) で調査して整理。**Skill 命中時のヒット系とは別カテゴリ**として「**何もしてなくても感じる底上げ**」を最優先。

## 🔊 Audio (現状: ほぼ無音)

| # | defacto | 工数 | 効果 | 出典 |
|---|---|---|---|---|
| S1 | **AudioMixer 5 channel** (Master/BGM/SFX/UI/Voice) | 30 分 | 後で Ducking / Snapshot 追加する土台。最初に作らないと地獄 | 全 AAA |
| S2 | **UI SE Kenney 5 種** (hover/select/confirm/cancel/error) | 30 分 | 1 時間で「無音 UI」→「品ある UI」、ROI 最大 | Splatoon / Fall Guys |
| S3 | **Jump 3 段 SE** (踏切/[頂点]/着地) | 1 時間 | 跳躍に重み感。`pitch ±5%` で連打感 | Mario Odyssey / Fall Guys |
| S4 | **BGM 1 曲ループ** (Pixabay 商用 CC0) + Win/Lose Stinger | 30 分 | 沈黙→生命感 | Fall Guys |
| S5 | **Bonk SFX 割当** (Kenney Impact、既存 slot 埋める) | 30 分 | 既存 Bonk が音つきで完成 | - |
| (M3) | Footstep Animation Event + Material Raycast | 中 (M3) | 床材質別 SE | Genshin / モンハン |
| (M3) | Skill Cast SE + Ducking | 中 (M3) | 大技時 BGM 一瞬下がる | モンハン |
| (M4) | Ambient Loop + Reverb Zone | 中 (M4) | 場所別空気感 | Genshin |

**音源**: [Kenney.nl](https://kenney.nl) (UI/Impact/Interface CC0) + [Pixabay Music](https://pixabay.com/music/) (CC0 商用 OK 無帰属) + [freesound.org](https://freesound.org) (CC0 filter)

## 🎨 Post-process (現状: URP デフォ未調整)

| # | defacto | 工数 | 効果 | 出典 |
|---|---|---|---|---|
| V1 | **URP Volume 4 点セット**: Vignette 0.25 + Bloom 0.3 + Color Adjustments+LUT + Film Grain 0.15 | 1 日 | 画面の "色温度" 確定で一気に AAA 級。EGGYBOT のカートゥーン色も締まる | Genshin / Splatoon / FF14 |

**注意**: 全部 **subliminal レベル** (intensity 0.15-0.3 帯) で。強いと逆にチープ。

## 📷 Camera (現状: Cinemachine OrbitalFollow + Radius lerp のみ)

| # | defacto | 工数 | 効果 | 出典 |
|---|---|---|---|---|
| C1 | **Camera Roll on Strafe** (横移動で 1-3° dutch tilt、SmoothDamp) | 半日 | TPS で「**Unity 標準カメラ**感」が消える | Quake / Apex / Mirror's Edge |
| C2 | **Cinemachine Noise Handheld_normal_mild** (amplitude 0.3 / freq 0.5、常時微弱) | 15 分 | 静止時もカメラが息してる感 | Death Stranding / Last of Us 2 / モンハン Wilds |
| C3 | **Look-Ahead Camera** (進行方向に 1-2m 先回り、`TrackedObjectOffset` lerp) | 半日 | カメラが置いてけぼりにならない | Splatoon / Spyro |

## 💨 移動 VFX (現状: ダッシュは Radius lerp のみ、他ゼロ)

| # | defacto | 工数 | 効果 | 出典 |
|---|---|---|---|---|
| F1 | **着地土煙リング** (URP Particle Burst 1 回、落下高さで scale) | 1 日 | 工数最小で「重力感」が劇的 | Splatoon / Genshin / Fall Guys |
| F2 | **走行・ダッシュ Footstep Dust Puff** (足 bone に attach、Animation Event で Emit) | 1 日 | Cuphead 系の小さい puff、走行に温度感 | Genshin / モンハン / Hat in Time |
| F3 | **ダッシュ Trail Renderer** (キャラ色 ribbon、0.3-0.5s) | 半日 | 4 色キャラ識別の AAA defacto | Splatoon / Genshin / ARMS |
| F4 | **ダッシュ FOV Punch + Speed Lines** (60→68° DOTween + MirzaBeig OSS) | 1 日 | 「速い！」体感に直結 | Splatoon / Mario Odyssey / Apex |
| F5 | **着地 Camera Dip** (CinemachineImpulse `Vector3.down * 0.3f`) | 半日 | 「重さ」を 1 行で defacto | Apex / CoD / Fortnite |

---

## 📅 着手順 (User 体感最大化)

| Day | テーマ | 候補 | 効果 |
|---|---|---|---|
| **1** | **Audio 5 つ** (S1〜S5) | 無音 → 人間味 | **最大の心理効果**。User が「全然やってない！」と気にしてた根本解消 |
| **2** | **URP Volume 4 点セット** (V1) | フラット → AAA 色 | **視覚的に最劇的**。dither と同レベルの「全体的な質感向上」 |
| **3** | **Camera 3 つ** (C1〜C3) | 硬い → 息する | 「**普通に歩いてるだけで気持ちいい**」が完成 |
| **4** | **移動 VFX 3 つ** (F1〜F3) | 素っ気ない → ジューシー | 着地・歩行・ダッシュが全部反応する |
| **5** | **ダッシュ仕上げ 2 つ** (F4〜F5) | ダッシュが本物の「Dash」になる | Speed Lines OSS で AAA Dash 完成 |

**5 日合計** で「歩いてるだけで Unity 臭が消える」+「ダッシュが気持ちいい」+「音がちゃんと鳴る」状態に到達。

