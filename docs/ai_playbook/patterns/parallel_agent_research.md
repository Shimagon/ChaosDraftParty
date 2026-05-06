# Pattern: Parallel Agent Research (並列 agent 調査フロー)

**確定**: 2026-04-29 → 2026-05-06、複数のセッションで実証済
**用途**: AI が「思い込みで proposal」せず、技術判断・検証・矛盾要件解消を **多角検証** で行う運用

---

## 🎯 なぜこの pattern が必要か

AI 単独で「これでいいでしょ」と proposal すると:
- 1 つの仮説に固執
- 業界 defacto を見落とす
- user 主観に合わない解答
- 試行錯誤で時間溶ける

特に **shader / 物理 / camera / SDF / shader graph / Editor 設定** の領域は、**実測 + 業界 defacto 確認 + 公式実装** の 3 軸で並列検証しないと真因に到達できない。

---

## 🔬 並列 agent 3 個ルール (User 明示要求 2026-04-30)

### Step 1: 仮説立てる前に「3 つの観点で並列調査」

User の質問に対して、AI は **即座に 3 個の agent を並列起動**。各 agent に検証ポイントを分割:

| Agent A | Agent B | Agent C |
|---|---|---|
| **既存仮説の検証 / 反証** | **公式実装 / source 確認** | **代替アプローチ網羅 / web 検索** |
| (例: 「dither が効かない仮説 = SphereCast 経路」を実測・コード読み込み) | (例: Cinemachine Deoccluder の Damping 内部実装ソース確認) | (例: Genshin / Hat in Time / Splatoon の defacto 数値) |

### Step 2: 結論を統合
3 個の agent の結論を AI 親が統合:
- 一致点 → 採用 candidate
- 不一致点 → 仮説検証必要
- 全部間違い → さらに 3 個並列

### Step 3: 実装 → 体感確認 → 必要なら再並列

---

## 📋 並列起動テンプレート

```
Agent A (検証):
- 既存仮説 X を「実測 / コード読み込み / 数値取得」で検証
- 反証となる事実を探す
- 出力: 仮説 X の正否 + 根拠

Agent B (公式 / source):
- Library/PackageCache の該当 source を Read
- Unity Forum / GitHub Issue で該当機能の挙動を確認
- 出力: 公式定義 + 内部実装 + AAA 標準値

Agent C (代替 / web):
- Web 検索 (Reddit / GameDev StackExchange / Cyanilux / GDC vault)
- 業界 defacto (Genshin / Splatoon / Fall Guys / Hat in Time 等)
- 代替アプローチを 3-5 個列挙
- 出力: 代替案 ranking + 採用推奨度
```

---

## ✅ 実証された成功事例 (本 playbook の発見元)

### Case 1: Camera Occluder Fade 「ぬるっと + 精度感」両立 (2026-05-06)
- 並列 agent 3 個 (Cinemachine source / Easing curve / 2 段階 lerp)
- 結論: **Anticipation Tick + Smooth Tail** pattern 発見
- → [recipes/camera_occluder_fade.md](../recipes/camera_occluder_fade.md)

### Case 2: 走行中視点回しで「ぐわんぐわん」酔い (2026-05-06)
- 並列 agent 3 個 (motion sickness / Cinemachine 公式 / AAA 数値)
- 結論: **TPS で Roll on Strafe は禁忌** + **Look-Ahead 視点入力中 OFF** + **Mouse 素直 linear**
- → [gotchas/unity6_urp17_pitfalls.md](../gotchas/unity6_urp17_pitfalls.md)

### Case 3: dither が効くときと効かないとき (2026-05-06)
- 並列 agent 2 個 (scene/コード実測 + AAA 数値)
- 結論: **distanceShorten 0.2 + targetUpOffset 1.0 が CORE FIX** (8 反復で確定)

### Case 4: Editor focus 問題 (2026-05-06)
- 並列 agent 1 個 (Web 検索 + reflection)
- 結論: **`Application.runInBackground` は Editor で意味なし**、真因は **InputSystem.backgroundBehavior**
- → [gotchas/unity6_urp17_pitfalls.md](../gotchas/unity6_urp17_pitfalls.md)

### Case 5: EGGYBOT v16 SDF clip (2026-04-29)
- 並列 agent 3 個 + 6 反復で真因到達
- 結論: **SkinnedMR の bounds.center は罠**、vertex 幾何中心を計算する

---

## 🧠 守るべきルール

### Rule 1: 思い込みで proposal するな (CLAUDE.md 原則 1)
shader / SDF / mesh / bone / camera / animator の挙動が想定と違う時:
1. **数値を取る前に修正案を出さない**
2. Unity MCP `execute_code` で実測:
   - mesh の vertex world position
   - shader uniform の global 値 (`Shader.GetGlobalVector`)
   - SkinnedMR の `bones`, `rootBone`, `bounds.center`
   - Animator の `humanScale`, `bodyPosition`, 各 bone localPos

### Rule 2: 並列 agent + AI review loop が default (User 明示 2026-04-30)
```
[ 並列 agent 3 個 (調査) ]
        ↓
[ 実装 ]
        ↓
[ AI review loop (unity-reviewer + code-reviser、1-2 周) ]
        ↓
[ commit ]
```

### Rule 3: 認識ズレを感じたら user に「絵で説明して」と依頼 (User 明示 2026-04-30)
shader rim と「淵 (hemisphere の底の円)」の混同で大幅時間ロスした事例。**早期に絵を依頼**。

### Rule 4: 失敗を残せ (CLAUDE.md 原則 4)
うまく行かなかった試行は **memory or playbook に記録**。同じ罠を二度踏まない。

### Rule 5: 視覚 / 微調整は user、機械的構築は AI (CLAUDE.md 原則 5)
bone 位置の最終微調整、色の最終決定、形状の判断は **user**。
rough rig 構築、script 実装、初期値設定、scene 操作は **AI**。

---

## 🚀 新規プロジェクトでこの pattern を運用する手順

1. **AI に CLAUDE.md でルール明記**:
   ```markdown
   ## AI 開発フロー (必須)
   - 技術判断 / 矛盾要件解消は **並列 agent 3 個** で多角検証してから proposal
   - 詳細は `docs/ai_playbook/patterns/parallel_agent_research.md` 参照
   ```
2. **User がルールを覚えるための合言葉**:
   - 「**徹底的に並列で調査して**」
   - 「**思い込みで proposal するな**」
   - 「**他のゲームの defacto も調べて**」

3. **AI review loop と組み合わせ**:
   - 並列調査 → 実装 → review → commit のフルサイクル

---

## 📚 参考

- [CLAUDE.md 「Shader / SDF / Visual デバッグの原則」](../../CLAUDE.md) — 本 pattern の元ルール
- [recipes/camera_occluder_fade.md](../recipes/camera_occluder_fade.md) — 本 pattern が確定した実例
- [gotchas/unity6_urp17_pitfalls.md](../gotchas/unity6_urp17_pitfalls.md) — 並列調査で発見した罠まとめ
