---
name: meshy-unity-workflow
description: ChaosDraftParty 専用の Meshy AI → Unity 6 URP アセット生成ワークフロー。ステージ小物（樽、看板、焚火、街灯等）を Meshy で生成し、Assets/ThirdParty/Meshy/Props/ に取込んで prefab 化する一気通貫フロー。credit 予算管理、プロンプトテンプレ、URP 変換、collider 設定、Pro プラン契約のライセンス条件を全部網羅。Claude は Meshy MCP / skill 呼び出しの前にこのスキルを参照。キャラクター生成は禁止（Synty + Mixamo 固定）。
---

# ChaosDraftParty × Meshy AI Workflow

ユーザーは **Meshy Pro プラン契約済**（2026-04-22）。このスキルは Meshy AI を Claude Code から呼び出して、ChaosDraftParty の**ステージ小物・装飾に限定**して生成・Unity 統合する workflow を定義する。

---

## 🚫 絶対ルール（破ると事故）

1. **キャラクター・NPC には Meshy を使わない**（Synty POLYGON Starter + Mixamo 固定、CLAUDE.md の Must Not）
2. **Meshy Community に公開するボタンを絶対に押さない**（押した瞬間 CC0 になって知的財産逸失）
3. **API キーを git に入れない**（`.env` gitignore 済、`~/.claude.json` の user scope で MCP 登録）
4. **credit を浪費しない**：実行前に見積もり提示、ユーザー確認取る
5. **Pro プラン維持必須**（Free プランは CC BY 4.0 で帰属必須 → 卒業発表スライドが汚れる）
6. **生成物は 3 日で URL 失効** → 即 DL → `Assets/ThirdParty/Meshy/Props/` に保存 → git LFS 管理
7. **他者 IP プロンプト禁止**（"pikachu style"、"mario clothes" 等、Meshy は権利保証しない）

---

## 🔑 API キー管理

### 初回セットアップ（すでに済んでいる想定）

1. https://www.meshy.ai/settings/api → `Create API Key`
2. 名前 `chaosdraftparty-local`（識別用）
3. **表示は一度だけ、即保存**（macOS Keychain 推奨）
4. Claude Code に **user scope** で登録（project scope だと `.mcp.json` に書かれて事故）：
   ```bash
   claude mcp add meshy --scope user -- npx -y @meshy-ai/meshy-mcp-server \
     -e MESHY_API_KEY=msy_YOUR_KEY_HERE
   ```

### Windows セットアップ
```powershell
claude mcp add meshy --scope user -- npx -y "@meshy-ai/meshy-mcp-server" `
  -e MESHY_API_KEY="msy_YOUR_KEY_HERE"
