# HANDOFF — 2026-04-28 v16 humanoid 切替中

**作成: 2026-04-28 21:00 JST (Mac で作業終了直前)**
**次の作業環境: Windows デスクトップ PC**
**ブランチ: `main` (最新 commit `aeb1a2e`)**

---

## 0. これは何か

EGGYBOT v16 の humanoid 化 (Mixamo Walking 流せる状態) 作業の **マシン跨ぎ引き継ぎ書**。
Windows デスクトップで続きから再開するため、全状態と次の手順をここに集約した。

---

## 1. 大目標 (Goal)

ChaosDraftParty (卒業制作 2026-04〜09) のマスコット **EGGYBOT** を、
**Mixamo の humanoid アニメーションを無加工で流せる + 形状歪みゼロ + bowl 内部にめり込まない** 状態にする。

最終形のイメージ:
- ガチャカプセル型 (ピンク不透明胴体 + クリア上半分)
- 頭は球体マスコット顔
- 手足は短い球 (Unity primitive)
- Mixamo Walking / Idle / Jump などのアニメをそのまま再生
- M3 でカプセル開閉ギミック (CapHinge bone)

---

## 2. やりたいこと (Goals — 直近)

| 順 | 内容 | 状態 |
|----|------|------|
| 1 | 透明 humanoid rig を v16 に組み込む | ✅ Step 1 完了 |
| 2 | rig 用 Avatar (humanoid) を生成 | ✅ Step 2 完了 |
| 3 | mesh (body/head/hands/feet) を rig bone の child に再 parent | ✅ Step 3 完了 |
| 4 | Animator を humanoid に切替、Mixamo Walking を流す | ✅ Step 4 完了 |
| **5** | **Windows で再開: bone 長さを v16 体格に合わせて微調整** | ⏸️ **次ここから** |
| 6 | M3 用 CapsuleOpen anim を作る (CapHinge 0→90°) | 未着手 |
| 7 | クリーンアップ: 旧 EggybotArmature 削除、main Sandbox 統合 | 未着手 |

---

## 3. 現在の状態 (Current State)

### ファイル構成

```
Assets/_Project/Scenes/
├─ Sandbox_v16.unity          ← 動作 baseline (clip 完成形, prefab 接続済)
└─ Sandbox_v16_humanoid.unity ← 作業用 (humanoid 切替済, prefab unpack 済) ★今ここ

Assets/_Project/Animations/
├─ EggybotIdleBob.anim        ← 旧 generic idle bob (humanoid 化で未使用)
├─ EggybotV16AC.controller    ← 旧 generic Animator Controller (未使用)
├─ EggybotV16Avatar.asset     ← Step 2 で生成した humanoid Avatar ★
└─ EggybotV16HumanoidAC.controller ← Step 4 で作成、Walking が default state ★

Assets/ThirdParty/Mixamo/Animations/
├─ Y Bot@Walking.fbx          ← Mixamo Walking (humanoid)
└─ Y Bot@TwistDance.fbx       ← 別アニメ (試したい場合)
```

### Sandbox_v16_humanoid.unity の Hierarchy (重要)

```
EGGYBOT_v16  (Animator: avatar=EggybotV16Avatar, controller=EggybotV16HumanoidAC)
├─ EggybotArmature           ← orphan (mesh 非接続、いずれ削除予定)
└─ EggybotHumanoidRig         ← 透明 humanoid rig (Empty Transform のみ)
    └─ mixamorig:Hips
        ├─ mixamorig:Spine → Spine1 → Spine2
        │   ├─ mixamorig:LeftShoulder → LeftArm → LeftForeArm → LeftHand
        │   │                                                    └─ Hand_L (球) ★
        │   ├─ mixamorig:Neck → Head
        │   │                  └─ HeadMesh, Eye_L, Eye_R, Mouth ★
        │   └─ mixamorig:RightShoulder → RightArm → RightForeArm → RightHand
        │                                                          └─ Hand_R ★
        ├─ mixamorig:LeftUpLeg → LeftLeg → LeftFoot → LeftToeBase → LeftToe_End
        │                                  └─ Foot_L (球) ★
        ├─ mixamorig:RightUpLeg → RightLeg → RightFoot → RightToeBase → RightToe_End
        │                                    └─ Foot_R ★
        ├─ BodyLower ★              ← Hips 直下 rigid parent
        ├─ BodyUpperClear ★          ← 同上
        └─ CapsuleVolume ★           ← SDF clip 中心 (Hips 配下なので body と一緒に bob)
```

