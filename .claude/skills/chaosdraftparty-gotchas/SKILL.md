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

## 追加候補（まだ踏んでない、踏んだら書く）

- [ ] Prefab に ClientNetworkTransform つけた時の挙動
- [ ] NGO 2.x の ServerRpc パラメータ制約
- [ ] Relay JoinCode の形式とバリデーション
- [ ] Input System アクション Assign 時の GUID ズレ
- [ ] Mixamo FBX リターゲット時の hip 固定問題
- [ ] LFS 越しの .dll に同期遅延
- [ ] ParrelSync クローンでの Library/ 競合
