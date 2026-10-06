# 2026-07-01 Claude 継続レビュー — Unreal 移行 (Claude → Codex ハンドオフ)

担当分担: Claude = レビュー / 設計判断 / 危険箇所発見。Codex = C++ 実装 / MCP 操作 / build・PIE 検証。
本レビューは read-only。Git 操作 / commit / push / binary uasset 編集は一切していない。

---

## 1. 現状要約 (5 行以内)

1. UE 5.8 + MCP は疎通済み、Eggybot FBX import・C++ `ACDP2DCharacter`(X-Z 平面固定 / coyote / jump buffer / jump cut / dash / knockback)・BP reparent・GameMode default pawn 差替え・PIE start/stop まで完了。
2. **致命的な設計矛盾**: `DefaultInput.ini` は `DefaultPlayerInputClass`/`DefaultInputComponentClass` を Enhanced Input に設定し Build.cs も `EnhancedInput` 依存済みなのに、C++ は legacy `BindAxis`/`BindAction` を使い、IMC/IA 資産は 0 個 — legacy と Enhanced のハイブリッド状態。
3. `Move2D`(axis)+ `MoveLeft/Right`(action)+ `KeyboardMovePulseRemaining` の手動 pulse という三重の入力経路が同居しており、これ自体が「axis が継続配信されない」問題への band-aid になっている(= 入力経路が自己矛盾)。
4. ステージ・カメラは ThirdPerson template のまま(`GameDefaultMap`/`EditorStartupMap` = `Lvl_ThirdPerson`、GameMode = `BP_ThirdPersonGameMode`)。2.5D 専用 map もサイドビュー camera も未整備。C++ pawn 側に Camera/SpringArm が無い点も要確認。
5. 「入力バインド成功」ログは lifecycle ログであって入力駆動ログではない — 実キー入力での Move/Dash 検証はまだ 0。

---

## 2. Codex が次に潰すべき問題(優先度順 5 つ)

### P1. 実入力で Move/Dash ログが出ない — まず「実測」で切り分ける
思い込みで直さず、以下を順に **測る**(RULES.md「思い込み禁止・まず実測」):
1. PIE を **フォーカスのある実ウィンドウ**で起動し、実際に A/D/Space/Shift を押す(MCP 経由の `StartPIE` だけだとキーボードフォーカスが viewport に無い / 疑似入力になり、そもそもキーが届いていない可能性が最有力)。押下時に Output Log(`LogTemp`)へ `Move2D input` / `Jump pressed` / `Dash pressed` が出るか確認。
2. PIE コンソールで `showdebug input` を叩き、legacy axis/action mapping が評価されているか、どの device が拾われているかを可視化。
3. それでも無反応なら、pawn が本当に PlayerController に possess されているか(`BeginPlay`/`input bound` ログは出ているので possess はほぼ確実だが、`GetController()` を実測)。
- **判定**: 1 で解決 → 単なる検証手順ミス。1 で無反応 → 下記 P2 の Enhanced Input 未配線が本命。**Codex は原因を断定する前に必ずこの 3 点のログを取得してから proposal を書く**(編集は Claude 側)。

### P2. Enhanced Input への完全移行(P1 の根治策・強く推奨)
- 現状は「Enhanced Input を default に指定しているのに legacy binding を使う」ハイブリッドで、UE5.8 で最も入力が"静かに動かない"パターン。詳細判断は §3-D。
- 作業: `IMC_CDP` + `IA_Move2D`(Axis1D)/`IA_Jump`(Digital)/`IA_Dash`(Digital)を作成 → `SetupPlayerInputComponent` を `UEnhancedInputComponent::BindAction` に書換え → `BeginPlay` で `UEnhancedInputLocalPlayerSubsystem` に `IMC_CDP` を `AddMappingContext`。
- これで `MoveLeft/Right` action と `KeyboardMovePulseRemaining` の手動 pulse を **全削除**でき、入力経路が 1 本化する。
- **注意**: IMC/IA は uasset(binary)なので **Codex/Claude は編集しない**。Codex が MCP で asset を生成するか、User に Editor 手動作成を依頼。C++ 側の書換えは Claude。

