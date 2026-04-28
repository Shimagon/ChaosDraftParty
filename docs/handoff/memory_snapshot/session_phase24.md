# Phase 24: バグ修正3件 + 日次収益ダッシュボード

**日付**: 2026-03-05
**結果**: 584件全パス → GCPデプロイ完了

---

## 発見されたバグ (Discord異常検知アラートから発覚)

### Bug 1: Portfolio FR計算がIDLE通貨を含んでいた (CRITICAL)
- **症状**: "Portfolio FR negative: avg -0.5%/年. All positions closing."
- **原因**: `_calc_portfolio_avg_fr_annual()` が全通貨 (IDLE含む) のFRを平均
  - ETH: HOLDING, FR +5.8%/年 → 稼働中
  - HYPE/BTC/SOL: IDLE, FR負 → 平均を-0.5%に引き下げ
  - → ETH (唯一の収益源) が誤退出される
- **修正**: `pos.state != BotState.HOLDING` のフィルタ追加
- **テスト追加**: `test_health_idle_negative_ignored` — IDLE負FRが無視されることを検証

### Bug 2: Exit失敗 "Reduce only would increase position"
- **症状**: perpクローズ時に "Reduce only order would increase position" エラー
- **原因**: ポジションが存在しない状態で `buy` + `reduceOnly=True` → エラー
  - `fetch_positions()` 例外 → デフォルト `perp_close_side = "buy"` のまま
- **修正**: perp close全体をtry-exceptで囲み、"reduce only" + "increase position" を含むエラーは「既にクローズ済み」として正常処理

### Bug 3: set_leverage 422エラー (根本原因判明)
- **症状**: "Cross position does not have sufficient margin available to decrease leverage"
- **原因**: 既存ポジション ($80, 20x) のレバ下げに$20マージンが必要だがvaultに$8しかない
  - int/float問題ではなかった (Phase 23で誤診断)
- **修正**: `fetch_positions()` で既存ポジション確認 → あればスキップ、なければset_leverage

---

## 新機能: 日次収益ダッシュボード

### BotStatusに追加したフィールド
- `daily_income_history`: 直近90日分の `{"date", "income", "capital"}`
- `last_daily_snapshot_funding`: 前回スナップショット時の累計純利益

### `_calculate_daily_metrics()` 新メソッド
- 今日の収益 (前回スナップショットからの差分)
- 7日平均/30日平均
- 年率換算 (7日平均ベース)
- $1/日到達に必要な資本 = $36,500 / 年率%

### `_send_daily_summary()` 拡張
Discord通知フォーマット:
```
--- Day N Revenue Dashboard ---
Today: $0.0160
7-day avg: $0.0160/day
Capital: $100.00
Net P&L: $0.1120
Fees: $0.0000
Portfolio FR: 5.8%/yr
Annual rate: 5.8%
$1/day target: need $6,293
  ETH: FR 5.8%/yr, earned $0.0034
```

---

## テスト結果
- 修正前: 573件パス, 4件失敗 (FR計算がIDLE含むため)
- 修正後: **584件全パス** (新テスト11件追加)
- 追加テスト:
  - `TestPortfolioFRHealth.test_health_idle_negative_ignored` — IDLE負FR除外
  - `TestReduceOnlyErrorHandling` — reduce onlyエラーハンドリング
  - `TestSetLeverageExistingPosition` — 既存ポジション時スキップ
  - `TestDailyRevenueDashboard` — 日次メトリクス計算 (6件)

---

## デプロイ
- `gcloud compute scp` → `/tmp/` → `sudo cp` → `sudo systemctl restart`
- Bot起動ログ:
  - "State restored from disk" ✓
  - "Reconciliation: found untracked position, recovering" (ETH short 0.1125) ✓
  - "Health check: active_positions=1, ETH FR ann=+5.8%" ✓
  - Portfolio FR negativeエラーなし ✓
  - set_leverage 422エラーなし ✓
