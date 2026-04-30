# HANDOFF — 2026-04-30 朝（コンパクト前引き継ぎ）

**前提:** `HANDOFF_20260428_step5_progress.md`、`HANDOFF_20260430_step5_mecanim_residual.md` の続き。今朝の作業 (MCP 再接続、permission 設定変更、シーン 0dbf3c1 状態に強制 reload) を踏まえた最新ハンドオフ。

---

## 0. 今すぐ次のセッションが知るべき要点

### 現在のシーン状態（Sandbox_v16_humanoid.unity）
- **0dbf3c1（user が「めっちゃよくなってきた」と褒めた SDF clip 完成版）に再 load 済み**
- Edit T-pose で bone は綺麗に v16 体格に収まってる
- BodyClip 判定球 (0, 0.50, 0)、半径 0.336 で bowl 全体カバー
- shader は SDF clip のみ（view-occlusion 削除済）
- material `EGGYBOT_Pink_Arm._Cull = 2 (Back)`

### 朝の重要トラブル: Unity が git checkout を上書きしてた

**症状:** `git checkout 0dbf3c1 -- Sandbox_v16_humanoid.unity` で作業ツリーに 0dbf3c1 状態を書いても、Unity 起動中はメモリ内の状態（edf9138 = ぐちゃった状態）でファイルを上書きする。

**回避手順:**
1. Unity で別シーン (例: `_temp_unload`) を作って active を切替 → Sandbox を unload
2. `git checkout 0dbf3c1 -- ...` で作業ツリーを書換え
3. `manage_scene load path=...Sandbox_v16_humanoid.unity` で再 load
4. 一時シーンを `manage_asset action=delete` で削除

これで Unity が新ファイルを読む。**「git checkout が機能しない時は Unity が捕まえてる」と覚える**。

---

## 1. permission 変更（settings.json 共有反映）

`.claude/settings.json` の deny から git destructive 系を削除、allow に追加。Mac/Windows 両方で `git pull` すれば反映される。

| Deny に残ってるもの（流石に危険） | Allow に追加したもの |
|---|---|
| Bash(rm -rf:\*), Bash(sudo:\*) | git reset / reset --hard / revert / rebase / cherry-pick / clean / push --force / push -f / push --force-with-lease / branch -d / branch -D / merge / remote / config / checkout -- |

→ `git reset --hard` などが**確認なしで実行可能**になった。

---

## 2. SDF clip 確定構成（0dbf3c1 で確立、変更禁止推奨）

| 項目 | 値 | 場所 |
|---|---|---|
| 判定球 center | world (0, 0.50, 0) | `BodyClip` GameObject (EGGYBOT_v16 root の child) |
| 判定球 radius | 0.336 | `EggybotShaderBinder.bodyRadius` |
| clipMargin | 5mm | `EggybotShaderBinder.clipMarginMM` |
| bodyCenterOffsetLocal | (0, 0, 0) | offset なし |
| trackHipsBone | false | bowl は Walking で動かないので追従不要 |
| BodyClip.localScale | 0.672 | radius 0.336 換算 |
| shader | `ChaosDraftParty/EGGYBOTArmClip` | view-occlusion 削除済み |
| material `_Cull` | 2 (Back) | 両面描画やめ |
| Walking bake | `lockRootHeightY=true, heightFromFeet=false, keepOriginalPositionY=true, keepOriginalOrientation=true` | Y root motion 抑制 |

**注:** bowl mesh は半径 0.336 の球として表現される。`BodyLower.bounds.center=(0,0.33,0)` は bone weight 由来で**幾何中心ではない**。判定球の center は (0, 0.50, 0)（bowl 全体の球中心）。

---

## 3. 残課題: Mecanim T-pose 補正問題

### 症状
User が Inspector で bone を綺麗に配置（例: Hips Y=0.26, Hand ±0.36, Y=0.42）しても、Avatar 再構築 + Animator 起動で:
- **Hips Y が 0.45〜0.49 に持ち上がる**（+0.19m）
- **左右反転**（LeftShoulder X = -0.36 → +0.38 に変わる）
- 全体的に体が浮く

### 真因（並列 agent 3 個で確定）
1. Mecanim humanoid Avatar が `humanScale=0.36` で T-pose を muscle constraint で再正規化
2. Mixamo クリップの Hips Y (= 0.94) が humanScale で scale → 0.34 → world 0.54 に強制注入
3. EGGYBOT root rotation Y=180° で Mecanim と Mixamo の forward が逆 → 腕の左右反転
4. `hasTranslationDoF=false` で Spine/Shoulder/UpperLeg の translation が消える

### 4 つの解決選択肢（user 判断待ち）