### P3. 2.5D 専用ステージ & サイドビュー camera の分離
- template map をコピーして `Lvl_CDP_2D_Proto`(仮)を新規作成し、`GameDefaultMap`/`EditorStartupMap` を差替え。ThirdPerson の追従ブーム camera は 2.5D では不適。
- **要確認(P3 の前提)**: 現 `BP_CDP2D_EggybotCharacter` は空の BP を `ACDP2DCharacter` に reparent した経緯なので、Camera/SpringArm component を持っていない可能性が高い → possess しても side camera が無く default view になる。まず「pawn に camera があるか」を実測。
- 方針: pawn に付けず、ステージ側に固定/軽追従の side camera(-Y 方向を向く)を置き `SetViewTargetWithBlend`、が Smash 風には素直。床は X-Z 平面(Y=0)に配置。

### P4. デバッグログを production から隔離 & Message Log 抑制(§3-B 参照)
- `LogTemp, Display` の常設ログを専用カテゴリ `LogCDP` に移し、`#if !UE_BUILD_SHIPPING` か cvar でゲート。performance.md「production に Debug.Log を残さない」の UE 版。
- Message Log の自動 pop を止める(Editor Preferences / map check 警告の解消)。

### P5. コードの危険箇所(§4)の是正 — 特に SnapTo2DPlane の毎 tick teleport と入力二経路の二重発火リスク
- Enhanced Input 移行後、`Jump()`/`StopJumping()` override と action binding が**両方**発火して jump が二重処理されないよう経路を 1 本に。
- `SnapTo2DPlane` は Movement の `bConstrainToPlane` と機能重複。毎 tick の `TeleportPhysics` は衝突解決に干渉しうるので、tolerance を超えた時だけに限定する現状ロジックは維持しつつ、Enhanced Input 化で入力起因の Y ずれが減れば発火頻度も下がる。

---

## 3. User 指定 4 論点への回答

### A. 「実入力で Move/Dash がログに出ない」
- **ログ設計上は出るはず**: `Move2D` は sign 変化時のみログ(押下初回は必ず出る)、`Jump/Dash pressed` は押下毎に出る。つまり "設計上は最初の 1 回は必ず出る" のに出ないなら、**キー入力がそもそも C++ に届いていない**のが本命。
- **最有力仮説(確度順)**:
  1. MCP `StartPIE` 起動の viewport にキーボードフォーカスが無い / 疑似入力で、実キーが届いていない。→ 実ウィンドウ + 実押下で再測(P1)。
  2. Enhanced Input を default に指定した状態で legacy mapping が期待通り評価されていない。→ Enhanced Input 移行で根治(P2)。`KeyboardMovePulseRemaining` という手動 pulse が既に存在すること自体が「axis が継続配信されていない」症状の傍証。
  3. possess されていない(確度低: `input bound` ログが出ているので反証寄り)。
- **Claude の姿勢**: ここは UE 内部挙動を当環境で実行検証できないため、**断定せず Codex に実測(P1 の 3 点ログ)を依頼**。RULES.md 通り、数値/ログが出る前に修正案を確定しない。

### B. 「Message Log が邪魔になる」
- 常設 `UE_LOG(LogTemp, Display, ...)` が Output Log を埋め、MCP の結果読み取りを阻害する。かつ shipping にも残る(performance.md 違反)。
- **対策**:
  1. `DECLARE_LOG_CATEGORY_EXTERN(LogCDP, Log, All)` で専用カテゴリ化 → フィルタ/一括 silence 可能に。
  2. 診断ログは `#if !UE_BUILD_SHIPPING` か cvar(`CDP.VerboseInput`)でゲート。
  3. Message Log window の自動 pop は Editor 設定 or map check 警告解消で止める。恒久ログは "状態変化時のみ"(Move2D は既にそう)を全ログに徹底。
- 注: 現状のログはスパムではない(Move2D は sign 変化時のみ)。問題は "常設 + LogTemp 混在で埋もれる" 点。カテゴリ分離が効く。

### C. 「2.5D 専用 stage 化」
- **やるべき、かつ P3 として今フェーズで着手推奨**。template 依存のままだと camera が追従ブームで 2.5D にならない。
- 手順: template map コピー → `Lvl_CDP_2D_Proto` → default map 差替え → 床を Y=0 平面に → side camera(-Y 向き固定/軽追従)をステージに配置 → GameMode も `BP_CDP2D_*` 専用に。
- **前提の落とし穴**: pawn(reparent 済み BP)に camera component が無い疑い。camera を pawn に持たせるかステージに持たせるかを先に決める(Smash 風 = ステージ側固定が素直)。

