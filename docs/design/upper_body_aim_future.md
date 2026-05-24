# Upper Body Aim 将来計画 (武器 / スキル投擲)

Date: 2026-05-24
Status: **設計変更不要 / 現行維持で M3+ 拡張可能**

## 結論

`PlayerController.cs` の **「camera-relative move + 体を進行方向に Slerp 回転」** 設計はパーティ TPS の defacto そのものなので、**M3 で pulse / bomb / 武器を追加しても変更不要**。

必要時に Animator に Upper Body Layer を 1 枚足すだけで上半身だけ視点方向に向けられる。

## 調査根拠 (2026-05-24)

| パターン | 採用タイトル | パーティ性 | ChaosDraftParty 適合 |
|---|---|---|---|
| **Movement-locked** (体 = 移動 = 発射) | Splatoon / Hades / Cuphead / Spiderman swing | ◎ | ★★★ (推奨) |
| Camera-aim decouple (体 = 移動 / 武器 = カメラ) | Fortnite / Gears / Apex | ○ | ★★ (上級者向け) |
| Strafe-mode (右トリガ構え) | RE4 / Last of Us | △ | ★ (シリアス系) |
| Auto-aim assist | Splatoon paintball / Knockout City | ◎ | ★★★ (将来 hit feel 改善) |
| Lock-on / Z-target | Zelda / Genshin | ○ | ★ (タップ to face のみ可) |

**推奨設計:** Movement-locked (現行) を維持。pulse / bomb は体の向きから発射 = 投擲弾の自然挙動 + 学習コストゼロ。

## 現行 PlayerController が既に持ってる強み

- `camera-relative` 移動方向計算 (`PlayerController.cs:540-549`)
- `Quaternion.LookRotation + Slerp` で体回転 (`PlayerController.cs:555-559`)
- `turnSmoothness = 12` で AAA 体感 (Mario Odyssey defacto)
- 武器発射 API は **`transform.forward` を発射方向に渡すだけ** で動く

## M3+ で Upper Body Aim が必要になった時の追加手順

現行 Animator は Base Layer のみ (Locomotion blend tree、Speed parameter)。Upper Body Layer を後付けするだけで完結:

1. **Avatar Mask 作成** — Chest 以上 (Spine1/2/3, LeftShoulder/Arm/Hand, RightShoulder/Arm/Hand, Head, Neck) を含む `EggybotUpperBody.mask`
2. **Animator に Upper Body Layer 追加**
   - Blending Mode: **Override**
   - Mask: `EggybotUpperBody.mask`
   - Weight: 0 (Aim 中だけ 1 に lerp)
3. **Aim state を追加** — pulse / bomb 構え中だけ Weight=1、IK で武器を視点方向に向ける
4. **NetworkAnimator** — Layer Weight も同期 (NGO 2.x は標準対応)

**所要時間目安:** 4-6 時間 (mask 設計 + state + IK + テスト)。Foot v3e 等の既存補正と衝突しないので独立して足せる。

## Auto-aim assist (パーティ性ブースト)

将来的に「pulse 弾の命中感」を上げたい場合、Splatoon / Knockout City defacto の弱誘導 (5-15° の自動補正、敵が近距離にいる時のみ) を後追加できる。これは weapon system 側の設計で、PlayerController を触らず実装可能。

## やってはいけない設計 (NG リスト)

- ❌ Strafe-mode (右トリガで体ロック) — パーティ性壊す、学習コスト高
- ❌ 永続 Lock-on — 4 人乱戦で誰にロックしたか分からなくなる
- ❌ Camera-aim 全採用 (Apex / Fortnite 路線) — 上級者前提でカジュアル排除

## 参照

- 調査レポート: 2026-05-24 並列 3 agent (本ファイル commit と同 batch、commit message 参照)
- 関連: [memory/feedback_research_before_user_asks.md] — 設計判断前に調査必須ルール (R11)
- 関連: `Assets/_Project/Scripts/Core/PlayerController.cs:540-559` — 移動方向計算 + 体回転
