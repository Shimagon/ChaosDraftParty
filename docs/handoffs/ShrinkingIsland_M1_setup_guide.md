# 縮む島 ミニゲーム雛形 (M1) — Unity scene セットアップ手順

書いたコード:
- `Assets/_Project/Scripts/Minigames/ShrinkingIsland/Tile.cs` — タイル単体 (踏み消し検知 + visual fade)
- `Assets/_Project/Scripts/Minigames/ShrinkingIsland/TileManager.cs` — 中央集権 NetworkBehaviour、NetworkList<byte> でタイル状態同期、踏み消し 1s + 外周 shrink 15s

これから user (or Unity MCP 復活時に Claude) が **Unity Editor で行う作業**:

---

## A. Tile prefab 作成

1. `Assets/_Project/Prefabs/Minigames/` フォルダ作成
2. **GameObject > 3D Object > Cube** で空 cube 作成、名前 `Tile`
3. Transform Scale を **(2, 0.3, 2)** に設定 (1 タイル 2m × 2m × 0.3m 厚)
4. component 構成:
   - **MeshRenderer** (default、Material は仮)
   - **MeshCollider** (Convex OFF、踏める面の collider)
   - 子 GameObject `StepTrigger` 追加
     - BoxCollider (size: (1.0, 0.4, 1.0)、center: (0, 0.4, 0)) ← 上面に薄い trigger
     - **isTrigger = ON**
   - Tile component 追加（自分作った C# script）
     - `visualRenderer` = self の MeshRenderer
     - `standCollider` = self の MeshCollider
     - `stepTrigger` = StepTrigger 子の BoxCollider
5. `Assets/_Project/Prefabs/Minigames/Tile.prefab` として保存
6. scene 上の Cube は削除

---

## B. ShrinkingIslandProto.unity scene 整備

1. `Assets/_Project/Scenes/ShrinkingIslandProto.unity` を開く
2. 既存の Ground / 仮 player があれば削除
3. **Main Camera + Directional Light** を確認 (なければ追加)
4. CinemachineCamera を Sandbox_v19 から複製 or 新規作成
   - CM_Player3rdPerson と同じ設定 (OrbitalFollow Sphere, Radius=5, etc.)
   - Follow / LookAt は空で OK (PlayerController.OnNetworkSpawn が自動 bind)
5. 空 GameObject `ShrinkingIslandManager` を作成 (root)
   - 位置 (0, 0, 0)
   - **NetworkObject** component 追加
   - **TileManager** component 追加
     - `gridSize` = 8
     - `tileSpacing` = 2.0
     - `tilePrefab` = 上で作った Tile.prefab を assign
     - `tileParent` = 自分の Transform (空 OK)
     - `fadeToGoneDelay` = 1.0
     - `shrinkInterval` = 15
     - `shrinkInitialDelay` = 5
6. **scene を Build Settings に追加**（既に追加済の確認）

---

## C. NetworkManager に prefab 登録

`Boot.unity` に DontDestroyOnLoad で残る NetworkManager を編集:

1. NetworkManager component の `NetworkPrefabsList` を開く
2. **ShrinkingIslandManager (実体は scene placed) は登録不要**（scene-placed NetworkObject は spawn 不要、scene 同期で自動）
3. **Tile prefab も登録不要**（NetworkObject 持ってないから、見た目だけ）

→ つまり追加の prefab list 登録は不要。

---

## D. Match scene を ShrinkingIslandProto に変更

1. `Bootstrap` GO の Inspector で:
   - `nextSceneName` = `ShrinkingIslandProto` (現状 `Sandbox_v19_player_controller_test`)
2. これで Boot 起動 → ShrinkingIslandProto に直接 load → Host で player spawn → 縮む島ループ開始

または、`MainMenu` 経由にしたい場合は MainMenuController.matchSceneName を変更。

---

## E. テスト

1. Boot scene で ▶ Play → ShrinkingIslandProto 自動 load
2. **Host (Local)** クリック → 自分の EGGYBOT が grid 中央 (0,0,0 付近) に spawn
3. WASD で歩く → タイル踏むと **赤く点滅 → 1 秒で消失**
4. 5 秒後から **15 秒ごとに外周 1 列消失** (8x8 → 6x6 → 4x4 → 2x2 と縮む)
5. 落下: 全タイル消えた中央付近で踏み外す → Y < -5 で **KO state** (NetworkVariable<bool> IsAlive = false)
6. KO 後は入力受付なし、CC は重力で落下続ける

MPPM 2 ウィンドウで:
- Editor 1 Host、Editor 2 Join (Local 127.0.0.1)
- 両ウィンドウで同じ場面 (タイル消失タイミングが host 権威で同期、両 client 視覚も一致)
- Crouch も両ウィンドウで反映 (NetworkVariable<bool> NetIsCrouching で同期)

---

## F. DoD (Definition of Done) — M1 milestone

- [ ] 2 ウィンドウで EGGYBOT 2 体 spawn
- [ ] 各自 owner で WASD 移動
- [ ] **タイル踏み消しが両ウィンドウで同期**
- [ ] **外周 shrink が両ウィンドウで同期**
- [ ] 落下 (Y < -5) で **両ウィンドウで KO state 反映** (IsAlive=false)
- [ ] crouch も両ウィンドウで visual + collider 同期

---

## G. 既知の落とし穴

- **TileManager の NetworkObject は scene placed**: Boot から ShrinkingIslandProto に load された時、scene 内の NetworkObject は host で auto-spawn される。client 側もそれを受信。manual Spawn は不要
- **Tile prefab は NetworkObject 持たない**: visual だけなので各 client で local Instantiate される。状態は TileManager の NetworkList から flow
- **Shrink interval が早すぎる場合**: TileManager の `shrinkInterval` を 20-30 秒に延ばす
- **Tile prefab の Material が存在しない**: 仮で URP Lit material 1 個作って assign。color は適当
- **player が tile 踏んでもイベント来ない**: StepTrigger の isTrigger=ON、BoxCollider が CC を覆える size であることを確認

---

## 完了したら

- Multiplayer Play Mode で動作確認
- AI review loop
- commit + push + 日報追記
