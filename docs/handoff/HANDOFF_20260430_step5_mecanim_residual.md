# HANDOFF — 2026-04-30 Step 5 残課題 (Mecanim T-pose 補正問題)

**前提:** `HANDOFF_20260428_step5_progress.md` の続き。SDF clip は完成 (commit 0dbf3c1) したが、Edit / Play で T-pose が違う問題が残ってる。

---

## 0. 今日のサマリー

### 完了
- ✅ SDF clip 完成（pixel 単位で bowl にめり込んだ部分だけ消える）
- ✅ shader の view-occlusion ロジック削除
- ✅ Walking root motion bake (lockRootHeightY=true)
- ✅ material `_Cull` を Back に
- ✅ BodyClip を bowl 全体中心 (0, 0.50, 0) に配置（commit 0dbf3c1）
- ✅ memory + skill + CLAUDE.md に教訓記録

### 残課題: Edit と Play で T-pose が違う

User が Inspector で bone を綺麗に配置（Hips Y=0.26, Shoulder ±0.36, Hand ±0.36 など）→ Avatar 再構築 → Play で **Mecanim が強制的に補正**:

| Bone | User 設定 | Play での値 | 差分 |
|---|---|---|---|
| Hips Y | 0.26 | 0.45 | +0.19 |
| Shoulder X | -0.36 | +0.38 | **左右反転** |
| Hand Y | 0.42 | 0.51 | +0.09 |
| Head Y | 0.88 | 1.07 | +0.19 |

体全体が +0.19m 上に飛び、腕が左右反転する。

---

## 1. 真因（並列 agent 3 個で調査済）

### 1-A. Mecanim humanoid の T-pose 正規化
- `humanScale = 0.3409` → EGGYBOT は標準アバターの 34% と認識
- Animator が起動した瞬間、HumanDescription.skeleton で渡した localPosition は「目安」扱い
- **muscle 角度 + 標準骨長 × humanScale** で T-pose 再構築
- `hasTranslationDoF=false` だと Spine/Shoulder/UpperLeg の translation が消える

### 1-B. Mixamo クリップの Y position 強制注入
- Y Bot Hips Y = 0.94（人間 1.7m）
- humanScale 0.36 で scale → 0.34
- EGGYBOT root Y (0.20) + 0.34 = **0.54** に強制移動
- `lockRootHeightY=true` (Bake Y) でも、`keepOriginalPositionY=true` だと original Y が残る

### 1-C. EGGYBOT root の Y=180° rotation
- Mecanim の forward と Mixamo の forward が逆
- 結果として腕の世界 X が左右反転

---

## 2. 4 つの解決選択肢（user 判断待ち）

| # | 方針 | 効果 | コスト | 副作用 |
|---|---|---|---|---|
| **A** | **EGGYBOT root rotation (0, 180, 0) → (0, 0, 0)** に変更 | Mecanim と Mixamo の forward 方向揃う、左右反転消える | 中 | bone 配置の世界 X が反転、全 mesh 再調整 |
| **B** | **Avatar Type を Generic に切替** | Walking 諦め、bone position 100% 保持 | 大 | Mixamo Walking 使えない、Idle Bob のみ |
| **C** | **OnAnimatorIK で bone を Edit 値に強制 reset** | Walking 動作維持、user 設定 bone 位置で動く | 中 | 30-50 行スクリプト + 検証必要、IK 干渉のリスク |
| **D** | **諦めて Mecanim 補正後の値に合わせて Edit を再配置** | 一発で Edit / Play 一致 | 軽 | user の bone 配置のやり直し |

---

## 3. 明日のスタート手順

### Unity 開く前
```bash
cd C:/Users/taise/Documents/GitHub/ChaosDraftParty
git pull origin main
git status --short
```

### Unity で
1. `Sandbox_v16_humanoid.unity` を開く
2. ▶ Play 押して現状確認（Hips Y=0.45 に飛ぶはず）
3. Stop して Edit T-pose を確認

### Claude に
```
HANDOFF_20260430_step5_mecanim_residual.md を読んで、選択肢 A〜D のうち [どれ] で進めて。
```

---

## 4. 推奨: 選択肢 D（軽量、一発確認）

**理由:**
- A は大手術で副作用大
- B は Walking 機能放棄
- C は実装複雑
- D は「Mecanim 補正後の値」を見て、それで OK なら user が Edit T-pose をその値に合わせるだけ

### D の手順案
1. Play モードに入る（補正後の Hips Y=0.45 等の値が決まる）
2. user がその状態で「これで OK」と判断
3. Stop で Edit に戻る
4. Edit でも Play 補正後の値（Hips Y=0.45）に合わせて bone を再配置
5. Avatar 再構築
6. 再度 Play で Edit と Play が一致するか確認

ただし **「腕の左右反転」は D で解決しない**（Mecanim の forward と root の forward の不一致が原因）。これは A しか直せない。

---

## 5. 重要な事実 / 制約

### 動かせるもの
- mixamorig:Hips ~ Toe 系 bones（Walking で animator-driven）
- Hand_L, Hand_R, Foot_L, Foot_R mesh（mixamorig:LeftHand 等の child）

### 動かないもの
- EggybotArmature 系の bones (Hips, Spine, Chest, Neck, Head, CapHinge) ← bowl の SkinnedMR rootBone
- BodyLower, BodyUpperClear（root child に reparent 済、SDF と同じ静止）
- HeadMesh, Eye_L, Eye_R, Mouth（同上）
- BodyClip（root child、SDF center 静止）

### 確定値
- BodyClip world (0, 0.50, 0)、scale 0.672、radius 0.336 → bowl 全体カバー
- shader: SDF only（view-occlusion 削除済）
- material `EGGYBOT_Pink_Arm`: `_Cull = 2 (Back)`

---

## 6. 関連 commit / file

- commit `0dbf3c1`: SDF clip 完成
- shader: `Assets/_Project/Art/Shaders/EGGYBOT_ArmClip.shader`
- binder: `Assets/_Project/Scripts/Core/EggybotShaderBinder.cs`
- material: `Assets/_Project/Art/Materials/EGGYBOT_Pink_Arm.mat`
- avatar: `Assets/_Project/Animations/EggybotV16Avatar.asset`
- Walking: `Assets/ThirdParty/Mixamo/Animations/Y Bot@Walking.fbx`

## 7. 失敗履歴 / 教訓
- `memory/session_2026-04-29_eggybot_v16_journey.md`（失敗 6 回の時系列）
- `memory/feedback_shader_pixel_first.md`（実測 → 並列 agent → 提案）
- `memory/feedback_bone_mesh_hierarchy.md`（SkinnedMR.bones は Transform.parent と別）
- `.claude/skills/chaosdraftparty-gotchas/SKILL.md` 罠 #51, #52
