# アーキテクチャ仕様書 (Phase 1-13: テクニカル+ML時代)

## 設計思想
「いつトレードしないか？」がデフォルト。ルネサンス・テクノロジーズ参考。
複数の無相関戦略を並列実行し、AIでしかできない多次元判断で「負けない」構造を作る。

## 6層アーキテクチャ

### Layer 0: 防御システム（最重要）
| モジュール | ファイル | 機能 |
|-----------|---------|------|
| 異常検知 | `src/defense/anomaly_detector.py` | Isolation Forest。市場が「いつもと違う」→全停止 |
| エントロピーフィルター【独自】 | `src/defense/entropy_filter.py` | シャノンエントロピーでランダム性判定。ランダム→何もしない |
| 戦略劣化検出 | `src/defense/strategy_decay.py` | ローリングシャープ比。30%低下→重み自動縮小 |
| シグナル混雑度【独自】 | `src/defense/signal_crowding.py` | 出来高スパイクで他Bot動作を推定。混雑→重み半減 |
| 段階的DD防御 | `src/defense/drawdown_guard.py` | 5段階: 0%→5%(50%)→10%(25%)→15%(停止)→20%(全決済) |
| トレードフィルター | `src/defense/trade_filter.py` | 全防御の統合判断。1つでも危険→HOLD |

### Layer 1: 情報収集（全て無料、並列取得）
| データ | ファイル | ソース |
|--------|---------|--------|
| OHLCV | `src/data/collector.py` | ccxt (bitFlyer) |
| VPIN | `src/data/vpin.py` | WebSocket生データから自前計算 (Bulk Volume Classification) |
| オンチェーン | `src/data/onchain.py` | DefiLlama (stablecoin supply) |
| ニュース感情 | `src/data/news.py` | RSS + VADER(即時) + DeepSeek V3(高精度) |
| SNS速度 | `src/data/social_velocity.py` | Google Trends (加速度=変化率の変化率) |
| Fear & Greed | `src/data/fear_greed.py` | Alternative.me API |
| ロンショ比率 | `src/data/long_short_ratio.py` | Binance Futures |

### Layer 2: レジーム検出
| モジュール | ファイル | 機能 |
|-----------|---------|------|
| HMM検出 | `src/regime/hmm_detector.py` | GaussianHMM 4状態 (Trend/Range/HighVol/Crisis) |
| メモリ長【独自】 | `src/regime/memory_length.py` | 自己相関で戦略有効度を推定 |
| TF矛盾【独自】 | `src/regime/timeframe_contradiction.py` | 6時間足の一致/矛盾パターン (64通り) |
| 重み配分 | `src/regime/allocator.py` | レジーム+メモリ長+エントロピーで動的配分 |

### Layer 3: 4つの無相関戦略
| 戦略 | ファイル | ロジック |
|------|---------|---------|
| A: トレンドフォロー | `src/strategy/trend_follow.py` | MA crossover(20/50) + MACD + ADXフィルター(>25) |
| B: ミーンリバージョン | `src/strategy/mean_reversion.py` | RSI極値(<25/>75) + BB逸脱 + 出来高確認 |
| C: センチメント逆張り | `src/strategy/sentiment_contrarian.py` | F&G<20→買い, >80→売り, ロンショ極値逆張り |
| D: オンチェーン | `src/strategy/onchain_fundamental.py` | NUPL>0.75→売り, SOPR<1→買い, MVRV<1→買い |

### Layer 4: アンサンブル信頼度ゲート
| モジュール | ファイル | 機能 |
|-----------|---------|------|
| 投票 | `src/ensemble/voter.py` | 加重投票。意見一致→実行、割れ→見送り |
| ゲート | `src/ensemble/confidence_gate.py` | 異常/エントロピー/TF矛盾/混雑/劣化/危機の最終チェック |

### Layer 5: リスク管理
| モジュール | ファイル | 機能 |
|-----------|---------|------|
| サイジング | `src/risk/kelly_sizer.py` | ハーフケリー × 信頼度 × DD補正 |
| SL/TP | `src/risk/stop_manager.py` | ATR×2 SL, ATR×2 TP1(50%決済), ATR×4 TP2(残り) |

## AI独自モジュール (6つ)
1. **エントロピーフィルター** — 市場がランダムか規則的かを数学的判定
2. **クロスTF矛盾マッピング** — 6時間足の64パターン分析
3. **情報伝播遅延** (`src/intelligence/propagation_delay.py`) — S&P500→BTC等の遅延を搾取
4. **シグナル混雑度** — 他Botが同じ信号で動いた痕跡を検出
5. **マーケットメモリ長** — 自己相関で戦略A/Bの有効度を推定
6. **敵対的自己テスト** (`src/intelligence/adversarial_test.py`) — SL狩り/出来高操作/相関崩壊/フラッシュクラッシュ

## インフラ
| 項目 | 詳細 |
|------|------|
| メインBot | `src/core/bot.py` — asyncioループ、60秒間隔 |
| バックテスト | `src/backtest/engine.py` — 全パイプライン再現 |
| DB | SQLite (WAL mode) — `data/db/trading.db` |
| 設定 | Pydantic + YAML (`config/settings.yaml`, `config/strategies/technical.yaml`) |
| 取引所 | bitFlyer (primary)。ABC基底 `src/exchange/base.py` |
| Docker | `Dockerfile` + `docker-compose.yaml` (bot + dashboard) |
| 監視 | Streamlit (port 8501) + Discord Webhook |
| デプロイ | Oracle Cloud Always Free (4CPU/24GB ARM) |
| テスト | pytest 95件、全パス |

## コスト
月額 ¥3〜45 (DeepSeek API のみ)。VPS/データソース/通知は全て無料。

## 結論 (Phase 11-13)
IS+31% → OOS-5%。全戦略エッジなし。方向予測を完全放棄し、Phase 14でFR収穫に転換。
