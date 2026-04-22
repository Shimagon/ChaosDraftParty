# Blender MCP セットアップ手順

ChaosDraftParty で EGGYBOT キャラクターを Blender 経由で生成・編集するための Blender MCP サーバー + Blender addon の設定。

## 前提
- Blender インストール済（v4.x 推奨）
- Claude Code 起動中
- `uvx` インストール済（`~/.local/bin/uvx` — Blender MCP の Python ランタイム）

## ステータスチェック（2026-04-22 時点）
```
✅ Claude MCP 登録: blender (uvx blender-mcp) 完了
✅ Python パッケージ取得: 68 packages installed
❌ Blender addon 未有効化 → 以下の手順で有効化
❌ Claude Code 未再起動 → addon 有効化後に再起動必要
```

## 手順

### 1. Blender に addon をインストール
addon.py は既に `~/Downloads/blender_mcp_addon.py` にダウンロード済み（112KB）。

1. Blender を起動
2. メニュー **Edit > Preferences > Add-ons**
3. **Install from Disk...**（右上ハンバーガーメニュー内）
4. `~/Downloads/blender_mcp_addon.py` を選択
5. Add-ons リストで **"Interface: Blender MCP"** のチェックを ON

### 2. Blender 側のサーバー起動
1. Blender の 3D Viewport で **`N` キー**を押してサイドバー開く
2. サイドバー右端に **"BlenderMCP"** タブが追加されているはず
3. タブを開き、**"Connect to Claude"** ボタンクリック
4. 「Server started on port 9876」等が出れば OK

### 3. Claude Code 再起動
Unity MCP tools も今のセッションに未登録状態なので、**同時に Unity MCP も再ロード**される。

```bash
# 現在のセッション exit
/exit

# 再起動
claude
```

### 4. 動作確認（Claude 側）
再起動後、Claude にこう頼む：
> "Blender MCP つながってる？ get_scene_info 呼んで確認して"

正常なら Blender 側で `BlenderMCP Server running at port 9876` みたいなログが出て、Claude から scene info 取れる。

## トラブル

### `uvx blender-mcp` が Blender に接続できない
- 症状: `Failed to connect to Blender: [Errno 61] Connection refused`
- 原因: Blender addon が起動していない
- 対処: Blender → サイドバー → BlenderMCP タブ → Connect ボタンを押す

### Claude 側に `mcp__blender__*` tools が出ない
- 原因: Claude Code セッションが addon 起動前のまま
- 対処: `/exit` して再起動

### 「uvx: command not found」
- 原因: uv/uvx がインストールされていない
- 対処: `curl -LsSf https://astral.sh/uv/install.sh | sh`

## Claude から使える主要 tool
再起動後に使える（スキーマはその時に load される）:

- `get_scene_info` — 現在の Blender scene の構造取得
- `execute_blender_code` — 任意の Blender Python 実行（最強、メッシュ作成〜修正までなんでも）
- `get_polyhaven_asset_list` / `download_polyhaven_asset` — HDRI/テクスチャ無料アセット
- `download_sketchfab_model` — Sketchfab 無料 3D モデル DL
- `generate_hyper3d_model_via_text` — Hyper3D Rodin（Meshy 競合、別 API）で text-to-3d

ChaosDraftParty での用途:
- **EGGYBOT 精密モデリング**: 球 primitive × N を Python で配置・結合 → FBX export → Unity
- **既存 Meshy モデルの修正**: 手足追加、Shape Key 設置、weight paint
- **URP ガラスマテリアルの事前 preview**: Blender 側で EEVEE 高速プレビュー → パラ Unity に移植
