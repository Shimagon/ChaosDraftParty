# EGGYBOT 表情アニメ 追加レシピ

**目的**: EGGYBOT (ChaosDraftParty 卵型キャラ) に新規表情を追加する手順をまとめる。  
**前提**: `EggybotFaceAnimator.cs` v2 (Mesh Swap 方式) が attach 済の `EggybotPlayer.prefab` 構造。  
**設計**: Mario Odyssey / Animal Crossing defacto = 「各表情を独立 GO で SetActive 切替」

---

## 現在実装済の表情 (2026-05-19 時点)

| Expression enum | MouthShape | GO 名 | Mesh / 形状 | 用途 |
|---|---|---|---|---|
| `Neutral` | `Smile` | `Mouth` | Baked decal (U 字スマイル) | default 常時 |
| `Surprised` | `Open` | `Mouth_Open` | Cylinder 円板 (5×0.3×7mm の縦長 ovaloid) | 落下中 / 驚き |
| `Hit` | `Sad` | `Mouth_Sad` | Cube 横棒 (5×0.8×0.5cm) | 被弾・無表情寄り |
| `Happy` | `Happy` | `Mouth_Happy` | Cylinder 大円板 (7×0.3×10cm) | 勝利・ハイテンション |
| `KO` | `KO` | `Mouth_KO` | 2 Cube 交差 (× 字、empty parent + 2 child Cube) | KO・倒れた |

目: `Eye_L/R` (開) と `EyeClosed_L/R` (Quad 横棒) の SetActive 切替で blink。

---

## 新規表情を追加する手順 (例: `Wink` 片目つむり)

### Step 1: mesh / GO を作る (Unity Editor 内、Blender 不要で OK)

`FaceVisual` の child に新規 GO を作成。**Mouth_Open と同じ Transform 位置**から相対調整。

```csharp
// MCP execute_code で実行する例 (Wink の場合 = 既存 Eye_L だけ閉じる、Mouth 変えない)
var fv = root.transform.Find("BodyVisual/HeadAnchor/FaceVisual");
var wink = UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Quad); // Eye 用
wink.name = "EyeWink_L";
var col = wink.GetComponent<UnityEngine.Collider>();
if (col != null) UnityEngine.Object.DestroyImmediate(col);
wink.transform.SetParent(fv, false);
wink.transform.localPosition = fv.Find("EyeClosed_L").localPosition; // 既存 base 位置
wink.transform.localRotation = fv.Find("EyeClosed_L").localRotation;
wink.transform.localScale    = fv.Find("EyeClosed_L").localScale;
wink.GetComponent<UnityEngine.MeshRenderer>().sharedMaterial =
    UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/_Project/Art/Materials/EGGYBOT_EyeClosed.mat");
wink.SetActive(false);
```

### Step 2: Material (色を変えるなら)

既存の `EGGYBOT_EyeClosed.mat` (黒、smoothness 0.5) を流用。色変更したい場合は duplicate して `EGGYBOT_Eye_Wink.mat` を作る:

```csharp
UnityEditor.AssetDatabase.CopyAsset(
    "Assets/_Project/Art/Materials/EGGYBOT_EyeClosed.mat",
    "Assets/_Project/Art/Materials/EGGYBOT_Eye_Wink.mat");
```

### Step 3: `EggybotFaceAnimator.cs` に enum + field 追加

```csharp
public enum MouthShape { Smile, Open, Sad, Happy, KO, Wink }  // Wink を末尾追加

[SerializeField] GameObject mouthWink;
// もし「目だけ」変える表情なら、別 enum/field を作る (EyeShape など、現状未実装)
```

`mouthMap` Dictionary にも追加:

```csharp
mouthMap = new Dictionary<MouthShape, GameObject> {
    { MouthShape.Smile, mouthSmile },
    { MouthShape.Open,  mouthOpen  },
    { MouthShape.Sad,   mouthSad   },
    { MouthShape.Happy, mouthHappy },
    { MouthShape.KO,    mouthKO    },
    { MouthShape.Wink,  mouthWink  },
};
```

### Step 4: `SetExpression` の switch case 追加

```csharp
case Expression.Wink:
    SetMouth(MouthShape.Wink);
    StartEmissiveTo(0f);
    break;
```

### Step 5: Prefab に bind

MCP `execute_code` で `SerializedObject` 経由:

```csharp
var so = new UnityEditor.SerializedObject(fa);
so.FindProperty("mouthWink").objectReferenceValue = winkGO;
so.ApplyModifiedPropertiesWithoutUndo();
```

