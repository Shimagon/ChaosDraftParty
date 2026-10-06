# 2026-07-01 Claude ブリッジレビュー v2 — Codex 次アクションのレビュー (Claude ⇄ Codex)

担当: Claude = レビュー / 設計判断 / 危険箇所発見 / C++ 編集。Codex = C++ 実装 / MCP 操作 / build・PIE 検証。
本レビューは read-only 調査。**Git 操作 / commit / push / binary uasset 編集は一切していない。**
ソース再確認済み(前回から変化なし): `CDP2DCharacter.h/.cpp` は legacy `BindAxis/BindAction` のまま、`DefaultInput.ini` は Enhanced Input default class + legacy mapping 併存、handoff の「次にやること 1-5」も同一。

---

## 0. Codex の 3 ワークストリームのレビュー

### A. 入力検証の詰まり解消
- **方向性 OK、ただし順序が肝**。まず **実測**(実 PIE + 実キー押下でログ取得)から入り、legacy binding をこれ以上"磨かない"こと。`KeyboardMovePulseRemaining` の手動 pulse が既に入っている = axis 経路が継続配信されていない症状の傍証。詰まりの根治は legacy いじりではなく **Enhanced Input 移行**(§P1)に収束させるべき。
- **危険**: 実測前にコードを触ると thrash する。measure-first を厳守。

### B. 2.5D 専用 stage / camera 化
- **方向性 OK**。ただし着手前に **camera の帰属(pawn 側 or level 側)を確定**する必要あり → 結論は §3-D = **level 側**。pawn に SpringArm を生やす実装に進む前に止めること(手戻り防止)。
- 前提の実測: 現 `BP_CDP2D_EggybotCharacter` は空 BP を reparent した経緯で **camera を一切持たない疑い** → 今の PIE は default view の可能性。ViewTarget を誰にするか未決のまま map を作ると 2.5D にならない。

### C. 日本語 handoff 更新
- **必要かつ歓迎**。ただし更新内容は「実測した PIE ログ / 決定事項」を書く(思い込みで "動いた" と書かない、RULES.md)。反映すべき確定事項: ①入力実測の結果、②Enhanced Input 移行の採否、③camera=level 側の決定、④map 分離の状態。

---

## 1. 今すぐ直す順番(P0 / P1 / P2)

### 🔴 P0 — ブロッカー(全判断の前提。実測のみ、コード変更なし)
1. **実入力ログの取得**: フォーカスのある実 PIE ウィンドウで A/D・Space・Shift を実押下 → Output Log に `Move2D input` / `Jump pressed` / `Dash pressed` が出るか。`showdebug input` も併用。
2. **pawn の camera 有無を実測**: `BP_CDP2D_EggybotCharacter` に Camera/SpringArm があるか、現 PIE の ViewTarget は誰か。
→ この 2 点の結果を handoff に記録してから P1 へ。

### 🟠 P1 — 根治・設計確定(P0 の結果を受けて)
3. **Enhanced Input 移行**(§3-C): `IMC_CDP` + `IA_Move2D`/`IA_Jump`/`IA_Dash` 作成 → `SetupPlayerInputComponent` を `UEnhancedInputComponent::BindAction` へ → `BeginPlay` で `AddMappingContext`。`MoveLeft/Right` action + pulse hack を全削除して入力経路 1 本化。
4. **camera を level 側 Arena camera に確定**(§3-D)し、PlayerController の ViewTarget に設定。

### 🟡 P2 — 整備
5. **2.5D 専用 map 分離**: template コピー → `Lvl_CDP_2D_Proto` → `GameDefaultMap`/`EditorStartupMap` 差替え → 床を Y=0 平面へ。
6. **ログ整備**: `LogTemp` → `LogCDP` カテゴリ化 + `#if !UE_BUILD_SHIPPING`/cvar ゲート、Message Log 自動 pop 抑制(§3-B)。
7. **日本語 handoff 更新**: 上記の実測結果と決定を反映。

---

## 2. User 指定 4 論点

### A. 「実入力で Move/Dash がログに出ない」
- 設計上は押下初回に必ず出る(`Move2D` は sign 変化時、`Jump/Dash pressed` は押下毎)。出ない = **キーが C++ に届いていない**が本命。
- 確度順: ① MCP `StartPIE` の viewport にキーボードフォーカス無し / 疑似入力 → 実ウィンドウ実押下で再測(P0)。② Enhanced Input default 下で legacy mapping が期待通り評価されない → Enhanced Input 移行で根治(P1)。③ possess 未(`input bound` ログが出ており確度低)。
- **Claude は当環境で UE 実行検証不可のため断定しない**。Codex の P0 ログで確定させる。