```

### 登録確認
```bash
claude mcp list | grep meshy   # ✓ Connected を期待
```

セッション再起動後、`mcp__meshy__*` tools が使えるようになる。

---

## 🧰 Meshy MCP ツール（19 件、公式）

| ツール | 用途 |
|---|---|
| `mcp__meshy__meshy_check_balance` | 残 credit 確認（実行前に必須） |
| `mcp__meshy__meshy_text_to_3d` | Text → Preview mesh 生成 |
| `mcp__meshy__meshy_text_to_3d_refine` | Preview → PBR テクスチャ付き Refine |
| `mcp__meshy__meshy_image_to_3d` | 画像 → 3D（単発、refine 不要） |
| `mcp__meshy__meshy_multi_image_to_3d` | 複数画像（多面図）→ 3D |
| `mcp__meshy__meshy_retexture` | 既存 mesh のテクスチャ差し替え |
| `mcp__meshy__meshy_remesh` | トポロジ再構築 |
| `mcp__meshy__meshy_rig` | Auto-Rigging（ChaosDraftParty では使わない） |
| `mcp__meshy__meshy_animate` | アニメ付け（ChaosDraftParty では使わない） |
| `mcp__meshy__meshy_text_to_image` | 2D 画像生成（プロンプト検証に便利） |
| `mcp__meshy__meshy_image_to_image` | 画像 → 画像（テクスチャバリエーション） |
| `mcp__meshy__meshy_get_task_status` | ポーリング（`wait=true` 内蔵推奨） |
| `mcp__meshy__meshy_list_tasks` | 履歴確認 |
| `mcp__meshy__meshy_cancel_task` | 誤発注キャンセル（credit は消費済なので要注意） |
| `mcp__meshy__meshy_download_model` | GLB ダウンロード（自動保存） |
| `mcp__meshy__meshy_list_models` | ワークスペース内モデル一覧 |
| `meshy_send_to_slicer` / `meshy_analyze_printability` / `meshy_process_multicolor` | 3D 印刷用（本プロジェクトでは不要） |

---

## 💰 Credit 予算と見積もり

### Pro プラン料金
- **月 1,000 credits** ≒ $20/月（年払い割引あり）
- 追加購入は Studio プラン ($40/月 4000 credits) に上げるのが最安

### 操作あたり消費
| 操作 | credits |
|---|---|
| Text-to-3D Preview (Meshy-6 lowpoly) | **20** |
| Text-to-3D Refine (PBR) | **10** |
| Image-to-3D (PBR) | **30** |
| Retexture | **10** |
| Remesh | **5** |
| 小物 1 個完成 (Preview + Refine) | **30** |

### ChaosDraftParty M1-M3 見積
- 20 小物生成 + 試行錯誤 ×2 = **~1400 credits**
- 2 ヶ月分 Pro で余裕（M1-M3 の 2026-04〜06 で完走）

### 実行前の見積もりテンプレ（必ずユーザーに出す）

```
🎨 Meshy 生成予告:
- タスク: "low poly wooden crate" × 5 個
- credit 消費: 20 (preview) × 5 + 10 (refine) × 5 = 150 credits
- 現在残高: XXX credits (check_balance で取得)
- 想定時間: 15 分（バッチ並行）
- 実行してよい？ [y/n]
```

---

## 📝 プロンプトテンプレ（ChaosDraftParty 用）

### 基本構造
```
[Style] + [Subject] + [Material] + [Detail x2] + [flat shading / stylized]
```

### 推奨共通パラメータ（JSON body）
```json
{
  "mode": "preview",
  "ai_model": "meshy-6",
  "model_type": "lowpoly",
  "target_polycount": 3000,
  "target_formats": ["glb"],
  "topology": "triangle",
  "symmetry_mode": "auto",
  "origin_at": "bottom"
}
```

Refine では追加で:
```json
{
  "enable_pbr": true,
  "hd_texture": false
}
```

### ステージ別プロンプト集（20 例、そのまま流用可）

#### 縮む島（shrinking_island）
1. `Low poly floating stone platform block, square chess tile, cracked edges, stylized, flat shading`
2. `Low poly wooden crate with iron bands, vibrant brown, stylized game asset, flat shading`
3. `Low poly old pirate sign board on wooden pole, weathered, stylized`
4. `Low poly small mossy rock cluster, round shapes, green moss, flat shading`
5. `Low poly rusted anchor chain coil, stylized, metallic`

#### 爆弾鬼（bomb_tag）
6. `Low poly cartoon street lamp, black iron, yellow glowing bulb, flat shading`
7. `Low poly green trash bin, overflowing with paper, stylized`
8. `Low poly red mailbox, tilted fallen on grass, vibrant colors`
9. `Low poly fire hydrant, red, cartoon style, simple shapes`
10. `Low poly park bench, wooden slats with iron legs, flat shading`

#### しっぽ狩りバトル（tail_hunt_battle）
11. `Low poly paint dye barrel, bright cyan fluid splashing, stylized`
12. `Low poly colorful flag on wooden pole, triangular pennant, flat shading`
13. `Low poly training cone, orange with white stripes, game asset`
14. `Low poly wooden crate with paint splashes, pink and yellow, stylized`
15. `Low poly signboard with arrow shape, wood texture, flat shading`

#### しっぽ狩り逃走（tail_hunt_escape）
16. `Low poly campfire with stacked logs, orange flame, stylized, flat shading`
17. `Low poly wooden barrel, iron bands, slightly charred, vibrant brown`
18. `Low poly old haunted sign, wood plank with iron nails, weathered`
19. `Low poly stone torch pedestal, carved runes, glowing top, stylized`
20. `Low poly hay bale, golden yellow, rounded cylinder, flat shading`

### 禁則プロンプト
- 他者 IP（Disney / Marvel / Nintendo / Pokemon / 商標名）
- 複数オブジェクト混在（"a barrel and a chest" → 別々に）
- ネガティブプロンプト単独使用（Meshy は negative に弱い、正プロンプトで除外記述）
- 曖昧語（"beautiful", "detailed" は効果薄、「polished brass」「weathered」など具体）

---

## 🔄 完全ワークフロー

### Step 1: 予算確認
```python
mcp__meshy__meshy_check_balance
# → 残 credit 確認、ユーザーに提示
```

### Step 2: 生成バッチ発注（並列可）
```python
# N 個のプロンプト preview を並列発注
task_ids = [mcp__meshy__meshy_text_to_3d(prompt=p, mode="preview", ai_model="meshy-6",
                                         model_type="lowpoly", target_polycount=3000,
                                         target_formats=["glb"], origin_at="bottom")
            for p in prompts]
