# セッション記録: Phase 22 徹底監査

## セッション名
**「Phase 22: 徹底監査13件修正 + Insufficient spot balance 根本解決」**

## 期間
2026-03-05

---

## 背景
Phase 21デプロイ後、HYPE/BTCの"Insufficient spot balance"exit失敗が5分毎に繰り返されていた。
ユーザーの指示:「徹底的に今の懸念点とか不安なポイント、重大なミスなどを徹底的に洗い出して」

## 発見した問題 (13件)

### CRITICAL (2件)
| ID | 問題 | 修正内容 |
|----|------|---------|
| C1 | exit前にSpot残高を再取得しない → "Insufficient spot balance"無限ループ | `fetch_balance({"type":"spot"})` でfree残高取得、min()クリップ、0なら skip + **spot sell失敗時もperp close続行 (独立try-except)** |
| C2 | systemd .serviceにEnvironmentFile欠落 | `EnvironmentFile=/home/bot/funding-bot/.env` 追加 |

### HIGH (5件)
| ID | 問題 | 修正内容 |
|----|------|---------|
| H1 | reconcile Spot vault fetch失敗が無音 | 警告ログ追加 + perp有+spot=0時の警告 |
| H2 | equity_peak初期値0 → DD検出無効 | `initialize()` でequity_peak初期化 |
| H3 | ENTERING/EXITING状態がメインループで未処理 → 永久スタック | amounts有→HOLDING、なし→IDLE に復帰 |
| H4 | order_executor指値cancel後のrace condition | cancel後sleep(0.5) + remaining確認 + fee合算 |
| H5 | fees_paid: entry `=` vs exit `+=` | 全箇所 `+=` に統一 |

### MEDIUM (6件)
| ID | 問題 | 修正内容 |
|----|------|---------|
| M1 | _check_fr_trend: avg_recent<0を検出しない | 条件 `avg_prev > 0` のみに緩和 |
| M2 | deploy.sh: rsyncでdata/が上書きされるリスク | `--exclude='data/'` に変更 |
| M3 | order_executor: synthetic response ログレベル低い | `awarning` → `aerror` |
| M4 | last_scan=0.0でスキャンタイミング狂い | `now - scan_interval_sec` にリセット |
| M5 | aggressive_harvester: ENTERING/EXITINGスタック未検出 | H3と同じパターン適用 |
| M6 | Spot vault fetch失敗時の警告なし | H1と同時修正 |

## ホットフィックス (デプロイ後に発見・追加修正)

### 問題: spot sell失敗時にperp closeが実行されない
C1修正デプロイ後のログで発見。spot sell の例外がperp close をブロックしていた。

### 修正内容
```python
# 修正前: spot sell と perp close が同じtryブロック
spot_order = await self._execute_order(exit_spot_sym, "sell", spot_sell_amount)
perp_order = await self._execute_order(pc.perp_symbol, "buy", pos.perp_amount)

# 修正後: spot sell を独立try-exceptで囲む
try:
    spot_order = await self._execute_order(exit_spot_sym, "sell", spot_sell_amount)
except Exception as spot_err:
    await logger.aerror("Spot sell failed — closing perp only", ...)
# perp close は必ず実行
perp_order = await self._execute_order(pc.perp_symbol, "buy", pos.perp_amount)
```

### balance取得の改善
```python
# 修正前: total (open ordersロック分を含む)
actual_spot = float(spot_bal.get(pc.base, {}).get("total", 0) or 0)

# 修正後: free優先 (open ordersを除外)
actual_spot = float(
    spot_bal.get(pc.base, {}).get("free", 0)
    or spot_bal.get(pc.base, {}).get("total", 0)
    or 0,
)
```

## デプロイ結果

### ログで確認した動作:
```
16:51:46 Safety exit triggered currency=HYPE reason=fr_trend_decline_193%
16:51:49 Limit order failed (Insufficient spot balance)
16:52:00 Spot sell failed — closing perp only (spot_amount=0.00585575)  ← NEW: spot失敗をキャッチ
16:52:15 Position exited currency=HYPE  ← ✅ perp close成功!

16:52:35 Safety exit triggered currency=BTC reason=fr_trend_decline_167%
16:52:38 Limit order failed (Insufficient spot balance)
16:52:49 Spot sell failed — closing perp only (spot_amount=9.4213e-06)
16:53:02 Position exited currency=BTC  ← ✅ perp close成功!
```

### 現在のポジション状態:
- HYPE: **IDLE** (exitした。dust 0.006 HYPE = $0.19 が残存)
- ETH: **HOLDING** (FR ann=+5.7%, 正常稼働)
- BTC: **IDLE** (exitした。dust 0.000009 BTC = $0 が残存)
- SOL: **IDLE** (entry失敗 — spot vaultにUSDC不足)

## テスト
- 新規追加: 13件 (Phase 22修正分)
- 全513件パス

