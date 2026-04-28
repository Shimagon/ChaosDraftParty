# セッション記録: Phase 23 限界突破 — 資本速度革命

## セッション名
**「Phase 23: 限界突破 — 小資本集中 + FR加重 + 複利修正 + ホワイトリスト拡張」**

## 期間
2026-03-04 〜 2026-03-05

---

## 背景
$100 (+$67 aggressive) 投入で年$2-3しか稼げていない問題。
原因: ETHだけHOLDING (5.7%)、他3通貨IDLE → 資本の75%が0%で休眠。

ユーザーの指示:
「今の懸念点とか不安なところをしっかり洗い出してほしい。あとこれが限界ですか？
AIなのにこれが限界なんですか？限界を超えて、AI自動売買に革命を起こせる。」

---

## 8つの懸念点 (特定済み)

| # | 懸念 | 深刻度 | 対応 |
|---|------|--------|------|
| 1 | $100で4通貨分散→min order未達 | CRITICAL | Stage 1.1 ✅ |
| 2 | 複利が名目だけ (二重計上バグ) | HIGH | Stage 1.3 ✅ |
| 3 | FR倍率がポートフォリオ全体平均 | HIGH | Stage 2.1 ✅ |
| 4 | effective_weightsがFR非考慮 | MEDIUM | Stage 2.2 ✅ |
| 5 | ホワイトリスト5通貨のみ | MEDIUM | Stage 2.3 ✅ |
| 6 | Aggressive harvester $67が遊び | MEDIUM | Stage 1.2 ✅ |
| 7 | FR退出後の資金退避先なし | MEDIUM | Stage 3 (将来) |
| 8 | kHYPE未活用 | LOW | 将来 |

---

## 実装内容

### Stage 1.1: 小資本集中モード ★最大インパクト
**ファイル**: `src/bot/funding_harvester.py` L505-547

`_get_effective_weights()` に小資本集中フィルタ追加:
- 各通貨のposition_sizeを計算 (total × weight/sum × spot_ratio × fr_mult)
- min_order_usd未満の通貨がある場合、FR最低の通貨から除外
- 最低1通貨は残す

**テスト**: 7件追加 (`TestSmallPortfolioConcentration`)

### Stage 1.2: Aggressive Harvester資金統合
コード変更なし。デプロイ時にaggressive-harvester停止 + --size 167に変更。

### Stage 1.3: 複利二重計上バグ修正 ★バグ発見
**発見**: `net_profit = total_cumulative_funding - total_fees_paid` (累積値) を毎回compound →
二重計上で `total_size_usd` が実資本以上に膨張する。

**修正**:
- `BotStatus.total_compounded: float = 0.0` 追加
- `new_to_compound = net_profit - self._status.total_compounded`
- `_save_state()` / `_load_state()` にtotal_compounded追加
- 後方互換: 古いstate fileではdefault 0.0

**テスト**: 5件追加 (`TestCompoundNoDoubleCount`)

### Stage 2.1: 通貨別FR倍率
**ファイル**: `src/bot/funding_harvester.py`

新メソッド追加:
- `_get_per_coin_fr_multiplier(base: str) -> float`: 個別通貨72h FRから0-1倍率
- `_coin_avg_fr_annual(base: str) -> float | None`: 通貨の72h平均FR年率%

変更:
- `_enter_position()`: `_get_fr_size_multiplier()` → `_get_per_coin_fr_multiplier(pc.base)`
- `_get_effective_weights()`: 集中フィルタのfr_multも通貨別に
- `_get_fr_size_multiplier()` (ポートフォリオ版): 日次サマリー・portfolio healthで維持

**テスト**: 11件追加 (`TestPerCoinFRMultiplier`)

### Stage 2.2: FR加重配分
**ファイル**: `src/bot/funding_harvester.py` L511-518

`_get_effective_weights()` Step 1のeligible dict値を変更:
- 旧: `eligible[pc.base] = pc.weight` (config weight)
- 新: `eligible[pc.base] = fr_annual` (FR年率に比例)
- データ不足時はconfig weightにフォールバック

**効果**: ETH 5.7%+SOL 4% → ETH 59%:SOL 41% (旧: 50:50)

**テスト**: 5件追加 (`TestFRWeightedAllocation`)
既存テスト3件更新 (config weight → FR加重に)

### Stage 2.3: ホワイトリスト拡張
**ファイル**: `src/bot/funding_harvester.py` L91-93

