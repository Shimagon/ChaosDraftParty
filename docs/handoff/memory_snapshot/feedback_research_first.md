---
name: まずは調べろルール（自己判断より先に一次情報）
description: 技術判断・見た目調整をする前に必ず WebSearch / WebFetch / 記事 / 海外ドキュメントを参照してから動く。ChaosDraftParty 全般で適用。
type: feedback
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
自分の推測で material / shader / rendering パラメータを弄る前に **必ず先に調べる**。WebSearch・WebFetch・英語記事・Unity フォーラム・Twitter/X の artist workflow 等、海外情報も含めて参照する。

**Why:** ユーザー発言（2026-04-23）「他の人とか記事とかも参照したら結構精度とかよくなりそうだから参考程度に見て。あなたなら海外のもみれるでしょ。これもルールにした方がいいかな。まずは調べろって」。EGGYBOT の透明カプセル material を alpha 0.18 → 0.42 → 0.62 と勘で当てたが、3 周目でもまだ「ガラスがおかしい」指摘あり。勘ベースの iteration は精度が出ない。

**How to apply:**
- URP / shader / material 調整、アニメーション原則、キャラデザ比率、UI レイアウト等、視覚的品質に関わる判断は **先に WebSearch** してから実装。
- 論文・Unity Manual・GDC talks・artist breakdown（ArtStation, 80.lv, Unity forum, r/Unity3D）を参照。英語記事もそのまま読む。
- 「既知の解決策があるか 3 分調べる → 無ければ自分で設計」の順序を徹底。
- commit message に参考にした URL / article を 1〜2 個添える（検証可能性）。
- 小さい一問一答（typo 修正等）は対象外。視覚品質・architecture・pipeline 設計に限る。
