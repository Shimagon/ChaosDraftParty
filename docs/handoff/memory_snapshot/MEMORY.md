# メモリインデックス

## 卒業制作 (ChaosDraftParty)
- [ChaosDraftParty 作業方針（全権委任）](feedback_chaosdraftparty_workflow.md) — MCP/Skills/サブエージェント全活用、確認不要、plan+ウルトラシンキング常時ON
- [完璧小刻み + 返信なし自律モード](feedback_perfect_small_save_autonomous.md) — 1 task=1 commit、検証ゲート必須、返信なしで ScheduleWakeup 自律継続、並列は研究のみ
- [ChaosDraftParty NotebookLM リソース](reference_chaosdraftparty_notebooklm.md) — 開発支援ノートブック `528f0394` と3ソースのID（仕様 Q&A/Deep research 投入先）
- [ChaosDraftParty Unity MCP セットアップ](reference_chaosdraftparty_unity_mcp.md) — CoplayDev/unity-mcp を user scope で登録済み、Unity 側 plugin URL と落とし穴
- [ChaosDraftParty アセット参照崩壊の防止（最重要）](feedback_unity_asset_integrity.md) — マテリアルピンク化・色ズレ・GUID 崩壊を絶対に避けるための運用ルール
- [ChaosDraftParty は Mac + Windows 両対応](feedback_cross_os_windows.md) — ドキュメント・CLI例は macOS/Windows 並記、ASCII 英字パス推奨、改行 LF 固定
- [ChaosDraftParty 外部資源カタログ](reference_chaosdraftparty_resources.md) — MCP/Skills/OSS/API のTier分類、6ヶ月の採用タイムラインと$180予算
- [ChaosDraftParty 罠回避スキル](reference_chaosdraftparty_gotchas_skill.md) — `.claude/skills/chaosdraftparty-gotchas/SKILL.md` に Unity/Claude 固有ハマりどころを成長型で蓄積（両OS共有、新罠踏むたび追記）
- [Meshy AI Pro 契約 + 使用方針](reference_meshy.md) — 2026-04-22 Pro 契約、ステージ小物専用、キャラ禁止、credit 管理、.claude/skills/meshy-unity-workflow/ が本体
- [ChaosDraftParty ビジュアル戦略](project_visual_target.md) — Fall Guys 75% + 原神 25% 目標、$145 投資、M1-M6 月次計画、5 つの禁則。詳細 docs/VISUAL_TARGET.md
- [2026-04-22 Autonomous セッション記録](session_autonomous_20260422.md) — ユーザー離席中の 10 commit 連続 push、23 SO + 7 C# class + 3 agent 定義 + 4 罠追加 + Unity session down 対処
- [Game UI Database (UI リファレンス)](reference_game_ui_database.md) — gameuidatabase.com。UI 改修時は先に 5 例見る。Alba / All of You 参考例、Unity 臭排除の基準
- [EGGYBOT キャラクターデザイン（確定）](project_eggybot_character.md) — ガチャカプセル型、pink+clear 2 形態変形。仕様書 /キャラクター/ 配下。Meshy は透明・変形が苦手、Blender MCP 併用推奨
- [まずは調べろルール（調査→実装）](feedback_research_first.md) — 視覚品質/shader/architecture 判断は先に WebSearch/記事/海外 artist workflow を読む。勘で iterate しない。commit に参考 URL 添える
- [視覚品質を勝手に変えるな、先に聞け](feedback_ask_before_visual_change.md) — approve 済みの色/材質/形状/比率を後続リクエストで自己判断で変更禁止。参考画像≠変更指示。先に proposal + 理由を提示して許可取る
- [褒め承認時だけナレッジ化する](feedback_save_only_on_praise.md) — skill / memory 追記はユーザーが「ナイス」「戻った」等の正フィードバック出した時のみ。correction 後の反射保存は禁止（未確定の判断が定着するため）
- [実装前に必ず確認、勝手にコード変えるな](feedback_ask_before_implement.md) — Edit/Write/build 実行は明示 "go" まで禁止。確認・分析・案提示で止める。Auto mode でも視覚デザイン破壊系は確認必須
- [EGGYBOT v16 SDF clip 構成（採用）](project_eggybot_v16_clip.md) — CapsuleVolume empty + EggybotShaderBinder + ArmClip shader で hand/foot 内部を discard。ユーザー承認済の v16 ベース構成（commit 90bcff7）
- [不安な所はユーザー、機械的な所は Claude（恒久方針）](feedback_division_of_labor.md) — bone 微調整・視覚審美は user、rough rig 構築・script・初期値は Claude。EGGYBOT 以降の全タスクで適用