5通貨 → 13通貨に拡張:
- 追加: DOGE, LINK, AVAX, SUI, AAVE, PEPE, ARB, OP
- 既存の流動性フィルタ (FR≥3%, depth, spread) が不適格通貨を自動排除

**テスト**: 3件追加 (`TestExpandedWhitelist`)

---

## テスト結果
- 新規追加: 31件
- 既存更新: 4件
- **全559件パス**

## 修正ファイル一覧
| ファイル | 修正内容 |
|---------|---------|
| `src/bot/funding_harvester.py` | 1.1, 1.3, 2.1, 2.2, 2.3 |
| `tests/test_funding_harvester.py` | テスト31件追加+4件更新 |

## 期待効果
| 段階 | 実効利回り | $167での年間収益 |
|------|-----------|-----------------|
| 旧 (Phase 22) | ~1.4% | ~$2.3 |
| Stage 1完了 | ~5.7% | ~$9.5 |
| Stage 2完了 | ~8-10% | ~$13-17 |
| ブル相場時 | ~15-20% | ~$25-33 |

## デプロイ結果 (2026-03-05)

### 障害1: equity_peak DD誤検知
- **原因**: --size 500→167変更時にbot_state.jsonのequity_peak=500が残存
- **影響**: 66.6% DDと誤判定 → ETH全exit
- **修正**: equity_peakを167.0に手動修正

### 障害2: アンヘッジETHポジション
- **原因**: re-entry時にSpot vault $100でspot buyは成功、Perp vault $0でperp売りが極小($1.7)
- **影響**: ETH spot 0.0474 (≈$101) vs perp 0.0008 (≈$1.7) — 裸ロング状態
- **修正**: fix_unhedged_eth.py でspot売却+perp close

### 障害3: Agent keyでvault間転送不可
- **原因**: private keyがagent wallet (0xda99...)、main wallet (0x9326...)と異なる
- **影響**: ccxt transfer() → "Must deposit before performing actions" エラー
- **解決**: vault間転送は不可。代わりにentry前vault残高チェックでブロック

### Stage 2.4: Vault残高チェック (統合アカウント対応)
**ファイル**: `src/bot/funding_harvester.py`

新メソッド `_ensure_vault_distribution(allocated_usd) -> bool`:
- entry前にtotal USDCを確認 (`max(perp, spot, perp+spot)`)
- 統合アカウント(Unified Account)ではSpot/Perpが1プール
- total USDC < min_order_usd → entry BLOCKED
- API失敗 → entry許可 (Botを止めない)

**テスト**: 6件 (`TestVaultDistribution`)

### 障害4: Spot指値がHyperliquid spot薄板で約定しない
- **原因**: ETH/USDCのspot板が薄い + fetch_ticker返却Noneでmarket fallbackも失敗
- **影響**: spot 0.0055 vs perp 0.0377 → 85%ミスマッチ → rollback発動
- **修正1**: Spot注文は常にmarket order (指値はperp側のみ)
- **修正2**: `_market_order()`でfetch_ticker None時にorder bookフォールバック

### Stage 2.5: Perp-First注文順序 + 自動ロールバック (NEW)
**ファイル**: `src/bot/funding_harvester.py`, `src/bot/order_executor.py`

**entry順序変更** (統合アカウント対応):
1. `set_leverage(4x, perp_symbol)` — マージン設定 (422エラーでも続行)
2. **Perp売り FIRST** (指値) — マージン$20だけロック、残$80でspot購入可能
3. **Spot買い** (market) — `perp_filled`量に合わせてデルタニュートラル保証
4. **ミスマッチ>50%** → 両方自動ロールバック
5. **ミスマッチ10-50%** → 警告のみ

**`_market_order`改修**:
- `fetch_ticker` None → `fetch_order_book`フォールバック

**テスト**: 8件追加 (`TestPerpFirstEntry`)
**全573件パス**

### 最終状態 (2026-03-05 04:28)
- Bot: `--size 100` で稼働中
- **ETH HOLDING**: spot=0.0375, perp=0.0375 **完全マッチ** ✓
- HYPE/BTC/SOL: IDLE (FR負)
- 資金: $100.10

## 残課題 (Stage 3以降)
- `set_leverage` 422エラー修正 (ccxt Hyperliquid APIフォーマット問題)
- HLP Vault活用 (idle資金の運用)
- FR Regime検出
- kHYPE統合完成
