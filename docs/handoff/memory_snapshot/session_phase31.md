# Phase 31: ポジション健全性革命

## 日付: 2026-03-07

## 問題
HYPE が $0.01 の HOLDING 状態。調査で5つの構造的欠陥を発見。

## 発見した5つの構造的欠陥
1. **CRITICAL**: `_reconcile_positions()` がdust位置を無検証で復元 (lines 450-468)
2. **HIGH**: orphan位置検出の盲点 — ポートフォリオ外のexchange positionを無視
3. **HIGH**: HOLDING位置のサイズ/デルタ健全性チェックなし
4. **MEDIUM**: `_avg_funding()` がIDLE通貨で0.0を返す不整合
5. **MEDIUM**: ローテーション機能のテストゼロ

## 実装した5つの変更

### 変更1: reconcile IDLE復元バリデーション
- Guard 1: perp_usd < min_order_usd → dust復元しない
- Guard 2: spot/perp USD不均衡 >50% → delta neutral破綻、復元しない
- Guard 3: allocated_usd推定 (perp_usd / spot_ratio)

### 変更2: orphanポジション検出
- `portfolio_perp_symbols` set でポートフォリオ外のperp_by_symbol項目を検出
- min_order_usd以上の孤立ポジション → ERROR log + Discord通知

### 変更3: zombie検出 (_check_safety)
- spot_usd or perp_usd < min_order_usd * 0.5 → "zombie_position_spot/perp" 退出
- _translate_exit_reason に日本語翻訳追加
- _exit_position の emergency_reasons に追加 (即market)

### 変更4: _avg_funding() 修正
- `pos.funding_history` → `self._get_fr_history(base)` に変更
- IDLE通貨がglobal_fr_historyにフォールバックして正確なFRを返すように

### 変更5: テスト18件追加
- TestReconcileValidation (4件)
- TestPositionHealthCheck (5件)
- TestShouldRotate (4件)
- TestRotatePositions (4件)
- TestAvgFundingFix (1件)
- 全653件pass (旧635+新18)

## 期待効果
- 年率: 5.3% → 7.3% (+2.0%pt) — HYPE正常entry後
- 資本稼働率: 38.7% → 64.5%
- zombie/orphan/dust の自動検出・防止

## コミット
- (未コミット — デプロイ後にコミット)
