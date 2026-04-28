# HANDOFF — 2026-04-28 Step 5 進捗 (Windows セッション末尾)

**前提:** `HANDOFF_20260428_v16_humanoid.md` の続き。Windows で Step 5 着手したが、設計上の問題が発覚し、bone と mesh を一時分離して再配置中。

---

## 0. 今日のサマリー (Windows 初日)

### 環境セットアップ完了
- Unity 6000.4.3f1 install
- Unity MCP (CoplayDev/unity-mcp v9.6.6) + Deps (Roslyn / ProBuilder / Cinemachine / VFX Graph)
- Mac drift `MainMenu.unity` を `git checkout` で破棄
- `git lfs pull` で Mixamo Walking 等取得済み
- Animation Rigging (1.4.1) install + BoneRenderer で骨格可視化

### Step 5 着手と問題発覚
1. HANDOFF.md の推奨値で全 bone 短縮 → 動かなくなった
2. 原因: **EggybotHumanoidRig が EGGYBOT_v16 から脱出した事故** (再 parent で復旧)
3. bone を元値に戻す → 動くが「腕が頭まで来る」問題再発
4. **真因判明**: bone と mesh の空間的乖離
   - bone は v16 体格 (humanScale 0.51) で体内に集中
   - mesh (Hand_L/R, Foot_L/R) は元 v16 設計の rigid offset で体外に配置
   - Walking で bone が回転すると **テコ効果で mesh が大振り** → 顔の高さまで来る
5. 対処: bone と mesh の再設計に方針転換

---

## 1. 現在のシーン状態 (`Sandbox_v16_humanoid.unity`)

### Hierarchy (重要変更)
```
EGGYBOT_v16 (Animator + BoneRenderer + EggybotShaderBinder)
├─ EggybotArmature (orphan)
├─ EggybotHumanoidRig
│  └─ mixamorig:Hips (bone のみ、mesh 全部外してある)
│     ├─ mixamorig:Spine → ... → Head
│     ├─ mixamorig:LeftUpLeg → ... → LeftToe_End
│     └─ mixamorig:RightUpLeg → ... → RightToe_End
├─ Hand_L         ← 元: LeftHand bone の child
├─ Hand_R         ← 元: RightHand bone の child
├─ Foot_L         ← 元: LeftFoot bone の child
├─ Foot_R         ← 元: RightFoot bone の child
├─ HeadMesh       ← 元: Head bone の child
├─ Eye_L          ← 元: Head bone の child
├─ Eye_R          ← 元: Head bone の child
├─ Mouth          ← 元: Head bone の child
├─ BodyLower      ← 元: Hips bone の child
├─ BodyUpperClear ← 元: Hips bone の child
└─ CapsuleVolume  ← 元: Hips bone の child (SDF center, scale 0.672)
```

**全 mesh が bone hierarchy から独立**。bone を Inspector で動かしても mesh は静止。
ユーザーが Blender 的に左右対称編集できないので、Claude が initial 配置 → user が微調整する分業。

### Bone 現在 World Position (機械配置)

| Bone | X | Y | 備考 |
|---|---|---|---|
| HeadTop_End | 0 | 0.95 | |
| Head | 0 | 0.85 | HeadMesh と一致 |
| Neck | 0 | 0.60 | |
| Spine2 | 0 | 0.45 | |
| Spine1 | 0 | 0.40 | |
| Spine | 0 | 0.35 | |
| Hips | 0 | **0.30** | bowl 下半分 (前回 0.50 から下げた) |
| LeftShoulder / RightShoulder | ±0.336 | 0.45 | カプセル縁 |
| LeftArm / RightArm | ±0.346 | 0.42 | |
| LeftForeArm / RightForeArm | ±0.351 | 0.41 | |
| LeftHand / RightHand | ±0.355 | 0.405 | Hand mesh と一致 |
| LeftUpLeg / RightUpLeg | ±0.07 | 0.26 | |
| LeftLeg / RightLeg | ±0.10 | 0.22 | |
| LeftFoot / RightFoot | ±0.13 | 0.18 | |
| LeftToeBase / RightToeBase | ±0.13 | 0.16 | Foot mesh と一致 |
| LeftToe_End / RightToe_End | ±0.13 | 0.16 | z=0.10 (前向き) |

### Mesh 現在 World Position
- Hand_L: (0.355, 0.405, 0)
- Hand_R: (-0.355, 0.405, 0)
- Foot_L: (0.13, 0.16, -0.02)
- Foot_R: (-0.13, 0.16, -0.02)
- HeadMesh / Eye_L / Eye_R: 元位置維持
- Mouth: 元位置維持
- BodyLower / BodyUpperClear / CapsuleVolume: (0, 0.50, 0)

---

## 2. 明日の続き手順

### Step 5a: bone の最終微調整 (user 手動 + Claude assist)
- Inspector で各 bone の Position を user が触る
- 困ったら Claude に execute_code で一括変更依頼
- **左右対称キープ**: 片側だけ動かしたら反対側も忘れずに

### Step 5b: mesh を bone に再 attach
完成後、execute_code で:
- Hand_L → mixamorig:LeftHand の child (localPos 0)
- Hand_R → mixamorig:RightHand の child (localPos 0)
- Foot_L → mixamorig:LeftFoot の child (localPos 0)
- Foot_R → mixamorig:RightFoot の child (localPos 0)
- HeadMesh, Eye_L, Eye_R → mixamorig:Head の child (localPos は元値復元)
- Mouth → mixamorig:Head の child (localPos は元値復元)
- BodyLower, BodyUpperClear, CapsuleVolume → mixamorig:Hips の child (localPos は元値復元)

### Step 5c: Walking 確認
- ▶ Play で再生
- 動かなければ **Avatar 再構築** (HumanDescription を新 bone position で作り直し)
  - `AvatarBuilder.BuildHumanAvatar()` 経由
  - 既存 EggybotV16Avatar.asset を上書き or 新規

### Step 5d: 失敗時の復旧
- `git checkout -- Assets/_Project/Scenes/Sandbox_v16_humanoid.unity`
- これで今夜の作業全部破棄して、Step 4 終了時の状態に戻る (Walking が流れる状態)

---

## 3. 既知のリスク

| リスク | 発生したか | 対処 |
|---|---|---|
| EggybotHumanoidRig が scene root に脱出 | ✓ 1 度発生 | manage_gameobject で再 parent 済み |
| bone を動かしすぎて Avatar invalid | △ 動くがズレる | Avatar 再構築で対処予定 |
| mesh の lossyScale が SetParent で歪む | 未発生 | 注意深く観察 |
| SDF clip で mesh が消える (bowl 内部) | 過去に発生 | 現在 bone と mesh 一致設計で回避 |

---

## 4. 戻したい時のコマンド (緊急用)

```bash
# 今夜の作業を全部破棄 (Step 4 終了時に戻す)
git checkout -- Assets/_Project/Scenes/Sandbox_v16_humanoid.unity

# Animation Rigging 等の install 取消 (普通は不要)
git checkout -- Packages/manifest.json Packages/packages-lock.json
```

---

## 5. メモ
- **Avatar Inspector が空白問題**: `EggybotV16Avatar.asset` は programmatic 構築なので Configure UI 出ない。muscle limit 編集は scripted のみ。
- **HumanScale = 0.5141**: キャラサイズに合わせて自動算出
- **Mixamo Walking クリップ**: Y Bot (1.9m) の muscle 動作。muscle 角度自体が大きいので、bone 短縮では解決しない (Mac の Step 5 推奨値の前提が誤り)
- **bone の真因対策**: bone と mesh を空間的に一致させる + Avatar 再構築 が王道
