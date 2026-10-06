# 2026-07-01 Claude レビュー — Codex の UE 5.8 C++ Character 移行

対象: `ACDP2DCharacter`（`Public/CDP2DCharacter.h` / `Private/CDP2DCharacter.cpp`）、`ChaosDraftPartyUE.uproject`、`2026-06-30_unreal_pivot_setup_ja.md`
レビュー担当: Claude（Codex との分担テスト）
制約: git 操作なし / binary asset 編集なし / ビルド実行なし（読取りのみ）

---

## 1. 結論（3行）

- C++ の 2.5D キャラクター実装そのものは**筋が良く、方向性は正しい**。2.5D 拘束・jump buffer・coyote・jump cut・dash が一通り入っている。
- ただし **最大の問題は「この C++ クラスがまだゲームに繋がっていない」こと**。`CDP2DCharacter` は自身の 2 ファイル以外どこからも参照されておらず、handoff の mesh 差し替え・movement 調整はすべて template の `BP_ThirdPersonCharacter` 側に対して行われている。**今 PIE を起動しても `ACDP2DCharacter` のロジックは 1 行も実行されない。**
- 個々のロジックにも「coyote/grounded で jump 経路が分裂」「jump cut が最短タップで効かない」等の**体感バグ候補**がある。接続を最優先に、その後で feel 調整。

---

## 2. いまの移行状況

| 領域 | 状態 | 補足 |
|---|---|---|
| C++ module / Build.cs | ✅ 妥当 | `Core/CoreUObject/Engine/InputCore/EnhancedInput`。コンパイルは通る想定 |
| 2.5D movement (plane 拘束) | ✅ 実装済 | `bConstrainToPlane` + `PlaneConstraintNormal=Y` + Tick で snap |
| jump buffer | ✅ 実装済 | `JumpBufferTime=0.10s`、Tick と入力時に消費 |
| coyote time | △ 実装済だが経路分裂 | `CoyoteTime=0.08s`。grounded jump と別経路（後述） |
| jump cut | △ 実装済だが穴 | 最短タップで効かないケースあり（後述） |
| dash | ✅ 実装済 | `LaunchCharacter` + Tick で速度維持。cooldown あり |
| knockback | ✅ 最小実装 | `ApplyKnockback` は Y をゼロ化して launch |
| **BP への接続 / input wiring** | ❌ **未接続** | ここが本丸。§3-A 参照 |
| C++ module のビルド反映 | ❓ 未確認 | 新規 UCLASS 追加。editor 再ビルドが必要（§5） |
| Enhanced Input asset (IMC/IA) | ❌ 未作成（handoff に記載なし） | Move2D/Jump/Dash を叩く経路が存在しない |

---

## 3. 危険箇所（優先度順）

### 🔴 A. C++ キャラが未接続（ブロッカー）

- grep 上、`CDP2DCharacter` / `Move2D` / `RequestDash` を参照するのは自身の `.h`/`.cpp` のみ。
- `DefaultEngine.ini` の `GlobalDefaultGameMode = BP_ThirdPersonGameMode` → default pawn は template の `BP_ThirdPersonCharacter`。これは `ACDP2DCharacter` の子ではない。
- handoff「Character Blueprint への反映」は `BP_ThirdPersonCharacter` の **mesh 差し替えと CharacterMovement 値の直接調整**のみ。reparent していない。
- `Move2D` / `RequestBufferedJump` / `RequestDash` は `BlueprintCallable` だが、これを呼ぶ **Enhanced Input の binding も IA/IMC も存在しない**（handoff にも記載なし）。
- **結果**: 現状 PIE で動くのは template の素の 3D 移動。2.5D 拘束も jump buffer も dash も走らない。
- **さらに**: reparent すると `ApplyMovementDefaults()` が BeginPlay で走り、handoff で MCP 調整した `BP_ThirdPersonCharacter` 側の CharacterMovement 値（maxWalkSpeed=800 等）を **C++ の default 値（RunSpeed=800 等）で上書き**する。値の source of truth を C++ 側 UPROPERTY に一本化する前提なら OK だが、二重管理のまま放置すると混乱する。