---

# Crypto Trading Bot — プロジェクトメモリ

## プロジェクト概要
仮想通貨デルタニュートラル・ファンディングレート収穫Bot。「負けないこと」が最優先。
- 場所: `/Users/taisei/Documents/課題/やりたいこと/株で稼ぐ/`
- 過学習防止ルール: `OVERFITTING_RULES.md`
- Bot運用ガイド: `FUNDING_BOT_GUIDE.md`

## 現在の状況 (Phase 34 — デプロイ済み)
- **GCP VM上で24/7ライブ稼働中** (Phase 32+33+34コード, 2026-03-07 04:39 UTC デプロイ)
- **Bot**: `funding_harvester.py` (本体)
- **テスト**: 723件全パス (305 bot + 51 dashboard + 367 legacy)
- **BTC HOLDING**, **HYPE HOLDING** — FR駆動で自動選択
- **資金**: ~$387 (--size 100 → 400に要変更)
- **GitHub**: https://github.com/Shimagon/funding-bot (Private)

## Phase 30+30.5 実装済み (デプロイ済み)
### Phase 30: 資本効率革命
- **FR二重割引排除**: position_sizeからfr_multiplier削除 (entry gateのみ維持)
- **NoneTypeガード**: order_executor.py fetch_order None対策
### Phase 30.5: FR駆動ローテーション
- **min_hold_hours**: 168→48h
- **_should_rotate()完全書き直し**: FR差>5%→即ローテ、通常→round_trip_cost×3比較
- **_check_rebalance_opportunity()**: 通知のみ→臨時スキャン+ローテーショントリガー
- **Early scan間隔**: 48h→1h
- **_last_scan_fr保存**: スキャンFR結果をrotation判断で使用
- **ポートフォリオ永続化**: active_portfolioをbot_state.jsonに保存/復元
- **IDLE通貨はglobal_fr_history優先**: pos.funding_historyは古いデータのため
- **entry_lookback**: 72→24h (FR regime change後の高速エントリー)
- **FR lookback統一**: _get_per_coin_fr_multiplier, _coin_avg_fr_annualもentry_lookback使用

## Phase 31 実装済み (デプロイ済み)
### ポジション健全性革命
- **reconcileバリデーション**: dust filter + spot/perp不均衡チェック + allocated_usd推定
- **orphan検出**: ポートフォリオ外のexchangeポジション → ERROR log + Discord通知
- **zombie検出**: HOLDING位置のspot/perp < min_order_usd*0.5 → 自動exit
- **_avg_funding修正**: pos.funding_history → _get_fr_history() (IDLE通貨がglobal FR使用)
- **テスト18件追加**: reconcile, zombie, rotation, avg_funding (合計653件)

## Phase 32 実装済み (デプロイ済み 2026-03-07)
### Churning防止 + デプロイガード
- **成功exit後4hクールダウン**: exit成功後もentry_cooldown_until設定 (振動防止)
- **日次ラウンドトリップ上限**: 1通貨あたり2回転/日 (daily_round_trips追跡)
- **デプロイ猶予期間**: 起動後1hは新規entry禁止 (既存HOLDINGは維持)
- **fr_trend_decline閾値**: 50%→70% (感度低下で振動抑制)
- **CLAUDE.mdにデプロイルール追加**: 閾値変更時の影響確認必須
- **インシデントレポート**: `memory/incident_report_phase31.md` (-$13損失の全記録)
- テスト11件追加 (計296件全pass)

