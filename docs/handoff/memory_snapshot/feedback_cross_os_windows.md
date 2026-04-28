---
name: ChaosDraftParty は Mac + Windows 両対応
description: ドキュメント・コマンド例・セットアップ手順は MacBook と Windows デスクトップの両方で動くように書く
type: feedback
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## ルール

ChaosDraftParty は **MacBook（現行）+ Windows デスクトップ PC** の 2 台で開発する。ドキュメントや setup 例を書く時は **両 OS のコマンドを併記** する（macOS 側 / Windows 側）。

**Why:** ユーザーは 2 台持ちで、デスクトップ PC（Windows）でも同じリポを clone して開発する。片方 OS でしか動かないスクリプトや手順を書くと、もう片方のマシンで詰まって時間が溶ける。卒業制作は締切固定で詰まる余裕がない。

**How to apply:**
- `README.md` / `HANDOFF.md` / `TODO.md` / `RESOURCES.md` 等は **macOS / Windows 並記** で書く
- CLI 例も両対応（`which claude` / `where claude`、`open -a` / `start`、`~/` / `%USERPROFILE%\`、`/` / `\` 等）
- Unity 側パス：**引用符で囲んで日本語・空白耐性**、リポは ASCII 英字パス（`~/Documents/Dev/ChaosDraftParty` / `C:\Dev\ChaosDraftParty`）を推奨
- シェルスクリプトを書くなら **PowerShell 版も添える** か、npm scripts / Unity の Editor script で OS 非依存化
- `.gitattributes` は `eol=lf` で改行固定（既設定済）、CRLF 事故を避ける
- Unity Hub 起動：macOS `open -a "Unity Hub"` / Windows `start "" "C:\Program Files\Unity Hub\Unity Hub.exe"`
- Claude Code 設定ディレクトリ：macOS `~/.claude/` / Windows `%USERPROFILE%\.claude\`
- パッケージマネージャ：macOS `brew` / Windows `winget` か `scoop`
- Unity Input System：**OS 差異なし**（1.19 で両対応）
- Steam ビルド：macOS `steamcmd` は brew 経由、Windows は Valve 公式 zip
- LFS：両 OS で `git lfs install --local && git lfs pull` を clone 直後に実行
- Python / uv / node：両 OS で path 通せば同じ（nvm-windows, uv Windows installer）
- ParrelSync：**両 OS 対応**

**恒久対応の基本形：**
`README.md` の「新マシン setup」に macOS / Windows 2 カラムの表を置く。新ツールを入れるたびに両 OS 列を埋める。

**すでに両対応済みの物：**
- Unity 6.4.3f1（Mac Silicon / Windows x64 両ビルド）
- MCP for Unity（HTTP 127.0.0.1:8080 はクロス OS）
- NGO / Relay / Lobby / Auth（UGS 全サービス）
- ParrelSync 1.5.2

**Windows 特有の注意：**
- パスに **日本語・空白を絶対入れない**（Mac 以上に事故る）。`C:\Dev\ChaosDraftParty\` 推奨
- Windows Defender が `Library/` への書込みを遅らせるケースあり → Unity プロジェクト配下を除外設定
- 改行コード CRLF 問題：`git config --global core.autocrlf false` 推奨（`.gitattributes` の eol=lf を優先）
- `.meta` の GUID は OS 非依存（テキスト）なので問題なし。ただし Asset import 時の **Texture Max Size / Compression が platform-specific に焼かれる** 場合、OS 差が出る可能性 → `.meta` で platform override が明示されているかチェック
