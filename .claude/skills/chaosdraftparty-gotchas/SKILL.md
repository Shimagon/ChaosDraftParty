---
name: chaosdraftparty-gotchas
description: ChaosDraftParty（Unity 6 + NGO + Relay + URP のソロ卒業制作）開発中に Claude と踏んだ罠と回避策の蓄積集。execute_code で Unity 操作する時、新規シーン作る時、環境構築時、マテリアルがピンク化した時、Unity/Claude 間で予期せぬ挙動が出た時に必ず参照。新規罠を踏んだら末尾に追記して commit。macOS / Windows 両対応。
---

# ChaosDraftParty 罠回避集

このスキルは **ChaosDraftParty プロジェクトでのみ有効** な現場知恵の蓄積ファイル。Unity 6.4 + NGO 2.11 + Relay + URP 17 + Input System 1.19 + Cinemachine 3.1 のスタックで、MCP for Unity + Claude Code 経由で開発している固有の罠。

一般的な Unity 知識は `nice-wolf-studio/unity-claude-skills` 群を使う。こちらは **個別ケース** を貯める。

## 運用ルール（growth model）

- **新しい罠を踏んだら即追記**。コミットは `docs(gotchas): add <症状名> 回避策`
- 各項目は **症状 → 原因 → 回避策 → 初遭遇日** の 4 要素を必ず埋める
- コードブロックは動作保証付き（自分で回した or ログで確認済み）のみ
- Unity / Claude Code のアップデートで挙動が変わったら「YYYY-MM-DD 以降は無効」等追記、削除はしない
- Mac / Windows の差分があれば両 OS 表記
- 章の index は下の目次に反映

## 目次（罠リスト）

