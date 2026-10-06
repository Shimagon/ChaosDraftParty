# 2026-07-01 Claude ブリッジレビュー — Codex Unreal 2.5D 進捗後 (Claude ⇄ Codex)

担当: Claude = レビュー / 設計判断 / 危険箇所発見 / C++ 編集。Codex = C++ 実装 / MCP 操作 / build・PIE 検証。
本レビューは read-only 調査。**Git commit / push / binary uasset の直接編集は一切していない。レビューと handoff のみ。**
対象: `2026-07-01_unreal_migration_codex_status_ja.md`, `CDP2DCharacter.h/.cpp`, `CDP2DArenaCamera.h/.cpp`, `DefaultEngine.ini`, `DefaultInput.ini`。

---

## 0. 進捗評価(良い点)

- **前回 P0「実入力で Move/Dash ログが出ない」は解決**。D→`Move2D/MoveRight`, Space→`Jump`, E→`Dash` の実キー到達を PIE 実測ログで確認済み。legacy mapping + `EnhancedInputComponent` のハイブリッドでも実キーが通ることが実証された(= 前回仮説「フォーカス欠如」が主因だった公算)。
- **jump 二重発火を `LastJumpRequestFrame == GFrameCounter` guard で抑止** — 前回指摘 #2 に的確に対処。Enhanced Input 移行後も override + action 同時発火を吸収できる良い防御。
- **level 側 shared camera を採用**(`ACDP2DArenaCamera`)— 前回の設計判断(camera は level 側)通り。2-4 人共有画面の方向性に合致。
- **専用 arena map 作成 + `bUseSplitscreen=False`** — 共有画面 brawler として正しい。

→ 移行は前進。残るは **「絵として正しいか」の検証(visual truth)** と **camera / mesh の具体不具合**。

---

## 1. 次にやるべき順(P0 / P1 / P2)

### 🔴 P0 — ゲームカメラの視覚確認(ブロッカー・実測のみ)
現状 editor screenshot API が editor viewport を拾い、**ゲームカメラの絵をまだ誰も見ていない**。これが確認できるまで mesh scale も ortho width も「勘」で触らない。
1. **PIE を standalone / new-window で起動**し、PIE 内コンソールで `HighResShot 1920x1080`(ゲーム viewport の実レンダを `Saved/Screenshots/` に書き出す)。editor viewport SS ではなく **これ**を見る。または User が PIE を直接目視。
2. 確認項目: (a) 実行中 ViewTarget が `ACDP2DArenaCamera` になっているか(`GetPlayerController(0)->GetViewTarget()` をログ)、(b) orthographic の横視点が意図通りか、(c) Eggybot が見えて足が platform に乗っているか、スケールが妥当か。
3. 結果(SS + ViewTarget ログ)を handoff に記録してから P1 へ。**思い込みで "見えているはず" と書かない**(RULES.md)。

### 🟠 P1 — camera / mesh の具体不具合是正(絵を正す)
4. **Mesh offset の facing 反転バグ**(§3-①, HIGH): `MeshRelativeOffset.Y=-80` は `bOrientRotationToMovement` で actor が左右反転(yaw 180°)する度に world Y が -80⇔+80 に反転し、mesh が 160cm 前後動する。**Y offset は 0 にし、depth はプレイ面(Y=0)で扱う**。Z offset のみ足合わせに使う。
5. **Mesh scale を実測ベースに**(§3-②, MED): `MeshRelativeScale=0.5` は勘。CLAUDE.md 原則 2 通り `SK_EggybotV16` の vertex/bounds を MCP `unity_reflect`/reflection で実測し、mesh 高さ ≒ capsule(半径42 × 半高96 = 全高192)に合う scale と Z offset を算出。足が浮く/めり込むのは実測で消す。
6. **ArenaCamera の ViewTarget 綱引き解消**(§3-③, MED): 毎 Tick `SetViewTarget` は `bAutoManageActiveCameraTarget`(既定 true)が possess 時に pawn を view target へ戻すのを打ち消す band-aid。**BeginPlay で 1 回 `SetViewTarget(this)` + `PC->bAutoManageActiveCameraTarget=false`** にし、per-tick refresh を撤去。
7. **Enhanced Input 完全移行**(§3-④): `IMC_CDP` + `IA_Move2D`/`IA_Jump`/`IA_Dash` 作成 → `SetupPlayerInputComponent` を `UEnhancedInputComponent::BindAction` へ → `BeginPlay` で `AddMappingContext`。`Move2D`(axis)+`MoveLeft/Right`(action)+`KeyboardMovePulseRemaining`(pulse)の三重経路と pulse hack を削除し 1 本化。**入力は今動いているので blocker ではないが、skill 実装に入る前(=入力が増える前)に済ませる**。

### 🟡 P2 — 整備
8. **ortho + Lumen/VSM の相性検証**(§3-⑤): `DefaultEngine.ini` は Lumen GI(`r.DynamicGlobalIlluminationMethod=1`)+ VSM(`r.Shadow.Virtual.Enable=1`)。orthographic camera は UE で影/GI が壊れやすい既知の癖。P0 の SS で shadow/GI が破綻していれば、ortho のまま調整 or 遠距離 perspective + 狭 FOV(疑似 ortho)へ切替を検討。
9. **専用 GameMode 化**: `GlobalDefaultGameMode` が未だ `BP_ThirdPersonGameMode`。`BP_CDP2D_GameMode` を作り DefaultPawnClass + ArenaCamera 連携を集約。arena map の World Settings で GameMode override が効いているかも確認。
10. **ログ整備**: `LogTemp` → `LogCDP` カテゴリ化 + `#if !UE_BUILD_SHIPPING`/cvar ゲート。→ handoff 更新。

