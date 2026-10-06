# 2026-06-30 Unreal 移行セットアップ引き継ぎ

## 結論

MCP はつながっています。

- Unreal Editor: UE 5.8
- Project: `Unreal/ChaosDraftPartyUE/ChaosDraftPartyUE.uproject`
- MCP endpoint: `http://127.0.0.1:8000/mcp`
- Epic 公式の Unreal MCP 手順と照合済み
- 接続確認: `initialize` / `tools/list` / `list_toolsets` / `call_tool` 成功
- PIE 確認: `StartPIE -> IsPIERunning=true -> StopPIE` 成功

## いまの UE プロジェクト

UE 5.8 の Third Person Blueprint template をベースに、2.5D 用の仮プロジェクトを作成済み。

有効化した主な plugin:

- `ModelContextProtocol`
- `MCPClientToolset`
- `AllToolsets`
- `PythonScriptPlugin`

MCP 設定ファイル:

- `Unreal/ChaosDraftPartyUE/.mcp.json`
- `Unreal/ChaosDraftPartyUE/Config/DefaultEditorPerProjectUserSettings.ini`

`.mcp.json` はこの内容:

```json
{
  "mcpServers": {
    "unreal-mcp": {
      "type": "http",
      "url": "http://127.0.0.1:8000/mcp"
    }
  }
}
```

公式手順上も、UE Editor 内の Unreal MCP server を起動し、MCP client 側に `http://127.0.0.1:8000/mcp` を登録する流れ。
Codex / Claude Code / Cursor / VS Code 向けの client config 生成にも対応している。

## Unity キャラ移行

Unity 側の Eggybot を UE に import 済み。

元ファイル:

- `Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_v16.fbx`

UE 側に生成された主な asset:

- `/Game/CDP/Characters/Eggybot/SK_EggybotV16`
- `/Game/CDP/Characters/Eggybot/SK_EggybotV16_Skeleton`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Pink`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Clear`
- `/Game/CDP/Characters/Eggybot/EGGYBOT_Eye`

FBX import は UE Python commandlet で実行。

使用スクリプト:

- `Unreal/ChaosDraftPartyUE/Scripts/import_eggybot.py`

## Character Blueprint への反映

`/Game/ThirdPerson/Blueprints/BP_ThirdPersonCharacter` の mesh を Eggybot に差し替え済み。

設定した Mesh component:

- `/Game/ThirdPerson/Blueprints/BP_ThirdPersonCharacter.Default__BP_ThirdPersonCharacter_C:CharacterMesh0`

設定値:

- `skeletalMesh = /Game/CDP/Characters/Eggybot/SK_EggybotV16.SK_EggybotV16`
- `relativeLocation = (0, 0, -83)`
- `relativeRotation = (0, -90, 0)`
- `relativeScale3D = (1.5, 1.5, 1.5)`

`SK_EggybotV16` の実測 bounds:

- 高さ: 約 114 cm
- slot: `EGGYBOT_Pink`, `EGGYBOT_Clear`, `EGGYBOT_Eye`
- bones: `EggybotArmature`, `Hips`, `Spine`, `Chest`, `Neck`, `Head`, `CapHinge`

既存 UE capsule に対してそのままだと小さめなので、いったん `1.5` 倍にして capsule 内に収めている。

## Unity の動きの移植状況

Unity の `PlayerController.cs` などから初期値を読んで、UE の CharacterMovement に反映済み。

主な設定:

| 項目 | Unity | UE |
|---|---:|---:|
| ダッシュ/走り速度 | `8 m/s` | `maxWalkSpeed = 800` |
| 歩き速度 | `1.5 m/s` | `minAnalogWalkSpeed = 150` |
| 地上加速 | `25 m/s^2` | `maxAcceleration = 2500` |
| 地上減速 | `36 m/s^2` | `brakingDecelerationWalking = 3600` |
| ジャンプ | `5 m/s` | `jumpZVelocity = 500` |
| 空中制御 | `0.65` | `airControl = 0.65` |
| 2.5D 固定 | X-Z 平面 | `planeConstraintNormal = (0, 1, 0)` |

## 2.5D ステージ

現在の level:

- `/Game/ThirdPerson/Lvl_ThirdPerson`

追加済み actor:

- `CDP_2_5D_MainPlatform`
- `CDP_2_5D_LeftPlatform`
- `CDP_2_5D_RightPlatform`
- `CDP_2_5D_SideCamera`

サイドカメラ:

- Location: `(0, -1600, 420)`
- FOV: `55`
- `autoActivateForPlayer = Player0`

## まだ未移植

ここは次にやる必要あり。

- coyote time
- jump buffer
- jump cut
- Dash skill
- JumpBoost skill
- knockback / KO
- 2-4 人 local input
- Eggybot 専用 AnimBP / animation retarget
- Unity の表情制御、squash/stretch、足接地補正

## 注意

- Unity prefab は UE に直接移植できない。Unity prefab は YAML + Unity component 依存なので、UE には FBX / texture / material / animation / gameplay logic を分けて持っていく必要がある。
- 今回はまず `eggybot_v16.fbx` を UE SkeletalMesh として import し、既存 ThirdPerson Character の mesh を差し替えた段階。
- commandlet 実行時に GameFeatureData 設定不足の UE 側 warning/error が出るが、FBX import と PIE 起動は成功している。
- Git commit / stage はしていない。
