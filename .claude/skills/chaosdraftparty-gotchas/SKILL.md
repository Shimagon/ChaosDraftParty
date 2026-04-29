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
9. [MCP ツール並列実行の安全性マップ](#9-mcp-ツール並列実行の安全性マップ)
10. [execute_code の名前空間 auto-import は root のみ](#10-execute_code-の名前空間-auto-import-は-root-のみ)
11. [safety_checks が blocked パターン一覧](#11-safety_checks-が-blocked-パターン一覧)
12. [C# 12 機能の稼働範囲（Roslyn Dep 有効時）](#12-c-12-機能の稼働範囲roslyn-dep-有効時)
13. [create_script の scheduledRefresh=false 問題](#13-create_script-の-scheduledrefreshfalse-問題)
14. [Write tool は既存ファイルへの上書きが silent fail](#14-write-tool-は既存ファイルへの上書きが-silent-fail)
15. [editor/state の advice.ready_for_tools が真の準備指標](#15-editorstate-の-adviceready_for_tools-が真の準備指標)
16. [manage_scriptable_object create の type_name は完全修飾必須](#16-manage_scriptable_object-create-の-type_name-は完全修飾必須)
17. [gitignored フォルダの .meta を忘れない](#17-gitignored-フォルダの-meta-を忘れない)
18. [TODO を信じる前に git ls-files で実プロジェクト状態を確認](#18-todo-を信じる前に-git-ls-files-で実プロジェクト状態を確認)
19. [batch_execute は默認 25 件上限、parallel=true でも実は逐次](#19-batch_execute-は默認-25-件上限paralleltrue-でも実は逐次)
20. [manage_scene load は path パラメータ（scene_path じゃない）](#20-manage_scene-load-は-path-パラメータscene_path-じゃない)
21. [scene 未保存状態では load 拒否される](#21-scene-未保存状態では-load-拒否される)
22. [create_script バリデータが helper method 呼び出しを重複定義誤判定](#22-create_script-バリデータが-helper-method-呼び出しを重複定義誤判定)
23. [apply_text_edits は precondition_sha256 必須（楽観ロック）](#23-apply_text_edits-は-precondition_sha256-必須楽観ロック)
24. [Tests asmdef includePlatforms 空だと PlayMode 默認](#24-tests-asmdef-includeplatforms-空だと-playmode-默認)
25. [PlayMode テスト後 Unity MCP session が一時切断](#25-playmode-テスト後-unity-mcp-session-が一時切断)
26. [delete_script / apply_text_edits は uri、それ以外は path の派閥](#26-delete_script--apply_text_edits-は-uriそれ以外は-path-の派閥)
27. [SO 参照の set_property は {"guid": "..."} 形式が確実](#27-so-参照の-set_property-は-guid--形式が確実)
28. [Prefab インスタンス化は manage_gameobject + prefab_path、manage_prefabs ではない](#28-prefab-インスタンス化は-manage_gameobject--prefab_pathmanage_prefabs-ではない)
29. [日本語フォント DL は jsdelivr の 50MB 制限と LFS を避ける](#29-日本語フォント-dl-は-jsdelivr-の-50mb-制限と-lfs-を避ける)
30. [TMP Essentials は TMP_PackageResourceImporter.ImportResources で無対話インストール](#30-tmp-essentials-は-tmp_packageresourceimporterimportresources-で無対話インストール)
31. [TMP_FontAsset.CreateFontAsset は Font.dynamic 必須](#31-tmp_fontassetcreatefontasset-は-fontdynamic-必須)
32. [ScreenSpaceOverlay Canvas は manage_camera screenshot に写らない](#32-screenspaceoverlay-canvas-は-manage_camera-screenshot-に写らない)
33. [component_properties は SerializedProperty 名（m_Controller 等）必須](#33-component_properties-は-serializedproperty-名m_controller-等必須)
34. [manage_vfx は component 追加が前提、trail_create 等は存在しない](#34-manage_vfx-は-component-追加が前提trail_create-等は存在しない)
35. [manage_vfx は一部プロパティを silent ignore する](#35-manage_vfx-は一部プロパティを-silent-ignore-する)
36. [.meta 削除後 Unity は Library/ キャッシュから同 GUID 再生成（このマシン限定）](#36-meta-削除後-unity-は-library-キャッシュから同-guid-再生成このマシン限定)
37. [render_ui は Edit Mode で blank、Play Mode 必須](#37-render_ui-は-edit-mode-で-blankplay-mode-必須)
38. [`claude mcp add -e` は `--` より前に置かないと env var が MCP に届かない](#38-claude-mcp-add--e-は----より前に置かないと-env-var-が-mcp-に届かない)
39. [URP Volume は Camera.volumeTrigger 未割当だと完全無効（silent fail）](#39-urp-volume-は-cameravolumetrigger-未割当だと完全無効silent-fail)
40. [URP RendererData.postProcessData = null で Post-Process 全体無効（最重要）](#40-urp-rendererdatapostprocessdata--null-で-post-process-全体無効最重要)
41. [TMP FontAsset の 1-arg CreateFontAsset は m_AtlasTextures 未初期化バグ](#41-tmp-fontasset-の-1-arg-createfontasset-は-m_atlastextures-未初期化バグ)
42. [カスタム URP Skybox Shader のハードル — Procedural tuning が速い](#42-カスタム-urp-skybox-shader-のハードル--procedural-tuning-が速い)
43. [Unity bridge が長時間 down する罠（create_script 連発後）](#43-unity-bridge-が長時間-down-する罠create_script-連発後)
44. [Unity session down 時の手書き .meta 戦略](#44-unity-session-down-時の手書き-meta-戦略)
49. [Blender viewport を Material Preview mode にすると Unity URP shader が壊れる](#49-blender-viewport-を-material-preview-mode-にすると-unity-urp-shader-が壊れる)
50. [ユーザー承認があったときだけナレッジ化する（無条件保存禁止）](#50-ユーザー承認があったときだけナレッジ化する無条件保存禁止)
51. [SDF 判定球の center は mesh の bounds.center じゃなく「全体の球中心」](#51-sdf-判定球の-center-は-mesh-の-boundscenter-じゃなく全体の球中心)
52. [shader / SDF 系問題は「実測 → 並列 agent レビュー → 提案」の順序固定](#52-shader--sdf-系問題は実測--並列-agent-レビュー--提案の順序固定)

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

## 9. MCP ツール並列実行の安全性マップ

**実測日:** 2026-04-22（Sandbox シーンで検証）

| パターン | 結果 | 推奨 |
|---|---|---|
| **read-only 並列**（`find_gameobjects` / `read_console` / `manage_scene get_*` / resource 読み）| 🟢 5 発同時全成功、結果独立 | 積極並列 OK |
| **独立オブジェクトへの write 並列**（`manage_gameobject create` ×3）| 🟢 全成功、instanceID 連番（内部シリアライズ） | 並列 OK だが `batch_execute` の方が token 効率良い |
| **同一オブジェクト・違うプロパティ write 並列**（position / rotation / scale）| 🟢 merge 動作、全変更保持 | 安全 |
| **同一オブジェクト・同一プロパティ write 並列** | 🟡 last-write-wins（送信順の最後が勝つ） | 意図した順序性あるなら逐次にせよ |
| `execute_code` 複数並列 | ⚠️ 未検証（`Thread.Sleep` 使うと Unity 主 thread 凍結しそう）。理屈上はシリアライズされるはず、ただし Roslyn compile の同時実行で干渉の可能性 | 逐次推奨 |
| **別 Claude Code セッション**から同 Unity に同時アクセス | 🔴 `set_active_instance` 未設定だと route 破綻、複数 token 衝突 | `unity_instance` 引数 or `set_active_instance` |
| Bash / WebFetch / 研究 Agent / skill install 並列 | 🟢 独立プロセス | 積極並列 OK |

**内部機構:** MCP for Unity は複数リクエストを内部キューで main thread 順次実行。Claude からは「並列に見える」が Unity 側は serial。そのため `batch_execute` を使うのと**レイテンシはさほど変わらない**（ただし token 数は batch の方が安い）。

**結論:**
- 複数 write → **`batch_execute`** を第一選択
- 読み混じり → 並列 tool call OK
- 純読み → 積極並列で OK
- 同オブジェクト同プロパティ並列は避ける（バグの温床）

---

## 10. execute_code の名前空間 auto-import は root のみ

**実測日:** 2026-04-22

| ネームスペース | 短縮形（FQN 不要） |
|---|---|
| `UnityEngine` 直下 | ✅ `GameObject` / `Vector3` / `Shader` / `Material` / `Camera` / `PrimitiveType` 等 |
| `UnityEditor` 直下 | ✅ `AssetDatabase` / `EditorUtility` / `Selection` / `PrefabUtility` 等 |
| `UnityEngine.Rendering` | ❌ 必要（例: `UnityEngine.Rendering.GraphicsSettings`） |
| `UnityEngine.Rendering.Universal` | ❌ 必要 |
| `UnityEditor.SceneManagement` | ❌ 必要（`EditorSceneManager` をそのまま書くと compile error） |
| `UnityEditor.Animations` | ❌ 必要（`AnimatorController` 等） |
| `System.*` 全般（Collections.Generic, Text, IO, Linq, Threading...）| ❌ 必要 |

**使用頻度の高い FQN 早見表:**
```csharp
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
UnityEngine.Rendering.GraphicsSettings.defaultRenderPipeline = urp;
UnityEngine.Rendering.Universal.UniversalRendererData rd;
UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset.Create(rd);
System.Collections.Generic.List<int> list = new() { 1, 2, 3 };
System.Text.StringBuilder sb = new();
System.Linq.Enumerable.Range(1, 10).Sum();
```

---

## 11. safety_checks が blocked パターン一覧

**実測日:** 2026-04-22（実際に弾かれたパターン）

| パターン | 状態 |
|---|---|
| `while (true)` | 🔴 blocked |
| `System.IO.File.Delete` | 🔴 blocked |
| `Process.Start` / `System.Diagnostics.Process.Start` | 🔴 blocked |

エラー形式：
```
Blocked pattern detected: Code contains blocked pattern: '<pattern>'.
Disable safety checks with safety_checks=false if this is intentional.
```

**解除方法:** `safety_checks=false` パラメータを `execute_code` 呼出に付ける（慎重に）。

**未検証だが blocked と予想:**
- `File.WriteAllText` / `File.Delete` 系
- `Registry` 操作
- `Environment.Exit`
- 長時間 `Thread.Sleep`

---

## 12. C# 12 機能の稼働範囲（Roslyn Dep 有効時）

**前提:** MCP for Unity の Deps タブで Roslyn (C# 12+ Compiler) インストール済み。
**実測日:** 2026-04-22

✅ 動く：
- **target-typed new**: `List<int> l = new() { 1, 2, 3 };`
- **switch expression**: `var r = x switch { 1 => "a", _ => "b" };`
- **pattern matching**: `o is int i && i > 0`
- **`using` declaration（ブロックなし）**: `using var ms = new MemoryStream();`
- **tuple**: `(int a, int b) t = (10, 20);`
- **null-conditional / null-coalescing**: `g?.name ?? "null"`
- **collection initializer**: `new Dictionary<string, int> { ["a"] = 1 }`
- **LINQ**: `System.Linq.Enumerable.Range(1, 5).Sum()`

❌ 動かない（メソッドボディ構文制限）：
- ローカル `class` / `record` / `struct` 宣言
- トップレベル `using` ディレクティブ（罠 #1 参照）
- 名前空間宣言

---

## 13. create_script の scheduledRefresh=false 問題

**症状:** `mcp__UnityMCP__create_script` が成功を返し `scheduledRefresh: false` となる。スクリプトファイルは作成されるが、Unity が自動的に refresh / compile しない。以降 `manage_scriptable_object create` で型を使おうとすると未定義エラー。

**原因:** Unity Editor がフォーカス外だと `create_script` の自動 refresh トリガーが作動しない。`assets.external_changes_dirty: true` にはなるが、`is_updating: false` のまま。

**診断:** `editor/state` の `external_changes_dirty: true` && `is_updating: false` && `staleness.is_stale: true` が同時に立つ。

**回避策:** 明示的に `refresh_unity` を呼ぶ：
```python
refresh_unity(mode="force", compile="request", wait_for_ready=True)
```

**予防策:** `create_script` / `script_apply_edits` / ファイル追加後は **必ず** `refresh_unity(wait_for_ready=True)` を挟んで、そのあと `read_console(types=["error"])` で compile 確認、それから新しい型を使う tool を呼ぶ。

**初遭遇:** 2026-04-22

---

## 14. Write tool は既存ファイルへの上書きが silent fail

**症状:** `Write` tool で既存 asmdef/設定ファイル 6 枚を上書きしようとしたら全 6 件 `File has not been read yet. Read it first before writing to it.` エラー。しかしその後ファイル内容を確認すると**元のまま**。

**原因:** Claude Code の Write tool は既存ファイル上書き時に **事前 Read を必須**とするガード。ガードに引っかかると書込処理はスキップされる（silent skip、エラーは出すがファイルは変更されない）。これは仕様通り、データ破壊防止の安全装置。

**回避策:**
- 既存ファイル変更は **Edit tool** か **`Read` 後に `Write`**
- 完全上書きを意図するなら Read tool で事前に読む → Write で上書き
- 6 個同時上書き等の一括操作は、Read 6 個 → Write 6 個、または `Bash` でスクリプト書き

**得られた教訓:** 上書きに見える操作で元ファイルが保持されるので、データ安全性は高い。ただし意図した上書きが通らないので、結果を必ず確認する。

**初遭遇:** 2026-04-22

---

## 15. editor/state の advice.ready_for_tools が真の準備指標

**確認日:** 2026-04-22

`mcpforunity://editor/state` リソースの最重要フィールド:

```json
"advice": {
  "ready_for_tools": true,        // これが true になるまで次操作を待つ
  "blocking_reasons": [],          // false 時の理由 (e.g., ["stale_status", "compiling"])
  "recommended_retry_after_ms": 0, // false 時は >0、推奨待機時間
  "recommended_next_action": "none"
},
"staleness": {
  "age_ms": 200,                   // state 情報の古さ
  "is_stale": false                // true なら情報自体が古い = 再取得しろ
}
```

**ワークフロー:**
```
1. refresh_unity(wait_for_ready=True) を呼ぶ
2. それでも ready_for_tools=false なら recommended_retry_after_ms 待って再取得
3. blocking_reasons を見て対処（compiling なら待つ、stale_status なら再 refresh）
4. ready_for_tools=true && is_stale=false を確認してから次ツール発動
```

これを守らないと「型が見つからない」「ファイルが無い」等のフライング系エラーで詰まる。

**初遭遇:** 2026-04-22

---

## 16. manage_scriptable_object create の type_name は完全修飾必須

**症状:** `type_name: "GameConfig"` だと型解決失敗エラー。

**原因:** Unity の SerializedObject 型参照はアセンブリ + 完全修飾名で保存される。`asset` ファイルの `m_Script` フィールドには `fileID` + `guid` の形で保存される。

**回避策:** namespace 込みで書く。
```python
manage_scriptable_object(
    action="create",
    type_name="ChaosDraftParty.Core.Configs.GameConfig",  # 完全修飾
    folder_path="Assets/_Project/ScriptableObjects/Configs",
    asset_name="GameConfig"
)
```

**前提条件:** スクリプトがコンパイル済み + domain reload 完了（罠 #15 参照）。

**初遭遇:** 2026-04-22

---

## 17. gitignored フォルダの .meta を忘れない

**症状:** `.gitignore` で `Assets/Foo/` をフォルダごと ignore したが、Unity が自動生成する `Assets/Foo.meta`（フォルダ自体の meta ファイル）は ignore 対象外 → Unity が .meta だけ tracked・フォルダ中身は untracked の不整合状態、他マシン clone 時に .meta だけあってフォルダ無い状態で GUID 迷子発生

**回避策:** フォルダを ignore したら必ず **folder.meta も ignore**。
```gitignore
# Unity 自動生成 .meta もセットで ignore
[Aa]ssets/Screenshots/
[Aa]ssets/Screenshots.meta
```

**汎用ルール:** `Assets/X/` を ignore したら `Assets/X.meta` も ignore。`Packages/X/` を ignore したら `Packages/X.meta` も ignore。meta ペアは対称を保つ。

**初遭遇:** 2026-04-22

---

## 18. TODO を信じる前に git ls-files で実プロジェクト状態を確認

**症状:** `docs/TODO.md` に「asmdef 6枚作成」が `[ ]` 未着手状態で載っていたので新規作成を試みたら、**全 6 枚既に初回コミット `c5cba93` で作成・commit 済み**だった。Write tool ガードのおかげで上書き事故は免れたが、30 秒の無駄作業。

**原因:** TODO.md が実装状態の反映を遅延していた。CLAUDE.md や HANDOFF.md に構造説明はあったが、具体ファイルまでチェックせず TODO を額面通り受け取った。

**回避策:** 「作業開始前に必ず git ls-files で実ファイル状態を見る」ことを作業パターンに組み込む。
```bash
# 作業対象領域の現状確認
git ls-files "Assets/_Project/Scripts/*/" | head -30
find Assets/_Project -name "*.cs" -type f | wc -l
```

特に：
- asmdef / .cs / .asset ファイルの有無
- `Packages/manifest.json` のパッケージ列
- `ProjectSettings/*.asset` の差分有無

**汎用ルール:** **TODO はプラン、git は真実**。両方確認してから着手。

**初遭遇:** 2026-04-22

---

## 19. batch_execute は默認 25 件上限、parallel=true でも実は逐次

**症状:** 30 件の create コマンドを `batch_execute` に渡すと即エラー。
```
Error calling tool 'batch_execute': batch_execute supports up to 25 commands (configured in Unity); received 30
```

**原因:** デフォルト上限 25（Unity MCP Tools window で最大 100 まで設定変更可）。

**回避策:**
- **分割**: 25 ずつ複数 batch_execute に分ける
- **設定変更**: `Window → MCP for Unity → Advanced → Batch` で上限引き上げ
- **execute_code 代替**: C# 直接記述なら無制限（縮む島の 64 タイル生成もこの方式）

**並列性について:** `parallel=true` ヒント渡しても `parallelApplied: false` が返る場合あり（本プロジェクトで実測）。Unity の main thread 制約で実質逐次。レイテンシは逐次 tool call と大差なく、**token 数の節約が主目的**。

**判定:**
- ~25 件以下の均質操作 → `batch_execute`（token 安い）
- 25-64 件程度 → 同じく `batch_execute` 2 分割か、上限引き上げ
- 数百件や複雑ロジック（座標計算 + ループ + 条件分岐）→ `execute_code`（Roslyn） が最速・最安

**初遭遇:** 2026-04-22

---

## 20. manage_scene load は path パラメータ（scene_path じゃない）

**症状:** `manage_scene(action="load", scene_path="Assets/..")` だと：
```
Either 'name'/'path' or 'buildIndex' must be provided for 'load' action.
```

**原因:** tool schema 上 `scene_path` も受けるはずだが、`load` 実装は `path` / `name` / `buildIndex` のみ見ている。Schema と実装の食い違い。

**回避策:** `path` を使う：
```python
manage_scene(action="load", path="Assets/_Project/Scenes/Foo.unity")
```

**初遭遇:** 2026-04-22

---

## 21. scene 未保存状態では load 拒否される

**症状:** `manage_scene load` で `Current scene has unsaved changes. Please save or discard changes before loading a new scene.`

**原因:** 未保存の dirty scene がある状態では上書き load を Unity が拒否（データ損失防止）。

**回避策:** 先に保存してから load：
```python
manage_scene(action="save")
manage_scene(action="load", path="Assets/_Project/Scenes/Other.unity")
```

または dirty を捨てたいなら：
```python
# execute_code で強制捨て
UnityEditor.SceneManagement.EditorSceneManager.OpenScene(newPath, UnityEditor.SceneManagement.OpenSceneMode.Single);
```

**予防:** scene 切替ワークフローを定型化 — `save → load` で必ずペア。

**初遭遇:** 2026-04-22

---

## 22. create_script バリデータが helper method 呼び出しを重複定義誤判定

**症状:** `create_script` で テストクラスを作ろうとすると、private helper method を複数テストから呼んでいるだけなのに：
```
Duplicate method signature detected: 'CreateDefaultConfig' with 0 parameter(s).
This may indicate a corrupted edit.
```

**原因:** script validator のバグ（false positive）。helper method 定義 1 個 + その呼び出し N 個を「N+1 個の定義」と誤認。

**回避策:**
- **インライン化**：helper を捨てて各 [Test] 内に直接書く
- または helper 呼び出しに引数を与えて signature をバラす（`CreateDefaultConfig(int seed)` 等、オーバーロード不可のため別名にする）

**実害:** `[Test]` 属性付き method が大量にあるテストクラスでヘルパー多用スタイルが書きにくい。テストは独立性重要なのでインライン化の方が実は望ましい、怪我の功名。

**初遭遇:** 2026-04-22

---

## 23. apply_text_edits は precondition_sha256 必須（楽観ロック）

**症状:** `apply_text_edits` 初回呼出で：
```json
{
  "success": false,
  "code": "precondition_required",
  "data": {
    "current_sha256": "ffda5ca1..."
  }
}
```

**原因:** stale_file 防止の楽観的ロック。ファイル SHA を事前に提示しないと拒否。他エディタ同時編集や古い SHA への上書きを防ぐ。

**回避策:** エラー response の `current_sha256` を拾って再送：
```python
# 1 回目（エラー、sha 取得目的）
result = apply_text_edits(uri=..., edits=[...])
sha = result["data"]["current_sha256"]

# 2 回目（実行）
apply_text_edits(
    uri=...,
    precondition_sha256=sha,
    edits=[...]
)
```

または `get_sha` ツールで事前取得：
```python
sha = get_sha(uri="Assets/...")["sha256"]
apply_text_edits(uri=..., precondition_sha256=sha, edits=[...])
```

**script_apply_edits はこのロックなし**（構造化編集で方が安全判定）。頻繁な編集なら script_apply_edits 選ぶ。

**初遭遇:** 2026-04-22

---

## 24. Tests asmdef includePlatforms 空だと PlayMode 默認

**症状:** NUnit `[Test]` 属性付きテストが `mode: PlayMode` としてリストされ、`run_tests(mode="EditMode")` で 0 件実行。Addressables 付属テストだけ動く。

**原因:** Unity Test Framework の規約 — Tests asmdef の `includePlatforms` が空 or `Editor` 含まない場合、テストは PlayMode 扱い（実機ビルドでも走るテスト）。

**回避策:** EditMode 専用にしたい（通常は速度優先でこう）：
```json
{
  "name": "ChaosDraftParty.Tests",
  "includePlatforms": ["Editor"],
  ...
}
```

**性能差:** PlayMode は play 遷移オーバーヘッド約 9 秒、EditMode は 0.6 秒。純ロジックテスト（GameConfig 計算等）は EditMode が正解。

**PlayMode が必要な時:** MonoBehaviour の `Start()` / `FixedUpdate()` を回したい、NGO の actual ネットワーク挙動、コルーチン、Physics シミュレーション — これらは PlayMode 必須。

**初遭遇:** 2026-04-22

---

## 25. PlayMode テスト後 Unity MCP session が一時切断

**症状:** PlayMode テスト実行後、`refresh_unity` が 60 秒タイムアウト、`mcpforunity://editor/state` が `"Unity session not available; please retry"` を返す状態が 15-30 秒続く。

**原因:** Play Mode の enter/exit は Unity の domain reload を大きく揺するため、MCP Bridge の内部状態が更新ラグを起こす。ping (`manage_editor telemetry_ping`) は通るが resource 読みは失敗する中間状態あり。

**回避策:**
- PlayMode テスト後は **15 秒ほど待つ**か `telemetry_ping` で接続確認
- `recommended_retry_after_ms` を見て指数バックオフ
- 可能なら EditMode テストに切り替える（セッション揺れなし）

**注意:** この現象中、ツール呼出は通る場合があるが state は古い可能性。重要な変更前は必ず `editor/state` で `ready_for_tools=true` 確認。

**初遭遇:** 2026-04-22

---

## 26. delete_script / apply_text_edits は uri、それ以外は path の派閥

Unity MCP ツールのパラメータ命名が微妙に不統一。

| ツール | ファイル指定 |
|---|---|
| `create_script` | `path` |
| `manage_script` | `path` + `name` |
| `delete_script` | **`uri`** |
| `apply_text_edits` | **`uri`** |
| `script_apply_edits` | `path` + `name` |
| `find_in_file` | **`uri`** |
| `manage_asset` | `path` |
| `manage_scene` | `path` (load/save 時、`scene_path` ではない、罠 #20) |

**回避策:** 呼び出し前に tool schema を確認。URI 系は `mcpforunity://path/Assets/...` か `Assets/...` 両方受け付けることが多い。

**初遭遇:** 2026-04-22

---

## 27. SO 参照の set_property は {"guid": "..."} 形式が確実

**症状:** `manage_components set_property` で `[SerializeField] GameConfig config` に SO アセット参照を代入したい。instance ID、asset path、`{"guid":"..."}`、`{"path":"..."}` の 4 形式が受け付けられるが、どれが確実？

**実測:**
- ✅ `{"guid": "d4705ac63c3494ab18f4d7ecc624dbdb"}` — 確実に動く（GUID は `.asset.meta` から取得 or `AssetDatabase.AssetPathToGUID`）
- ✅ `{"path": "Assets/_Project/ScriptableObjects/Configs/GameConfig.asset"}` — 動く
- ⚠️ asset path 直接文字列 — 状況依存
- ⚠️ instance ID — runtime 依存（アセットは持続 ID ない）

**推奨:** GUID 形式。マシン跨ぎで安定、リネーム耐性あり。
```python
manage_components(
    action="set_property",
    target="Player",
    component_type="ChaosDraftParty.Core.PlayerController",
    property="config",
    value={"guid": "d4705ac63c3494ab18f4d7ecc624dbdb"}
)
```

**GUID 取得:**
```csharp
// execute_code 内で
var guid = AssetDatabase.AssetPathToGUID("Assets/.../GameConfig.asset");
return guid;
```

または `.asset.meta` ファイルの `guid:` 行を読む。

**初遭遇:** 2026-04-22

---

## 28. Prefab インスタンス化は manage_gameobject + prefab_path、manage_prefabs ではない

**症状:** `manage_prefabs` というツールがあるのに、プレハブをシーンに**インスタンス化**する action が見当たらない。

**原因:** ツールの責務分担:
- `manage_prefabs` = **prefab アセット自体**の操作（create_from_gameobject, modify_contents, open_prefab_stage 等）
- `manage_gameobject` = **シーン内 GameObject** の操作 + プレハブインスタンス化

**正解:**
```python
# プレハブをシーンに出す
manage_gameobject(
    action="create",
    name="PlayerCharacter_Instance",
    prefab_path="Assets/_Project/Prefabs/Player/PlayerCharacter.prefab",
    position=[0, 1, 0]
)
```

`primitive_type` と `prefab_path` はどちらか一方のみ指定（両方だと `prefab_path` が優先）。

**完全フロー（Player 系の daily workflow）:**
```python
# 1. シーン内でオブジェクト組み立て
manage_gameobject(action="create", name="Tmp", primitive_type="Capsule")
manage_components(action="add", target="Tmp", component_type="MyController")
manage_components(action="set_property", target="Tmp", component_type="MyController",
                  property="config", value={"guid": "..."})

# 2. プレハブ化
manage_prefabs(action="create_from_gameobject", target="Tmp",
               prefab_path="Assets/.../Tmp.prefab")

# 3. 再インスタンス化
manage_gameobject(action="delete", target="Tmp")
manage_gameobject(action="create", prefab_path="Assets/.../Tmp.prefab")
```

**初遭遇:** 2026-04-22

---

## 29. 日本語フォント DL は jsdelivr の 50MB 制限と LFS を避ける

**症状:**
- `curl https://github.com/googlefonts/noto-cjk/raw/main/.../NotoSansJP-Regular.otf` → HTML redirect page が降る（LFS pointer ページ）
- `curl https://raw.githubusercontent.com/.../NotoSansJP-Regular.otf` → exit 56 (接続切断、LFS のため)
- `curl https://cdn.jsdelivr.net/gh/googlefonts/noto-cjk@main/...NotoSansJP-Regular.otf` → 156 bytes の「Package size exceeded 50 MB」エラーテキスト
- `curl https://fonts.google.com/download?family=Noto+Sans+JP` → HTML redirect
- `npm pack @fontsource/noto-sans-jp` → 81MB 展開、`.woff`/`.woff2` のみで `.ttf` なし

**原因:** noto-cjk 系は LFS 管理、CDN/raw ともに通常ダウンロードできない。Google Fonts の DL API は CORS / referer 制限。

**回避策:**
- **`google/fonts` リポは LFS 不使用** → `https://raw.githubusercontent.com/google/fonts/main/apache/kosugimaru/KosugiMaru-Regular.ttf` で直接取得可能
- 推奨日本語 OSS フォント（小さい + 商用可）:
  - **Kosugi Maru** (Apache 2.0、3.4MB) — `google/fonts/main/apache/kosugimaru/KosugiMaru-Regular.ttf`
  - **Kosugi** (Apache 2.0)
  - **Sawarabi Gothic** (OFL)
  - **M PLUS 1p** (OFL、より大きい)
- Noto Sans JP が必要な場合は `git clone https://github.com/notofonts/noto-cjk` + LFS pull（容量大）

**初遭遇:** 2026-04-22

---

## 30. TMP Essentials は TMP_PackageResourceImporter.ImportResources で無対話インストール

**症状:** メニュー `Window/TextMeshPro/Import TMP Essential Resources` を `execute_menu_item` で発火しても、対話ダイアログを要求する UI ベースの挙動で、バックグラウンドから確認不能。`Assets/TextMesh Pro/` が生成されない。

**回避策:** `execute_code` から直接 API 呼出（`unity_reflect` で確認した通り）：
```csharp
TMPro.TMP_PackageResourceImporter.ImportResources(
    importEssentials: true,
    importExamples: false,
    interactive: false   // ← これが鍵
);
AssetDatabase.Refresh();
```

これで `Assets/TextMesh Pro/` に Fonts/Shaders/Resources が展開される。

**初遭遇:** 2026-04-22

---

## 31. TMP_FontAsset.CreateFontAsset は Font.dynamic 必須

**症状:** `TMPro.TMP_FontAsset.CreateFontAsset(font)` が `NullReferenceException` で失敗。

**原因:** 渡された `Font` アセットが `dynamic=false` だと TMP が内部で atlas rasterizer を初期化できず NRE。

**回避策:** TrueTypeFontImporter の設定を Dynamic に：
```csharp
var imp = (UnityEditor.TrueTypeFontImporter)AssetImporter.GetAtPath(fontPath);
imp.fontTextureCase = UnityEditor.FontTextureCase.Dynamic;
imp.includeFontData = true;
imp.SaveAndReimport();

var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
var fontAsset = TMPro.TMP_FontAsset.CreateFontAsset(font);
```

**余談:** `font.dynamic` プロパティが `false` に見えるケースあり（Unity 6 の表示バグ？）、実挙動では Dynamic atlas に設定されていれば CreateFontAsset 成功。

**初遭遇:** 2026-04-22

---

## 32. ScreenSpaceOverlay Canvas は manage_camera screenshot に写らない

**症状:** `Canvas.renderMode = ScreenSpaceOverlay` で UI 構築 → `manage_camera screenshot` したら UI が消えて 3D だけ写る。

**原因:** `ScreenSpaceOverlay` は Camera のレンダリングパス外、Unity のゲームビュー最終合成時に重ねる。`manage_camera screenshot` は Camera.Render を呼ぶだけで overlay 段階を含まない。

**回避策:** Canvas モードを `ScreenSpaceCamera` に変更、Main Camera を割当：
```csharp
canvas.renderMode = RenderMode.ScreenSpaceCamera;
canvas.worldCamera = Camera.main;
canvas.planeDistance = 1f;  // カメラから 1m 前
```

これで UI が Camera のレンダーパスに組込まれ、screenshot に写る。

**注意:** 実機 Build でも UI は ScreenSpaceCamera で良い（多くの 3D ゲームの標準）。Overlay は独立解像度の UI（ローディング画面等）限定。

**初遭遇:** 2026-04-22

---

## 33. component_properties は SerializedProperty 名（m_Controller 等）必須

**症状:** `manage_prefabs modify_contents` の `component_properties={"Animator": {"runtimeAnimatorController": {...}}}` で `SerializedProperty 'runtimeAnimatorController' not found`。

**原因:** Unity の AssetDatabase は **SerializedProperty 内部名**（`m_` プレフィックス + PascalCase）で値書込。C# の public プロパティ名（`runtimeAnimatorController`）は getter/setter であって SerializedField ではない。

**回避策:** SerializedProperty 名を使う：
- `runtimeAnimatorController` → **`m_Controller`**
- `sharedMaterial` → **`m_Materials`** (配列)
- `enabled` → **`m_Enabled`**
- `mass` (Rigidbody) → **`m_Mass`**
- `color` (Light) → **`m_Color`**
- `intensity` (Light) → **`m_Intensity`**

**探す方法:**
1. Unity Inspector を右クリック → Debug mode にすると SerializedProperty 名が見える
2. asset ファイル（.prefab や .asset）を text エディタで開いて確認
3. `unity_reflect` で探しても public プロパティしか出ない、SerializedProperty は出ない → ファイル直読みが早い

**初遭遇:** 2026-04-22

---

## 34. manage_vfx は component 追加が前提、trail_create 等は存在しない

**症状:** `manage_vfx(action="trail_create", ...)` で `Unknown action 'trail_create'`。

**原因:** `manage_vfx` は **既存 component の設定変更**専用。component 自体の追加は `manage_components` の仕事。

**正しいフロー:**
```python
# 1. GameObject 作成
manage_gameobject(action="create", name="TailTrail")

# 2. component 追加（manage_components）
manage_components(action="add", target="TailTrail", component_type="TrailRenderer")

# 3. manage_vfx で設定
manage_vfx(action="trail_set_properties", target="TailTrail", 
           properties={"time": 2.0, "startWidth": 0.4, ...})
manage_vfx(action="trail_set_color", target="TailTrail",
           properties={"startColor": [...], "endColor": [...]})
```

利用可能な action prefix:
- `particle_*` (ParticleSystem)
- `vfx_*` (VisualEffect / VFX Graph)
- `line_*` (LineRenderer)
- `trail_*` (TrailRenderer)

**初遭遇:** 2026-04-22

---

## 35. manage_vfx は一部プロパティを silent ignore する

**症状:** `trail_set_properties` に color を含めても無反応。response の `"Updated: ..."` リストで silent 省略されてる。

**実測:**
- `trail_set_properties` に `startColor`/`endColor` 渡す → **ignored**（`trail_set_color` で別途）
- `particle_set_emission` に `bursts` 渡す → **ignored**（別の action で設定？）
- `particle_set_main` に `loop` 渡す → **ignored**

**回避策:**
1. response の `"Updated: <list>"` を必ず確認。指定したキーが全部含まれているか verify
2. 含まれてないキーは別の専用 action が必要（`trail_set_color`, etc）
3. 不明な場合は `execute_code` で直接 C# 書いて設定（全プロパティ操作可）

**初遭遇:** 2026-04-22

---

## 36. .meta 削除後 Unity は Library/ キャッシュから同 GUID 再生成（このマシン限定）

**実験（2026-04-22）:**
1. `meta_test.png` 生成 → `.meta` に GUID `3a0956c7...`
2. `rm meta_test.png.meta`（`.meta` のみ削除、本体残す）
3. `refresh_unity` → `.meta` 再生成
4. **GUID が元と同じ `3a0956c7...`**

**原因:** Unity の `Library/metadata/` には各アセットの GUID インデックスが path 単位でキャッシュされてる。`.meta` が消えても同じ path でアセットが残ってれば Library/ から GUID を復元。

**ただし注意:** これは **このマシン限定の恩恵**。別マシン clone 時に `.meta` が無い状態（git 忘れ等）なら、Library/ もゼロなので **新 GUID が生成されて参照全滅**。

**追加実験（asset 本体も削除）:**
- Material が texture 参照している状態で `texture.png` + `texture.png.meta` 両方削除 → `refresh_unity`
- 結果: Material は shader 参照保持、texture reference は **NULL (Missing)** になる
- 復旧: git 履歴から元ファイル復元（`git checkout HEAD~ -- path`）が最短。再制作なら Material 側で再 assign 必要

**教訓:**
- **`.meta` を絶対コミットする**（HANDOFF.md の最重要ルールの再確認）
- Unity 内での `.meta` 再生成が同 GUID でも油断しない、他マシンでは失敗する
- 参照切れ `.mat` を見つけたら即 git log で直近削除の履歴追跡

**初遭遇:** 2026-04-22

---

## 37. render_ui は Edit Mode で blank、Play Mode 必須

**症状:** `manage_ui render_ui` で UXML をプレビューしようとしたら、保存された PNG が真っ白（全黒、`hasContent: false`）。

**原因:** UI Toolkit は UIDocument 経由で Panel Settings の RenderTexture に描画されるが、これは **Play Mode でのみ actual render がトリガー**される。Edit Mode では PanelSettings に RenderTexture を assign するだけで描画パスは走らない。

**回避策:**
- **Play Mode 中に render_ui を呼ぶ** — 1 回目で WaitForEndOfFrame 待ちキュー、2 回目で PNG 取得
- または Edit Mode で visual 確認したいなら **UI Builder window を開く** (`UIToolkit/UIBuilder/OpenUIBuilder` 等のメニュー項目)
- または UXML + USS を直接 read して構造を把握（`manage_ui read` + `get_visual_tree`）

**Play Mode の副作用に注意:** 罠 #25（PlayMode テスト後 Unity MCP session 一時切断）と同じ現象が render_ui でも起こる可能性。短時間 Play Mode で済ませて、Play 抜けて MCP stabilize まで待つ。

**初遭遇:** 2026-04-22

---

## 38. `claude mcp add -e` は `--` より前に置かないと env var が MCP に届かない

**症状:** `claude mcp add meshy --scope user -- npx -y @meshy-ai/meshy-mcp-server -e MESHY_API_KEY=msy_...` で登録して `claude mcp list` 見ると `✗ Failed to connect`。

**原因:** `--` 以降は実行コマンドとその引数として扱われる。`-e KEY=VAL` が `--` の後にあると、claude CLI ではなく npx への引数として渡り、npx はそのまま MCP server の argv に流す。結果、環境変数は設定されず、server は API キーを読めず認証失敗。

**正解:** `-e` を `--` の前に置く。
```bash
# OK
claude mcp add meshy --scope user -e MESHY_API_KEY=msy_... -- npx -y @meshy-ai/meshy-mcp-server

# NG（silent 失敗、認証エラーになる）
claude mcp add meshy --scope user -- npx -y @meshy-ai/meshy-mcp-server -e MESHY_API_KEY=msy_...
```

**確認:** 登録後 `claude mcp list | grep meshy` で `✓ Connected` なら OK、`✗ Failed to connect` なら再登録。

**一般則:** `claude mcp add <name> [--scope <s>] [-e K=V ...] [--transport ...] -- <command> <args...>` の順番。claude 自身のフラグは `--` 前、実行コマンドは `--` 後。

**初遭遇:** 2026-04-22

---

## 39. URP Volume は Camera.volumeTrigger 未割当だと完全無効（silent fail）

**症状:** URP Volume Profile を作成し、Global Volume に割当、Camera.renderPostProcessing=true でも**ポストエフェクトが全く見えない**。Bloom/Color Adjustments/Vignette すべて反映なし。

**原因:** `UniversalAdditionalCameraData.m_VolumeTrigger` が `None (Transform)` のまま。Volume Stack の Update は `VolumeTrigger.position` を基に近接 Volume を集める設計、`null` だと Stack 構築失敗し全 override が無視される。エラー出ない **silent fail**。

**診断:**
```csharp
var urpCam = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
Debug.Log($"VolumeTrigger: {urpCam.volumeTrigger?.name ?? "NULL ← ここ！"}");
Debug.Log($"VolumeLayerMask: {urpCam.volumeLayerMask.value}");
```

もし `volumeTrigger.name` を読もうとすると `UnassignedReferenceException` が出る（Inspector で見ていなかったから気づかない）。

**回避策:**
```csharp
var urpCam = cam.GetComponent<UniversalAdditionalCameraData>();
urpCam.volumeTrigger = cam.transform;  // 自カメラが Trigger
urpCam.volumeLayerMask = -1;           // Everything
EditorUtility.SetDirty(urpCam);
```

**恒久対策:** 新規 Camera 作成時は必ず **VolumeTrigger=Self、VolumeLayerMask=Everything** を明示設定。GameManager.cs 等で自動補正ロジックを仕込む。

**初遭遇:** 2026-04-22

---

## 40. URP RendererData.postProcessData = null で Post-Process 全体無効（最重要）

**症状:** URP Volume Profile + Global Volume + Camera PostFx ON + VolumeTrigger 全部正設定でも、ポストエフェクト完全に無効。

**原因:** **URP の Renderer Data asset の `postProcessData` フィールドがデフォルト null**。コードで URP Asset を新規生成した場合（`UniversalRenderPipelineAsset.Create(rendererData)`）、Renderer Data の postProcessData は未割当。これで URP の Render Pipeline は「post data 無し」と判断して **Post Pass 全体をスキップ**。

**診断:**
```csharp
var rpa = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
var rdField = rpa.GetType().GetField("m_RendererDataList",
    BindingFlags.NonPublic | BindingFlags.Instance);
var rds = rdField.GetValue(rpa) as ScriptableRendererData[];
var rdSo = new SerializedObject(rds[0]);
var prop = rdSo.FindProperty("postProcessData");
Debug.Log($"postProcessData: {prop.objectReferenceValue?.name ?? "NULL ← ここ！"}");
```

**回避策:** URP パッケージ標準の PostProcessData asset を assign：
```csharp
var guids = AssetDatabase.FindAssets("t:PostProcessData");
string path = null;
foreach (var g in guids)
{
    var p = AssetDatabase.GUIDToAssetPath(g);
    if (p.Contains("render-pipelines.universal")) { path = p; break; }
}
var data = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.PostProcessData>(path);

var rdSo = new SerializedObject(rd);
rdSo.FindProperty("postProcessData").objectReferenceValue = data;
rdSo.ApplyModifiedProperties();
AssetDatabase.SaveAssets();
```

**パス確定**: `Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset`

**なぜ重要**: ChaosDraftParty は罠 #2 で URP Asset を自前生成した → postProcessData が null のまま。Unity テンプレから始めた新規プロジェクトでは自動割当済のはず（今回は Unity 6 空プロジェクト + 後付 URP のパターン）。

**恒久対策**: URP Asset 生成コード内で必ず postProcessData 割当を含める。`.claude/skills/chaosdraftparty-gotchas/` の罠 #2（URP Asset 自動生成）にも追記必要。

**初遭遇:** 2026-04-22

---

## 41. TMP FontAsset の 1-arg CreateFontAsset は m_AtlasTextures 未初期化バグ

**症状:** `TMP_FontAsset.CreateFontAsset(font)` で作成した FontAsset を Text に assign すると：
```
UnassignedReferenceException: The variable m_AtlasTextures of TMP_FontAsset has not been assigned.
```

**原因:** 1-arg overload は atlas サイズ等の defaults で生成するが、**atlas texture 配列が lazy init** で、最初の文字追加まで null のまま。Text の set_font 時に atlas を参照しようとして null ref。

**回避策:** 7-arg overload で明示初期化 + 事前 TryAddCharacters：
```csharp
var fontAsset = TMP_FontAsset.CreateFontAsset(
    font, 90, 9,
    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
    1024, 1024,
    TMPro.AtlasPopulationMode.Dynamic,
    true);  // enableMultiAtlasSupport

// 事前にアルファベット追加 (atlas 初期化させる)
uint[] codepoints = new uint[62];
for (int i = 0; i < 26; i++) codepoints[i] = (uint)('A' + i);
for (int i = 0; i < 26; i++) codepoints[26 + i] = (uint)('a' + i);
for (int i = 0; i < 10; i++) codepoints[52 + i] = (uint)('0' + i);
fontAsset.TryAddCharacters(codepoints);
```

**関連:** 再生成（delete + recreate）時に fallback FontAsset の material reference が dangling になる別の問題あり（`m_Material doesn't exist anymore`）。fallback 使うなら fallback 側も同時再生成が安全。

### ⚠️ 再発: 7-arg だけでは **Editor 再起動で m_AtlasTextures が null 化**（2026-04-22 夜 遭遇）

**追加症状:** 上記の 7-arg で作成した FontAsset を `AssetDatabase.CreateAsset` で .asset に保存、scene 側で使用 → その場は OK → **Unity を閉じて再起動するとまた UnassignedReferenceException が出る**。.asset YAML を開くと `m_Material: {fileID: 0}` / `m_AtlasTextures: [{fileID: 0}]` / `m_GlyphTable: []` / `m_CharacterTable: []` と **全部空**。

**原因:** `CreateFontAsset` は atlas `Texture2D` と fill `Material` を内部で生成するが、**これらは RAM 上のオブジェクト**。`AssetDatabase.CreateAsset` は FontAsset 本体のみディスクに書き、atlas texture と material は匿名のまま放置される → Editor 閉じると GC → 再起動時 .asset の ObjectReference が dangling pointer（`fileID: 0`）。

**恒久回避策:** atlas texture と material を **必ず FontAsset の sub-asset として追加** する：
```csharp
var fontAsset = TMP_FontAsset.CreateFontAsset(
    sourceFont, 90, 9,
    GlyphRenderMode.SDFAA, 1024, 1024,
    AtlasPopulationMode.Dynamic, true);

AssetDatabase.CreateAsset(fontAsset, assetPath);

// --- これが無いと再起動で null になる ---
if (fontAsset.material != null) {
    fontAsset.material.name = fontAsset.name + " Material";
    fontAsset.material.hideFlags = HideFlags.HideInHierarchy;
    AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
}
foreach (var tex in fontAsset.atlasTextures) {
    if (tex == null) continue;
    tex.name = fontAsset.name + " Atlas";
    tex.hideFlags = HideFlags.HideInHierarchy;
    AssetDatabase.AddObjectToAsset(tex, fontAsset);
}

// 事前 populate（atlas 0 も追加される）
fontAsset.TryAddCharacters(preloadString);

EditorUtility.SetDirty(fontAsset);
AssetDatabase.SaveAssets();
AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
```

**検証:** 再生成後、Project window で .asset を展開 → `FontAsset_Atlas`（Texture2D）と `FontAsset Material`（Material）が子として visible なら OK。無ければ dangling。

**恒久対策スクリプト:** `Assets/_Project/Scripts/Editor/TMPFontAssetRepair.cs` に Editor 用 menu item `Tools/ChaosDraftParty/Repair TMP Font Asset (Kosugi Maru)` を実装。ボタン 1 つで再生成可。

**MCP 経由 execute_code で行うと再発必至:** Editor script として保存して UI 経由で実行する方が安全（atlas texture が確実に sub-asset 化される、ImportAsset が走る）。

**初遭遇:** 2026-04-22（Outfit_Dynamic 作成時） / **再発:** 2026-04-22 夜（Editor 再起動で復活）

---

## 42. カスタム URP Skybox Shader のハードル — Procedural tuning が速い

**症状:** HLSL で `Shader "ChaosDraftParty/GradientSkybox"` を自作、RenderSettings.skybox に設定 → **全画面ピンク**（Missing Shader 扱い）。Console にエラー出ず。

**原因:**
- URP Skybox は `Queue=Background RenderType=Background PreviewType=Skybox` 必要
- 3D スカイボックスの位置計算（direction vector）が URP standard include と不整合
- `Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl` include だけでは不十分、`UnitySkybox.hlsl` 等も要る可能性

**回避策（短期）:** `Skybox/Procedural` を Material 化して tuning：
```csharp
var mat = new Material(Shader.Find("Skybox/Procedural"));
mat.SetFloat("_AtmosphereThickness", 1.2f);
mat.SetColor("_SkyTint", new Color(0.75f, 0.85f, 1.0f));
mat.SetColor("_GroundColor", new Color(0.4f, 0.3f, 0.2f));
mat.SetFloat("_SunSize", 0.06f);
mat.SetFloat("_Exposure", 1.0f);
RenderSettings.skybox = mat;
// Directional Light で太陽位置を低く (夕焼け角度)
dl.transform.rotation = Quaternion.Euler(15f, 120f, 0f);
```

効果: 青空 Procedural Skybox の Unity 臭が消え、暖色夕焼け空に。**工数 30 秒**。

**長期策:** Shader Graph で Gradient Skybox 作る or Unity Asset Store の無料 `Skybox Mobile Stylized` 等を採用。自作は GraphicsPass 挿入点とライブラリ include が面倒、**DIY は費用対効果低い**。

**初遭遇:** 2026-04-22

---

## 43. Unity bridge が長時間 down する罠（create_script 連発後）

**症状:** `create_script` や `manage_scriptable_object` を複数回連続で叩いた後、Unity session が `"Unity session not available; please retry"` 状態に陥り 15-20 分以上回復しない。`telemetry_ping` は success 返すが `editor/state` resource は常に no_unity_session。Unity プロセス自体は生きている（pgrep で PID 確認可）。

**原因推定:**
- MCP bridge は健在（`curl http://127.0.0.1:8080/health` で healthy）
- だが Unity Editor 内の bridge クライアントが domain reload 中 or 連続 API 処理で deadlock 気味
- telemetry_ping はキューに入るだけ（queued）で実際に処理されない
- AssemblyReload 中は全ての Editor API が固まる、これが長引くと session available 判定に失敗

**回避策:**
- Claude 側でやれることなし、**Unity Editor 側で手動復旧必要**
- 具体的には: Unity Editor のコンソールで入力イベント発生（ウィンドウクリック等）、または Unity 再起動
- 予防: `create_script` を連発しない、1 ファイル作ったら `refresh_unity(wait_for_ready=true)` で **必ずコンパイル完了を待つ**、それから次のファイル

**autonomous session での運用:**
- 5 分待って復帰しなければ、**Unity 不要の作業にシフト**（ファイル直書き、.meta 手動生成、docs 更新、commit/push）
- ScheduleWakeup で 15-20 分後に再試行
- 連続 3 回 (1 時間) 復帰しなければユーザー判断を仰ぐ方針

**初遭遇:** 2026-04-22（autonomous session 中、create_script 6 連発 + SO 作成 23 連発後）

---

## 44. Unity session down 時の手書き .meta 戦略

**症状:** Unity session down 中に .cs ファイル作成したい、でも Unity が refresh しないと `.meta` 生成されず、commit しても他マシン pull 時に GUID mismatch リスク。

**解決策:** **.meta を手動生成してコミット**。Unity は valid GUID の .meta を尊重する。

**手順:**
```bash
python3 -c "import secrets; [print(secrets.token_hex(16)) for _ in range(N)]"
```

各 .cs 用に .meta 生成:
```yaml
fileFormatVersion: 2
guid: <16 byte hex、python secrets.token_hex(16)>
```

MonoScript 用の最小 .meta はこの 2 行で足りる（既存の `GameConfig.cs.meta` 参照）。MonoImporter の他 field はデフォルトで OK。

**注意:**
- .asset / .prefab / .mat / .png 等の複雑な asset は **手書き禁止**。Unity の import 処理が独自のフィールド（guid 以外）を書き込むので、空 meta だと assets の内容と不整合
- .cs / .asmdef / .shader 等の **ソース系のみ** OK
- GUID は 16 バイト (32 hex) fixed length、secrets.token_hex(16) がちょうど

**この戦略の効果:**
- autonomous 中に .cs ファイル 3 件作成 → 手書き .meta 3 件 → commit 安全
- 他マシン pull 時、その meta で Unity 初期 import、GUID 保持
- 後で自分のマシンの Unity が refresh しても既存 meta 尊重、GUID 変わらず

**初遭遇:** 2026-04-22（autonomous セッションで Unity down 時）

---

## #45. `manage_asset create` の color プロパティは URP Lit に反映されない

**症状:** `manage_asset action=create asset_type=Material properties={color:{...}}` で作ると `.mat` は生成されるが `_BaseColor` が 1,1,1,1 のまま、シーンに貼ると真っ白。

**原因:** MCP の create_material は legacy の `color` 属性に書き込むが、URP Lit は `_BaseColor` しか認識しない。

**解決策:** `execute_code` で後追い設定。

```csharp
var mat = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/.../Foo.mat");
mat.SetColor("_BaseColor", new Color(0.85f, 0.72f, 0.56f, 1f));
mat.SetFloat("_Smoothness", 0.2f);
UnityEditor.EditorUtility.SetDirty(mat);
UnityEditor.AssetDatabase.SaveAssets();
```

**URP Unlit Transparent 用:**
```csharp
mat.SetColor("_BaseColor", new Color(1f, 0.96f, 0.88f, 0.7f));
mat.SetFloat("_Surface", 1f);
mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
mat.SetFloat("_ZWrite", 0f);
mat.renderQueue = 3000;
mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
```

**検証:** 作った直後に screenshot 撮って目視。console error 0 件では色不正を検出できない。

**初遭遇:** 2026-04-22（MainMenu 3D ステージで Floor/Pillar/Cloud マテリアル全部白になった）

---

## #46. ScreenSpaceCamera Canvas 下に空 GameObject 作ると localPosition.z が残留

**症状:** `manage_gameobject action=create parent=Canvas` で Transform 空 GO を作ると、Canvas のスケール（0.00122）を補正した localPosition = `(0, -814, 7330)` などの異常値が入る。後から TextMeshProUGUI で RectTransform に変えても z は残留 → **world Z が 0**、Canvas (z=-9) より手前だが Main Camera (z=-10) からは逆方向の誤位置で描画されないケースあり。

**解決策:** 作成後に明示リセット。

```csharp
var sub = canvas.transform.Find("Subtitle");
sub.localPosition = new Vector3(sub.localPosition.x, sub.localPosition.y, 0f);
sub.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(x, y, 0f);
```

**ベスト:** `execute_code` で `new GameObject("Name").AddComponent<TextMeshProUGUI>()` する方が初手から RectTransform できれい。

**検証:** `GetWorldCorners()` で Canvas と同じ Z 値か確認。

**初遭遇:** 2026-04-22（MainMenu Subtitle 不可視バグ）

---

## #47. TMP enableVertexGradient=true を dict で設定 → テキスト完全透明

**症状:** `manage_components set_property {m_enableVertexGradient:true, m_fontColorGradient:{topLeft:{r,g,b,a},...}}` で 4 頂点グラデ設定 → TMP のフェイスが透明になり outline/shadow も機能せず、テキスト完全不可視。

**原因:** TMP の `VertexGradient` は struct で、MCP の set_property が nested Color パッキングに対応しきれていない。

**解決策 A（安全）:** グラデ諦めて solid color + Outline + Shadow 三段で立体感。

```csharp
tmp.enableVertexGradient = false;
tmp.color = new Color(1f, 0.88f, 0.40f, 1f);
// + AddComponent<Outline>() & <Shadow>() で太ドロップシャドウ
```

**解決策 B（グラデほしい時）:** `execute_code` から C# で直接構築。
```csharp
tmp.enableVertexGradient = true;
tmp.colorGradient = new TMPro.VertexGradient(
    new Color(1, 0.95f, 0.55f, 1), new Color(1, 0.92f, 0.45f, 1),
    new Color(0.95f, 0.42f, 0.18f, 1), new Color(0.92f, 0.35f, 0.15f, 1));
```

**初遭遇:** 2026-04-22（MainMenu Title 再設計で 2 回 commit してから気付いた）

---

## #48. 完成判定に console error 0 件だけでは不十分、必ず Game View スクショ

**症状:** `refresh_unity` + `read_console error 0` で「完璧」と commit、実は背景白飛び + テキスト不可視 + 変なオレンジ点 = 酷評。error 0 件はコンパイル + 例外の話で、**見た目の正しさを保証しない**。

**強制手順:**

1. commit 前に `manage_camera action=screenshot include_image=true max_resolution=900` を **必ず** 撮る
2. 主要オブジェクトが意図通りの色・位置で写ってるか目視
3. 不自然あれば `execute_code` で状態 dump（material 色、rect world corners、TMP vertexCount）→ バグ特定
4. 直して再スクショ → OK なら commit
5. commit message の検証節に `screenshot: Assets/Screenshots/xxx.png で verified` を明記

**DoD 更新:** feedback_perfect_small_save_autonomous.md の「完璧の基準」5 項目に 6 を追加：
> 6. Visual 系の変更なら Game View スクショで目視 OK

**初遭遇:** 2026-04-22（MainMenu Commit A-C error 0 件で push → ユーザー「本当に確認してる？笑」）

---

---

## #49. Blender viewport を Material Preview mode にすると Unity URP shader が壊れる

**症状:** Blender 内で material 確認のため `space.shading.type = 'MATERIAL'`（Material Preview mode）に切替えると、Unity URP HLSL shader（`EGGYBOT_Glass` など）が Eevee preview と互換性なく、**ガラスが砂嵐ノイズ** + **ピンクが HDRI 反射で薄く** 見えてユーザーから「変になった」クレーム。

**原因:** Unity URP 用 `.shader`（`Shader "ChaosDraftParty/EGGYBOTGlass"`）は HLSL + URP ライブラリ依存で、Blender の Eevee は解釈できない。Material Preview mode は node-based shader を HDRI で評価するが、URP shader は node 化されてないため fallback 表現が破綻する。

**回避策:**

```python
# Blender viewport は「Solid mode + color_type='MATERIAL'」で固定する
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        for space in area.spaces:
            if space.type == 'VIEW_3D':
                space.shading.type = 'SOLID'
                space.shading.color_type = 'MATERIAL'  # diffuse_color 表示
```

**両 material に `diffuse_color` を Solid mode 用に必ず設定する**（Principled BSDF の Base Color だけでは Solid mode に反映されない）:

```python
pink.diffuse_color  = (0.96, 0.55, 0.65, 1.0)
clear.diffuse_color = (0.82, 0.86, 0.92, 0.35)  # alpha 低めで透け感
```

**教訓:** Blender viewport は「**実 render のプレビュー**」じゃなく「**形状の確認ツール**」として扱う。マテリアルの最終見え方は Unity 側 URP render で検証する。Blender で綺麗に見せたい誘惑で Material Preview に切替えない。

**初遭遇:** 2026-04-24（v15 レビュー中に色確認のため切替 → ノイズまみれで即 revert）

---

## #50. ユーザー承認があったときだけナレッジ化する（無条件保存禁止）

**ルール:** このスキルと `memory/` への追記は、**ユーザーが fix を明示的に褒めた / 承認した時のみ** 行う。毎 correction 後に反射的に保存しない。

**理由:** 途中経過の誤った学習（ユーザーが後で「やっぱ違う」と翻す候補案）を memory 化すると、将来セッションが間違った前提で動く。褒め（「ナイス」「いい感じ」「これで OK」「戻った」など正フィードバック）はユーザーが **最終確定** を意思表示したシグナル。

**判定フロー:**

1. 修正 / 実装完了を Claude が報告
2. ユーザーの次レス確認：
   - **「ナイス」「良い」「戻った」「OK」等の正フィードバック** → skill / memory 追記の合図
   - **「まだ変」「違う」等の negative** → 保存しない、追加修正
   - **無言で次タスク指示** → 保存しない（承認未確定）
3. 正フィードバック + 「記録して」と明示されたら即追記
4. 正フィードバックのみ（記録指示なし）の場合は、**重要な罠ならユーザーに「skill に残していい？」と 1 回だけ確認**

**悪い例（禁止）:** Claude が勝手に「次から気をつけます」と memory に書いて push。

**良い例（推奨）:** 「ナイス戻った、ナレッジに残して」→ その時だけ skill 追記 + commit。

**初遭遇:** 2026-04-24（Material Preview 事故の直後、ユーザーが「褒めた時だけ記録して」とルール明言）

---

## #51. SDF 判定球の center は mesh の bounds.center じゃなく「全体の球中心」

**症状:** EGGYBOT v16 SDF body clip shader が「Hand が bowl にめり込んでも消えない」「離れた所で切れる」「Walking で挙動が変」。判定球の center を `BodyLower.bounds.center` (= (0, 0.33, 0)) に置いてたが、bowl の上ドーム (BodyUpperClear) が SDF 射程外で discard されない。

**原因:** bowl は **下椀 + 上ドームで完全な球**（中心 (0, 0.50, 0)、半径 0.336）として表現される。しかし SkinnedMeshRenderer の `bounds.center` は bone weight に応じて計算され、**幾何中心ではなく bone weight bounds の中心**（BodyLower は下半球、BodyUpperClear は上半球）。これを center に使うと半分しかカバーしない。

**回避策:**
```csharp
// BodyClip 判定球は bowl 全体の幾何中心に置く
bodyClip.position = new Vector3(0f, 0.50f, 0f);  // ← bowl 全体 sphere の center
bodyClip.localScale = Vector3.one * 0.672f;      // radius 0.336 (bowl mesh sphere 半径)

binder.bodyCenterTransform = bodyClip.transform;
binder.bodyCenterOffsetLocal = Vector3.zero;     // offset 0
binder.bodyRadius = 0.336f;
binder.clipMarginMM = 5f;                        // 安全マージン
binder.trackHipsBone = false;                    // bowl 自体が動かないので追従不要
```

判定球の center を確かめる手順:
1. `mesh.bounds.center` を見ない（罠）
2. mesh の **vertex の min/max world position** から幾何中心を計算
3. 球状 mesh なら `(min + max) / 2`、楕円体なら別途計算

**確認方法（execute_code）:**
```csharp
var smr = bowl.GetComponent<SkinnedMeshRenderer>();
var verts = smr.sharedMesh.vertices;
var min = verts[0]; var max = verts[0];
foreach (var v in verts) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
var center = (min + max) / 2f;  // local
var worldCenter = smr.transform.TransformPoint(center);
// これが真の幾何中心。bounds.center とは違うことが多い
```

**初遭遇:** 2026-04-29（EGGYBOT v16 humanoid SDF clip 修正セッション、ユーザー「めっちゃよくなってきた」承認）

---

## #52. shader / SDF 系問題は「実測 → 並列 agent レビュー → 提案」の順序固定

**症状:** shader / mesh / bone の挙動デバッグで、思い込みで仮説を立てて修正 → 全部外す → 何時間も溶ける。

**原因:** Claude が「Walking が原因だろう」「muscle が大きすぎる」「Avatar humanScale が」と数値も見ずに仮説を立てる癖。実態を見ると全部見当違いなことが多い。

**回避策（フロー固定）:**

1. **数値を取る前に proposal しない。** ユーザーから「修正して」と言われても、まず実測。
2. **execute_code で実測:**
   - mesh の全 vertex world position
   - vertex 距離 (target との)
   - shader uniform の global 値（`Shader.GetGlobalVector`）
   - SkinnedMR の `bones`, `rootBone`, `bounds.center`, weights
   - Animator の `humanScale`, `bodyPosition`, 各 bone localPos
3. **並列 Agent 3 個（標準 set）:**
   - **Agent A**: 既存仮説の検証 or 反証（数値ベース）
   - **Agent B**: pixel-level / vertex-level の実態解析
   - **Agent C**: 代替アプローチ網羅（Web 検索、Unity Doc、Forum 含む）
4. 並列結果を統合 → 真因特定 → そこから修正
5. 「思い込み」proposal の前に「俺、実測してないけど proposal していい？」と自問

**Token / 時間コスト:**
- 並列 3 agent で約 600〜900 sec、token 数千〜1万
- 思い込みベースの 5 回試行錯誤の時間より遥かに速い
- ユーザーが「徹底的に調べて」「マルチで」と言った時はこの方式必須

**典型的な誤った思い込み（避けるべき）:**
- 「Walking クリップが原因」→ 実は static T-pose ですでに破綻してた
- 「Avatar humanScale で Hips Y がスケールされて 0.49 にズレる」→ 実は Mecanim の足元基準補正で正しい高さ
- 「Mesh の bounds.center = 幾何中心」→ SkinnedMR では bone weight 由来で違う
- 「bone を動かして直す」→ Avatar 不整合の原因にもなる、最後の手段
- 「shader が動いてない」→ 実は仕様通り、判定範囲が間違ってるだけ

**初遭遇:** 2026-04-29（EGGYBOT v16 SDF clip ズレで 5 仮説外した後、並列 agent 3 個で 1 回で真因特定）

---

## 追加候補（まだ踏んでない、踏んだら書く）

- [ ] Prefab に ClientNetworkTransform つけた時の挙動
- [ ] NGO 2.x の ServerRpc パラメータ制約
- [ ] Relay JoinCode の形式とバリデーション
- [ ] Input System アクション Assign 時の GUID ズレ
- [ ] Mixamo FBX リターゲット時の hip 固定問題
- [ ] LFS 越しの .dll に同期遅延
- [ ] ParrelSync クローンでの Library/ 競合
