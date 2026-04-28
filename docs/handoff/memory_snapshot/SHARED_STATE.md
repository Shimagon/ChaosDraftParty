# 全セッション共有ボード
**最終更新: 2026-03-07 — Phase 32 Churning防止 + デプロイガード 実装済(デプロイ待ち)**

---

## 現在のプロジェクト状態

**仮想通貨デルタニュートラルFR収穫Bot — $100 ライブ稼働中 (GCP VM)**

| 項目 | 状態 |
|------|------|
| Bot本体 | `funding_harvester.py` — Phase 32 **実装済(デプロイ待ち)** |
| テスト | **296件全パス** |
| 最新Phase | **Phase 32 (Churning防止 + デプロイガード)** |
| ポートフォリオ | **[BTC, HYPE]** — FR駆動で自動選択 |
| BTC | **HOLDING** |
| HYPE | **HOLDING** |
| 資金 | ~$387 (--size 100 → 400に変更予定) |
| 既知問題 | Phase 32デプロイ待ち, --size 400に変更必要 |

---

## セッション記録 (時系列)

### Phase 1-13: テクニカル+ML時代 (過去)
- 6層アーキテクチャ、4戦略、HMM、Isolation Forest等
- **結論: IS+31% → OOS-5%。方向予測は不可能。全廃棄**
- 記録: `architecture.md`, `architecture_phase1_13.md`, `filemap.md`

### Phase 14: FR収穫戦略発見
- デルタニュートラル (Long Spot + Short Perp)
- **+5.63%/年, DD 0.41%, パラメータ0 = 過学習不可能**

### Phase 15-16: Bot実装 + ディープリサーチ改修
- Grid MM廃止 (API枯渇+手数料負け)
- AIローテーション: 4h→週次 (手数料$81→$12/月)

### Phase 18-19: FR Turbo + GCPデプロイ
- `aggressive_harvester.py` 新規作成 → 後に停止
- `order_executor.py` 共有化
- GCP e2-micro VM構築、systemdサービス化

### Phase 20-21: セキュリティ監査 + 攻め施策
- CRITICAL 3件、HIGH 5件修正
- SOL追加 (4通貨)、exit_lookback 48h、FR傾き退出

### Phase 22: 徹底監査
- 13件修正 + ホットフィックス#1+#2

### Phase 23: 限界突破 — 資本速度革命
- 小資本集中モード、FR加重配分、通貨別FR倍率
- Perp-First Entry + 自動ロールバック
- ホワイトリスト5→13通貨

### Phase 24: バグ修正3件 + 日次ダッシュボード
- Portfolio FR計算HOLDING限定、set_leverage修正

### Phase 25: HLP Vault
- アイドル資本をHLP Vault → 10-17% APY
- Agent key制限で実質無効 (main key必要)

### Phase 26: グローバルFR収集
- 30通貨のFRを毎時1 APIコールで記録
- ホワイトリスト13→30通貨

### Phase 27: FR収穫最適化
- グローバルFR機会検出、エントリークールダウン適応化

### Phase 28: FR収穫革命
- Entry Spot指値化、閾値最適化、lookback短縮

### Phase 29: 構造的ボトルネック破壊
- 流動性フィルタ比例化、FR純粋ランキング
- スキャン間隔3日→1日、リバランス機会検出

### Phase 30: 資本効率革命 ★
- **FR二重割引排除**: position_sizeからfr_multiplier削除 (+1-3%/年)
- **entry_lookback**: 168→72→24h
- **NoneTypeガード**: fetch_order None→continue/market fallback

### Phase 30.5: FR駆動ローテーション革命 ★
- **min_hold_hours**: 168→48h
- **_should_rotate()完全書き直し**: FR差>5%→即ローテ
- **_check_rebalance_opportunity()**: 通知→臨時スキャン+ローテーショントリガー
- **Early scan間隔**: 48h→1h
- **ポートフォリオ永続化**: active_portfolioをbot_state.jsonに保存
- **IDLE通貨はglobal_fr_history優先**
- **FR lookback統一**: 全FR関数がentry_lookback(24h)を使用
- **デプロイ中5件の問題を修復** (詳細は`session_phase30.md`)
- **ETH→BTC+HYPEにFR駆動ローテーション成功**
- 記録: `session_phase30.md`

### Phase 31: ポジション健全性革命 ★
- **HYPE $0.01の根本原因**: reconcileが dust/imbalance位置を無検証で HOLDING 復元
- **reconcileバリデーション**: dust filter + spot/perp不均衡チェック + allocated_usd推定
- **orphan検出**: ポートフォリオ外の取引所ポジション → ERROR + Discord通知
- **zombie検出**: spot/perp < min_order_usd*0.5 → 自動exit (emergency market)
- **_avg_funding修正**: IDLE通貨がglobal_fr_historyを使用するように
- **テスト18件追加** (653件全pass)
- 記録: `session_phase31.md`

