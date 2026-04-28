# アーキテクチャ仕様書 (Phase 14-20: デルタニュートラルFR収穫)

## 設計思想
「負けないこと」が最優先。方向予測を完全放棄。Perp市場のリテールロングバイアスから構造的プレミアム(FR)を刈り取るデルタニュートラル戦略。パラメータ数0 = 過学習不可能。

## 戦略: デルタニュートラルFR収穫
- Long Spot + Short Perp → 価格変動をヘッジ
- FR(ファンディングレート)がlonger pays shorterの構造的バイアスを利用
- 80/20 Spot/Perp配分 → 4xレバレッジ、清算距離25%
- 期待リターン: ベア市場~5%/年、ブル市場~16%/年

## Bot構成
```
src/bot/
├── funding_harvester.py    ← 本体 (2層構造)
│   ├── Layer 1: FR収穫 (1h毎FR記録、エントリー/退出判定)
│   └── Layer 2: AI週次ローテーション (DeepSeek通貨選択)
├── aggressive_harvester.py ← FR Turbo (スパイク検出型、独立資本)
├── ai_engine.py            ← DeepSeek API通貨選択
├── order_executor.py       ← 共有注文実行 (成行/指値)
└── rate_limiter.py         ← API累積制限監視
```

### funding_harvester.py クラス構成
- `PositionConfig` — 通貨設定 (base, spot/perp/lst_symbol, weight)
- `BotConfig` — Bot全体設定 (ポートフォリオ, レバレッジ, 安全装置, 指値)
- `PositionStatus` — 通貨ごとの状態 (state, amounts, funding, fees)
- `BotStatus` — Bot全体の状態 (positions dict, cumulative funding, fees)
- `FundingHarvesterBot` — Bot本体

### 主要メソッド
- `initialize()` — 取引所接続 + 状態復元 + ポジション照合
- `_reconcile_positions()` — 取引所の実ポジションと同期 (Spot/Perp vault分離対応)
- `_should_be_in_position(base)` — 非対称lookback (入り500h/出72h)
- `_enter_position(pc)` — デルタニュートラルエントリー (LST自動フォールバック付)
- `_exit_position(pc)` — exit_okフラグパターンで安全退出
- `_check_safety(pc)` — FR退出/neg FR急速退出/basis乖離/basis spike
- `_check_portfolio_drawdown()` — 全体DD 3%超→全クローズ
- `_record_all_funding()` — 1h毎FR記録
- `run_forever()` — メインループ (2層)

### 安全装置 (Phase 20強化済)
| 装置 | 条件 | 動作 |
|------|------|------|
| FR撤退 | 72h平均FR < 0 | 自動クローズ |
| Neg FR急速退出 | 直近3回全negative | 即座にクローズ |
| Basis警告 | |basis| > 0.5% | Discord通知 |
| Basis緊急 | |basis| > 2.0% | 強制クローズ |
| Basis spike | 前回から1%以上変化 | 緊急クローズ |
| DD上限 | ポートフォリオDD > 3% | 全ポジションクローズ |
| Exit失敗保護 | exit注文失敗 | HOLDING復元 (孤児防止) |
| Spot/Perp乖離 | 10%超乖離 | 警告ログ |

### 取引所: Hyperliquid
- DEX、KYC不要、日本制限なし、保有コスト0
- **重要**: SpotボルトとPerpボルトが分離
  - `fetch_balance()` → Perpボルト (USDCのみ)
  - `fetch_balance({"type": "spot"})` → Spotボルト (トークン)
- FR: 1h毎 (8h rateの1/8ずつ)
- 成行注文にprice必須

### インフラ
- GCP VM / systemd (funding-harvester + aggressive-harvester)
- 状態永続化: data/bot_state.json
- 通知: Discord Webhook
- AI: DeepSeek API (週次ローテーション)
- テスト: pytest 477件