```

### Step 3: ポーリング（wait 内蔵）
```python
for tid in task_ids:
    result = mcp__meshy__meshy_get_task_status(task_id=tid, wait=True)
    assert result["status"] == "SUCCEEDED"
```

### Step 4: Refine 発注（PBR）
```python
refine_ids = [mcp__meshy__meshy_text_to_3d_refine(preview_task_id=tid, enable_pbr=True,
                                                    target_formats=["glb"])
              for tid in task_ids]
# 同様にポーリング
```

### Step 5: DL → Unity 取込
```python
for tid in refine_ids:
    mcp__meshy__meshy_download_model(task_id=tid, output_dir="./meshy_output/")
    # meshy_output/{timestamp}_{slug}/refined.glb に保存される
```

### Step 6: Assets/ へコピー + Unity 取込
```bash
cp meshy_output/*/refined.glb Assets/ThirdParty/Meshy/Props/<name>.glb
```
```python
mcp__UnityMCP__refresh_unity(mode="force", compile="none", wait_for_ready=True)
mcp__UnityMCP__manage_asset(action="get_info", path="Assets/ThirdParty/Meshy/Props/<name>.glb")
```

### Step 7: Prefab 化 + Collider 設定
```python
# シーンに配置
mcp__UnityMCP__manage_gameobject(action="create", prefab_path="Assets/ThirdParty/Meshy/Props/<name>.glb")
# Collider 追加（樽なら Capsule、箱なら Box）
mcp__UnityMCP__manage_components(action="add", target="<name>", component_type="BoxCollider")
# Prefab 化
mcp__UnityMCP__manage_prefabs(action="create_from_gameobject",
                               target="<name>",
                               prefab_path="Assets/_Project/Prefabs/Props/<name>.prefab")
```

---

## ⚠️ URP 変換の罠（最重要）

### glTFast 自動インポート（推奨）
Unity 6 は `com.unity.cloud.gltfast` で GLB を自動インポート。URP 環境では自動的に **glTF-pbrMetallicRoughness** Shader Graph マテリアルを当てる → **正しく表示される**、追加作業不要。

### URP/Lit に差し替える場合の注意

Meshy の `roughness` マップは glTF 標準（**white=rough**）。URP/Lit の `smoothness` は逆（**white=smooth**）。そのまま繋ぐと艶/マット反転。

回避：
- glTFast のままが一番無難
- 差し替える場合は `AssetPostprocessor.OnPostprocessTexture` で G チャンネル反転
- または事前に ImageMagick で `convert in.png -channel G -negate out.png`

### テクスチャカラースペース
- `base_color` は **sRGB**（Inspector で `sRGB (Color Texture)` ON）
- `metallic` / `roughness` / `normal` / `occlusion` は **Linear**（OFF）
- glTF インポート時は自動、FBX は要手動

---

## 🎯 Collider 戦略（ホスト権威 NGO 前提）

**MeshCollider 直当て禁止**（物理コストが全員ホストに乗る）。

| 小物 | 推奨 Collider | Rigidbody |
|---|---|---|
| 壁・床・タイル | BoxCollider 手動 | なし（static） |
| 樽・ドラム缶 | CapsuleCollider | なし（static） |
| 看板・ポスト | BoxCollider ×複数 | なし |
| 焚火・装飾 | なし | なし |
| 爆弾・投擲物 | SphereCollider + Convex MeshCollider | Rigidbody 必須 |
| 複雑形状 | Convex MeshCollider（頂点≤255） | 用途次第 |

---

## 📋 命名規則

- **プロンプト名から slug 化** して保存：`A wooden barrel...` → `wooden_barrel`
- Unity path: `Assets/ThirdParty/Meshy/Props/<slug>.glb`
- Prefab path: `Assets/_Project/Prefabs/Props/<slug>.prefab`
- マテリアル: glTFast 自動生成、`<slug>.glb` 内蔵か Extract で `Materials/<slug>_mat.mat`

---

## 👥 Claude × User 分業モデル

Meshy は Web UI（https://www.meshy.ai/workspace）と MCP API の**両方**アクセス可能。役割分担：

### Pattern A: Claude フル自動（大量バッチ）
- 20+ プロンプト一気に投げる
- 命名・フォルダ整理・Unity 取込まで自動
- Credit: 20-30cr/個 × 個数
- **適用:** M1-M3 初期スポーン小物の量産

### Pattern B: User Web UI で選別 → Claude で取込（微調整向き）🎯
1. **User** が https://www.meshy.ai/workspace で生成・比較・色調整
2. 気に入った task の ID を Claude に渡す（URL 末尾 or Library から）
3. **Claude** が `meshy_download_model(task_id=…)` + `mcp__UnityMCP__*` で Unity 取込・Prefab 化
- **適用:** ボス級装飾、看板、特徴的な prop 等「見栄えが重要」なもの

### Pattern C: Retexture だけ Claude に任せる
「この木箱もっと明るく」等の微調整：
```python
mcp__meshy__meshy_retexture(
    input_task_id="<元タスク>",
    text_style_prompt="brighter vibrant warm wood, cheerful tone",
    enable_pbr=True
)
```
- Credit: **10cr**（Refine 30cr の 1/3）
- 形状は変えずテクスチャだけ、試行錯誤に最適

### Pattern D: User 側で rig/animate/変形（Web UI のみの機能）
- Web UI のドラッグ変形、マテリアルスライダー、rig 詳細調整は Claude MCP で再現しきれない
- Web UI で完成させた task ID を Claude に渡して取込

### Unity Plugin の是非

公式 Meshy Unity Plugin（https://www.meshy.ai/product/plugins）は **入れなくて良い**：
- GLB 取込は `com.unity.cloud.gltfast` が担当
- Meshy API 操作は MCP が担当
- Plugin が足すのは「Unity 内から過去生成を GUI でブラウズ」だけ、Web UI の方がリッチ
- 入れるデメリット：別の依存、ワークスペース登録の二重管理

**入れる価値があるのは**：User が Unity Editor から一歩も出ずに Meshy を触りたい UX こだわり時のみ。Claude 経由なら常に不要。

---

## 🔁 再現性と credit 節約

- **seed は Meshy API に無い**（2026-04 時点）→ 完全再現不可。気に入った生成は **即コミット**
- 失敗しても credit 消費される → **プロンプトは小バッチ（3-5 個）で試す**
- Retexture は既存 mesh のバリエーションに有効（新規 preview よりコスト半分）
- `meshy_text_to_image` ($0.03-0.09/回) で **事前にスタイル確認** してから Text-to-3D に回す手も

---

## 📚 関連リソース

- `docs/RESOURCES.md` — 本プロジェクト全資産カタログ（Meshy 含む）
- `.claude/skills/chaosdraftparty-gotchas/SKILL.md` — Unity/Claude 罠集
- `.claude/skills/meshy-3d-generation/` — 公式 skill（credit 見積もり規約）
- `CLAUDE.md` — Must Not: Meshy でキャラ生成しない
- https://docs.meshy.ai/ — 公式 API ドキュメント
- https://help.meshy.ai/ — FAQ（商用利用・ライセンス）

---

## 📝 Commit 規約（Meshy 成果物）

```
feat(assets): ステージ小物 N 個取込 (mcp__meshy text_to_3d + refine、mcp__UnityMCP manage_asset)

- Assets/ThirdParty/Meshy/Props/<slug>.glb (N 個)
- Assets/_Project/Prefabs/Props/<slug>.prefab (N 個、BoxCollider 付)
- credit 消費: XXX (残 YYY)
- プロンプト: docs/meshy-prompts.md (履歴)
```

---

## ✅ 初回ラウンドトリップ検証済（2026-04-22）

**wooden_crate で実走検証成功**。30 credits 消費 (20 preview + 10 refine)、残 1570/1600。

確認できたこと：
- `model_type: "lowpoly"` でも **Refine PBR 可能**（ai_model/topology 等は ignored だが refine は通る）
- Preview 20 cr（lowpoly）→ 2 分で完了
- Refine + PBR 10 cr → 2 分で完了、5 テクスチャ（base_color/metallic/roughness/normal/emission）自動 DL
- Unity 側は **`com.unity.cloud.gltfast` v6.18.0** 必須（未インストールだと `.glb` を Unity が認識せず `LoadAssetAtPath<GameObject>` が null）
- glTFast 導入後は GLB が自動で prefab 化され、URP Shader Graph `glTF-pbrMetallicRoughness` マテリアル自動生成、**roughness 反転問題なし**（glTFast が自動処理）
- prefab を import 済の glb 同一 GameObject から `manage_prefabs create_from_gameobject` しようとすると「既に prefab link」エラー → `unlink_if_instance: true` で解消
- `Assets/ThirdParty/Meshy/Props/<slug>.glb` + 5 テクスチャ PNG 保存、`Assets/_Project/Prefabs/Props/<slug>.prefab`（BoxCollider 付）が最終形

### 事前インストール（初回のみ）
```bash
# Unity MCP で：
mcp__UnityMCP__manage_packages(action="add_package", package="com.unity.cloud.gltfast")
```

---

**初版:** 2026-04-22
**初検証:** 2026-04-22 wooden_crate ラウンドトリップ成功