### 🟠 B. jump 経路の分裂（feel 一貫性）

`TryConsumeBufferedJump()`:
- **grounded 時** → `Super::Jump()`（CharacterMovement 正規の jump。`bPressedJump`/`JumpCurrentCount`/hold air control 等が正しく回る）
- **coyote 時** → `LaunchCharacter((0,0,JumpVelocity), false, true)`（CMC の jump state を経由しない生の速度付与）

問題:
- coyote jump は「jump」として登録されないので、後で double jump / jump count / anim notify / 可変ジャンプ高さ（hold）等を足すと **grounded と coyote で挙動が食い違う**。
- 可能なら coyote も `bPressedJump=true → Super::Jump()`（または `Movement->DoJump`）に寄せて経路を一本化するのが定石。少なくとも「なぜ launch なのか」をコメントで残す。

### 🟠 C. jump cut が最短タップで効かない

- `ReleaseBufferedJump()` は `Velocity.Z > 0` の時だけ `*= JumpCutVelocityFactor`。
- grounded jump は `Super::Jump()` → 実際の Z 速度付与は **次の CharacterMovement tick（`DoJump`）**。1 フレーム以内に press→release されると release 時点で `Velocity.Z==0` のため **cut が効かず最大高さジャンプ**になる。
- coyote/buffered 側は `LaunchCharacter` で即時に Z が乗るので cut は効く → **ここでも grounded と挙動が非対称**。
- 対策案: cut を「保留フラグ」にして、DoJump 後の最初の上昇フレームで適用する / もしくは grounded も launch に寄せて経路統一（B と同じ話）。

### 🟡 D. マルチプレイモデルの前提が変わっている

- Unity 時代 CLAUDE.md は「オンライン host 権威型」。一方 UE 側 `DefaultEngine.ini` は `bUseSplitscreen=True` = **ローカル couch splitscreen** 前提。handoff の未移植項目も「2-4 人 **local** input」。
- `Tick` で `Movement->Velocity` を直書き・`LaunchCharacter` は **ローカル splitscreen なら問題なし**。ただし将来オンラインに戻すなら、これらは replicate されず desync するので RPC / networked movement 経由が必須。
- **要確認**: UE 版はローカル専用に舵を切ったのか？ CLAUDE.md（Unity 前提のネット同期ポリシー）との整合を handoff に明記すべき。

### 🟡 E. 細かい点

- `PlaneY` は `BlueprintReadWrite` だが、runtime で変更しても `SetPlaneConstraintOrigin` は再適用されない（`ApplyMovementDefaults` を呼ばない限り）。`SnapTo2DPlane` だけが追随するので**拘束原点と snap 先がズレうる**。当面 PlaneY=0 固定なら実害なし。
- dash は `Velocity.Z` を触らないので、空中 dash 中も落下し続ける（重力継続）。意図的なら OK だが「dash 中は浮遊感」を狙うなら Z ゼロ化を検討。設計判断（user 案件）。
- `MaxFallSpeed` の手動クランプは OK だが、UE 標準の PhysicsVolume `TerminalVelocity` と二重になりうる。片方に寄せると保守が楽。
- `SnapTo2DPlane` を毎 Tick 呼ぶのは、`bConstrainToPlane` が効いていれば基本冗長（保険としては可）。
- `bOrientRotationToMovement=true` + `RotationRate=(0,720,0)` は 2.5D で左右反転に使える良い選択。`LastFacingSign`（入力ベース）と実際の mesh yaw が一瞬ズレる可能性はあるが、dash 方向は `LastFacingSign` 基準なので体感上は自然。

---

## 4. 良い点

