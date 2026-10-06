# 2026-07-01 Unreal MCP / Claude 分担ステータス

## 結論

止まっていません。Unreal Editor 5.8 は MCP 付きで起動できており、Codex から `http://127.0.0.1:8000/mcp` に接続して Editor 操作まで通っています。

## 許可待ち対策

- Codex 側: この環境は `approval policy: never` なので、Codex の PowerShell 実行で追加の許可ボタン待ちは基本的に発生しません。
- Claude 側: `claude --dangerously-skip-permissions` で起動済み。Claude Code の通常の確認待ちを避ける設定です。
- 例外: Epic/Windows/ブラウザ/ログイン/セキュリティ系の OS レベル確認は、事前に一括許可できない場合があります。現時点でそれによるブロックはありません。

## 画面の右側ターミナルについて

Epic 公式ドキュメントの動画に出ている右側の黒い端末は、Unreal Editor 内または横に置いた Claude Code / Codex などの MCP 対応 AI クライアントです。

今回こちらで構成したものも同じ考え方です。

- Unreal Editor: MCP server
- Claude / Codex: MCP client
- 接続先: `http://127.0.0.1:8000/mcp`

既存ターミナルのプロンプトへ UI 入力を代行する操作は安全ルール上やっていません。その代わり、同じ Windows Terminal に `Claude interactive - Unreal MCP` タブを追加し、`claude --dangerously-skip-permissions` を起動しました。

## GitHub Flow

作業ブランチを切りました。

- current branch: `codex/unreal-migration`
- base: `main`

GitHub Flow は、main から短命の作業ブランチを切って、作業後に PR で戻す形で進めます。Claude 側を別作業にする場合は `claude/unreal-review` や `claude/unreal-input-wiring` のように分けるのがよいです。

## Unreal / MCP セットアップ

- UE project: `Unreal/ChaosDraftPartyUE/ChaosDraftPartyUE.uproject`
- UE version: 5.8
- MCP endpoint: `http://127.0.0.1:8000/mcp`
- `.mcp.json`: `Unreal/ChaosDraftPartyUE/.mcp.json`

有効化済み plugin:

- `ModelContextProtocol`
- `MCPClientToolset`
- `AllToolsets`
- `PythonScriptPlugin`

確認済み:

- `initialize`
- `tools/list`
- `list_toolsets`
- `call_tool`
- `StartPIE`
- `IsPIERunning`
- `StopPIE`

## Unity キャラ移行

Unity 側の Eggybot FBX を Unreal に import 済みです。

元ファイル:

- `Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_v16.fbx`

Unreal 側 asset:

- `/Game/CDP/Characters/Eggybot/SK_EggybotV16`
- `/Game/CDP/Characters/Eggybot/SK_EggybotV16_Skeleton`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Pink`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Clear`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Eye`

## C++ 2.5D キャラ

C++ module を追加し、ビルド成功済みです。

主要ファイル:

- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Public/CDP2DCharacter.h`
- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Private/CDP2DCharacter.cpp`

実装済み:

- X-Z 平面固定、Y=0 snap
- coyote time
- jump buffer
- jump cut
- dash
- knockback API
- `Jump()` / `StopJumping()` override
- BlueprintCallable: `Move2D`, `RequestBufferedJump`, `ReleaseBufferedJump`, `RequestDash`
- legacy input binding: `CDP_Move2D`, `CDP_Jump`, `CDP_Dash`

入力 mapping も `Config/DefaultInput.ini` に追加済みです。

- Move: `A/D`, `Left/Right`, `Gamepad_LeftX`
- Jump: `SpaceBar`, `Gamepad_FaceButton_Bottom`
- Dash: `LeftShift`, `Gamepad_FaceButton_Right`

## Blueprint 接続

新しい Eggybot 用 BP を作成し、C++ キャラへ reparent 済みです。

- `/Game/CDP/Characters/Eggybot/BP_CDP2D_EggybotCharacter`
- parent: `/Script/ChaosDraftPartyUE.CDP2DCharacter`

GameMode の default pawn も切り替え済みです。

- GameMode: `/Game/ThirdPerson/Blueprints/BP_ThirdPersonGameMode`
- `DefaultPawnClass`: `/Game/CDP/Characters/Eggybot/BP_CDP2D_EggybotCharacter.BP_CDP2D_EggybotCharacter_C`

## PIE 確認

MCP から PIE 起動と停止は通りました。

- `StartPIE`: success
- `IsPIERunning`: true
- `StopPIE`: success
- stop 後 `IsPIERunning`: false
- 入力 binding 追加後の C++ full rebuild: success
- Editor 再起動後も GameMode の `DefaultPawnClass` は `BP_CDP2D_EggybotCharacter_C` のまま維持
- runtime pawn 直接検証: success
- log: `CDP2DCharacter input bound: BP_CDP2D_EggybotCharacter_C_0 component=EnhancedInputComponent`
- log: `CDP2DCharacter BeginPlay: BP_CDP2D_EggybotCharacter_C_0 class=/Game/CDP/Characters/Eggybot/BP_CDP2D_EggybotCharacter.BP_CDP2D_EggybotCharacter_C`

ただし、実入力で左右移動・ジャンプ・ダッシュする視覚確認はまだ次タスクです。

## 次にやること

1. 実入力で左右移動・ジャンプ・ダッシュを確認する。
2. 必要なら legacy input から Enhanced Input の `IA_Move2D` / `IA_Jump` / `IA_Dash` / `IMC_CDP` へ移行する。
3. 2.5D スマブラ風ステージを template map から専用 map に分ける。
4. Eggybot 用 AnimBP / idle-run-jump の最低限を作る。
5. Claude 側レビューを文字化けしない形で再出力する。
