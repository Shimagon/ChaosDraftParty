# HANDOFF — 2026-05-01 朝再開用

**前提:** `HANDOFF_20260430_step5_complete.md` の続き。EGGYBOT v16 humanoid 完了 + カプセル蓋開閉 + 蓋淵 (LineRenderer 円) 実装完了 → メインシーンに昇格済。

---

## 0. 現状

### シーン
- **`Sandbox_v16_humanoid.unity`** = メイン（test scene を昇格、すべて入り）
  - EGGYBOT_v16 (humanoid 完全対称、Hand world ±0.36)
  - EggybotPoseLock (yOffset=-0.219, symmetrize=true)
  - EggybotCapsule (蓋開閉、isOpen toggle)
  - EggybotCrouch (頭収納、isCrouching toggle)
  - CapsuleLid_Hinge (root child、world (0, 0.50, 0.336))
  - BodyUpperClear (Hinge child、MeshFilter+MeshRenderer 化、material EGGYBOT_Lid_Test)
  - CapsuleLid_Rim (Hinge child、LineRenderer で半径 0.336 の白円)
- `Sandbox_v17_capsule_test.unity` 削除済

### 直近 commit
- `bfaa646` — feat(eggybot): カプセル開閉 + 蓋の淵 (LineRenderer 円)

### 朝起きてすぐの状態
- 作業ツリー: シーン promotion の差分 (M Sandbox_v16, D Sandbox_v17) が staged 待ち
- 明日の最初の commit: シーン昇格 commit

---

## 1. 明日の続き (BC)

ユーザー指示: 「メインに昇格して、頭収納しよう。BCやろ」

### B. 頭収納 (Crouch) 実装
EggybotCrouch component は実装済 (Head bone Y を Lerp で 0.91→0.30 に沈める)。Inspector の `isCrouching` toggle で動作確認可能。

**user 確認待ちの認識すり合わせ** (寝る直前に俺が出した質問):
> しゃがみ動作で蓋と頭はどう連動する？
> - (A) 独立: 蓋閉じたまま頭が bowl 内に消える
> - (B) sequence: 蓋開く → 頭下がる → 蓋閉じる
> - (C) 同時: 蓋開きながら頭下がる

**未回答** → 朝再開時に user に絵 / 言葉で確認 (`feedback_ask_for_drawing_when_unclear.md` ルール準拠)

### C. Animation Clip 化
B の動作モード次第:
- (A) なら component bool toggle で十分、Animation Clip 不要
- (B)(C) なら AnimatorController + AnimationClip で sequence 実装
  - 蓋: Hinge.localRotation の keyframe
  - 頭: Head bone Y の keyframe (or Crouch.isCrouching trigger)

---

## 2. 朝の起動手順

```bash
cd C:/Users/taise/Documents/GitHub/ChaosDraftParty
git pull origin main  # bfaa646 + 今夜の昇格 commit が来てる
git status --short    # クリーンか確認
```

Unity:
1. Hub から ChaosDraftParty open
2. `Sandbox_v16_humanoid.unity` 開く (メイン昇格済)
3. ▶ Play → 蓋開閉 + 頭収納両方動くこと確認
4. 既知問題: Walking 中の bone 補正 + 蓋の rim 円描画

Claude:
```
HANDOFF_20260501_capsule_main_promotion.md を読んで、
B. 頭収納の動作モード (A/B/C) を確認してから実装入って。
ルール: 認識ズレ感じたら絵で説明依頼すること。
```

---

## 3. 今セッション学んだこと（teaching）

### 認識ズレで時間溶けた事例
「淵」= shader rim じゃなく **hemisphere の底の円形 geometry edge** だった。
- 試行錯誤 5 案 (shader rim 強調 → inverted hull → silhouette pass → LightMode 修正 → 真っ白強調)
- 全部外す
- user が絵で示してくれて即解決 (LineRenderer で円描画)
- → memory `feedback_ask_for_drawing_when_unclear.md` に記録

### 新ルール（記録済）
- `feedback_workflow_parallel_review_default.md`: 全タスク並列 agent + AI review loop
- `feedback_ask_for_drawing_when_unclear.md`: 認識ズレ感じたら早く絵を依頼

---

## 4. 残課題 (M1 進行)

EGGYBOT は完成寄りなので、M1 本タスクへ進む候補:
- NGO + Relay 疎通プロト (M1 最重要)
- PlayerController 移動 (W/A/S/D)
- 縮む島ミニゲーム雛形

カプセル / 頭収納が落ち着いたら NGO へ。

---

**最終更新:** 2026-05-01 朝（夜寝る前 handoff）
**HEAD:** bfaa646 → 今夜の commit で更新予定
**作業ツリー:** Sandbox 昇格差分 staged 待ち