## Phase 33 実装済み (デプロイ済み 2026-03-07)
### unknownOid二重約定防止 + ロールバック検証
- **unknownOid二重約定防止** (`order_executor.py`): 指値発注前後のポジション差分でfilled量推定。全量filled→market不要、部分filled→残りだけmarket、失敗→全額market (現行動作フォールバック)
- **ロールバック結果検証** (`funding_harvester.py`): spot/perp各ロールバックの結果確認(90%閾値) + perp残存検証(fetch_positions) + Discord通知 + raiseは必ず実行
- テスト7件追加 (計305件全pass)

## Phase 34 実装済み
### パフォーマンスダッシュボード (Vault革命の基盤)
- **`scripts/performance_dashboard.py`**: bot_state.json読み取り専用の週次レポート
- **CSV記録**: `data/performance_history.csv` に週次追記 (90日制限なし)
- **Discord週次サマリー**: 稼働日数, 累積純利益, 年換算, 勝率, DD, ポートフォリオ
- **cron**: 日曜 UTC 00:00 (JST 09:00)
- 既存ファイル一切変更なし (CLAUDE.mdのドキュメント更新のみ)

## Phase 23-29 実装済み (省略、詳細は各session_phaseXX.md参照)

## メモリフォルダ構成 (時系列、絶対消さないこと)
- **`SHARED_STATE.md`** — 全セッション共有ボード
- `session_phase18_19.md` — FR Turbo作成 + GCPデプロイ
- `session_phase20_21.md` — セキュリティ監査 + Spot vault修正
- `session_phase22.md` — 徹底監査13件+ホットフィックス
- `session_phase23.md` — 限界突破: 資本速度革命
- `session_phase24.md` — バグ修正3件 + 日次ダッシュボード
- `session_phase25.md` — HLP Vault + Discord日本語化
- `session_phase26.md` — グローバルFR収集 (30通貨常時監視)
- `phase26_concerns.md` — Phase 26 懸念・分析レポート
- `session_phase27.md` — FR収穫最適化 (4つの改善)
- `session_phase28.md` — FR収穫革命 (手数料半減+閾値最適化)
- `session_phase29.md` — 構造的ボトルネック破壊
- `session_phase30.md` — 資本効率革命+FR駆動ローテーション
- `session_phase31.md` — ポジション健全性革命
- `incident_report_phase31.md` — Phase 31デプロイ時$13損失の詳細記録
- `session_phase34.md` — パフォーマンスダッシュボード (Vault基盤)

## ライブBot設定 (GCP VM)
- 資金: **~$124** (--size 100) / FR上位通貨に集中
- 80/20 spot/perp (4xレバ, 清算距離25%)
- 安全装置: FR退出(24h lookback, 閾値1.0%/年), neg FR急速退出(3連続), FR傾き退出(**70%低下** Phase 32), basis 2%緊急, DD 3%, ポートフォリオFR監視
- **Anti-churning (Phase 32)**: 成功exit後4hクールダウン, 日次2回転上限, デプロイ猶予1h
- **entry_lookback: 24h**, exit_lookback: 72h, min_hold_hours: 48h
- **FR駆動ローテーション**: FR差>5%→即ローテ, 通常→round_trip_cost×3(0.18%)
- **ポートフォリオ永続化**: active_portfolioがbot_state.jsonに保存→再起動で復元
- 指値ON, AI週次ローテON, 複利12hON, 日次レポートON
- **ホワイトリスト30通貨**
- **グローバルFR収集**: 30通貨のFRを毎時記録
- **IDLE通貨はglobal_fr_history優先** (pos.funding_historyは古くなるため)

