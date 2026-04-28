# Phase 30+30.5: 資本効率革命 + FR駆動ローテーション

## 日付: 2026-03-06

## Phase 30: 資本効率革命 (3項目)

### 変更1: FR二重割引排除 (+1~3%/年)
- `_enter_position()`: `position_size = allocated * spot_ratio * fr_multiplier` → `position_size = allocated * spot_ratio`
- `_get_effective_weights()`: Step 2フィルタからcoin_fr_mult除去
- fr_multiplier <= 0のentry gateは維持

### 変更2: entry_lookback 168→72→24
- BotConfig: 168→72 (Phase 30), 72→24 (Phase 30.5)

### 変更3: fetch_order NoneTypeガード
- `order_executor.py`: ポーリングループのfetch_order None → continue
- キャンセル後のfetch_order None → market fallback (全額)

## Phase 30.5: FR駆動ローテーション革命 (8項目)

### 変更4: min_hold_hours 168→48
### 変更5: _should_rotate()完全書き直し
- FR差>5%→即ローテ (min_hold無視)
- 通常→round_trip_cost=0.06×3比較
- _last_scan_frを使用 (歴史平均ではなくスキャン最新FR)

### 変更6: _check_rebalance_opportunity()→アクショントリガー
- 通知のみ→臨時スキャン+ローテーションをトリガー

### 変更7: Early scan間隔 48h→1h
### 変更8: ポートフォリオ永続化
- _save_state(): active_portfolio追加
- _load_state(): 復元 + PositionConfig再構築

### 変更9: IDLE通貨はglobal_fr_history優先
- _get_fr_history(): HOLDING→pos.funding_history, IDLE→global_fr_history
- 理由: pos.funding_historyはexit後に更新停止→古いデータで判断が歪む

### 変更10: FR lookback統一 (24h)
- _get_per_coin_fr_multiplier: ハードコード72→entry_lookback
- _coin_avg_fr_annual: ハードコード72→entry_lookback

## デプロイ中の問題5件
1. --size 400→100修正 (サービスファイル不整合)
2. equity_peak DD誤検知 (400→100変更)
3. ポートフォリオ非永続化 (restart時にデフォルト復帰)
4. IDLE通貨の古いFR (BTC -2.9%/年 vs 実際11%/年)
5. 72h lookback統一漏れ (_coin_avg_fr_annualがハードコード)

## ライブ確認結果
- ETH自動退出 (fr_trend_decline_57%) ✓
- FR gap override rotation (ETH 5.7%→BTC+HYPE 11%) ✓
- ENA入場失敗 (spot板薄→68%ミスマッチ→ロールバック) ✓
- BTC入場成功 (FR 11%, $48.36) ✓
- ポートフォリオ永続化確認 ([BTC,HYPE]がrestart後に復元) ✓

## テスト
- 635件全パス (複数テスト更新)

## コミット
- 5d2957d Phase 30 (FR二重割引+lookback+NoneType)
- 80d95f6 Phase 30.5 (FR駆動ローテーション)
- 5591101 Scan FR fix (_last_scan_fr)
- 8b87091 Portfolio persistence
- f1e3647 IDLE coin stale FR fix
- ceed11d entry_lookback 72→24
- 2e2157a FR lookback alignment (24h統一)