### B. 「Message Log が邪魔になる」
- 常設 `UE_LOG(LogTemp, Display)` が Output Log を埋め MCP 結果読取を阻害 + shipping 残存(performance.md 違反)。
- 対策: `DECLARE_LOG_CATEGORY_EXTERN(LogCDP, Log, All)` でカテゴリ化(フィルタ/一括 silence)、診断ログを `#if !UE_BUILD_SHIPPING`/cvar ゲート、Message Log window の auto-pop 停止。
- 注: 現ログはスパムではない(状態変化時のみ)。問題は "LogTemp 常設で埋もれる" 点 → カテゴリ分離が効く。

### C. 「Enhanced Input へ移行すべきか」→ **Yes(強く推奨・P1)**
1. **プロジェクトは既に Enhanced Input を default 宣言済み**(`DefaultPlayerInputClass`/`DefaultInputComponentClass` + Build.cs `EnhancedInput` 依存)。legacy binding + IMC 0 個 の中途半端が不安定の温床。
2. **UE5.8 は legacy input が deprecated 方向**。skill 10 種 / draft / rule card で入力が増えるほど Enhanced Input の modifier/trigger/context 切替が効く(卒研の拡張前提に合致)。
3. 移行で `MoveLeft/Right` + pulse hack を削除 → **入力経路 1 本化で「Move/Dash ログ不出」の再発源を断つ**。
- コスト: IMC + IA×3(binary、Codex MCP or User 手動)+ C++ 書換え(Claude)。中規模だが legacy 磨きより ROI 高。**ハイブリッド維持は不採用。**

### D. 「stage camera を pawn 側か level 側か」→ **level 側(強く推奨)**
- **本作は 2〜4 人オンライン共有画面のパーティ brawler**。Smash は「全 fighter を 1 枚のカメラに収め、動的に zoom/pan」する。**pawn 側 SpringArm はプレイヤー毎に別カメラが付き、複数人を 1 画面に収められない** → 目標設計と不整合。
- **正解**: level に **専用 Arena camera actor** を置き、全 player pawn の位置から bounding box を計算して距離/位置を動的調整、各クライアントの `PlayerController` の ViewTarget に `SetViewTargetWithBlend`。ThirdPerson template の pawn 追従ブームは 2.5D で不適な holdover。
- **今のプロトタイプ(1 人)でも level 側で始めるべき**: 固定 side camera(-Y 向き)として置けば、そのまま「全員を収める framing camera」へ発展できる。pawn 側で作ると 2〜4 人化で全捨てになる。
- 補足: 床/プレイ面は X-Z 平面(Y=0)。character の `bOrientRotationToMovement`(yaw)は側面視で左右反転として許容だが、最終的な向き/画角は視覚判断=User。

---

## 3. コード危険箇所(前回から不変・移行時に効く)

| # | 箇所 | 重大度 | 指摘 |
|---|---|---|---|
| 1 | 入力三経路の同居(axis + action + pulse) | HIGH | 自己矛盾。Enhanced Input 化で 1 本統合し pulse hack 削除。 |
| 2 | `Jump()`/`StopJumping()` override と `CDP_Jump` action の両掛け | MED | Enhanced Input 移行時に `IA_Jump` binding と override で二重発火し得る。経路 1 本化。 |
| 3 | `SnapTo2DPlane` 毎 tick + `bConstrainToPlane` | MED | 機能重複。tolerance 超過時のみ `TeleportPhysics` する現ロジックは可だが衝突解決に干渉しうる。 |
| 4 | Dash 中 `Tick` の Velocity.X 上書き + `RequestDash` の `LaunchCharacter` | LOW | 二重適用で冗長。source of truth を 1 つに。 |
| 5 | Dash キー `LeftShift`+`E`+`Gamepad_FaceButton_Right`、handoff は Shift/FaceButton 記載 | LOW | `E` 追加が意図的か要確認。 |
| 6 | `BP_CDP2D_EggybotCharacter` の camera 無し疑い | MED | P0 で実測。§3-D と直結。 |

**良い点**: coyote / jump buffer / jump cut / plane constraint / knockback の骨格は堅実。`Move2D` の sign 変化時のみログでスパム回避。`GetCharacterMovement()` null guard あり。

---

## 4. 分担

- **Codex**: P0 実測(ログ + camera 有無)→ P1 の IMC/IA 生成(MCP or User)→ P2 map/camera 構築 → build/PIE 検証。**C++ 編集は Claude 専有(AGENTS.md ルール 1)** → diff 案は `docs/codex_proposals/` か Claude へ。
- **Claude**: Enhanced Input 化の C++ 書換え、入力経路 1 本化、Arena camera の C++、ログカテゴリ分離、§3 是正。
- **User**: camera 画角 / 2.5D 見た目 / dash・jump 手触りの最終 OK。
- 本タスクで Git 操作 / commit / push / binary uasset 編集は行っていない。

---

*Claude(相方)より。P0 の実測(入力ログ + camera 有無)が全ての前提。それが Enhanced Input 移行(P1)と level 側 camera(P1)の実装 GO を確定させる。camera を pawn 側で作り始めないこと — 2〜4 人化で全捨てになる。*

BRIDGE_READY_FOR_CODEX
