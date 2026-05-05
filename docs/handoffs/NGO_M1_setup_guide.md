# M1 NGO + Relay 疎通プロト — Unity セットアップ手順

書いたコード（Phase 1 完了済）:
- `Assets/_Project/Scripts/Network/RelayConnection.cs` — 静的 Relay Host/Join helper
- `Assets/_Project/Scripts/Network/ConnectionManager.cs` — DontDestroyOnLoad の接続管理 singleton
- `Assets/_Project/Scripts/Core/PlayerController.cs` — `: NetworkBehaviour` 化、`OnNetworkSpawn` で IsOwner ガード
- `Assets/_Project/Scripts/UI/MainMenuController.cs` — Host/Join + JoinCode 入出力配線

これから user (or Unity MCP 復活時に Claude) が **Unity Editor で行う作業** が以下。

---

## A. Boot.unity に NetworkManager + ConnectionManager

1. **Boot.unity** を開く
2. 空 GameObject 作成 → 名前 `NetworkManager`
   - `NetworkManager` component 追加
   - `Unity Transport` component 追加（auto で）
   - **Network Prefabs List**: Player Prefab を後で登録（手順 C）
   - **Player Prefab**: 後で登録（手順 C）
3. 同じ GameObject（or 別の DontDestroy）に **`ConnectionManager`** component 追加
4. （任意）GameObject を Prefab 化して `Assets/_Project/Prefabs/Network/` に保存

---

## B. EGGYBOT Player Prefab 化

1. EGGYBOT_v16 GameObject を **`Assets/_Project/Prefabs/Player/EggybotPlayer.prefab`** に保存
2. Prefab 編集モードで以下追加:
   - `NetworkObject` component（ChecksOnSpawn等は default）
   - `NetworkTransform` component（NGO 2.x、`AuthorityMode = Owner` に設定 ← **重要、CLAUDE.md ホスト権威例外で位置だけ Owner 同期**）
     - Position / Rotation を ON、Scale は OFF（帯域削減）
     - Use Half Float Precision = ON 推奨
   - `NetworkAnimator` component（owner→all で Animator パラメータ同期）
3. Prefab 上の PlayerController フィールド:
   - `cinemachineCamera` は **null のままで OK**（OnNetworkSpawn で自動 find）
   - `cameraTransform` も null OK（Camera.main 自動 fallback）
4. Prefab に `NetworkManager.Player Prefab` を assign（A.2 の `Player Prefab` slot）

---

## C. MainMenu.unity の UI 拡張

1. MainMenu Canvas に以下追加:
   - **TMP_InputField** "JoinCodeInput"（Placeholder = "JoinCode を入力"）
   - **TMP_Text** "JoinCodeDisplay"（Host 起動後の code 表示）
   - **TMP_Text** "StatusDisplay"（接続状態）
2. **MainMenuController** に上記 4 つを Inspector で wire:
   - `joinCodeInput`, `joinCodeDisplay`, `statusDisplay`
   - 既存 `hostButton`, `joinButton`, `quitButton` はそのまま
3. **`matchSceneName`** = `Sandbox_v19_player_controller_test`（M1 はこれで OK）

---

## D. Match scene (Sandbox_v19) を NGO 用に調整

1. Sandbox_v19_player_controller_test.unity を開く
2. **既存の EGGYBOT_v16 GameObject を削除**（NetworkManager が自動 spawn するため、scene に置いておくと二重）
3. CinemachineCamera (CM_Player3rdPerson) は scene に残す（Follow/LookAt は OnNetworkSpawn で自動配線）
4. Ground / Light は残す
5. シーンを **Build Settings に追加**

---

## E. 2 ウィンドウ動作確認 (Unity 6 Multiplayer Play Mode)

1. `Window > Multiplayer > Multiplayer Play Mode` を開く
2. **Player 2** にチェック（or 必要なら Player 3,4 も）
3. ▶ Play
4. Editor 1 (host): Host ボタン → JoinCode 表示
5. Editor 2 (client): JoinCode 入力 → Join ボタン
6. **両ウィンドウで EGGYBOT が同期して動けば疎通成功** 🎉

ParrelSync を使う場合は:
- ParrelSync で Clone 作成 → 別 Editor 起動
- 同じ手順で Host/Join

---

## F. 確認項目（DoD）

- [ ] Host 起動 → console に `[ConnectionManager] ====== JoinCode: XXXX ======`
- [ ] Client 接続 → console に `[ConnectionManager] Self connected (clientId=N)`
- [ ] 両ウィンドウで EGGYBOT が **2 体表示** される
- [ ] それぞれ自分のキャラのみを WASD で動かせる
- [ ] 相手キャラの動きが滑らかに同期される（補間）
- [ ] Animator (Idle/Walk/Run/Jump) が両ウィンドウで一致

---

## G. 既知の落とし穴

- **NetworkManager.Singleton が null** → Boot シーンに NetworkManager を置いて DontDestroyOnLoad 確認
- **Player が spawn されない** → NetworkManager の `Player Prefab` slot に EggybotPlayer.prefab assign してるか確認
- **Cinemachine が動かない** → `OnNetworkSpawn` の `SetupOwnerCamera` が scene 内 CinemachineCamera を find してるか console で確認
- **両 client でカーソル lock** → 想定通り。1 ウィンドウは Esc で unlock してマウス操作
- **JoinCode が古い** → host を Stop してから新規 Host 起動、code は毎回変わる
- **Relay region** が遠い → `RelayConnection.StartHost(maxPeers, "asia-northeast1")` 明示推奨

---

## 完了したら

- 動作確認 OK → AI review loop → commit + push
- 動かなかったら Console エラー full text 持ってくる
