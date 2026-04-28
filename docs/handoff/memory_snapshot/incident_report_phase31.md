# インシデントレポート: Phase 31 デプロイ時の$13損失

## 日付: 2026-03-07
## 損失額: -$13.35 ($400入金 → $386.91)

---

## 損失の内訳

| 原因 | 損失額 | 割合 |
|------|--------|------|
| ENA未ヘッジspot放置 (価格下落) | -$9.51 | 71% |
| BTC/ETH/HYPEポジション大量回転(churning)による手数料+スリッページ | -$3.8 | 29% |
| FR収入 | +$0.048 | — |
| **合計** | **-$13.26** | |

---

## 根本原因1: ポジション大量回転 (CHURNING) ★最重要

### 発見
取引履歴から、BotがBTC/ETH/HYPEのポジションを**10-15分ごとに入場→退場→入場→退場**を繰り返していた。

**BTC perp (3/6, 4時間で30回転):**
```
16:00 sell (entry) → 16:07 buy (exit) → 16:19 sell → 16:26 buy →
16:37 sell → 16:43 buy → 16:55 sell → 17:02 buy → 17:15 sell → ...
```

**ETH perp (3/5, 同様のパターン):**
- 1時間ごとに entry/exit を繰り返し

**Volume $8,637 / 資本$400 = 21.6x turnover**

FR収穫戦略は**月1-2回のエントリーで長期保有**が前提。
10-15分ごとの回転は戦略を完全に破壊する。

### 原因の仮説
安全装置（特にfr_trend_decline 50%閾値）が頻繁にトリガー → exit。
次のサイクルでFR平均がentry閾値を超えている → re-entry。
このoscillationが永遠に繰り返される。

entry_lookback=24hにしたことで、1時間のFR変動が平均に大きく影響。
FR 0.001% → FR 0.0005% = 50%低下 → exit → 次のサイクルで平均は依然1%超 → re-entry

### 修正方針
- [ ] entry後最低Nhは安全装置を緩和 (basis緊急とDD以外)
- [ ] fr_trend_decline閾値の見直し (50%は敏感すぎ?)
- [ ] entry_lookbackを24→48hなどに戻すことも検討
- [ ] churning防止: exit後のcooldown強化

---

## 根本原因2: ENA未ヘッジspot放置 (-$9.51)

### 経緯
1. Bot (--size 400時代) がENA entry開始
2. ENA perp short: 2044 ENA sold → 成功
3. ENA spot buy: 2044 + 646.2 = 2690.2 ENA purchased ($290.81)
4. spot/perp mismatch > 50% → rollback トリガー
5. rollback: perp closed (buy 2044 @ 0.10796) → 成功
6. rollback: spot sell → **646.2 ENAだけ売れた** (残り2042 ENA放置)
7. Agent keyからfetch_balance(ENA) = 0 → Botは残高0と認識 → 以後無視
8. 2042 ENA ($221) が**perpヘッジなし**で放置
9. ENA価格 $0.1081 → $0.1037 (-4.1%) = -$9.51 損失
10. 手動で売却して回収

### なぜrollbackが不完全だったか
- `fetch_balance({"type": "spot"})` がagent keyではENA残高を0と返す
- ccxt createOrderは動くが、balance checkは別のAPI → 不整合
- rollbackは「balance=0なら売るものなし」と判断して終了
- **Botは成功したと思っているが実際は失敗**

### 修正方針
- [ ] rollback後にexchange positionを再確認 (balance checkではなくorder result確認)
- [ ] spot売却はorder結果のfilled量で判断、balance非依存
- [ ] orphan検出にspot残高チェックを追加 (main wallet直接クエリ)

---

## 根本原因3: 指値→market二重約定 (unknownOid)

### 経緯
1. BTC perp指値注文 → Hyperliquid返答: `{"status":"unknownOid"}`
2. order_executor: エラーと判断 → market fallback実行
3. 実は指値も約定していた → perp 0.00056 × 2 = 0.00112
4. Spot側も同様: 指値timeout(filled=0) → market fallback → 実は指値も約定 → 2倍
5. BTC $38 → $76 の二重ポジション

### 修正方針
- [ ] unknownOid時: marketに行く前にfetch_positions()で確認
- [ ] limit timeout時: filled=0でもcancel→fetch→実際のfilled確認
- [ ] 全般: market fallback前に必ず「既存ポジション量」を確認

---

## 根本原因4: Agent key Spot残高不可視

### 問題
- `fetch_balance({"type":"spot"})` → ENA残高=0 (実際は2042 ENA存在)
- `create_order("ENA/USDC", "sell", 2042)` → 成功 (売れる!)
- 見えないけど売れる = 残高チェックベースの全ロジックが破綻

### 影響範囲
- rollbackの残高確認 → 0を見て「売るものなし」→ spot放置
- reconcileのspot確認 → 0を見てorphan検出できず
- zombie検出のspot確認 → 同上

### 修正方針
- [ ] spot残高チェックをmain wallet address直接クエリに変更
- [ ] 代替: rollbackはbalance非依存、order filled量で判断

---

## 時系列

| 時刻 | 出来事 |
|------|--------|
| ~3/3 15:00 | HYPE初回entry |
| ~3/3-3/5 | HYPE/ETHが頻繁にentry/exit繰り返し (churning) |
| 3/5 06:57-18:25 | ETH 50+回のentry/exit → 手数料蓄積 |
| 3/6 12:16 | HYPE re-entry (現在のポジション) |
| 3/6 14:53 | ETH最後のentry |
| 3/6 14:55 | **ENA entry: 2690 ENA spot買い + 2044 perp short** |
| 3/6 14:56 | **ENA rollback: perp closed, spotは646ENAだけ売却** |
| 3/6 ~15:00-20:20 | BTCがentry/exit繰り返し (30+ 回転) |
| 3/7 01:13 | BTC cleanup (手動) |
| 3/7 01:16 | BTC re-entry → **二重約定** (0.00112) |
| 3/7 01:28 | BTC半分売却 (手動修正) |
| 3/7 01:45 | **ENA 2042枚 手動売却** (-$9.51実現損) |

---

## 教訓

1. **FR収穫は長期保有が前提。10分ごとのentry/exitは戦略を破壊する**
2. **安全装置の感度 vs 再entry閾値のバランスが取れていない → oscillation**
3. **Agent keyのAPI制限は予測不能。balance=0でも実際は残高がある**
4. **unknownOid = 失敗ではない。市場注文前に必ず現状確認**
5. **rollbackは結果を検証すべき。「売ったつもり」は危険**

---

## 次のステップ

### 即座 (Phase 32)
1. **churning防止**: exit後の再entry cooldownを大幅延長
2. **--size 400に更新**: 実資本に合わせる
3. **fr_trend_decline閾値見直し**: 50%は敏感すぎ → 70%以上に?

### 中期
4. **order_executor unknownOid対策**: market前にposition確認
5. **rollback検証**: order filled量ベース (balance非依存)
6. **agent key spot残高問題の調査**: どの通貨で発生するか特定
