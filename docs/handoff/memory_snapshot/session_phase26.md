# Phase 26: グローバルFR収集 — 30通貨常時監視

**日付**: 2026-03-06
**状態**: デプロイ済み・稼働確認完了

---

## 実装内容

### 1. グローバルFR履歴ストア
- `BotStatus.global_fr_history: dict[str, deque]` — 全ホワイトリスト通貨のFR履歴
- maxlen=2160 (90日分)
- `_save_state()` / `_load_state()` で永続化

### 2. 一括API取得
- `_record_all_funding()` を完全リライト
- 旧: 4通貨 × 個別 `fetch_funding_rate(symbol)` = 4 APIコール/h
- 新: `fetch_funding_rates()` 1コールで全通貨取得 → 30通貨記録
- APIコスト: 実質ゼロ増加 (4→1に削減)

### 3. 起動時ブートストラップ
- `_bootstrap_global_fr_history()`: データ100h未満の通貨に500h分を補完
- `fetch_funding_rate_history(symbol, limit=500)` で取得
- 初回起動でも即座に500h分のデータを持てる
- ライブ確認: 29通貨ブートストラップ成功

### 4. フォールバックパターン
- `_get_fr_history(base)`: pos.funding_history → global_fr_history のフォールバック
- `_should_be_in_position()`: globalデータでエントリー判定可能
- `_get_per_coin_fr_multiplier()`: globalデータでFR倍率計算可能
- `_coin_avg_fr_annual()`: globalデータでFR年率計算可能
- HOLDING中の通貨は常にpos.funding_historyを優先 (リアルタイム)

### 5. ホワイトリスト拡張
- 13→30通貨に拡張
- 追加: BNB, NEAR, ADA, DOT, UNI, MATIC, ATOM, FIL, LTC, APT, SEI, TIA, INJ, WLD, ENA, ONDO, RENDER
- 流動性フィルタが不適格通貨を自動排除するため安全

### 6. スキャン高速化
- `scan_interval_sec`: 604800 (7日) → 259200 (3日)
- FR機会の検出速度2倍以上

### 7. 起動メッセージ
- Discord通知に「グローバルFR追跡: 29/30通貨」表示追加

---

## 技術メモ

### fetch_funding_rates() の仕様
- 引数なしで全通貨のFRを1回のAPIコールで返す
- 戻り値: `{ "ETH/USDC:USDC": {"fundingRate": 0.000125, ...}, ... }`
- 4コール→1コールに削減しつつ30通貨カバー

### ブートストラップの動作
- 起動時に30通貨を順番に `fetch_funding_rate_history(limit=500)` で取得
- 約60秒で完了 (API rate limit考慮)
- データが100h以上ある通貨はスキップ (bot_state.jsonから復元済み)
- 29/30通貨成功 (1通貨は取引所に存在しない可能性)

### フォールバックの安全性
- lookback 500h は一切変更なし
- 安全装置13層はそのまま
- データが「増える」だけなので安全性は現状以上

---

## テスト結果
- 620件全パス (605 + 15 Phase 26テスト)
- 既存テスト2件を更新 (ホワイトリスト数, スキャン間隔)

## ライブ確認
- GCP VM上で正常稼働確認
- 29通貨FR履歴ブートストラップ完了
- 毎時29通貨のFR記録確認 (`Global FR recorded coins=29 total_tracked=29`)
- ETH HOLDING継続 (FR ann=+5.9%)