★ = 可視 mesh / collider 有り
他は全部 Empty Transform = 不可視

### 動作確認済 (Step 4 終了時点)

- Play mode で Mixamo Walking ループ再生 ✓
- 全 mesh が bone に追従 (歩行時に body bob, 手足 swing) ✓
- SDF clip 継続動作 (CapsuleVolume が Hips に追従、bowl 内部が discard) ✓
- Game / Scene view 一致 ✓

---

## 4. 失敗したこと (What failed and why)

### 4.1 v10-v15 の試行錯誤 (4/19-4/24)

| ver | 試したこと | 失敗理由 |
|-----|-----------|---------|
| v10 | Bridge Edge Loops で manifold 化 | shape は出たがリギングしづらい |
| v11 | 手足 organic 化 + カプセル開閉 rig | bone weight ごとの distortion |
| v12 | Subsurf + interior fix | moiré 改善するが root 問題は残る |
| v13 | 内壁貫通穴 fix + rim crease | shape は綺麗だが skinning で歪む |
| v14 | bisect で seam 根絶 | seam 消えたが「引き伸ばされてる」継続 |
| v15 | Metaball で「一つの粘土」body | 形状は理想形だが skinning artifact 解消せず |

**根本原因 (v15.18 で確定):**
- SkinnedMeshRenderer の bind-pose 補間で 100% bone weight でも微小に歪む
- → **形状で解決 + Skinning を一切使わない** に方針転換 (v16 へ)

### 4.2 v15.17 SDF clip + view-occlusion shader の沼

`Assets/_Project/Art/Shaders/EGGYBOT_ArmClip.shader` を 3 回書き直した:
1. v15.17: Body 半球内 SDF discard → bowl 内部の侵入手 pixel が消える ◎
2. v15.18.2: View-occlusion 追加 (camera→vert ray が body sphere と交差なら裏側 hand を discard)
3. shader だけでは Cull Back × 薄殻 BodyLower の問題が完全には解けず

**学び:** shader trick は完璧にはならない、形状＋構造で解くのが本筋。  
**v16 で残ってる shader:** ArmClip は今も使ってる (Hand/Foot mesh に適用、bowl 内部の埋まり消去用)。

### 4.3 v16 Step 3 で Prefab unpack が必要だった

- EGGYBOT_v16 は `eggybot_v16.fbx` の Model Prefab Instance だった
- 内部 mesh (BodyLower 等) を re-parent しても Unity が **silent revert** する仕様
- 解決: `PrefabUtility.UnpackPrefabInstance(root, Completely, AutomatedAction)`
- 影響: Sandbox_v16_humanoid 限定で unpack。FBX 更新時の自動反映を失う (許容)

### 4.4 bone 直交軸の落とし穴 (Step 1)

- 最初 Mixamo の bone localPos を verbatim コピーした → 腕も足も上向きに伸びて T-pose にならず
- 原因: Mixamo は bone 自身に rotation 持っており、localPos.y が「bone Y軸 (= bone の長さ方向)」
- 解決: 全 bone identity rotation で再構築、T-pose を localPosition のみで表現
  - 腕: -X (左) / +X (右) 方向
  - 脚: -Y (下) 方向
  - つま先: +Z local (= world -Z = キャラ前)

### 4.5 EGGYBOT_v16 root の Y=180° rotation 罠

- 既存 root は Y=180° 回転してる (FBX import 都合)
- そのため local +X = world -X、local +Z = world -Z
- Mixamo bone の +X が character "right" だが、root rotation 経由で world -X になるため画面では左に見える
- 一度間違えて Z+60 を wave に入れて手が下に行った経験あり

---

## 5. やること (Todo for Windows)

### Step 5: bone 長さの user 微調整 ★ ここから

**現状の問題:**
- Mixamo は人間 1.9m 想定で arm を 50cm 振る
- v16 (0.7m) では swing 幅が相対的に大きく、**手が顔の高さまで来る**

**手順:**

