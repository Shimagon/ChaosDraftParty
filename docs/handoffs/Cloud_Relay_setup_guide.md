# Unity Cloud Project link + Relay 有効化手順

## ゴール

ChaosDraftParty を Unity Cloud に link して、`Bootstrap.requireUGS=true` で Relay 経由の Host/Join を動作させる。所要 **10〜15 分**。

無料枠で M1〜M5 のテストは余裕（Relay 月 50 平均 CCU、1 人開発で十分）。

---

## A. Unity Editor で link (5 分)

1. Unity Hub で Unity ID に sign in 済みを確認
2. ChaosDraftParty を開く → **Edit > Project Settings > Services**
3. Sign in 要求が出たら Unity ID で完了
4. **Organization 選択**（個人なら自分の名前）
5. **Use an existing cloud project** または **Create a new cloud project**
   - 既存 Dashboard プロジェクトが無ければ後者推奨。Project 名は `ChaosDraftParty` で OK
6. **Link Unity project to cloud project** クリック
   - `ProjectSettings/ProjectSettings.asset` の `cloudProjectId` / `organizationId` / `projectName` が即書込まれる（git diff で確認可能）
7. COPPA 設定の dialog が出たら **「13 歳以上対象」** で進める
8. Editor 再起動は **不要**（Services window が更新されない時のみ再起動）

---

## B. Unity Cloud Dashboard で各サービス有効化 (5 分)

[https://cloud.unity.com](https://cloud.unity.com) → 該当 project を選択 → 以下を有効化:

### Authentication
- **Authentication > Sign-in methods > Anonymous Sign-In: ON**
- これだけで `AuthenticationService.SignInAnonymouslyAsync` が通るようになる

### Multiplayer > Relay
- **Get Started** ボタン押すだけで有効化
- region は `auto` で OK（必要なら `asia-northeast1`）

### Multiplayer > Lobby (M1 では不要、後で使う想定なので pre-enable)
- Get Started で有効化、設定は default

### Project Visibility
- Dashboard の Project Settings で **private** になってることを確認

---

## C. Editor で動作確認

### 設定切替
- `Boot.unity` → `Bootstrap` GO 選択 → **`requireUGS` を ON** に
- `Bootstrap.autoAdvance` は ON のまま、`nextSceneName` も `Sandbox_v19_player_controller_test` のまま

### テスト
1. Boot scene で ▶ Play
2. Console に **`[Boot] Authenticated as <playerId>`** が出れば UGS 認証 OK
3. DebugConnectionUI で **Host (Relay)** 押す → JoinCode 表示
4. MPPM Player 2 で JoinCode 入力 → **Join (Relay Client)** 押す
5. 両ウィンドウで EGGYBOT 2 体表示

### トラブルシュート
- `[Boot] UGS init failed` → Cloud project link が反映されてない、Editor 再起動 + Services window 確認
- `RelayService exception` → Dashboard で Relay 有効化されてない、または region 設定ミス
- 認証は通るが Allocation 失敗 → Auth と Relay の両方が enabled か再確認

---

## D. .gitignore チェック

`ProjectSettings/ProjectSettings.asset` は元々追跡対象。`cloudProjectId` / `organizationId` 等が書き込まれた状態で **commit が必要**（チームで共有するため）。

`.gitignore` 調整は不要。

---

## E. 落とし穴メモ

- **ParrelSync clone 側も同じ ProjectSettings.asset を共有** → link は MainEditor で 1 回やれば clone 側も認証通る
- **MPPM Virtual Player は同 process** → 同様に link 1 回で OK
- **Organization 間違えると剥がせない** ことはないが、Dashboard 上で project 移動するのが面倒。最初に個人 org を選ぶ
- **Relay region** が遠いと latency 悪化。日本テストなら `asia-northeast1` を `RelayConnection.StartHost(maxPeers, "asia-northeast1")` で明示推奨

---

## 関連 source

- [Configure a project for Unity Cloud (Unity Docs)](https://docs.unity.com/en-us/cloud/projects/configure-project-for-unity-cloud)
- [Authentication Get Started (UGS Docs)](https://docs.unity.com/ugs/en-us/manual/authentication/manual/get-started)
- [Relay Pricing FAQ (Unity Support)](https://support.unity.com/hc/en-us/articles/4410136449812)
- [UGS Pricing](https://unity.com/products/gaming-services/pricing)

---

## 次に進む

Cloud Relay 動作確認 OK なら:
- Bootstrap.requireUGS=true で commit
- 友達と別 PC から Internet 経由 Join をテスト
- M2: Lobby サービスを使った friendly UI (JoinCode 共有を Lobby browse に置換)
