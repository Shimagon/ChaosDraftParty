# 2026-07-01 Unreal 移行 Codex 作業ログ

## 現在地

- ブランチ: `codex/unreal-migration`
- Unreal project: `Unreal/ChaosDraftPartyUE/ChaosDraftPartyUE.uproject`
- Editor: UE 5.8
- MCP: `http://127.0.0.1:8000/mcp`
- Claude 連携: `tools/claude_bridge/claude_prompt_bridge.ps1` を使うファイル bridge 方式

## 完了

1. Unreal MCP
   - `.mcp.json` と UE MCP plugin 設定済み。
   - Codex から `list_toolsets` / `SceneTools` / `ConfigSettingsToolset` / `SlateInspectorToolset` / `LogsToolset` を実行確認済み。

2. Unity Eggybot 移行
   - Unity FBX: `Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_v16.fbx`
   - UE asset: `/Game/CDP/Characters/Eggybot/SK_EggybotV16`
   - Pawn BP: `/Game/CDP/Characters/Eggybot/BP_CDP2D_EggybotCharacter`
   - GameMode default pawn は `BP_CDP2D_EggybotCharacter_C`。

3. 2.5D movement C++
   - `ACDP2DCharacter` 追加済み。
   - X/Z 平面移動、Y=0 plane lock、coyote time、jump buffer、jump cut、dash、knockback entry を実装。
   - Spawn collision で Pawn が止まらないよう `AdjustIfPossibleButAlwaysSpawn` と `MinimumStartZ` を追加。
   - Eggybot mesh 表示用に `MeshRelativeOffset` / `MeshRelativeScale` を BeginPlay で適用。

4. 入力実測
   - PIE + MCP Slate key press で以下を確認済み。
   - `D`: `Move2D input` / `MoveRight pressed`
   - `Space`: `Jump pressed`
   - `E`: `Dash pressed`
   - Jump は同一フレーム二重発火を抑止済み。

5. 2.5D arena
   - 専用 map 作成: `/Game/CDP/Maps/Lvl_CDP_2_5D_Arena`
   - `DefaultEngine.ini` の `EditorStartupMap` / `GameDefaultMap` を arena に変更。
   - `bUseSplitscreen=False` に変更。
   - main / left / right / top platform、backdrop、level-side shared camera を配置。
   - `ACDP2DArenaCamera` 追加済み。全 `ACDP2DCharacter` の位置から X/Z center を追う orthographic shared camera。

## 最新検証ログ要点

PIE on `/Game/CDP/Maps/Lvl_CDP_2_5D_Arena`:

```text
CDP2DCharacter input bound: BP_CDP2D_EggybotCharacter_C_0 component=EnhancedInputComponent
CDP2DCharacter BeginPlay: BP_CDP2D_EggybotCharacter_C_0
CDP2DCharacter Move2D input: ... axis=1.00
CDP2DCharacter MoveRight pressed: ...
CDP2DCharacter Jump pressed: ...
CDP2DCharacter Dash pressed: ... direction=1.0
```

## Claude bridge 返答要約

bridge 出力は一部 mojibake したが、以下の指摘を確認した。

- P0: Editor screenshot ではなく、PIE の game view を `HighResShot` / Standalone 相当で取り、`GetViewTarget()` が `ACDP2DArenaCamera` かログで確認する。これが終わるまで mesh / ortho の見た目調整を断定しない。
- P1: `MeshRelativeOffset=(0,-80,-80)` はキャラ左右反転時に depth pop する恐れがあるため、Y offset は 0 に戻す方向が安全。
- P1: `ACDP2DArenaCamera` は毎 Tick `SetViewTarget` ではなく、PlayerController の `bAutoManageActiveCameraTarget=false` と BeginPlay 1 回設定へ寄せる。
- P1: Enhanced Input は `IMC_CDP` + `IA_Move2D` / `IA_Jump` / `IA_Dash` に完全移行する。
- P2: Orthographic + Lumen/VSM の見え方確認、専用 `BP_CDP2D_GameMode`、`LogCDP` カテゴリ化。

## 注意点 / 次にやること

- Editor screenshot API はゲームカメラではなく editor viewport を拾っている挙動があり、視覚確認はまだ弱い。
- 次は実際の PIE viewport をユーザー目線で見ながら、Eggybot の見え方、mesh scale、camera ortho width を調整する。
- Enhanced Input asset (`IA_Move2D`, `IA_Jump`, `IA_Dash`, `IMC_CDP`) への完全移行は未実施。今は legacy mapping + EnhancedInputComponent で実キー到達は確認済み。
- 旧 Unity 側の変更は触っていない。Unreal 側だけ進めた。

## Claude への渡し方

`work/claude_bridge/inbox.md` の `---` より下に依頼を書くと bridge が `claude -c -p` に流す。

今回 Claude に渡すなら:

```text
Codex が Unreal 2.5D migration を進めた。docs/handoffs/2026-07-01_unreal_migration_codex_status_ja.md を読んで、次に優先すべき順で P0/P1/P2 のレビューを返して。特に PIE のゲームカメラ視覚確認、Eggybot mesh scale、Enhanced Input 完全移行を見て。
```
