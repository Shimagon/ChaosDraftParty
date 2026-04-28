---
name: EGGYBOT v16 SDF clip 構成（採用）
description: カプセル内部に侵入した hand/foot を SDF discard で消す方式。v16 で「変に伸ばされない」承認済構成
type: project
originSessionId: 52a42a6e-ba88-454c-870f-4d359bd4370f
---
EGGYBOT v16 で「腕/足が伸びて見える」「bowl 内部に侵入した sphere が透けて見える」問題を解決した構成。2026-04-28 ユーザーから「いい感じ。素晴らしい。変に伸ばされてない」承認。

**Why:**
- v15 までの skinned mesh + HAND_STRETCH 焼き込みでは bind-pose 補間で形状歪みが出ていた
- v15.18 で rigid Transform parent に切り替えても、bowl 内部に侵入した hand sphere の内側が透けて見える問題が残った
- shader の view-occlusion (v15.18.2) では完全には消せず、ユーザー判断で「形状で解決」へ

**How to apply:**
- `EGGYBOT_v16` root 直下に空の `CapsuleVolume` を作る (localPos 0,0.3,0 / scale 0.672 程度＝body radius と一致)
- `EggybotShaderBinder` を root にアタッチし `bodyCenterTransform = CapsuleVolume` を設定（bodyRadius=0.336, clipMarginMM=0, trackHipsBone=false）
- `EGGYBOT_Pink_Arm.mat` (`ChaosDraftParty/EGGYBOTArmClip` shader) を Hand_L/R + Foot_L/R の MeshRenderer に適用
- shader が `worldPos - bodyCenter` の SDF 距離 < bodyRadius のピクセルを discard する → カプセルに「埋まった」部分だけ消える
- カプセルサイズを変えたいときは `CapsuleVolume.localScale` を Inspector で動かすだけで良い（binder 経由で global uniform に流れる）

採用 commit: `90bcff7` (`feat(eggybot): v16 — CapsuleVolume + ArmClip で内部 hand/foot 消去`)
