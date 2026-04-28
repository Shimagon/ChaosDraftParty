---
name: ChaosDraftParty 外部資源カタログ
description: 採用候補のMCP・Skills・OSS・APIをTier分類で管理。詳細はdocs/RESOURCES.mdを参照
type: reference
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## 本体

詳細は `~/Documents/Dev/ChaosDraftParty/docs/RESOURCES.md`（git管理、両OS対応）。

## 要点

- **Tier S（即採用）**：nice-wolf/unity-claude-skills, donchitos/claude-code-game-studios, anthropics/skills@pptx, KitchenChaosNetcode, Bitesize, EGamePlay, Sentry+GameAnalytics, Discord Webhook, Claude Haiku+Groq, Poly Haven+Freesound
- **Tier A（M2-M6で採用）**：IvanMurzak/Unity-MCP, blender-mcp, ElevenLabs, Meshy Pro, Stable Audio, VOICEVOX, TCleard/PRN, Steamworks.NET
- **赤札（絶対採用しない）**：Suno/Udio（訴訟）、CSM Cube（終了）、X API v2新規（有料化）、Meshy でキャラ生成、CoderGamester/mcp-unity（機能重複）、Figma MCP Pro（過剰）

## 予算

6ヶ月合計 ≈ $180（約27,000円）で卒業発表まで完走可能。内訳：
- M2: $5 (Flux FAL)
- M3: $31 (+Meshy Pro $20 +ElevenLabs Starter $6)
- M4: $36 (+Recraft $5)
- M5: $36 (同上、Groq・Discord は無料)
- M6: +$100 買切 (Steam Direct)

## 運用メモ

- 新ツール導入時は **docs/RESOURCES.md を更新 + `.gitattributes` eol=lf 準拠**
- **Mac / Windows 両対応の install コマンド併記**（feedback_cross_os_windows.md 参照）
- 採用決定したら TODO.md の該当Mタスクに追記
- Tier 変更（B→A等）があったら理由と日付を追記