- 2.5D 拘束を `bConstrainToPlane`（UE 標準機構）で実装 → 自前で毎フレーム座標を殴るより堅い。
- パラメータが全部 `UPROPERTY(EditAnywhere, BlueprintReadWrite)` で露出、`ClampMin/Max` も一部付与 → tuning しやすい。
- dash の cooldown / duration 分離、`IsDashActive` の BlueprintPure 公開など、BP/anim 連携を意識した API 設計。
- input を `BlueprintCallable` の薄い口（Move2D 等）にしている方針自体は、Enhanced Input を BP で組む前提なら妥当。

---

## 5. 次にやるべきこと（順序付き）

1. **C++ module を再ビルドして `ACDP2DCharacter` を editor に載せる**（§3-A/§B の前提）。新規 UCLASS 追加は Live Coding では反映が不安定なので、**editor を閉じて full rebuild** が安全。※ビルドは user 側で実行（Claude は禁止）。
2. **キャラを接続する**。いずれか:
   - `BP_ThirdPersonCharacter` を `CDP2DCharacter` に reparent（既存 mesh 設定を活かせる）、または
   - `BP_CDP2DCharacter` を新規作成し mesh/capsule を設定 → GameMode の `DefaultPawnClass` に指定。
   - どちらでも、movement 値の source of truth を **C++ UPROPERTY に一本化**（MCP で BP 側を直接いじった値は捨てるか、C++ default に反映）。
3. **Enhanced Input を組む**。`IA_Move2D`（Axis1D）/ `IA_Jump`（Digital, press→`RequestBufferedJump`, release→`ReleaseBufferedJump`）/ `IA_Dash` を作り、`IMC_CDP` に bind。BP Event Graph（または C++ `SetupPlayerInputComponent`）から各 `BlueprintCallable` を呼ぶ。※ローカル 2-4 人前提なら player ごとの IMC / gamepad 割当（`bOffsetPlayerGamepadIds` 含む）も設計。
4. **jump 経路を一本化**（§3-B）: coyote も grounded も同じ jump 機構に寄せる。合わせて **jump cut の 1 フレーム穴**（§3-C）を保留フラグ方式で解消。
5. **PIE で feel 検証 → 値調整**。`CoyoteTime` は 0.08s と短め、業界的には 0.10–0.15s が体感良好な帯（要 user 確認 / AAA defacto 調査）。jump buffer 0.10s は妥当。
6. **マルチプレイ前提を確定して handoff に明記**（§3-D）。ローカル splitscreen 確定なら CLAUDE.md のネット同期ポリシーを UE 版向けに更新 or 別紙化。
7. **未移植リスト**（coyote 済なので実質: JumpBoost skill / 表情 / squash&stretch / 足接地 / AnimBP retarget）を TODO 化。

---

## 6. Unreal MCP 運用の所感

- MCP（`:8000`）は「BP 生成・reparent・asset import・PIE 起動・component 値設定」までは回るが、**C++ の追加/変更コンパイルは MCP では不可**。今回のように `.h/.cpp` をファイル直接編集 → user が editor rebuild → その後 MCP で reparent / input asset 作成、という**役割分担が正解**。
- handoff の「commandlet で GameFeatureData warning が出るが import/PIE は成功」は、今のところ無害だが、放置すると後で GameFeature 系の機能を足す時に効いてくる。原因メモを残しておくと良い。
- Codex/Claude 分担としては、**Codex=C++ ロジック実装、Claude=レビュー + MCP での接続/シーン配線** のような切り分けが噛み合いそう（今回の接続漏れは「C++ を書く側」と「MCP で繋ぐ側」の境界で落ちた典型）。次からは「C++ を書いたら誰がいつ繋ぐか」を handoff の完了条件に含めると漏れない。

---

## 付録: 確認できなかったこと（binary / build 禁止のため）

- `BP_ThirdPersonCharacter` が実際に `CDP2DCharacter` を parent にしているか（.uasset は binary、grep 不可）。ただし handoff 記述と grep 結果から**未 reparent と判断**。
- C++ module が既にビルド済みで editor に載っているか。
- Enhanced Input の IMC/IA asset の有無（handoff に記載がなく、**未作成と推定**）。
