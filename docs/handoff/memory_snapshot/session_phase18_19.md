# セッション記録: Phase 18-19 — FR Turbo Bot作成 + GCPデプロイ + ライブ稼働開始

## セッション名
**「Phase 18 FR Turbo Bot + 懸念点13件対応 → Phase 19 GCPデプロイ + ライブ取引開始」**

## 期間
2026-03-03 〜 2026-03-04

## セッション番号
**セッション1** (この期間中に複数セッションが並行稼働していた)

---

## やったこと一覧

### 1. Phase 18: FR Turbo Bot作成 + 現行Bot改善

#### 現行Bot (funding_harvester.py) 改善

**改善1: Basis有利エントリー**
- `BotConfig` に3フィールド追加: `basis_entry_enabled`, `basis_entry_max=0.003`, `basis_entry_patience_hours=24`
- `PositionStatus` に `basis_wait_start: datetime | None` 追加
- エントリー時にbasis ≤ 0.3%を待つ。24h超えたら妥協して入る
- `_save_state()` / `_load_state()` にbasis_wait_start追加

**改善2: ホワイトリスト** → 変更なし (Phase 16の5通貨維持を決定)

#### FR Turbo Bot (aggressive_harvester.py) — 新規作成 ~800行

**コンセプト**: 「待ち伏せ型」FR収穫。FRスパイク検出時のみ高レバでデルタニュートラル参入。

| 項目 | 現行Bot | FR Turbo |
|------|---------|----------|
| 資本 | $500 | $67 (¥10,000) |
| Spot比率 | 80% (4x) | 82% (4.56x) |
| 清算距離 | 25% | 22% |
| ポジション | マルチ通貨 (3) | シングル通貨 (最高FR) |
| エントリー | 500h平均FR>0 | FRスパイク>0.002%/h (年率17.5%) |
| デフォルト状態 | 常時投資 | キャッシュ待機 |

**状態遷移**: SCANNING → ENTERING → HOLDING → EXITING → COOLDOWN → (SCANNING)

**安全装置**: max_basis 1.0%, max_dd 5%, neg FR 2連続→即退出, max_hold 72h, cooldown 60min

#### 共有モジュール: order_executor.py — 新規作成 ~109行
- 両Botの `_execute_order` を共通化 (DRY原則)
- 指値→成行フォールバック、タイムアウト、手数料計算

#### 懸念点13件の対応

プラン段階で13件の懸念点を洗い出し、設計段階で11件対応。残り2件を実装:

