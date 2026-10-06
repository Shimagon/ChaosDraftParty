# 2026-06-30 Unreal Pivot Setup Handoff

## Summary

- Created Unreal Engine 5.8 project at `Unreal/ChaosDraftPartyUE`.
- Based on UE 5.8 `TP_ThirdPersonBP` template because the installed UE 5.8 template set did not expose a Side Scroller template.
- Enabled UE 5.8 experimental MCP plugins:
  - `ModelContextProtocol`
  - `MCPClientToolset`
  - `AllToolsets`
- Configured MCP server auto-start on `http://127.0.0.1:8000/mcp`.
- Verified MCP initialize, tool listing, toolset listing, Blueprint compile/save, and PIE start/stop.

## Project Paths

- Unreal project: `Unreal/ChaosDraftPartyUE/ChaosDraftPartyUE.uproject`
- MCP client config: `Unreal/ChaosDraftPartyUE/.mcp.json`
- MCP editor settings: `Unreal/ChaosDraftPartyUE/Config/DefaultEditorPerProjectUserSettings.ini`
- Active level: `/Game/ThirdPerson/Lvl_ThirdPerson`
- Player Blueprint: `/Game/ThirdPerson/Blueprints/BP_ThirdPersonCharacter`

## MCP Connection

Local MCP endpoint:

```json
{
  "mcpServers": {
    "unreal-mcp": {
      "type": "streamable-http",
      "url": "http://localhost:8000/mcp"
    }
  }
}
```

Verified available toolsets include:

- `EditorToolset.EditorAppToolset`
- `editor_toolset.toolsets.scene.SceneTools`
- `editor_toolset.toolsets.actor.ActorTools`
- `editor_toolset.toolsets.object.ObjectTools`
- `editor_toolset.toolsets.asset.AssetTools`
- `editor_toolset.toolsets.blueprint.BlueprintTools`
- `editor_toolset.toolsets.primitive.PrimitiveTools`

## Unity Movement Values Carried Forward

Source inspected:

- `Assets/_Project/Scripts/Core/PlayerController.cs`
- `Assets/_Project/ScriptableObjects/Configs/GameConfig.asset`
- `Assets/_Project/Scripts/Skills/Effects/DashSkillEffect.cs`
- `Assets/_Project/Scripts/Skills/Effects/JumpBoostEffect.cs`

UE `BP_ThirdPersonCharacter` CharacterMovement defaults set:

| Unity intent | Unity value | UE setting |
|---|---:|---:|
| Dash/run speed | `8 m/s` | `maxWalkSpeed = 800` |
| Walk/min analog speed | `1.5 m/s` | `minAnalogWalkSpeed = 150` |
| Ground acceleration | `25 m/s^2` | `maxAcceleration = 2500` |
| Ground deceleration | `36 m/s^2` | `brakingDecelerationWalking = 3600` |
| Air acceleration feel | `9 m/s^2` | `brakingDecelerationFalling = 900` |
| Jump speed | `5 m/s` | `jumpZVelocity = 500` |
| Air control factor | `0.65` | `airControl = 0.65` |
| 2.5D plane lock | X-Z plane | `bConstrainToPlane = true`, `planeConstraintNormal = (0, 1, 0)`, `bSnapToPlaneAtStart = true` |

Also set on the character CDO:

- `bUseControllerRotationYaw = false`
- `bReplicates = true`
- `bReplicateMovement = true`

Still not ported:

- coyote time
- jump buffer
- jump cut
- Dash/JumpBoost skill activation
- KO/knockback loop
- local multiplayer input mapping

## 2.5D Prototype Scene

Added actors to `/Game/ThirdPerson/Lvl_ThirdPerson`:

- `CDP_2_5D_MainPlatform`
- `CDP_2_5D_LeftPlatform`
- `CDP_2_5D_RightPlatform`
- `CDP_2_5D_SideCamera`

Camera:

- Location: `(0, -1600, 420)`
- FOV: `55`
- `autoActivateForPlayer = Player0`

PIE verification:

- `save_assets([])` returned `true`
- `StartPIE` returned success
- `IsPIERunning` returned `true`
- `StopPIE` returned success
- `IsPIERunning` returned `false`

## Notes

- `.gitignore` was updated to ignore Unreal generated folders: `Binaries`, `DerivedDataCache`, `Intermediate`, `Saved`.
- Existing Unity project files were not reverted or cleaned. The worktree already had many Unity-side pending changes before the Unreal setup.
- The UE project browser process may still be open separately from the active `ChaosDraftPartyUE - Unreal Editor` window.

## Next Recommended Work

1. Create a dedicated UE `BP_CDP_2_5D_Character` instead of continuing to modify the template Blueprint directly.
2. Implement coyote time, jump buffer, jump cut, dash, and knockback in Blueprint or C++.
3. Replace the temporary cube platforms with a real Smash-style shrinking/edge-KO stage.
4. Configure Enhanced Input for local 2-4 players.
5. Add replication checks once the basic 2.5D feel is stable.
