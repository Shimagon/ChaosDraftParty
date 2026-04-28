---
name: 不安な所はユーザー、機械的な所は Claude（恒久方針）
description: 視覚判断・微調整はユーザー、骨格構築・script・初期値は Claude。EGGYBOT 以降の全タスクで適用
type: feedback
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
ユーザーが不安・主観判断が必要と感じる所はユーザー本人がやる。Claude は機械的に組める所だけ担当する。

**Why:**
- 2026-04-28 EGGYBOT v16 humanoid rig 化の議論中、bone 長さ微調整を誰がやるかで明示確認 → ユーザーが「不安なところは俺がやる。こういう方針にしよう。これからずっと」と恒久方針として宣言。
- v15 までで Claude が「見た目を勝手に整えようとして破壊した」前例が複数あるため、視覚審美判断は user に固定するのが安全。
- 一方で「rough rig 作成」「Avatar script 構築」「Mixamo retarget setup」などの機械的作業は Claude のほうが速い。

**How to apply:**
- **Claude 側** (機械的・script 化可能):
  - 骨格・階層・コンポーネントの初期構築
  - HumanDescription / Avatar / Animator Controller の script 構築
  - Mixamo retarget / FBX import 設定
  - 初期値はベストエフォート (例: Mixamo 標準位置 ×0.4 倍)
- **User 側** (視覚判断・微調整):
  - bone 長さ・位置の Inspector 微調整
  - キャラ性に合わせた scale / rotation の最終決定
  - 「いい感じ」「変」の判断
- Claude は **rough 状態で渡す → user が触れる状態にする** のが完成点。仕上げに踏み込まない。
- 提案時も「俺が作る範囲」「user が触る範囲」を明示分割する。
