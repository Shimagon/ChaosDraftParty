# Unity localScale で mesh が動く罠 — pivot offset 補正レシピ

**実証**: 2026-05-19 ChaosDraftParty EGGYBOT 表情アニメ Blink 実装  
**症状**: `localScale.Y` で目を潰したら **目が下に移動して消えた**  
**真因**: mesh の bounds.center が Transform origin からオフセット + Transform に rotation が掛かってる  
**普遍**: scale tween で「縮む方向の visible 中心が pivot に向かって移動」する Unity 標準仕様

---

## なぜ良い解決だったか (User 高評価の理由)

1. **数値で根本原因を特定** — 「scale Y で消える」と言われた時、即「mesh が薄いから」と表面的判断せず、`mesh.bounds.center.z = 0.6845` という具体的数値で「pivot がオフセットしてる」「rotation X -90° で軸対応が swap してる」と物理計算した
2. **失敗を素早く撤回** — 1 周目 (localScale.Y) で失敗 → 2 周目 (Mesh Swap) で失敗 → 3 周目 (localScale.Z) で部分修正 → 4 周目 (scale.Z + position 補正) で本当の解決
3. **再現可能なレシピ化** — 数値で原因特定したので、別 mesh でも同じ手順で適用可

---

## 罠の説明

### Unity の `localScale` の仕様

`Transform.localScale` は **Transform origin を center** に頂点座標を scaling する。

そのため:
- mesh の `bounds.center` が Transform origin と一致してれば scale で「同じ位置で縮む」
- mesh の `bounds.center` がオフセット (例: `bounds.center.z = 0.6845`) してれば、scale で「Transform origin に近づく方向」に visible が移動する

### Transform に rotation がある場合

mesh-local 軸 ≠ world 軸 になる。

例: Transform に `rotation X -90°` が掛かってる場合
- mesh-local +Y → world +Z (前)
- mesh-local +Z → world -Y (下)
- → `localScale.Y` を縮めても world では「上下」じゃなく「前後」に薄くなる

「scale.Y で潰したのに正面から消えた」現象の正体。

### EGGYBOT Eye_L の具体例

```
Eye_L Transform:
  localPosition = (0, -0.5235, -0.0104)
  localRotation = X -90° (eulerAngles 270, 0, 0)
  localScale    = (1, 1, 1)

Eye_L mesh (sharedMesh.bounds):
  center = (-0.100, 0.2516, 0.6845)  ← Transform origin から 0.68m Z オフセット
  size   = (0.090, 0.0315, 0.099)
```

`localScale.Z = 0.05` (5%) にすると:
- mesh の頂点が全部 0.05 倍 → bounds.center.z が 0.6845 → 0.0342
- visible 中心は mesh-local で 0.65m Z 方向に縮む
- rotation X -90° で **world -Y 方向 (下) に 0.65m 移動** ← これが「目が下に行く」

---

## 解決策: scale + position 補正

scale で動く分だけ Transform.localPosition を逆方向に加算して打ち消す。

### コード骨格 (universal pattern)

```csharp
// Awake で取得
Vector3 basePos;
float meshBoundsCenterAxis; // 縮めたい軸 (今回は Z)

void Awake() {
    basePos = target.transform.localPosition;
    var mf = target.GetComponent<MeshFilter>();
    if (mf != null && mf.sharedMesh != null)
        meshBoundsCenterAxis = mf.sharedMesh.bounds.center.z; // mesh の pivot offset
}

void SetScaleWithCorrection(float scaleMul) {
    // 1. scale 適用
    target.transform.localScale = new Vector3(baseScale.x, baseScale.y, baseScale.z * scaleMul);

    // 2. pivot offset 補正
    float displacement = meshBoundsCenterAxis * (1f - scaleMul);
    // Transform rotation を考慮して、displacement を localPosition の対応軸に加算
    // 例: rotation X -90° なら mesh-local +Z = world -Y → localPosition.Y に + displacement
    target.transform.localPosition = new Vector3(basePos.x, basePos.y + displacement, basePos.z);
}
```

### 補正方向の決定方法

1. Transform の rotation を確認 (Euler angles で)
2. 縮めたい mesh-local 軸を特定
3. その mesh-local 軸が **world で どちら方向** か計算
4. visible 中心が動く方向と逆方向に position を補正

EGGYBOT Eye の場合:
- rotation X -90°、scale.Z で縮 → mesh-local +Z は world -Y → 「下」に動く
- 補正: localPosition.Y に + displacement (= 上にずらす)

別 case (rotation Y +90°、scale.X で縮 など) の場合は補正軸が変わる。Transform の rotation matrix を見れば一意に決まる。

---

## デバッグ手順 (同じ罠を踏んだ時)

1. **`mesh.bounds.center` を MCP `execute_code` で出力** → オフセットがあるか確認
2. **`Transform.localRotation` を確認** → rotation で軸対応がズレてるか
3. **scale 適用前後で `transform.position` (world) を比較** → 動いてれば pivot offset 確定
4. **補正値を計算** → `bounds.center.<軸> × (1 - scaleMul)` を localPosition の対応軸に加算

---

## 普遍化された 4 step

任意の Unity プロジェクトで scale tween 系を実装する時:

```
Step 1: mesh.bounds.center を確認 — origin と一致してるか
Step 2: 一致してなければ → pivot offset があるので scale で visible が動く
Step 3: Transform.rotation で軸対応をマップ — mesh-local 軸 → world 軸
Step 4: scale + position 補正をペアで実装 (本レシピのコード骨格を流用)
```

---

## NG パターン (前回の失敗)

| NG | 真因 |
|---|---|
| `localScale.Y` で潰したら mesh が前後方向に薄くなって正面から消失 | rotation X -90° で mesh-local Y = world Z (前後)、上下じゃない |
| `localScale.Z` で潰したら mesh が下に移動 | pivot offset (bounds.center.z=0.68) + rotation で下方向に動く |
| `closedY = 0` で完全に潰す | 3D 凸 mesh が板化 → 数 mm 視認限界以下で消失 |
| `closedY = 0.10` でも消失 | mesh bounds.size.y = 0.032m × 0.10 = 0.003m で視認限界以下 |
| mesh の `bounds.size` で判断 | bounds.size は AABB、`bounds.center` も合わせて pivot offset 把握 |

---

## 関連 memory / docs

- [[research-psychology-industry-pro-view-mandatory]] — 視覚 / 感覚系は並列調査必須
- [[iterative-plan-review-loop-mandatory]] — 計画 → 計画 review → 実装 → review、OK まで反復
- [[shader-pixel-first]] — 数値で実測してから判断 (Shader 系も同じ思想)
- `Assets/_Project/Scripts/Core/EggybotFaceAnimator.cs:SetEyeYScale` — 本レシピの実装

---

## 拡張アイデア

- **mouth_open / mouth_sad** 等 face mesh も同じ罠を踏む可能性。各 mesh の bounds.center を確認して必要なら補正
- **Body squash & stretch** で頭以外の部位も pivot offset があれば応用
- **Camera shake** で position 揺らす時、pivot 中心を考慮

---

**作成**: 2026-05-19  
**根拠**: User フィードバック「目が下に行く、徹底的に調べろ」→ 数値解析 → 補正実装 → User 評価「目の動き良くなった、これ記録、なぜ良かったか」
