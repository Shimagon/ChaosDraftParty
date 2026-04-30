# HANDOFF — 2026-04-30 EGGYBOT v16 Step 5 完全完了

**前提:** `HANDOFF_20260430_compact.md`、`HANDOFF_20260430_step5_mecanim_residual.md` の続き。Step 5 残課題（Mecanim T-pose 補正）を解決し、AI レビューループ機構も導入完了。

---

## 0. 結論

ユーザー「**きた！！！！！！これ！**」承認で **EGGYBOT v16 Step 5 完全完了**。

- ✅ SDF clip（前回完了、commit `0dbf3c1`）
- ✅ bone 再配置 + 左右対称化（commit `716af15`）
- ✅ Mecanim T-pose 補正 = LateUpdate Hips 直書きで解決（commit `c155759`）
- ✅ AI レビューループ機構導入（commit `8ff1fc7`）

---

## 1. 解決した残課題

### Edit/Play T-pose 不一致（+0.19m 浮上）

**真因（Agent A 実測で確定）:**
- Walking クリップの `RootT.y = 0.9683`（Mixamo 1m 身長想定）が humanScale=0.36 で scale → world Y +0.19m を Hips に強制注入
- Avatar 再構築では治らない（humanScale + Mixamo 想定の根本乖離）
- `anim.bodyPosition` shift は `lockRootHeightY=true` で baked された Y を打ち消せない

**解決:**
- `Assets/_Project/Scripts/Core/EggybotPoseLock.cs` を追加
- LateUpdate で `hips.position.y += yOffset (-0.19f)` で全身一括シフト
- Animator.Update 後の LateUpdate で Mecanim 出力に直接補正

**コード:**
```csharp
[RequireComponent(typeof(Animator))]
public class EggybotPoseLock : MonoBehaviour {
    [SerializeField] float yOffset = -0.19f;
    Animator anim; Transform hips;
    void Awake() {
        anim = GetComponent<Animator>();
        hips = anim.GetBoneTransform(HumanBodyBones.Hips);
    }
    void LateUpdate() {
        var p = hips.position;
        p.y += yOffset;
        hips.position = p;
    }
}
```

EGGYBOT_v16 root に component 追加、yOffset=-0.19f、シーン保存済み。

---

## 2. AI レビューループ機構導入

User 要求「コードを変えたり修正した時に、別の AI がレビューして、それを元に修正してまたレビューして実装するみたいな仕組み」を実装。

**作成ファイル:**
- `.claude/agents/code-reviser.md` — 修正役 subagent
- `.claude/skills/ai-review-loop/SKILL.md` — orchestrator skill
- `tools/ai-review-loop/run.sh` + `README.md` — 将来の CI 化用ラッパー

**設計判断:**
- Anthropic Python SDK 入れない（Claude Code subagent で済む、依存ゼロ）
- 既存 `unity-reviewer` を改造せず、そのまま指摘役として呼ぶ
- 3 周で打ち切り（無限ループ防止）
- pre-commit hook 化保留（5 秒 commit が 30 秒になるコスト > 利益）

**初実用結果（EggybotPoseLock.cs 74 行に対して）:**
- 1 周目: HIGH 1 + MEDIUM 2 + LOW 2 → PASS w/ nits
- 1 周目修正: コメント追記で全 FIXED（LOW-2 は DEFERRED）
- 2 周目: 全解決確認、PASS で loop 終了

合計 5 件の指摘が **commit 前** に取り除かれた。

**起動方法:** Claude セッションで「今の pending 変更に AI review loop 回して」と頼むだけ。

---

## 3. 残課題（M1 後で OK、放置可）

### 左右反転（root rotation Y=180° 起因）
- LeftShoulder world X が Edit -0.358 → Play +0.353 に符号反転
- 腕 mesh は左右対称の球なので **見た目バレない**、放置 OK
- 直すなら root rotation Y=180→0 + 子 mesh の方向再調整

