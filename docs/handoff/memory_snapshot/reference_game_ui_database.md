---
name: Game UI Database (UI リファレンス)
description: ChaosDraftParty の UI 設計で参照する Game UI Database のブックマーク。Unity 臭消し・最強タイトル画面の参考に使う。
type: reference
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
## URL

https://www.gameuidatabase.com/index.php

## 用途

- **Unity 臭/AI 臭の排除**：市販ゲームのタイトル・メニュー・HUD を片っ端から見て「普通のゲーム感」を学ぶ
- **タイトル画面レイアウト研究**：ロゴ配置、背景の扱い（3D シーン vs イラスト vs 単色）、ボタン装飾
- **カテゴリで絞れる**：Genre, Platform, UI Screen, Art Style などで検索可

## ユーザー taisei が 2026-04-22 に指定した具体例

1. **Alba: A Wildlife Adventure** — 手書き風ロゴ + 3D のどかな風景背景 + 最小限のメニューテキスト
2. **All of You** — カートゥーン風ランドスケープ + スタイライズドロゴ + 装飾的な角丸ボタン（CONTINUE / NEW GAME / LOAD GAME / SETTINGS）

## ChaosDraftParty のタイトル画面への落とし込み方針

- **背景**：フラット Skybox ではなく 3D のちょっとした風景（浮島 / ステージ / キャラ立ちポーズ等）
- **ロゴ**：デフォルト TMP 黄色文字は禁止。カスタムロゴ画像 or 装飾テキスト
- **ボタン**：矩形・単色は禁止。角丸 + アイコン + ホバーで弾む Fall Guys 感
- **カメラ**：静止禁止、微かにドリー or ブリージング
- **パーティクル**：花びら / 光 / 小物でアンビエンス

## When to use

- タイトル / メニュー / HUD を新設・改修する時は先にここを開いて 5 個以上例を見る
- 「Unity 臭がする」とユーザーに言われたら即参照