## 修正ファイル一覧
| ファイル | 修正ID |
|---------|--------|
| `src/bot/funding_harvester.py` | C1, H1, H2, H3, H5, M1, M4, M6 + ホットフィックス |
| `src/bot/order_executor.py` | H4, M3 |
| `src/bot/aggressive_harvester.py` | C1, M5 + ホットフィックス |
| `scripts/deploy/funding-harvester.service` | C2 |
| `scripts/deploy/aggressive-harvester.service` | C2 |
| `scripts/deploy/deploy.sh` | M2 |
| `tests/test_funding_harvester.py` | 新規テスト13件 |

## ホットフィックス#2 (Phase 22 続き — 2026-03-05 深夜)

### 問題1: $10未満注文エラーの無限ループ
**症状**: Discord に "Order must have minimum value of 10 USDC" が18分毎に繰り返し
**原因**: FR size multiplier が低下 (0.43→0.24→0.086) → ポジションサイズが $10 未満に
- 例: BTC: $500 × 0.20 × 0.80 × 0.086 = $6.88 < $10
- entry前のサイズチェックがなかった
- entry失敗後のクールダウンがなかった

**修正**:
- `BotConfig.min_order_usd = 11.0` 追加
- `_enter_position()` に事前サイズチェック
- `PositionStatus.entry_cooldown_until` 追加 (失敗後1h)
- `aggressive_harvester.py` にも同様のmin_orderチェック
- テスト6件追加 (519件パス → 528件パス)

### 問題2: Perp close "Insufficient margin" → "Reduce only would increase position"
**症状**: HYPE/BTC perp close が "Insufficient margin to place order" で失敗し続ける
**根本原因 (3段階の障害連鎖)**:
1. **Synthetic response**: Phase 22 ホットフィックスのperp close で ccxt が None 返却 → order_executor がsynthetic responseを生成 → Bot は「closed成功」と認識したが、実際は注文未執行
2. **ポジション反転**: 後続の再デプロイでreconcile がperp positionを再検出 → exit試行 → BUY注文が実行され、shortがcloseされLONGが開いた (net effect: short 1.08 → long 1.08)
3. **side非対応**: reconcile/exitロジックがperp sideを確認しない → LONGなのにBUYで閉じようとして "Reduce only order would increase position" エラー

**修正内容**:
```
order_executor.py:
  - reduce_only パラメータ追加 (全注文パスに伝搬)
  - reduceOnly=True でHyperliquidがmargin不足でもclose許可

funding_harvester.py:
  - _execute_order() に reduce_only パラメータ追加
  - _exit_position(): perp close前に fetch_positions() でside確認
    - short → buy (従来)
    - long → sell (NEW: 反転ポジション対応)
    - なし → skip (既にclose済み)
  - reduceOnly=True でperp close
  - exit_cooldown_until 追加 (失敗後1h)
  - reconcile: perp side!=short を [error] ログ出力

aggressive_harvester.py:
  - 同様のside確認 + reduceOnly追加
```

**デプロイ結果**:
```
23:52:15 Reconciliation: perp is WRONG SIDE contracts=1.08 currency=HYPE side=long  ← NEW
23:52:15 Reconciliation: perp is WRONG SIDE contracts=0.00042 currency=BTC side=long  ← NEW
23:56:01 Perp is LONG (expected SHORT) — selling to close contracts=1.08 currency=HYPE
23:56:04 Limit order filled price=32.306 side=sell  ← HYPE LONG close成功 ✅
23:56:39 Perp is LONG (expected SHORT) — selling to close contracts=0.00042 currency=BTC
23:56:53 Position exited currency=BTC  ← BTC LONG close成功 ✅
```

### 最終ポジション状態:
- HYPE: **IDLE** (LONG close 完了)
- ETH: **HOLDING** (正常、short perp + long spot)
- BTC: **IDLE** (LONG close 完了)
- SOL: **IDLE** (spot vault USDC不足)
- Perp vault: $2.35 (ETH用に十分)

### テスト
- 新規追加: 9件 (Phase 22 hotfix #2)
- 全528件パス

### 教訓 (再発防止)
1. **synthetic response は危険** — ccxt None返却時のフォールバックが「成功」と見なされ、実際は未執行
2. **perp sideを常に確認** — delta-neutralではshortが前提だが、close失敗→再試行でLONGに反転するケースがある
3. **reduceOnly=True** — perp close には必ず設定。margin不足でもclose可能
4. **exit失敗にもクールダウン** — entry同様、exit失敗後のリトライ爆撃を防止
5. **reconcileはside-aware** — wrong sideポジションを即座に検出・警告

## 残課題
- SOL: spot vaultにUSDCを転送すればentry可能
- HYPE/BTC dust残高の清掃 (手動 or dust変換)
- kHYPE上場時のreconcile対応
- synthetic response問題の根本解決 (ccxt None返却時に注文確認ロジック追加検討)