---

## 2. User 指定フォーカスへの回答

### A. PIE のゲームカメラ視覚確認 → **P0・最優先**
editor SS はゲームカメラを映さない。**PIE 内 `HighResShot` でゲーム viewport の実レンダを書き出す**か User 目視。まず「ViewTarget が ArenaCamera か」をログで確定してから絵を評価。ここが埋まらないと mesh/ortho の調整は全部当て推量になる。

### B. Eggybot mesh scale → **実測して決める(P1)**
`MeshRelativeScale=0.5` / `MeshRelativeOffset=(0,-80,-80)` は勘。CLAUDE.md「原則 2: bounds を信じず vertex min/max から幾何中心」。`SK_EggybotV16` の bounds を実測 → capsule 全高 192 に合わせ scale と Z を算出。**Y=-80 は facing 反転で depth pop するので 0 にする**(§3-①)。最終的な見た目 OK は User。

### C. Arena camera 設計 → **方向性◎、実装に 3 つの穴**
- ViewTarget 綱引き(毎 Tick SetViewTarget)→ 1 回設定 + `bAutoManageActiveCameraTarget=false` へ(§3-③)。
- aspect 16:9 ハードコード(`NeededHeightAsWidth = H * 16/9`)→ 実 viewport aspect を読む(prototype 中は許容だが TODO)。
- constructor の `SetActorRotation(0,90,0)` は **配置済み actor の level transform に上書きされ得る**上、Tick は rotation を再設定しない。rotation を BeginPlay で明示 or 配置 instance の transform を正とする運用に。
- 単体キャラ時 `MinX==MaxX` で NeededWidth=padding のみ → `MinOrthoWidth=1200` にクランプ、妥当。ロジック自体は健全。

### D. Enhanced Input 完全移行 → **やる(P1)。ただし今は blocker ではない**
実キーは既に到達済みなので緊急度は前回より下がった。位置づけは「skill/draft で入力が増える前に三重経路を 1 本化する tech-debt 返済」。既に default class は Enhanced 指定・Build.cs 依存済みなので移行の地ならしは完了。ハイブリッド放置は不可。

---

## 3. コードレビュー詳細(危険箇所)

| # | 箇所 | 重大度 | 指摘 / 対処 |
|---|---|---|---|
| ① | `CDP2DCharacter.cpp` BeginPlay `MeshRelativeOffset=(0,-80,-80)` | **HIGH** | Y=-80 が `bOrientRotationToMovement` の左右反転で world Y ±80 に反転 → mesh 160cm depth pop。**Y=0 にする**。depth はプレイ面で管理。 |
| ② | `MeshRelativeScale=0.5`(勘) | MED | 実測 bounds ベースで算出(CLAUDE.md 原則 2)。足浮き/めり込みは実測で解消。 |
| ③ | `CDP2DArenaCamera.cpp` 毎 Tick `RefreshViewTarget→SetViewTarget` | MED | `bAutoManageActiveCameraTarget` との綱引き band-aid。BeginPlay 1 回設定 + auto-manage 無効化へ。 |
| ④ | 入力三経路(axis+action+pulse)+ pulse hack 残存 | MED | Enhanced Input 移行で 1 本化・pulse 削除(P1-7)。 |
| ⑤ | ortho camera + Lumen/VSM | MED | 影/GI 破綻リスク。P0 SS で確認、必要なら疑似 ortho へ。 |
| ⑥ | `GlobalDefaultGameMode=BP_ThirdPersonGameMode` | LOW | template GameMode 流用。専用 `BP_CDP2D_GameMode` へ(P2)。 |
| ⑦ | ArenaCamera aspect 16:9 ハードコード / constructor rotation | LOW | 実 viewport aspect 読取 / rotation を BeginPlay で明示。 |
| ⑧ | `SnapTo2DPlane` 毎 tick `TeleportPhysics` + `bConstrainToPlane` 重複 | LOW | 前回同様。plane constraint 有効なので発火は稀、許容。 |

**良い点**: jump 同フレーム二重発火 guard、spawn collision(`AdjustIfPossibleButAlwaysSpawn`+`MinimumStartZ`)、coyote/buffer/cut/knockback、camera の bounding-box framing ロジック自体は健全。`GetMesh()`/`GetCharacterMovement()` null guard あり。

---

## 4. 分担

- **Codex**: P0 の PIE `HighResShot` + ViewTarget ログ取得 → P1 の IMC/IA 生成(MCP or User)+ mesh bounds 実測値提示 → P2 map/GameMode/ログ整備 → build・PIE 検証。**C++ 編集は Claude 専有(AGENTS.md ルール 1)** → 変更は `docs/codex_proposals/` に diff 案 or Claude へ。
- **Claude**: mesh offset/scale の C++ 修正(実測値を受けて)、ArenaCamera の ViewTarget 一本化、Enhanced Input 化の C++、ログカテゴリ分離。
- **User**: PIE 目視での mesh 見た目 / camera 画角 / dash・jump 手触りの最終 OK(視覚・体感判断)。
- 本タスクで Git commit / push / binary uasset 直接編集は行っていない。

---

*Claude(相方)より。入力は通った。次は「絵の真実」を P0 で取る — editor SS ではなく PIE `HighResShot` とゲーム ViewTarget ログ。mesh は勘の 0.5 をやめて実測へ。mesh Y offset=-80 の facing 反転 depth pop は先に潰すこと。*

BRIDGE_READY_FOR_CODEX