| # | 方針 | 効果 | コスト | 副作用 |
|---|---|---|---|---|
| **A** | EGGYBOT root rotation (0,180,0) → (0,0,0) | 左右反転消える、Mecanim と Mixamo の forward 揃う | 中 | bone 世界 X 反転、全 mesh 再調整 |
| **B** | Avatar Type を Generic に切替 | Walking 諦め、bone position 100% 保持 | 大 | Mixamo Walking 使えない、Idle Bob のみ |
| **C** | OnAnimatorIK で bone を Edit 値に強制 reset | Walking 動作維持、user 設定で動く | 中 | 30-50 行スクリプト + 検証 |
| **D** | 諦めて Mecanim 補正後の値に Edit を合わせる | 一発 Edit/Play 一致 | 軽 | bone 配置やり直し、左右反転は直らない |

詳細は `HANDOFF_20260430_step5_mecanim_residual.md` 参照。

---

## 4. 朝の失敗履歴（同じ罠を踏まないため）

### 失敗 7: bone を Edit で動かすと Avatar 補正と喧嘩する
- user が Inspector で bone 編集 → Avatar 再構築 → Play で違う位置に
- Mecanim humanScale 0.36 + Mixamo Y=0.94 注入で +0.19m 持ち上げ
- **Edit と Play で T-pose が違う → 永久にぐちゃる**

### 失敗 8: git checkout だけで Unity が再 load しない
- Unity 起動中に `git checkout` で作業ツリー書換え → Unity がメモリ内状態で上書き
- **必ず別シーン load → checkout → 再 load の手順**

### 失敗 9: bone 動かす方向で解こうとする
- 結局 Mecanim 補正と無限に喧嘩する
- **正解は「Mecanim と喧嘩しない方針」（選択肢 A, B, C, D）**

---

## 5. 確定した教訓 / docs

### memory（Windows ローカル `.claude/projects/.../memory/`）
- `MEMORY.md` ← index
- `project_eggybot_v16_clip_v16final.md` ← SDF 確定構成
- `feedback_shader_pixel_first.md` ← 実測 → 並列 agent → 提案
- `feedback_bone_mesh_hierarchy.md` ← SkinnedMR.bones と Transform.parent の罠
- `session_2026-04-29_eggybot_v16_journey.md` ← 失敗 6 個の時系列

### docs（リポジトリ共有）
- `CLAUDE.md` v1.1 「Shader / SDF / Visual デバッグの原則」セクション
- `.claude/skills/chaosdraftparty-gotchas/SKILL.md` 罠 #51, #52
- `docs/handoff/HANDOFF_20260428_step5_progress.md`
- `docs/handoff/HANDOFF_20260430_step5_mecanim_residual.md`
- `docs/handoff/HANDOFF_20260430_compact.md` ← 本文書（最新）

---

## 6. 次のセッション開始手順

### git pull
```bash
cd C:/Users/taise/Documents/GitHub/ChaosDraftParty
git pull origin main
```

### Unity 起動 + シーン open
1. Unity Hub から ChaosDraftParty を Open
2. `Sandbox_v16_humanoid.unity` をダブルクリック
3. ▶ Play で動作確認（bone +0.19m 持ち上がるはず、これは既知問題）

### Claude にプロンプト
```
HANDOFF_20260430_compact.md を読んで現状把握して。
Mecanim T-pose 補正問題を選択肢 [A/B/C/D] で進めて。
```

---

## 7. 推奨次アクション

**最も軽量かつ実用的: 選択肢 D（補正後値で再配置）**

手順:
1. ▶ Play 押して、Mecanim 補正後の bone world 値を実測（execute_code で）
2. その値を「正解の T-pose」として記録
3. Edit でも同じ位置に bone を配置（Animator disabled で手動 set）
4. Avatar 再構築（その位置を T-pose として記録）
5. Play で Edit と Play が一致するか確認

ただし**左右反転は D で直らない**。気になるなら追加で **A（root rotation 0）** で根本修正。

**M1 の納期優先なら**: 選択肢 D で「動く形」を作って、Mecanim と完全に喧嘩しない設計に妥協する。

---

## 8. 確実な事実 / 制約

### 動かないもの（触らない）
- bowl (BodyLower / BodyUpperClear) — `EggybotArmature/Hips` で bind、Walking で動かない
- BodyClip — root child で固定 (0, 0.50, 0)
- HeadMesh / Eye_L / Eye_R / Mouth — 0dbf3c1 では Hips/Head の child（後で root に外した経緯あり、今は 0dbf3c1 状態）

### 動かしたい時に触るもの
- mixamorig:Hips ~ Hand/Foot (Walking で動く)
- Hand_L/R, Foot_L/R mesh (bone child、Walking で連動)

### 既知の罠
- bone を Edit で動かしても Play で Mecanim 補正で書き戻される
- `git checkout` は Unity 起動中だと上書きされる
- SkinnedMR.bones と Transform.parent は別物（feedback_bone_mesh_hierarchy.md）

---

**最終更新:** 2026-04-30 朝（コンパクト前）
**HEAD:** edf9138（handoff 残ってる）
**作業ツリー:** 0dbf3c1 + シーン/Avatar/Walking.meta が staged
**次の commit:** 本 handoff 追加 + 状態 checkpoint
