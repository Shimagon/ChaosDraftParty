---
name: Meshy AI Pro 契約 + ChaosDraftParty 使用方針
description: ユーザーが Meshy Pro 契約済（2026-04-22）。ChaosDraftParty でのステージ小物生成専用ワークフロー、MCP/skill 登録状況、API キー管理、絶対ルール
type: reference
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## 契約状況

- **プラン:** Meshy Pro ($20/月、1000 credits/月)
- **契約日:** 2026-04-22
- **所有権:** 生成物完全所有、商用利用 OK、帰属表示不要、Steam 配布 OK
- **ライセンス絶対禁止:** Meshy Community への公開ボタン押下（押したら CC0 化）

## 登録済みツール

### MCP サーバー
- **`@meshy-ai/meshy-mcp-server`** (公式、19 ツール、MIT)
- 登録コマンド（user scope 必須、project scope だと .mcp.json に key が漏れる）：
  ```bash
  claude mcp add meshy --scope user -- npx -y @meshy-ai/meshy-mcp-server \
    -e MESHY_API_KEY=msy_...
  ```

### Skills
- `meshy-dev/meshy-3d-agent`（公式、credit 見積もり組込）— global 導入済
- `opusgamelabs/game-creator`（meshyai + game-3d-assets 他）— global 導入済
- `.claude/skills/meshy-unity-workflow/`（プロジェクト専用、git 管理）— 20 プロンプト例 + URP 変換ガイド入り

## 絶対ルール

1. **キャラクター・NPC には使わない**（Synty + Mixamo 固定、CLAUDE.md Must Not）
2. **Meshy Community に公開しない**（CC0 化で IP 逸失）
3. **API キーを git に入れない**（`~/.claude.json` user scope のみ、`.env` gitignore）
4. **実行前に credit 見積もりをユーザーに提示**（浪費防止）
5. **Pro プラン維持**（Free は CC BY 帰属必須、発表向き NG）
6. **生成物は 3 日で URL 失効** → 即 DL → `Assets/ThirdParty/Meshy/Props/` に LFS コミット
7. **他者 IP 名をプロンプトに入れない**（Pokemon / Mario 等）

## Credit 予算

- M1-M3（4-6月）で 20 小物 × 30cr + 試行錯誤 ×2 = **~1400 credits**
- Pro 2 ヶ月分（2000 credits）で完走可能
- 枯渇したら Studio プラン ($40/月 4000cr) に一時アップグレード

## 推奨パラメータ（ChaosDraftParty 標準）

```json
{
  "ai_model": "meshy-6",
  "model_type": "lowpoly",
  "target_polycount": 3000,
  "target_formats": ["glb"],
  "topology": "triangle",
  "symmetry_mode": "auto",
  "origin_at": "bottom",
  "enable_pbr": true (Refine 時)
}
```

## Unity 統合

- **推奨:** glTFast 自動インポート（URP 自動認識、`com.unity.cloud.gltfast` パッケージ標準）
- **URP/Lit 手動差し替え時の罠:** Meshy roughness (white=rough) vs URP smoothness (white=smooth) 反転
- **Collider:** MeshCollider 直当て禁止、用途別に Box/Capsule/Sphere 手動配置
- **パス:** `Assets/ThirdParty/Meshy/Props/<slug>.glb` → prefab `Assets/_Project/Prefabs/Props/<slug>.prefab`

## API キー取得手順（ユーザー側、Claude はやらない）

1. https://www.meshy.ai/settings/api → `Create API Key`
2. 名前 `chaosdraftparty-local` 推奨
3. **表示は 1 回限り**、即保存
4. Claude に MCP 登録コマンド渡す（キー含む、user scope）
5. Claude Code 再起動で `mcp__meshy__*` ツール使用可

## 関連

- `docs/RESOURCES.md` — Tier S に昇格反映済
- `.claude/skills/meshy-unity-workflow/SKILL.md` — 実行 workflow
- CLAUDE.md の Must Not セクション
