# Phase 34: パフォーマンスダッシュボード (Vault革命の基盤)

## 日付: 2026-03-07

## 実装内容

### 新規ファイル
1. **`scripts/performance_dashboard.py`** — 週次パフォーマンスレポート
   - bot_state.json読み取り専用 (一切書き込まない)
   - `calc_metrics()`: daily_income_historyから7日/30日平均, 年率, 勝率, 最大DD計算
   - `save_to_csv()`: data/performance_history.csvに週次追記 (90日制限なし)
   - `format_discord_message()`: Discord週次サマリー
   - `send_discord()`: funding_monitor.pyと同じSSL/certifiパターン
   - capital: equity_peak → allocated_usd合計のフォールバック
   - ゼロ除算回避, 空state対応, フィールド欠損対応

2. **`tests/test_performance_dashboard.py`** — 51件テスト
   - TestCalcMetrics: 28件 (空state, net_pnl, capital, avg, win_rate, DD, portfolio)
   - TestCalcMetricsRealistic: 1件 (14日間本番風データ)
   - TestSaveToCSV: 4件 (新規ファイル, 追記, カラム数, 精度)
   - TestFormatDiscordMessage: 3件 (全フィールド, コードブロック, 空state)
   - TestReadBotState: 3件 (正常読取, ファイルなし→exit, 読み取り専用)
   - TestSendDiscord: 3件 (webhook未設定, HTTP送信, ネットワークエラー)
   - TestMain: 2件 (統合テスト: CSV+Discord, 空state)
   - TestEdgeCases: 7件 (1日, 全マイナス, 大資本, フィールド欠損, 90日)

### 変更ファイル
- **CLAUDE.md**: クイックリファレンス, ディレクトリ構成, cron設定にダッシュボード追加

### 変更しなかったファイル
- src/bot/funding_harvester.py — 触らず
- src/bot/order_executor.py — 触らず
- data/bot_state.json — 読むだけ
- 既存テスト全て — 触らず

## テスト結果
- 新規: 51件全パス
- 全体: **723件全パス** (0 failures)
- 既存305件に影響なし

## CSV出力フォーマット
```
date,capital,net_pnl,gross_funding,total_fees,avg_7d,avg_30d,annual_rate_pct,num_holding,portfolio
```

## cron設定 (GCPデプロイ時に追加)
```
0 0 * * 0 cd /home/bot/funding-bot && PYTHONPATH=. python3 scripts/performance_dashboard.py >> /tmp/performance_dashboard.log 2>&1
```

## 次のステップ
- Phase 32+33+34をGCP VMにデプロイ
- cron追加 (週次レポート)
