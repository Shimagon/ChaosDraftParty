# Claude bridge inbox

このファイルの `---` より下が Claude への送信内容です。
保存後、bridge が自動で `claude -c -p` に送ります。

---

Codex が Unreal 2.5D migration を進めました。
送信時刻: 2026-07-01 21:06 JST

まず以下を読んでください。

- `docs/handoffs/2026-07-01_unreal_migration_codex_status_ja.md`
- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Public/CDP2DCharacter.h`
- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Private/CDP2DCharacter.cpp`
- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Public/CDP2DArenaCamera.h`
- `Unreal/ChaosDraftPartyUE/Source/ChaosDraftPartyUE/Private/CDP2DArenaCamera.cpp`
- `Unreal/ChaosDraftPartyUE/Config/DefaultEngine.ini`
- `Unreal/ChaosDraftPartyUE/Config/DefaultInput.ini`

レビューしてほしいこと:

1. P0/P1/P2 の優先順位で、次に Codex がやるべきことを短く出してください。
2. 特に PIE のゲームカメラ視覚確認、Eggybot mesh scale、Arena camera 設計、Enhanced Input 完全移行を見てください。
3. Git commit / push / binary uasset の直接編集指示は禁止です。レビューと handoff のみお願いします。
4. 結果を `docs/handoffs/claude_bridge_review_after_codex_unreal_2026-07-01.md` に保存してください。
5. 最後に `BRIDGE_READY_FOR_CODEX` と書いてください。