1. Unity を起動、`Sandbox_v16_humanoid.unity` を開く
2. Play mode で Walking を再生したまま観察
3. Hierarchy で `EGGYBOT_v16/EggybotHumanoidRig/mixamorig:Hips/...` を辿り、Inspector で各 bone の `Position` を調整

**調整目安 (Mixamo×0.37 → 推奨):**

| Bone | 現在 (X,Y,Z) | 推奨方向 | 理由 |
|------|-------------|---------|------|
| `mixamorig:LeftArm` | (-0.0477, 0, 0) | (-0.030, 0, 0) | 上腕短く |
| `mixamorig:LeftForeArm` | (-0.1014, 0, 0) | (-0.050, 0, 0) | 前腕短く |
| `mixamorig:LeftHand` | (-0.1021, 0, 0) | (0, 0, 0) | hand 球を forearm 端に置く |
| `mixamorig:RightArm` | (+0.0477, 0, 0) | (+0.030, 0, 0) | 同上、Right |
| `mixamorig:RightForeArm` | (+0.1014, 0, 0) | (+0.050, 0, 0) | |
| `mixamorig:RightHand` | (+0.1021, 0, 0) | (0, 0, 0) | |
| `mixamorig:LeftLeg` | (0, -0.150, 0) | (0, -0.100, 0) | 太もも短く |
| `mixamorig:LeftFoot` | (0, -0.156, 0) | (0, -0.100, 0) | 脛短く |
| `mixamorig:RightLeg` / `RightFoot` | 同 left | 同 left | |

調整中は **球の世界位置を bowl 表面付近 (T-pose 時 Y≈0.4-0.6)** に保つ。

4. 良い感じになったら Play mode を **停止** してから値を Inspector で再入力 (Play mode の値は保存されない)
5. 検証用 screenshot を撮る (`mcp__UnityMCP__manage_camera screenshot orbit_angles=4`)
6. commit `feat(eggybot): v16 humanoid Step 5 — bone 長さ微調整`
7. push

### Step 6: M3 CapsuleOpen anim 作成

- CapHinge bone は今 EggybotArmature に残ってる (orphan rig 内)
- 新 humanoid rig に **追加で CapHinge bone** が必要
- 作り方: mixamorig:Hips の child に `CapHinge` Empty を作る (localPos 0, 0.30, 0 = bowl 上端)
- アニメ: `EggybotCapsuleOpen.anim` 新規、CapHinge.localEulerAngles.x 0→90° (0.6s)
- 同時に Head bone を pop scale (0→1) するなら別 anim trigger

### Step 7: クリーンアップ + main 統合

- `EggybotArmature` (orphan) を Hierarchy から削除
- 動作を最終確認
- Sandbox_v16.unity に同じ humanoid 構造を移植 OR Sandbox_v16_humanoid.unity を main にする
- 旧 generic 関連 (EggybotIdleBob.anim, EggybotV16AC.controller) を削除
- prefab 化検討 (`Assets/_Project/Prefabs/Player/EGGYBOT_v16.prefab`)

---

## 6. Windows でのセットアップ

### 6.1 リポジトリ取得

```powershell
# Windows でリポジトリの最新を pull
cd C:\Users\<you>\Documents\Dev
git clone https://github.com/Shimagon/ChaosDraftParty.git
# または既に clone 済なら:
cd ChaosDraftParty
git pull origin main
```

### 6.2 Unity / Tools