---

### Phase 32: Churning防止 + デプロイガード ★ NEW
- **成功exit後4hクールダウン**: entry_cooldown_untilを成功時も設定
- **日次ラウンドトリップ上限**: 1通貨2回転/日 (daily_round_trips追跡)
- **デプロイ猶予期間**: 起動後1hは新規entry禁止
- **fr_trend_decline**: 50%→70% (感度低下)
- **CLAUDE.mdにデプロイルール追加**: 閾値変更時の影響確認必須
- **インシデントレポート**: `incident_report_phase31.md`
- テスト11件追加 (計296件)

---

## 次にやること

| # | 施策 | 詳細 |
|---|------|------|
| 1 | **Phase 32デプロイ** | GCP VMに反映 + --size 400に変更 |
| 2 | **order_executor修正** | unknownOid→market前にposition確認, rollback検証 |
| 3 | **Main Wallet Key** | Agent keyだとHLP Vault不可→main key変更 |
| 4 | **クロス取引所** | Binance spot + HL perp (ENA等の薄板通貨対応) |

---

## 重要な技術的発見 (全セッション共有)

### FR駆動ローテーション (Phase 30.5 NEW)
- `_should_rotate()`: FR差>5%→即ローテ (min_hold無視), 通常→round_trip_cost×3
- `_check_rebalance_opportunity()`: IDLE高FR検出→臨時スキャン→ローテーション
- `active_portfolio`がbot_state.jsonに永続化

### IDLE通貨のFR履歴 (Phase 30.5 NEW)
```python
# _get_fr_history(): HOLDING→pos.funding_history, IDLE→global_fr_history優先
# pos.funding_historyはexit後に更新停止するため、IDLEでは古いデータになる
```

### FR lookback統一 (Phase 30.5 NEW)
```python
# _should_be_in_position, _get_per_coin_fr_multiplier, _coin_avg_fr_annual
# 全てself._config.entry_lookback (24h) を使用
```

### 通貨別FR倍率 (Phase 23)
```python
def _get_per_coin_fr_multiplier(self, base: str) -> float:
    # entry_lookback期間のFRから0-1線形補間
    # entry gate (0以下なら弾く) として機能
```

### Hyperliquid Spot/Perp Vault分離
```python
await exchange.fetch_balance()               # Perpボルトのみ
await exchange.fetch_balance({"type": "spot"}) # Spotボルト
```

### GCPデプロイ方法
```bash
gcloud compute scp --zone=us-central1-a file1.py file2.py funding-bot:/tmp/
gcloud compute ssh funding-bot --zone=us-central1-a --command='sudo cp /tmp/*.py /home/bot/...'
# ※ --recurse はネストするので個別ファイル転送
# ※ VM上にgit無し
```

---

## GCPインフラ
- プロジェクト: `project-f04accdc-ccc6-4bc7-992`
- VM: `funding-bot` / zone: `us-central1-a` / e2-micro (無料枠)
- IP: 130.211.126.155
- systemd: `funding-harvester.service`
- EnvironmentFile=/home/bot/funding-bot/.env

---

## ルール (全セッション共通)

1. **過学習禁止**: パラメータを増やす提案は即却下
2. **方向予測禁止**: 「上がりそう/下がりそう」に基づく判断を一切入れない
3. **秘密鍵に触らない**: `.env`の秘密鍵を表示・コピー・変更しない
4. **テスト必須**: コード変更後は `.venv/bin/pytest tests/ -v`
5. **記録ファイルは絶対消さない**: 新しいファイルを追加する
6. **日本語**: ユーザーとのコミュニケーションは日本語
7. **確認不要**: 全部一気に、自律的に判断・実行

---

## メモリフォルダの読み方

| ファイル | 内容 | 読むべきタイミング |
|---------|------|------------------|
| `MEMORY.md` | 最新サマリー (常にシステムにロード) | 自動ロード |
| `SHARED_STATE.md` | **このファイル** | セッション開始時 |
| `session_phase30.md` | Phase 30+30.5詳細 | **最新の実装詳細** |
| `session_phase29.md` | Phase 29詳細 | 構造改善の詳細 |
| `session_phase28.md` | Phase 28詳細 | 閾値最適化 |
| `session_phase27.md` | Phase 27詳細 | FR最適化 |
| `session_phase26.md` | Phase 26詳細 | グローバルFR |
| `session_phase25.md` | Phase 25詳細 | HLP Vault |
| `session_phase24.md` | Phase 24詳細 | バグ修正 |
| `session_phase23.md` | Phase 23詳細 | 限界突破 |
| `session_phase22.md` | Phase 22詳細 | 徹底監査 |
| `session_phase20_21.md` | Phase 20-21詳細 | セキュリティ+攻め |
| `session_phase18_19.md` | Phase 18-19詳細 | GCPデプロイ |
