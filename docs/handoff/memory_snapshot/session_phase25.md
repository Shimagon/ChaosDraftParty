# Phase 25: HLP Vault + Discord日本語化 + GitHub管理

**日付**: 2026-03-05
**状態**: デプロイ済み

---

## 実装内容

### 1. Discord通知 完全日本語化
- `_translate_exit_reason()` 関数追加 — 退出理由コード→日本語変換
- 戦略名: `"funding_harvest"` → `"FR収穫"`
- 退出理由: `"neg_fr_rapid_exit_3_consecutive"` → `"FR連続マイナス"` 等
- 全エラーメッセージ日本語化 (Entry failed→エントリー失敗, Exit failed→決済失敗)
- 日次ダッシュボード全面日本語化
- notify_drawdown: タイトル・フィールド名日本語化

### 2. GitHub Private Repo
- `git init` → `gh repo create funding-bot --private`
- https://github.com/Shimagon/funding-bot
- `.gitignore` に `.claude/`, `.pytest_cache/` 追加

### 3. HLP Vault自動管理 (メインフィーチャー)

#### 仕組み
- 全ポジションIDLE × 24h以上 → 資本80%をHLP Vaultに自動入金
- HLP Vault = Hyperliquidのマーケットメイキングプール (年10-17% APY)
- FR回復 → 自動出金 → FR収穫に復帰
- 20%はリザーブ (FR回復時の即時entry用)
- 4日間ロックアップ追跡

#### API実装
- **Info API** (残高): aiohttp POST → `userVaultEquities`
- **Exchange API** (入出金): ccxt `sign_l1_action()` + `privatePostExchange()`
  - ccxtの`withdraw()`が`vaultTransfer` + `isDeposit:False`で実装されていることを発見
  - 同じメカニズムを`isDeposit:True`で再利用 → 追加依存なし

#### Agent Key制限
- Agent key (0xda99...) でvaultTransferが動作するかは未検証
- 失敗時は自動検出 → `hlp_vault_disabled_reason` に記録 → graceful degradation
- 将来的にmain key (0x9326...) を.envに追加すればフル機能利用可能

#### コード変更
- `BotConfig`: hlp_vault_enabled, hlp_vault_idle_hours, hlp_vault_reserve_ratio, hlp_vault_check_interval_sec
- `BotStatus`: hlp_vault_deposited, hlp_vault_deposit_time, hlp_vault_withdrawal_pending, hlp_vault_withdrawal_time, hlp_vault_disabled_reason
- 新メソッド: `_fetch_hlp_vault_equity()`, `_hlp_vault_transfer()`, `_all_positions_idle()`, `_any_position_should_enter()`, `_hlp_lockup_expired()`, `_manage_hlp_vault()`
- `_save_state()` / `_load_state()`: HLP状態永続化
- `_send_daily_summary()`: HLP Vault残高・損益表示
- `run_forever()`: 1h毎にHLP管理呼び出し
- CLI: `--no-hlp` フラグ

#### テスト (21件追加)
- 定数, Config, Status, 状態永続化
- _all_positions_idle, _hlp_lockup_expired
- _any_position_should_enter
- _manage_hlp_vault (disabled/dry_run/auto_disabled)
- _hlp_vault_transfer (dry run)
- CLI flag

---

## 技術メモ

### ccxt Hyperliquid vaultTransfer署名
```python
# ccxt withdrawのパターンを再利用
nonce = self._exchange.milliseconds()
action = {
    "type": "vaultTransfer",
    "vaultAddress": "0xdfc24b077bc1425ad1dea75bcb6f8158e10df303",
    "isDeposit": True,  # ccxt withdraw()ではFalse固定
    "usd": amount_usd,  # ccxt規約: raw USD float
}
sig = self._exchange.sign_l1_action(action, nonce)
response = await self._exchange.privatePostExchange({
    "action": action, "nonce": nonce, "signature": sig,
})
```

### HLP Vault定数
- Address: `0xdfc24b077bc1425ad1dea75bcb6f8158e10df303`
- ロックアップ: 4日 (新規入金でリセット)
- TVL: ~$423M
- APY: 変動大 (17-42% lifetime CAGR, 短期は負もあり)

### GCPデプロイ注意
- `--project=project-f04accdc-ccc6-4bc7-992` が必須 (デフォルトprojectが違う)

---

## テスト結果
- 605件全パス (584 + 21 HLP Vault)