- **Unity Hub** + **Unity 6 LTS** をインストール (Mac と同じバージョン推奨)
- Unity Hub から `ChaosDraftParty` フォルダを Add → Open
- Library/ Temp/ Logs/ は再生成されるので待つ
- Visual Studio または JetBrains Rider (C# 編集用)
- **Blender 4.x** (使う場合)

### 6.3 Unity MCP セットアップ (Windows 用)

`docs/handoff/memory_snapshot/reference_chaosdraftparty_unity_mcp.md` を参照。
要点:
- Claude Code 側 (Claude Desktop or CLI) で `CoplayDev/unity-mcp` を user scope で登録済
- Unity 側で MCP plugin URL から install
- 詳細手順 / 落とし穴は上記 reference 参照

### 6.4 Claude Code memory の同期

このリポジトリの `docs/handoff/memory_snapshot/` に Mac で使ってた global memory のコピーが入ってる (40 ファイル)。
Windows の Claude にも同じ memory を持たせたい場合:

```powershell
# Windows
mkdir -Force "$HOME\.claude\projects\-C--Users-<you>-Documents-Dev-ChaosDraftParty\memory"
copy docs\handoff\memory_snapshot\*.md "$HOME\.claude\projects\-C--Users-<you>-Documents-Dev-ChaosDraftParty\memory\"
```

(プロジェクトパスのスラッシュ→ハイフン変換は Claude Code の規約に従う)

### 6.5 .claude/ プロジェクトローカル設定

`.claude/` フォルダはリポジトリに含まれてる:
- `agents/` (todo-progressor, unity-error-triager, unity-reviewer)
- `skills/` (chaosdraftparty-gotchas, meshy-unity-workflow, unity-urp-quality-uplift)
- `settings.json` (project-specific Claude Code settings)

`settings.local.json` と `scheduled_tasks.lock` は gitignore 済 (Mac/Windows 別物で OK)。

### 6.6 Python tools

```powershell
pip install python-pptx
```

`tools/make_lab_pptx_20260428.py` は Mac/Windows 共通で動く (パス計算は os.path 使ってる)。

---

## 7. クロス OS の注意点 (詳細は `docs/handoff/memory_snapshot/feedback_cross_os_windows.md`)

- **改行コード**: `.gitattributes` で LF 強制済み
- **日本語パス**: `Assets/ThirdParty/Mixamo/Animations/Y Bot@Walking.fbx` の半角スペースは OK、`キャラクター/` フォルダもあるので注意
- **ファイル名大文字小文字**: macOS は case-insensitive、Windows も既定 case-insensitive
- **Unity の meta GUID**: Mac で生成した GUID は Windows でも有効

---

## 8. 直近の commit 履歴 (humanoid 化作業分)

```
aeb1a2e feat(eggybot): v16 humanoid Step 4 — Mixamo Walking が流れた
f6703ef feat(eggybot): v16 humanoid Step 3 — 球/body/head を humanoid bone に再 parent
729767a feat(eggybot): v16 humanoid Step 2 — Avatar 構築 (isValid=True)
ba6844f feat(eggybot): v16 humanoid Step 1 — 透明 humanoid rig 骨格構築
90bcff7 feat(eggybot): v16 — CapsuleVolume + ArmClip で内部 hand/foot 消去
4230c12 feat(eggybot): v16 — rig 動作検証完了 + Cull Off 専用 material 追加
15a1f21 feat(eggybot): v16 Phase B — body_lower の foot 凸を vertex 削除
04f4bed feat(eggybot): v16 rig — limb anchor 追加
b35e07c fix(eggybot): v16 IdleBob — anim path を root に移動
13e751c feat(eggybot): v16 — 手足 mesh/bone 削除 + Unity primitive 手 + generic Animator
```

---

## 9. 困ったときの参照先

| 質問 | 参照ファイル |
|------|------------|
| Claude との作業ルール | `CLAUDE.md` (リポジトリルート) |
| 不安はユーザー、機械的は Claude | `docs/handoff/memory_snapshot/feedback_division_of_labor.md` |
| 視覚デザイン勝手に変えない | `docs/handoff/memory_snapshot/feedback_ask_before_visual_change.md` |
| 実装前に必ず確認 | `docs/handoff/memory_snapshot/feedback_ask_before_implement.md` |
| EGGYBOT v16 SDF 構成 | `docs/handoff/memory_snapshot/project_eggybot_v16_clip.md` |
| 罠回避 skill | `.claude/skills/chaosdraftparty-gotchas/SKILL.md` |
| Meshy ワークフロー | `.claude/skills/meshy-unity-workflow/SKILL.md` |
| URP 品質チェック | `.claude/skills/unity-urp-quality-uplift/SKILL.md` |
| 進捗 / 心境スライド | `docs/presentations/2026-04-28_キャラクター総括.pptx` |

---

## 10. Windows で再開する際のおすすめプロンプト

```
docs/handoff/HANDOFF_20260428_v16_humanoid.md を読んで、現状を把握してください。
今日のタスクは Step 5 の bone 長さ微調整です。慎重に、不安なところは俺がやります。
```
