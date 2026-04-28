# Phase 29: 構造的ボトルネック破壊

## 日付: 2026-03-06

## 実装内容 (5項目)

### 変更1: 流動性フィルタ比例化
- `_scan_and_select_portfolio()`: spot_depth≥500, perp_depth≥5000 → ポジションサイズ比例
- max_pos_usd = total_size / max_positions
- min_spot_depth = max(100, max_pos_usd * 5)
- min_perp_depth = max(500, max_pos_usd * 10)
- $100/5=$20 → spot $100+, perp $500+ (現在の1/10の要求)

### 変更2: FR純粋ランキング
- AI分析をログのみに留め、常にFR年率でソート
- AIがBTC(FR-4.9%)にスコア72を付ける問題を解消
- 過学習防止原則に合致: シンプルなFR順

### 変更3: スキャン間隔短縮
- scan_interval_sec: 259200(3日) → 86400(1日)
- min_hold_hours=168h(7日)は維持 → ローテーション自体は抑制

### 変更4: リバランス機会検出
- `_check_rebalance_opportunity()` 新設
- IDLE通貨のFR > HOLDING通貨最低FR × 1.5 → Discord通知
- 6hクールダウンで通知スパム防止
- メインループで毎チェック呼び出し
- 今回は通知のみ (自動リバランスはPhase 30)

### 変更5: vault残高チェック改善
- `_ensure_vault_distribution()`: min_order_usd → max(min_order_usd, allocated * spot_ratio)
- より正確な必要額チェック

## テスト
- 266件全パス (scan_interval_secの2件更新)

## ユーザーへの提案
- **資本追加**: $100→$300-500が最大の革命 (分散効果)
- **Main Wallet Key確認**: Agent keyならHLP Vault不可
- **Binanceアカウント**: クロス取引所対応の準備