## GCPインフラ
- プロジェクト: `project-f04accdc-ccc6-4bc7-992` (BitCoin-project)
- VM: `funding-bot` / zone: `us-central1-a` / e2-micro (無料枠)
- IP: 130.211.126.155
- systemd: `funding-harvester.service`
- **重要**: EnvironmentFile=/home/bot/funding-bot/.env
- **デプロイ方法**: gcloud compute scpで/tmp→sudo cp (permissionのため)
- **VM上にgit無し**: ファイル個別転送

## 重要な技術メモ
- PYTHONPATH=. が必要 (scripts/ から実行時)
- pytest: `.venv/bin/pytest tests/ -v` (305件)
- **IDLE FR履歴優先順位**: global_fr_history > pos.funding_history (exit後は更新停止するため)
- **equity_peak問題**: --size変更時にbot_state.jsonのequity_peakも変更必要
- **Perp-First Entry**: 統合アカウントではPerp売り→Spot買い
- **order_executor NoneType対策**: fetch_order→Noneならcontinue/market fallback

## Phase 30.5 デプロイ中に発見した問題と修正
1. **--size 400→100修正**: サービスファイルが400のままだった→残高不足でentry不能
2. **equity_peak DD誤検知**: 400→100変更でDD 75%検出→equity_peak手動修正
3. **ポートフォリオ非永続化**: rotation結果がrestart時に消失→active_portfolio保存追加
4. **IDLE通貨の古いFR**: pos.funding_historyがexit後に更新停止→global_fr_history優先
5. **72h lookback統一漏れ**: entry_lookback=24hだが_coin_avg_fr_annualは72hハードコード→統一

## Phase 31 デプロイ中の追加対応
- **BTC不均衡ポジション**: $100 spot vs $40 perp → 手動クリーンアップ (spot売り+perp close)
- **reconcileがimbalance検出**: Phase 31の新バリデーションが正常に機能
- **ccxt async walletAddress問題**: dotenvのload_dotenv()使用時にwalletAddressが空になる → .env手動パースで解決
- **BTC re-entry成功**: cleanup後に自動entry (0.00056 BTC, $38)

## 次にやること
- **agent key spot残高問題**: main wallet直接クエリ (中期)
- **Main Wallet Key変更**: Agent keyだとHLP Vault不可
- **クロス取引所**: Binance spot + HL perp (ENA等Spot板薄い通貨)

## AI自己規律ルール (絶対遵守)

**教訓**: Phase 34で「手動exit→re-entry」を提案したが、累計利益$0.031に対し手数料$0.34で大赤字になる初歩的ミス。数字を確認せずに提案した。

### 提案前チェックリスト
1. **損益計算を先にやる** — 「こうしたらどうか」を言う前に、必ずコスト(手数料、スリッページ)と期待利益を計算。数字を出す。数字なしの提案禁止
2. **現状を壊す提案は3回疑う** — 今動いているものを変える提案は、(1)コスト計算 (2)リスク列挙 (3)何もしない場合との比較、の3つを済ませてから口にする
3. **「やれることない？」に無理に答えない** — 本当にやることがないなら「ない。放置が最適」と言う。仕事を作るな
4. **AIであることを自覚する** — 自分は計算が得意で判断が苦手。感覚で「こうした方がいい」と言うな。必ず数字で裏付けろ

## ユーザーの好み
- 日本語コミュニケーション
- 全部一気にやる派、確認不要
- 「あなたが決めて」→ 自律的に判断・実行
- 過学習に敏感 (Phase 11のトラウマ)
- **記録ファイルは絶対消さない** — 失敗も成功も全て残す
- **一個ずつ徹底的に** — 全部一気にではなく、1タスク徹底→次へ
- **数字なしの提案をするな** — 提案には必ずコスト/利益の数字を添えろ