### D. 「Enhanced Input へ移行すべきか」→ **Yes(強く推奨)**
理由:
1. **プロジェクトは既に Enhanced Input を default に宣言済み**(`DefaultPlayerInputClass`/`DefaultInputComponentClass` + Build.cs 依存)。にもかかわらず legacy binding + IMC 0 個 という中途半端が現状の不安定さの温床。
2. **UE5.8 は legacy input が deprecated 方向**。将来の skill 10 種 / draft / rule card 等、入力が増えるほど Enhanced Input の modifier/trigger/context 切替が効く(gameplay 拡張前提の卒研に合致)。
3. 移行で `MoveLeft/Right` action + `KeyboardMovePulseRemaining` 手動 pulse を削除でき、**入力経路が 1 本化 → P1 の再発源を断てる**。
- **移行コスト**: IMC_CDP + IA×3 の asset 作成(binary、Codex が MCP 生成 or User 手動)+ C++ の `SetupPlayerInputComponent`/`BeginPlay` 書換え(Claude)。中規模だが、今 legacy を磨くより ROI が高い。
- **判断**: legacy を捨てて Enhanced Input 一本化。ハイブリッド維持は不採用。

---

## 4. コードレビュー — 危険箇所 / 気づき(`CDP2DCharacter.cpp/.h`)

| # | 箇所 | 重大度 | 指摘 |
|---|---|---|---|
| 1 | 入力三経路の同居(`Move2D` axis + `MoveLeft/Right` action + `KeyboardMovePulseRemaining` pulse) | HIGH | 経路が自己矛盾。Enhanced Input 化で 1 本に統合し pulse hack を削除。二重発火の温床。 |
| 2 | `Jump()`/`StopJumping()` override と `CDP_Jump` action binding が両方 `Request/ReleaseBufferedJump` を呼ぶ | MED | 現 legacy では二重にはならないが、Enhanced Input 移行時に `IA_Jump` binding と override が両発火して jump 二重処理になり得る。経路を 1 本に。 |
| 3 | `SnapTo2DPlane()` を毎 tick 実行 + `bConstrainToPlane` も有効 | MED | 機能重複。tolerance 超過時のみ `TeleportPhysics` する現ロジックは可だが、teleport は衝突解決に干渉しうる。Movement の plane constraint に一本化する余地。 |
| 4 | Dash 中 `Tick` で `Velocity.X = DashDirection*DashSpeed` を毎 tick 上書き + `RequestDash` でも `LaunchCharacter` | LOW | 二重適用で冗長。挙動は破綻しないが、dash 速度の source of truth を 1 つに。 |
| 5 | Dash キーが `LeftShift` + `E` + `Gamepad_FaceButton_Right`(ini)だが handoff は Shift/FaceButton_Right 記載 | LOW | `E` の追加が意図的か要確認。ドキュメントと ini の齟齬。 |
| 6 | `BP_CDP2D_EggybotCharacter` に Camera/SpringArm 無し疑い | MED(2.5D 前提) | reparent 元が空 BP のため。P3 の前に実測。 |
| 7 | `bOrientRotationToMovement=true` + `RotationRate` yaw | LOW | side view では左右反転(yaw 0/180)にしたい。現状でも±X で左右向くので許容だが、2.5D では yaw snap の方が Smash 風。要視覚確認(視覚判断は User)。 |

**良い点**: coyote / jump buffer / jump cut / plane constraint / knockback API の骨格は堅実。`Move2D` の sign 変化時のみログはスパム回避済み。`GetCharacterMovement()` null guard も入っている。

---

## 5. 分担の明確化(このレビューを受けての依頼)

- **Codex(実装/検証)**: P1 の実測ログ取得 → P2 Enhanced Input asset(IMC/IA)生成(MCP or User 依頼)→ P3 map/camera 構築 → build/PIE 検証。**C++ script の編集は Claude 専有(AGENTS.md ルール 1)なので、C++ 書換えは diff 案を `docs/codex_proposals/` に出すか Claude に振る**。
- **Claude(設計/レビュー/C++ 編集)**: Enhanced Input 化の C++ 書換え、入力経路 1 本化、ログカテゴリ分離、危険箇所 §4 の是正。
- **User(視覚/体感判断)**: camera 画角、2.5D の見た目、dash/jump の手触り最終 OK。
- 禁止事項の遵守: 本タスクで Git 操作 / commit / push / binary uasset 編集は行っていない。

---

*Claude(相方)より。原因断定より先に P1 の実測ログを。それが Enhanced Input 移行(P2)の要否を最終確定させる。*