### forearm 2.6 倍拡大（hasTranslationDoF=false 起因）
- Edit Shoulder→Hand 距離 0.013m → Play 0.034m
- stretch muscle が骨長補正に流用される
- 手が体から少し離れて見えるが Hand mesh 球で隠れる、放置 OK
- 直すなら armStretch=0 + hasTranslationDoF=true で Avatar 再構築（ただし罠 #54）

---

## 4. 学び（記録済み）

### memory（Windows ローカル）
- `feedback_mecanim_hips_lateupdate.md` — bodyPosition shift は効かない、LateUpdate Hips 直書きが正解
- `feedback_avatar_rebuild_caution.md` — Avatar 再構築は user 手動編集を破壊する罠
- `feedback_ai_review_loop_first_use.md` — AI review loop 初実用で品質明確に向上、積極使用推奨
- `session_2026-04-30_eggybot_v16_step5_complete.md` — 本セッション全記録（失敗 10〜12 + 成功）

### .claude/skills/chaosdraftparty-gotchas
- 罠 #53: Mecanim Hips Y 浮上 = LateUpdate Hips 直書き
- 罠 #54: humanoid Avatar 再構築は user 手動編集を破壊

---

## 5. 確定構成

### EGGYBOT v16 シーン
- シーン: `Assets/_Project/Scenes/Sandbox_v16_humanoid.unity`
- ルート: `EGGYBOT_v16` (rotation Y=180°)
- Avatar: `Assets/_Project/Animations/EggybotV16Avatar.asset`, humanScale=0.3642 (commit `0dbf3c1` の元 Avatar、再構築せず)
- Walking: `Assets/ThirdParty/Mixamo/Animations/Y Bot@Walking.fbx`, lockRootHeightY=true, keepOriginalPositionY=true

### bone 配置（user 編集 + Claude 対称化）
| bone | world Y | world X (左右対称) |
|---|---|---|
| Hips | 0.267 | 0 |
| UpperChest (Spine2) | 0.509 | 0 |
| LeftHand / RightHand | 0.406 | ±0.358 |
| LeftFoot / RightFoot | 0.156 | ±0.122 |
| Head | 0.908 | 0 |

### Component
- `EggybotPoseLock`: yOffset=-0.19f, applyRootMotion=false, editHipsY=0.267f
- `EggybotShaderBinder` (前から): SDF clip 用
- `Animator`: Avatar=EggybotV16Avatar, runtimeAC=EggybotV16HumanoidAC, applyRootMotion=false

### SDF Clip（前回完成、commit `0dbf3c1` 起源）
- BodyClip world (0, 0.50, 0), radius 0.336, clipMargin 5mm
- shader `ChaosDraftParty/EGGYBOTArmClip` (view-occlusion 削除済)
- material `EGGYBOT_Pink_Arm._Cull = 2 (Back)`

---

## 6. 次のセッション開始手順

```bash
cd C:/Users/taise/Documents/GitHub/ChaosDraftParty
git pull origin main
```

Unity で `Sandbox_v16_humanoid.unity` を開いて ▶ Play → bone が綺麗、Walking 動く、Y 浮上なし、を確認。

**次のタスク（M1 進行）:**
- NGO + Relay 疎通プロトタイプ
- プレイヤー移動（ホスト権威）
- 縮む島ミニゲーム雛形

詳細は `CLAUDE.md` の M1 マイルストーン参照。

---

## 7. 関連 commit（今セッション）

- `716af15` — checkpoint(eggybot): bone 再配置 + 左右対称化
- `c155759` — feat(eggybot): Mecanim T-pose 補正問題解決 — LateUpdate Hips Y shift
- `8ff1fc7` — feat(infra): AI レビューループ機構を導入

---

**最終更新:** 2026-04-30 夕方（EGGYBOT v16 Step 5 完了）
**HEAD:** 8ff1fc7（push 待ち）
**作業ツリー:** clean (上記 commit 後)
**次の commit:** memory + skill + handoff 追記