**修正A: $67最低注文サイズチェック (#6)**
- `aggressive_harvester.py` の `_scan_for_spikes()` に `exchange.market()` で最低注文量チェック追加
- BTC@$90kで$67 → 0.00061 BTC < min 0.001 → スキップ

**修正B: Basis APIスロットル (#12)**
- `funding_harvester.py` の `run_forever()` で IDLE通貨のbasisチェックを5分→15分毎に
- API呼び出し 1,728/日 → 576/日に削減

#### テスト
- Phase 18完了時: **457テスト全パス**
- 追加テスト: min order size (2件) + basis throttle (2件) + FR Turbo全体 (45件) + 現行Bot改善 (13件)

---

### 2. Phase 19: ドライラン検証

1. **全テスト実行**: 457テスト全パス (6.6秒)
2. **FRスキャン**: 7通貨通過、ポートフォリオ予想年率9.7%
   - ETH 11.0%, BTC 5.0%, HYPE 6.6%
   - FR Turbo閾値 (17.5%/年) は現実的、調整不要
3. **現行Bot ドライラン**: 初期化OK、3通貨 `should_be_in=True`
4. **FR Turbo ドライラン**: 初期化OK、スパイクなし (想定通り)

---

### 3. Phase 19: GCPデプロイ

#### 作成ファイル (4件)
| ファイル | 内容 |
|---------|------|
| `scripts/deploy/funding-harvester.service` | systemdサービス (現行Bot $500) |
| `scripts/deploy/aggressive-harvester.service` | systemdサービス (FR Turbo $67) |
| `scripts/deploy/setup_vm.sh` | VM環境セットアップ (Python 3.13 + pyenv + deps) |
| `scripts/deploy/deploy.sh` | ワンコマンドデプロイ (rsync + .env + systemd) |

#### デプロイ手順 (実行済)
1. `gcloud auth login` → Google認証
2. `gcloud config set project project-f04accdc-ccc6-4bc7-992`
3. Billingアカウント「自分」(01F56E-28589F-56D3AC) をリンク
4. Compute Engine API有効化
5. VM作成: `gcloud compute instances create funding-bot --zone=us-central1-a --machine-type=e2-micro`
6. コード転送: `gcloud compute scp`
7. .env転送 (chmod 600)
8. Python 3.13.2ビルド (pyenv, e2-microで約15分)
9. 依存パッケージインストール (ccxt, structlog等)
10. systemdサービスインストール・起動

#### 初回起動エラーと修正
- **エラー**: `HYPERLIQUID_PRIVATE_KEY not set` → systemd環境に.envが渡っていない
- **修正**: `EnvironmentFile=/home/bot/funding-bot/.env` をサービスファイルに追加
- **注意**: ローカルの.serviceファイルから `EnvironmentFile` 行が後に別セッションで削除された模様。VM上では動いているが、ローカルファイルとVM上のファイルが乖離している可能性あり

#### systemdサービスファイルの注意
- `StartLimitIntervalSec` と `StartLimitBurst` は `[Unit]` セクションに置く
- `[Service]` に置くとDebian 12のsystemdで警告が出る
- `EnvironmentFile=/home/bot/funding-bot/.env` が **VM上のサービスファイルには必須**
  (Botは `os.getenv()` で秘密鍵を読むため)

---

### 4. ライブ稼働確認 (2026-03-04)

#### Funding Harvester — 3通貨全てHOLDING
| 通貨 | Spot | Perp Short | Entry Price | FR収入 |
|------|------|-----------|-------------|--------|
| HYPE | 1.027 | 1.02 | $31.32 | $0.0041 |
| ETH | 0.0211 | 0.012 | $1,961.55 | $0.0019 |
| BTC | 0.000300 | 0.0003 | $66,604 | $0.0032 |

合計FR収入: $0.0079 (約21時間稼働)

#### FR Turbo — SCANNING
- ポジション: なし (スパイク待ち = 正常動作)

#### システムリソース
- メモリ: 575MB / 969MB (59%)
- funding-harvester: ~119MB, aggressive-harvester: ~106MB

---

## GCPインフラ情報

| 項目 | 値 |
|------|-----|
| プロジェクト名 | BitCoin-project |
| プロジェクトID | project-f04accdc-ccc6-4bc7-992 |
| プロジェクト番号 | 462373754594 |
| Billingアカウント | 01F56E-28589F-56D3AC (「自分」) |
| VM名 | funding-bot |
| ゾーン | us-central1-a |
| マシンタイプ | e2-micro (無料枠) |
| OS | Debian 12 |
| ディスク | 10GB pd-standard |
| 外部IP | 130.211.126.155 |
| Python | 3.13.2 (pyenv) |
| ユーザー | bot (/home/bot/funding-bot/) |

---

## 運用コマンド集

```bash
# SSH接続
gcloud compute ssh funding-bot --zone=us-central1-a

# ログ確認 (リアルタイム)
gcloud compute ssh funding-bot --zone=us-central1-a --command='sudo journalctl -u funding-harvester -f'
gcloud compute ssh funding-bot --zone=us-central1-a --command='sudo journalctl -u aggressive-harvester -f'

# Bot停止
gcloud compute ssh funding-bot --zone=us-central1-a --command='sudo systemctl stop funding-harvester aggressive-harvester'

# Bot再起動
gcloud compute ssh funding-bot --zone=us-central1-a --command='sudo systemctl restart funding-harvester aggressive-harvester'

# 状態確認
gcloud compute ssh funding-bot --zone=us-central1-a --command='cat /home/bot/funding-bot/data/bot_state.json | python3 -m json.tool'

# コード更新デプロイ
bash scripts/deploy/deploy.sh funding-bot us-central1-a
```

---

## トラブルシューティング (このセッションで遭遇)

1. **EnvironmentFile問題**: systemdサービスから.envが読めない → `EnvironmentFile=` ディレクティブ追加で解決
2. **StartLimitIntervalSecの位置**: `[Service]`ではなく`[Unit]`に置く
3. **e2-microでのPythonビルド**: 約15分。忍耐が必要
4. **ネットワーク一時切断**: gcloud SSH中にDNS解決失敗 → 自然復帰を待つ
5. **テスト失敗 (min order size)**: mockのorder book depthが不足 → `bid_amount = max(10000.0 / spot_price, 100.0)` に修正
6. **funding_harvester.py インデントエラー**: basis throttle追加時に `elif HOLDING:` がforループから分離 → `last_basis_check` 更新をforループ外に移動で解決

---

## セッション間の競合・注意

- このセッション中に**別セッション(Phase 20-21)**が並行して動いていた
- 別セッションが以下を実行:
  - Phase 20: セキュリティ監査 (12件修正) + Spot vault分離バグ修正
  - Phase 21: 攻め施策計画 (kHYPE, SOL追加等)
  - テスト数が457 → 477に増加
  - `funding-harvester.service` から `EnvironmentFile` 行が削除された模様
- **VM上のサービスファイルには `EnvironmentFile` が入っているはず** (直接VMに反映したため)
- ローカルとVMのファイル乖離に注意

---

## 変更ファイル一覧 (このセッション)

### 新規作成
| ファイル | 行数 | 内容 |
|---------|------|------|
| `src/bot/aggressive_harvester.py` | ~800 | FR Turbo Bot本体 |
| `src/bot/order_executor.py` | ~109 | 共有注文実行モジュール |
| `tests/test_aggressive_harvester.py` | ~700 | FR Turboテスト (45件) |
| `scripts/deploy/funding-harvester.service` | 31 | systemdサービス |
| `scripts/deploy/aggressive-harvester.service` | 31 | systemdサービス |
| `scripts/deploy/setup_vm.sh` | ~80 | VMセットアップ |
| `scripts/deploy/deploy.sh` | ~80 | デプロイスクリプト |
| `SESSION_LOG.md` | ~200 | セッションログ (プロジェクトroot) |

### 変更
| ファイル | 変更内容 |
|---------|---------|
| `src/bot/funding_harvester.py` | basis有利エントリー追加, basis APIスロットル追加 |
| `tests/test_funding_harvester.py` | basis throttleテスト2件, basis entryテスト8件追加 |
