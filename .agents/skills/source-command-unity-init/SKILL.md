---
name: "source-command-unity-init"
description: "Scans a Unity project and generates a tailored AGENTS.md with detected configuration, packages, render pipeline, and recommended skills."
---

# source-command-unity-init

Use this skill when the user asks to run the migrated source command `unity-init`.

## Command Template

# /unity-init — Project Setup

Scan this Unity project and generate a tailored AGENTS.md configuration.

## Steps

1. **Read project info** via MCP `project_info` resource to get Unity version, platform, and state.

2. **Scan Packages/manifest.json** to detect installed packages:
   - Render pipeline (URP, HDRP, or Built-in)
   - Input System, Addressables, Cinemachine, Timeline, TextMeshPro
   - Networking (Netcode, Mirror, Photon, Fish-Net)
   - Third-party (DOTween, UniTask, VContainer, Zenject, Odin)

3. **Scan for assembly definitions** (`.asmdef` files) — map the project's assembly structure.

4. **Scan for scenes** — list all `.unity` files in `Assets/`.

5. **Check existing AGENTS.md** — if one exists, preserve user customizations.

6. **Generate AGENTS.md** with:
   - Project overview (Unity version, render pipeline, detected packages)
   - Assembly structure
   - Scene list
   - References to rules files (`.Codex/rules/*.md`)
   - Recommended skills based on detected packages
   - MCP integration notes
   - Key conventions summary

7. **Report** what was detected and configured. Suggest next steps:
   - Review and customize the generated AGENTS.md
   - Install unity-mcp if not already present
   - Try `/unity-audit` for a full project health check

## Output

Present the results in a clear summary table showing what was detected and which skills are recommended.
