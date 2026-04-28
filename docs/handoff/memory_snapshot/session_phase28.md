# Phase 28: FR収穫革命 — 手数料半減+閾値最適化

## 日付: 2026-03-06

## 実装内容 (10項目)

### 柱1: Entry Spot指値化 — 手数料半減
- Entry spotを`use_limit=False`→`use_limit=use_limit`に変更
- 指値timeout: 10s→30s (Entry時は急がない)
- 効果: Round trip 0.12%→0.06% (全指値fill時)
- リスクゼロ: timeout→market fallback

### 柱2: 閾値最適化
- `min_entry_fr_annual`: 1.5→1.0% (指値化で損益分岐22日)
- `fr_full_size_annual`: 5.0→3.0% (FR 3%でフルサイズ)
- `entry_lookback`: 500h→168h (7日で反応)
- `exit_lookback`: 48h→72h (不要exit減少)
- `funding_exit_threshold`: 0.0→0.00000114 (年率1.0%相当, 0.01/8760)
- `basis_entry_patience_hours`: 24→8h
- `hlp_vault_idle_hours`: 24→6h
- スキャンフィルタ: ハードコード3.0%→config.min_entry_fr_annual

### 柱3: Exit高速化
- Exit適応バックオフ: 1h固定→15/30/60min (exit_fail_count追跡)
- 緊急Exit即market: safety reason時は指値スキップ
  - basis_spike, basis_divergence, max_drawdown, neg_fr_rapid_exit, portfolio_avg_fr_negative

## 重要な計算ミス修正
- プラン上の`funding_exit_threshold=0.0000114`は10倍大きかった
- 正解: 1.0%/年 = 0.01/8760 = **0.00000114**/h
- 0.0000114にすると全通貨が`_should_be_in_position`=Falseになりconfig weightにフォールバック

## テスト
- 633件全パス (19件のデフォルト値アサーション更新)
- FR補間テストを新閾値に合わせて再計算

## 期待効果
- 年率+3.0-6.5% (現在~5.9%→目標~9-12%)