1. [execute_code 本文に using ディレクティブを書けない](#1-execute_code-本文に-using-ディレクティブを書けない)
2. [Unity 6 新規シーンで Material が全部ピンク（URP 未 assign）](#2-unity-6-新規シーンで-material-が全部ピンクurp-未-assign)
3. [Unity を Finder / Explorer のダブルクリックで起動しない](#3-unity-を-finder--explorer-のダブルクリックで起動しない)
4. [MCP 登録の Claude CLI Path に `.exe` が付いてても macOS で正常](#4-mcp-登録の-claude-cli-path-に-exe-が付いてても-macos-で正常)
5. [symlink 経由で Claude Code 起動すると memory が分離する](#5-symlink-経由で-claude-code-起動すると-memory-が分離する)
6. [`npx skills add` は `-y` フラグがないと対話プロンプトで止まる](#6-npx-skills-add-は--y-フラグがないと対話プロンプトで止まる)
7. [Configure All Detected Clients ボタンで既存 MCP 登録が消える](#7-configure-all-detected-clients-ボタンで既存-mcp-登録が消える)
8. [新規 MCP 登録は既存 Claude セッションに反映されない](#8-新規-mcp-登録は既存-claude-セッションに反映されない)

---

## 1. execute_code 本文に using ディレクティブを書けない

**症状:** Roslyn compile error `'UnityEngine' is a namespace but is used like a type` / `Identifier expected` など。

**原因:** `mcp__UnityMCP__execute_code` のコードはメソッドボディとしてコンパイルされる。`using` はファイルトップレベル構文、メソッド内では使えない。

**回避策:** 完全修飾名で書く。`UnityEngine.*` と `UnityEditor.*` は暗黙的に利用可能なので `GameObject`, `Shader`, `AssetDatabase` はそのまま書ける。ただし下のネームスペースは完全修飾必須：
- `UnityEditor.SceneManagement.EditorSceneManager`
- `UnityEngine.Rendering.GraphicsSettings`
- `UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset`
- `UnityEngine.Rendering.Universal.UniversalRendererData`
- `System.Collections.Generic.List<T>`（単発なら `System.Collections.Generic.List<string>` と書く）

```csharp
// OK
var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
var rpa = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
```

**初遭遇:** 2026-04-21

---

## 2. Unity 6 新規シーンで Material が全部ピンク（URP 未 assign）

**症状:** URP shader を正しくアサインしているのに、Game View ですべて Missing Shader のマゼンタ表示になる。

**原因:** Unity 6 テンプレの新規プロジェクトは URP パッケージは入っているが `GraphicsSettings.currentRenderPipeline = NULL`。Pipeline Asset 未 assign なので URP shader がレンダリングできず Missing 扱い。

**診断 (execute_code):**
```csharp
var rpa = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
return $"RPA={rpa?.GetType().Name ?? "NULL"}";  // "NULL" なら原因確定
```

**回避策:** URP Asset + RendererData を生成して Graphics + Quality Settings に assign する：
```csharp
if (!AssetDatabase.IsValidFolder("Assets/Settings"))
    AssetDatabase.CreateFolder("Assets", "Settings");

var rendererData = ScriptableObject.CreateInstance<UnityEngine.Rendering.Universal.UniversalRendererData>();
rendererData.name = "DefaultRendererData";
AssetDatabase.CreateAsset(rendererData, "Assets/Settings/DefaultRendererData.asset");

var urp = UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(rendererData);
urp.name = "DefaultURP";
AssetDatabase.CreateAsset(urp, "Assets/Settings/DefaultURP.asset");

UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = urp;
int before = QualitySettings.GetQualityLevel();
for (int i = 0; i < QualitySettings.names.Length; i++) {
    QualitySettings.SetQualityLevel(i, false);
    QualitySettings.renderPipeline = urp;
}
QualitySettings.SetQualityLevel(before, false);
AssetDatabase.SaveAssets();
AssetDatabase.Refresh();
```

**注意:** 本プロジェクトは既に 2026-04-22 時点で Assets/Settings/DefaultURP.asset がコミット済み。新しいクローンでは既に assign されているので、新規シーンのみで再度この設定が必要になることは通常ない。別マシン clone 時に URPProjectSettings.asset と ProjectSettings/GraphicsSettings.asset が正しく同期されているか確認すれば足りる。

**初遭遇:** 2026-04-21

---

## 3. Unity を Finder / Explorer のダブルクリックで起動しない

**症状:** MCP for Unity の Bridge が接続まではするが、Claude CLI を呼び出せずツールが動かない。

**原因:** GUI から起動した Unity プロセスは親シェルの `PATH` を継承しない。`claude` バイナリがパス上に見つからない。

**回避策:** ターミナル/PowerShell 経由で起動。
- **macOS:** `open -a "Unity Hub"`
- **Windows:** スタートメニューから、または PowerShell で `start "" "C:\Program Files\Unity Hub\Unity Hub.exe"`

**初遭遇:** 2026-04-21

---

## 4. MCP 登録の Claude CLI Path に `.exe` が付いてても macOS で正常

**症状:** MCP for Unity の Client Configuration で Claude CLI Path が `/Users/<name>/.nvm/.../lib/node_modules/@anthropic-ai/claude-code/bin/claude.exe` と表示される（macOS 環境で `.exe` 付き）。

**原因:** Claude Code npm パッケージの実ファイル名が `claude.exe`（macOS でも同じ）。`bin/claude` はこれへの symlink。MCP が symlink 解決して実ファイル直指定しているだけ。

**判定:** バグではない、触らなくてよい。見た目を揃えたければ Browse で `.exe` なしの symlink パス（`.../bin/claude`）を指定も可。機能差なし。

**確認方法:**
```bash
# macOS
ls -la $(which claude)
# → lrwxr-xr-x ... /Users/.../bin/claude -> ../lib/.../bin/claude.exe
```

**初遭遇:** 2026-04-21

---

## 5. symlink 経由で Claude Code 起動すると memory が分離する

**症状:** 前セッションまで貯めた memory が新セッションで見えない。`~/.claude/projects/-Users-taisei-Documents-Dev-ChaosDraftParty/memory/` が空。

**原因:** Claude Code は cwd のテキストパスをハッシュ化（スラッシュを `-` 置換）して `~/.claude/projects/` 配下のディレクトリを決定する。symlink 越しのパスと実体パスで別ディレクトリになる。

**回避策:** **常に実体パスで起動**。
- **macOS:** `cd ~/Documents/Dev/ChaosDraftParty` から起動（旧 `~/Documents/課題/やりたいこと/卒業制作` は symlink、非推奨）
- **Windows:** `cd C:\Dev\ChaosDraftParty`（日本語・空白パス禁止）

**既に事故った場合の回復:**
```bash
# 旧パス memory を実体パスに symlink
rmdir ~/.claude/projects/-Users-taisei-Documents-Dev-ChaosDraftParty/memory
ln -s ~/.claude/projects/-Users-taisei-Documents---------------/memory \
      ~/.claude/projects/-Users-taisei-Documents-Dev-ChaosDraftParty/memory
```

**初遭遇:** 2026-04-21

---

## 6. `npx skills add` は `-y` フラグがないと対話プロンプトで止まる

**症状:** バックグラウンド実行で exit 0 なのに skill が実際に入ってない。ログ末尾に `◆ Select skills to install (space to toggle)` で止まっている。

**原因:** `skills` CLI の `add` サブコマンドは skill 選択 UI を表示する。TTY なしの環境だとユーザー入力待ちのまま終了する挙動。

**回避策:** `-y`（全選択）を明示。`-g`（グローバル）も基本つける。
```bash
# 両 OS
npx -y skills add https://github.com/<owner>/<repo> -g -y
```

**初遭遇:** 2026-04-21

---

## 7. Configure All Detected Clients ボタンで既存 MCP 登録が消える

**症状:** Unity MCP の Client Configuration で `Configure All Detected Clients` を押すと、それまで `claude mcp add --scope local ...` で登録していた UnityMCP 登録が消える。

**原因:** `Configure All` は検出クライアント全部を初期化する動作。既存の `claude mcp add` を上書き。

**回避策:** **Configure All は初回のみ**。以降は個別の `Configure` ボタン、または：
```bash
cd ~/Documents/Dev/ChaosDraftParty        # macOS 実体パス
# cd C:\Dev\ChaosDraftParty                # Windows
claude mcp add --scope local --transport http UnityMCP http://127.0.0.1:8080/mcp
```

**初遭遇:** 2026-04-21

---

## 8. 新規 MCP 登録は既存 Claude セッションに反映されない

**症状:** `claude mcp add` 成功、`claude mcp list` で ✓ Connected 確認済みなのに、走っている Claude Code セッションで `mcp__UnityMCP__*` tool が見えない。

**原因:** Claude Code はセッション開始時に MCP クライアントを初期化する。セッション中の追加は反映されない。

**回避策:** Claude Code 再起動。次セッションから利用可。

**初遭遇:** 2026-04-21

---

## 追加候補（まだ踏んでない、踏んだら書く）

- [ ] Prefab に ClientNetworkTransform つけた時の挙動
- [ ] NGO 2.x の ServerRpc パラメータ制約
- [ ] Relay JoinCode の形式とバリデーション
- [ ] Input System アクション Assign 時の GUID ズレ
- [ ] Mixamo FBX リターゲット時の hip 固定問題
- [ ] LFS 越しの .dll に同期遅延
- [ ] ParrelSync クローンでの Library/ 競合
