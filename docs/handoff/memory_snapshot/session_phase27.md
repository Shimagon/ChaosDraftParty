# Phase 27: FR収穫最適化 — 4つの改善

**日付**: 2026-03-06
**状態**: デプロイ済み・稼働確認完了

---

## 実装内容

### 1. グローバルFR機会検出 (最も効果大)
- `_check_fr_opportunity()` 新設
- 非ポートフォリオ通貨の `global_fr_history` をチェック
- 72h平均FR年率が `fr_full_size_annual` (5%) 以上 → 臨時スキャントリガー
- 既存の `_check_fr_deterioration()` と同じ48h最短間隔ガードを共有
- **ライブ確認**: デプロイ直後に「非ポートフォリオ通貨にFR機会検出」ログ確認

### 2. effective_weightsフォールバック修正 (バグ修正)
- `_enter_position()` L1025-1026
- 旧: `effective_weights.get(pc.base, pc.weight)` → フィルタ済み通貨がデフォルトweightで進む
- 新: `effective_weights.get(pc.base)` → None or ≤0 で即IDLE復帰
- 不要なvault check + cooldownを回避

### 3. エントリークールダウン適応化
- `PositionStatus.entry_fail_count` 追加
- 固定1h → 指数バックオフ: 10min → 30min → 60min (キャップ)
- 計算式: `min(10 * 3^(n-1), 60)` minutes
- エントリー成功時にリセット (`entry_fail_count = 0`)
- vault失敗 (L1053) + 例外キャッチ (L1289) の両箇所を適応化
- `_save_state()` / `_load_state()` で永続化

### 4. 複利閾値緩和
- `min_compound_usd`: $1.0 → $0.50
- $100資本で62日→31日で複利開始

---

## テスト結果
- 633件全パス (620 + 13 Phase 27テスト)
- 新テスト: TestPhase27Optimizations (13件)
  - _check_fr_opportunity: 5件 (no data, portfolio excluded, high/low FR, main loop)
  - effective_weights fallback: 2件 (filtered returns none, code check)
  - adaptive backoff: 4件 (field, persisted, formula, code check, no fixed 1h)
  - compound threshold: 1件
- 既存テスト1件更新: test_auto_compound_defaults (min_compound_usd 1.0→0.50)

## ライブ確認
- GCP VM正常起動
- `Early scan triggered  reason=非ポートフォリオ通貨にFR機会検出` ログ確認
- ETH HOLDING継続 (FR ann=+5.9%)
- Global FR recorded: coins=29 total_tracked=29
