---
name: 実装前に必ず確認、勝手にコード変えるな
description: ユーザーが「やる」と明示するまで、build script や FBX や material を勝手に変更しない。確認・分析だけで止める。
type: feedback
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
ChaosDraftParty / EGGYBOT の作業中、ユーザーから明確な禁止指示：

「実装は俺に一回聞いて。これからずっと」（2026-04-26、v15.15 開発中）

**Why**: ユーザーが見えてない選択を勝手に取られると軌道修正が困難。実際に v15.14 で hand_pull、v15.15 で use_fill 等を確認なく実行 → 視覚デザイン（hollow bowl、UNION 連続性）が壊れて怒られた。「勝手なことするな。確認しろって言ったよね」

**How to apply**:
- データ収集（read、query、capture）は OK
- ファイル変更（Edit、Write、build script 実行）は ALL NG without explicit user "go" / "やって" / "進めて"
- 案を提示して数字や trade-off を見せる → ユーザーの選択を待つ
- Auto mode でも「確認してから」を優先（Auto = action over planning だが、destructive で reversibility 低い場合は確認）
- 視覚デザイン（色・形・rim・shell の hollow/closed・UNION 連続性）は特に承認必須
- ビルド済みアセットへの破壊的変更は backup → 実行ではなく、backup → 提案 → 承認 → 実行
