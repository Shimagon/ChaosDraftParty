# Scope 決定: 2026-05-24

> 「縮む島が本当に面白いのか」「ChaosDraftParty 全体は成立するのか」を 9 並列 agent で徹底議論。User 最終判断と採用方針を記録。

## 議論プロセス (9 並列 agent)

### Phase 1: 並列調査 6 agent
| Agent | 結論 | 立場 |
|---|---|---|
| 心理学 (Flow/SDT/4 Keys/Loss Aversion/Spectacle/Skinner) | 4 Keys 4/4 達成 = Lazzaro 最高評価、「名作」 | 楽観 |
| ゲームデザイン理論 (MDA/8 Kinds/Game Feel/Sid Meier/Koster) | Interesting Decision 9.2s/回 (AAA下限+)、飽和 3-4 週、GO 条件付き | 中立 |
| AAA 成功例 (Fall Guys/Mario Party/Splatoon/Smash 等 10 タイトル) | 構造は AAA defacto 完全準拠、9.2/10 は「保守的、9.5+ 行ける」 | 楽観 |
| AAA 失敗例 (Knockout City/Foamstars/Lawbreakers 等 12 タイトル) | 失敗確率 60%、罠 5 つ (マッチング/学習/PC限定/配信/サポ) | 慎重 |
| Hex-a-gone 直接比較 (Stumble/Bumper Balls/PUBG/TowerFall 等 10 ゲーム) | 「独自ピークあり、磨き不足」、shrink 15→10 秒等改良提案 5 つ | 中立 |
| CDP 整合性 audit | 矛盾 3 つ (CD/コンボ発生率 3-5%/Season逆向き)、実数 8.5-8.8 | 慎重 |

楽観 3 vs 慎重 3 で割れた。

### Phase 2: 議論 3 agent
| Agent | 主張 | 失敗確率 |
|---|---|---|
| Devil's Advocate | Foamstars コース確定、全廃して縮む島 single-game roguelike へ転換 | **78%** |
| Defender | Lazzaro 4/4 + AAA hybrid + EGGYBOT + AI 武装、軽微修正で OK | **10%** |
| Synthesizer (中立) | 商業 KPI ベースの Devil's は卒研で過大、Defender は楽観バイアス。ハイブリッド C 案 | **25-30%** (卒研軸) |

### Synthesizer 中立評価 (卒研軸)
- 教授「完成度」評価: **82%**
- 同期「面白い」評価: **65%** (polish 集中で 75-80%)
- 大学院/就活 portfolio 説得力: **88%**
- Steam 1 ヶ月 65% positive: **45%** (= 卒研関係なし)
- **卒研総合成功確率 70-75%**

## User 最終判断 (2026-05-24)

> 「全然時間あるしMAX20やからいける。並列で爆速ですすめまくろう！」

- **scope 維持** (Synthesizer 推奨の scope 削減 C 案は採用しない)
- **breadth 全部やる** + Synthesizer / Devil's の改善案も取り込む
- 5 ミニゲーム / skill 10+ / rule 6 / season 3 すべて実装方針
- 並列 agent + AI review loop で爆速進める

## 採用する改善 (議論結果から取り込む)

### Devil's の鋭い指摘
1. **「Bot 混合は設計敗北」→「Bot 混合は前提仕様」へ正面化**
   - docs に Bot spec 明記、逃げ道扱いを止める
   - `docs/minigames/shrinking_island_v2.md` で Bot 動作仕様も含める
2. **「実装してから判断」は危険 → 6/30 中間 playtest 期限**
   - 6/30 に「縮む島が面白くないか」評価
   - 失格なら plan B (scope 削減 + 縮む島 depth 集中) 発動

### Synthesizer の鋭い指摘
1. **縮む島 polish 集中**: タイル variant 3 種 / 演出 / tutorial / Comeback 機構 / コンボ重み付け / 視覚 feedback (particle + SE)
2. **コンボ発生率 3-5% → ドラフト重み付けで 15-20%** (1 行修正)
3. **shrink 15s → 10s** (Hex-a-gone defacto に寄せる)
4. **fadeToGoneDelay 1.0s → 0.5s** (即消失感)
5. **Comeback 機構**: 最下位ボーナス CD-20% 等

### Hex-a-gone 比較 agent の提案
1. **タイル warning フェーズ追加** (1 秒間赤点滅 → fade → gone)
2. **タイル pattern ランダム化 option** (障害物として欠落 tile 配置)

## 明示的に「やらない」リスト (爆速のため判断疲れ排除)
**現時点ではゼロ — User は全部 max scope を選択。**

ただし 6/30 中間 playtest で「面白くない」と判定された場合の plan B として:
- minigame 5→3 (玉入れ + しっぽ狩り逃走カット候補)
- skill 10→6 (grapple/hover/mirage/revive カット候補)
- rule 6→3 (fog/bounce/short_cd カット候補)
- season 3→1 (winner_takes_all のみ候補)

## 関連
- `docs/handoffs/ShrinkingIsland_M1_setup_guide.md` (M1 spec 詳細)
- `docs/minigames/shrinking_island_v2.md` (本日作成、Spelunky 級 polish 仕様)
- `CLAUDE.md` の scope セクション (変更なし)
- `RULES.md` R11 (調査必須ルール、今回適用ケース)

## 関連 task
- #104 縮む島面白さ徹底検証 Phase 1 (completed)
- #105 Synthesizer 統合判定 (completed)
- #106 議論結果 docs 化 (本ファイル、completed)
- #107 縮む島 scene blockout (in_progress)
- #108 ShrinkingIslandManager 実装 (completed)
- #109 SkillDefinition SO 基盤 + 3 effect (completed)
- #110 縮む島 v2 仕様書 (in_progress)
