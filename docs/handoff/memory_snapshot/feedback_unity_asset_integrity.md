---
name: ChaosDraftParty アセット参照崩壊の防止（最重要）
description: Unity プロジェクトを複数マシンで開いた時にマテリアル/シェーダーがピンク化・色ズレ・リンク切れするのを絶対に避けるための運用ルール
type: feedback
originSessionId: 5e61ca86-8045-42d6-9ce5-e2ecb4b5949c
---
## ルール

Unity プロジェクトで「GitHub で引き継いだらキャラクターの色が違う／ピンクになる／バグる」という **「取り返しのつかない」事故を最優先で避ける**。ユーザーは過去にこの痛みを受けており、卒業制作は複数マシン（MacBook + デスクトップ）で開発するため、**GUID 不整合・アセット参照崩壊で作業時間が吹き飛ぶリスクが現実**にある。

## 毎コミット前に必ず守る

1. **`.meta` は必ずコミット**。`.gitignore` で `*.meta` を除外しない。Assets/ 配下で生成される `.meta` すべて、サブフォルダに新規ファイルを足したら対応する `.meta` も同じコミットに含める。片方だけ追加は絶対 NG（clone 先で参照切れ）。
2. **Packages/packages-lock.json** を必ずコミット。マシン間でパッケージの解決結果を固定。
3. **ProjectSettings/** を丸ごとコミット。特に：
   - `GraphicsSettings.asset`（URP Pipeline Asset 参照を含む）
   - `QualitySettings.asset`（Render Pipeline Asset のクオリティ別上書き）
   - `EditorSettings.asset`（Asset Serialization / Version Control モード）
   - `ProjectSettings.asset`（各プラットフォーム設定）
4. **Assets 直下の Unity 自動生成アセット** とその `.meta` を必ずコミット：
   - `UniversalRenderPipelineGlobalSettings.asset`
   - `DefaultVolumeProfile.asset`
   - `DefaultNetworkPrefabs.asset`
   - 他 URP / Volume / Render Pipeline 関連で新規生成されたもの
5. **EditorSettings は Unity 6 デフォルトを維持**：
   - Asset Serialization Mode: **Force Text**（YAML マージ可能にするため）
   - Version Control Mode: **Visible Meta Files**
   - Line Endings for New Scripts: **OS Native** か **Unix**
   - これらが変わっていたら即修正して確定コミット。
6. **`.gitattributes`**：
   - `*.asset *.prefab *.unity *.mat *.anim *.controller` を `text merge=unityyamlmerge eol=lf` で管理（設定済み）
   - `.fbx .png .wav` などバイナリは LFS（設定済み）
7. **URP / Render Pipeline の参照変更は「変更 → 確定 → すぐコミット」**。
   - Graphics Settings → Render Pipeline Asset の差し替え、Forward/Deferred Renderer の差し替えは中途半端な状態で別マシンに持ち込まない。
   - URP Asset ファイル自体を消して別 GUID で作り直す操作は特に危険。するなら全マシン同時。
8. **Script を移動・リネーム・asmdef 再編する時は `.meta` を必ず一緒に動かす**（Finder/Unix `mv` ではなく Unity Editor 上で操作すれば自動）。Unity 経由で動かせば GUID は保存される。
9. **`git push` 前に `git status --short` で対称性チェック**：
   - `Assets/Foo.cs` があるなら `Assets/Foo.cs.meta` も同じコミットに。
   - フォルダ新設なら `Assets/Bar/` だけでなく `Assets/Bar.meta` も追加されるはず。
10. **Shader Graph / Render Pipeline Asset は「保存 → 即テスト → 即コミット」ループ**。壊れかけの状態を残さない。

## 痛い症状と対処（未然防止＞事後対応）

| 症状 | 原因 | 対処 |
|---|---|---|
| Material がピンク（Missing Shader）| Shader の GUID 参照切れ | まず `Library/` を消して Unity 再起動 → ほとんど直る。ダメなら `.meta` 漏れ疑う |
| キャラの色が違う | Material → Texture 参照切れ or Import Setting の差分 | `.meta` の TextureImporter セクションを `git log -p` で確認、直前 commit で失われた設定を復元 |
| シーン開くと Component が `None` | Script の GUID ズレ（asmdef 移動／Script 削除→復活で発生） | Script の `.meta` を GUID ごと戻すか、シーン側で再割当 |
| 起動直後に大量のコンパイルエラー | Packages/packages-lock.json と manifest.json の不整合 | 両方同時に push、別マシンは両方 pull した状態で Unity 起動 |
| 別マシンで開くと Post Processing が効かない | Default Volume Profile の GUID 変化／未コミット | Assets/DefaultVolumeProfile.asset と .meta の同時コミットを確認 |

## なぜこれを重視するか（歴史）

ユーザーが過去の Unity プロジェクトで「キャラの色が違う／ピンク化／取り返しのつかない状態」を経験し、2026-04-21 の卒業制作セットアップ時に強く念押しした。卒業制作は締切固定（2026-09）で、マテリアル参照が壊れると復旧だけで数時間〜数日溶ける。守れば防げるので、**常時 ON** のルール。

## デスクトップ引き継ぎの安全手順

新マシンで clone した時の最短確認：

```bash
git clone https://github.com/Shimagon/ChaosDraftParty ~/Dev/ChaosDraftParty
cd ~/Dev/ChaosDraftParty
git lfs install --local && git lfs pull
# Unity Hub → Add project from disk → Open
# 初回は 5〜10 分待つ。Library/ が生成される
# この時点で色がおかしければ Library/ を消して再起動
```

Windows での注意：
- 改行コード問題があれば `.gitattributes` の eol=lf が効いてるか確認
- パスに日本語や空白が入らないところに置く（`~/Dev/ChaosDraftParty` 推奨）