### Step 6: コンパイル確認 → AI review loop → Play test → commit

```bash
mcp UnityMCP refresh_unity
mcp UnityMCP read_console types=[error,warning]
# AI review loop で unity-reviewer → code-reviser → PASS まで反復
git add ... && git commit ...
```

---

## 表情の AAA defacto 値 (Agent A 心理学調査結果)

| 表情 | mesh 形状ヒント | 持続時間 | 発火条件 | 参考 |
|---|---|---|---|---|
| Surprised | 縦長 ovaloid (1.0w × 1.3h)、横長は「叫び」感 NG | 即時 + 0.05-0.15s hold | Driver event (Jump / Hit / Pickup) | Mario Odyssey、Animal Crossing |
| Happy | 大笑顔 (Smile を 1.2× or 別 mesh) | 勝利演出全体 | Win / Pickup / Level Up | Splatoon Inkling |
| Sad | 下向き曲線 or 細い横棒 (無表情寄り) | 失敗演出 + 1-2s | Lose / Damage / Sad event | Hat in Time |
| KO | × 字 / 渦巻き / 斜め線 | KO 演出全体 | HP 0 / KO | Mario series、Kirby |
| Wink | 片目だけ閉じる | 即時 + 0.3s hold | Taunt / Greeting | LocoRoco、Mario |
| Hit | 一瞬 squash (頭 1.1×) + sad mouth | 100ms 即時 + 200ms restore | 被弾 | Cuphead、Splatoon |
| Sleepy | 半閉じ目 + 細い口 | 数秒 | Idle 長時間 | Hat in Time "halfblink" |

---

## NG パターン (前回失敗の反省)

1. **`localScale.Y = 0` で目消す** → 3D 凸 mesh が板化して視認消失 (Among Us 系の平面 face なら OK だが EGGYBOT は凸 mesh)
2. **decal を scale で「Oh」表現** → mesh の pivot offset で下方向に伸びるだけ、「開いた口」に見えない
3. **完全対称 blink (close = open)** → bot 感、Disney Research が「不自然」と実証。Mesh Swap なら hold time だけで OK
4. **横長口 surprise** → 「叫び」「悲鳴」感、cute 表情には NG。**縦長 ovaloid 1.0 × 1.3** が「Oh!」defacto
5. **頬 emissive 入れない** → 「生命感」出ない (Animal Crossing 全採用)。Surprised +0.3 / Falling -0.2
6. **Falling 中 blink 継続** → 恐怖中の人間生理に反する、blink suppress 1.0s

---

## 拡張アイデア (M3 以降)

- **Texture Swap** (Mario Odyssey eye PAT defacto): EGGYBOT_Eye mat に texture 入れて目線 (gaze) 追加可能
- **Passive Eye Track** (Empathic Gaze): カメラに目線が微追従 (weight 0.3-0.5)
- **Anticipation Blink** (Disney "Tangled"): Hit / Damage 直前に 1 拍 blink (既に `PlayBlink()` で実装、外部から呼出)
- **Microexpression** (40-100ms 即時): KO の瞬間に「><」目とか
- **Sleepy / Tired**: Idle 長時間で半閉じ目 + 口がたゆむ

---

## 関連ファイル

- `Assets/_Project/Scripts/Core/EggybotFaceAnimator.cs` — 本体
- `Assets/_Project/Prefabs/Player/EggybotPlayer.prefab` — 配線済 prefab
- `Assets/_Project/Art/Materials/EGGYBOT_Eye.mat` / `EGGYBOT_EyeClosed.mat` — 既存 material
- `Assets/_Project/Scripts/Core/PlayerController.cs` — Jump 踏切で `PlayBlink()` 呼出

## 関連 memory / docs

- [[research-psychology-industry-pro-view-mandatory]] — 新表情追加前は必ず並列調査
- [[ai-review-loop-mandatory]] — 実装後 commit 前に必ず review loop
- [[iterative-plan-review-loop-mandatory]] — 計画も review、OK まで反復
- `docs/ai_playbook/patterns/anticipation_tick_smooth_tail.md` — Anticipation Tick pattern
- `memory/feedback_aaa_emulation_priority.md` — AAA defacto を盗む方針

---

**作成**: 2026-05-19  
**根拠**: 並列 agent 6 個 (Squash 3 + 表情 3) + AAA defacto 調査 + User フィードバック反復
