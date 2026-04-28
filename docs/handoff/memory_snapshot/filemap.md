# ファイルマップ

## 設定
- `pyproject.toml` — 依存関係 (build-backend: setuptools.build_meta)
- `config/settings.yaml` — Bot設定 (mode, exchange, trading, risk, drawdown_stages)
- `config/strategies/technical.yaml` — 各戦略のパラメータ
- `.env.example` — APIキーテンプレート (bitFlyer, GMO, DeepSeek, Discord)
- `src/config/models.py` — Pydanticモデル (Settings, RiskConfig等)
- `src/config/loader.py` — YAMLローダー (deep merge対応)

## データ層 (src/data/)
- `models.py` — SQLAlchemy ORM (OHLCVRecord, TradeRecord, EquityCurve)
- `storage.py` — async SQLite (WAL mode, upsert_ohlcv, save_trade, save_equity)
- `collector.py` — OHLCVCollector (取引所→SQLite)
- `fear_greed.py` — FearGreedFetcher (Alternative.me)
- `news.py` — VADERAnalyzer + DeepSeekAnalyzer + NewsSentimentPipeline
- `social_velocity.py` — SocialVelocityTracker (Google Trends 速度+加速度)
- `long_short_ratio.py` — LongShortFetcher (Binance Futures)
- `onchain.py` — OnchainFetcher (DefiLlama), OnchainMetrics dataclass
- `vpin.py` — VPINCalculator (Bulk Volume Classification, scipy.stats.norm CDF)

## 戦略 (src/strategy/)
- `signal.py` — Signal dataclass (BUY/SELL/HOLD, confidence 0-1, score property)
- `base.py` — BaseStrategy ABC (name, generate_signal, min_bars_required)
- `indicators.py` — pandas-ta wrappers (動的BBカラム名対応)
- `trend_follow.py` — 戦略A (MA20/50 + MACD + ADX>25)
- `mean_reversion.py` — 戦略B (RSI<25/>75 + BB + volume)
- `sentiment_contrarian.py` — 戦略C (F&G + LongShort + News + Social)
- `onchain_fundamental.py` — 戦略D (NUPL + SOPR + MVRV + exchange flow + stablecoin)

## 防御 (src/defense/)
- `anomaly_detector.py` — Isolation Forest
- `entropy_filter.py` — シャノンエントロピー【独自】
- `strategy_decay.py` — ローリングシャープ比監視
- `signal_crowding.py` — 出来高スパイク検出【独自】
- `drawdown_guard.py` — 5段階DD防御 (DrawdownState)
- `trade_filter.py` — 統合フィルター (DD + 信頼度 + 日次損失 + 連敗冷却)

## レジーム (src/regime/)
- `hmm_detector.py` — GaussianHMM 4状態
- `memory_length.py` — 自己相関ラグ構造【独自】
- `timeframe_contradiction.py` — 6TF矛盾パターン【独自】
- `allocator.py` — 動的重み配分

## アンサンブル (src/ensemble/)
- `voter.py` — 加重投票 (min_agreement閾値)
- `confidence_gate.py` — 最終ゲート (全防御モジュール統合)

## リスク (src/risk/)
- `kelly_sizer.py` — ハーフケリー × 信頼度 × DD補正
- `stop_manager.py` — ATR×2 SL, ATR×2/×4 段階TP

## インテリジェンス (src/intelligence/)
- `propagation_delay.py` — 相互相関で伝播遅延推定【独自】
- `adversarial_test.py` — 4シナリオ生成【独自】

## コア
- `src/core/bot.py` — TradingBot (asyncioメインループ, tick cycle)
- `src/backtest/engine.py` — BacktestEngine (BacktestResult)

## 取引所
- `src/exchange/base.py` — ExchangeAdapter ABC + dataclasses
- `src/exchange/bitflyer.py` — BitFlyerAdapter (ccxt async, 300 req/min)
- `src/exchange/rate_limiter.py` — SlidingWindowRateLimiter

## 監視・デプロイ
- `src/monitoring/dashboard.py` — Streamlit (equity curve, candle, trades)
- `src/monitoring/discord_notifier.py` — Discord Webhook (trade/DD/daily/anomaly)
- `Dockerfile` — Python 3.11-slim, non-root (botuser)
- `docker-compose.yaml` — bot + dashboard(monitoring profile)
- `scripts/start.sh` — 起動スクリプト
- `scripts/crypto-bot.service` — systemdユニット
- `scripts/setup-oracle.sh` — Oracle Cloud初期設定
- `scripts/deploy-oracle.sh` — リモートデプロイ

## テスト (95件)
- `tests/unit/test_config.py` (7)
- `tests/unit/test_drawdown_guard.py` (8)
- `tests/unit/test_strategy.py` (7)
- `tests/unit/test_risk.py` (9)
- `tests/unit/test_backtest.py` (4)
- `tests/unit/test_phase2.py` (20)
- `tests/unit/test_phase3_4.py` (22)
- `tests/unit/test_phase5.py` (18)

## 既知の注意点
- bitFlyer は ccxt の fetchOHLCV 非対応 → CryptoCompare 無料API で代替実装済み
- macOS Python の SSL証明書問題 → certifi + ssl.create_default_context で対応済み
- greenlet が SQLAlchemy async に必須 → pyproject.toml に追加済み
- pandas-ta BBカラム名がバージョンで変わる → indicators.py で動的検出済み
- Kelly基準: エッジなし(win50%/RR1:1) → 0を返す (正しい動作)
- Anomaly Detector: threshold -0.3 は通常データでも発火しうる → テストでは -0.5 使用
- Strategy Decay: 同一リターンだとstd=0でSharpe=0 → ノイズ必要
