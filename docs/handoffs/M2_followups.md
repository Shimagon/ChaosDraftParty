# M2 フォローアップ一覧

M1 で意図的に先送りにした TODO をまとめる。M2 着手前に確認すること。

---

## PlayerController

- **CanStandUp を全 client 同期 + HUD フィードバック**
  - 現状: Owner-local 判定のみ。他 client では立ち上がってしまう (PlayerController.cs L399 コメント参照)。
  - 対応方針: NetworkVariable<bool> `NetIsBlocked` をホスト側で書き込み、全 client でしゃがみ強制維持 + HUD に「頭がつかえています」表示。
  - 優先度: MEDIUM
