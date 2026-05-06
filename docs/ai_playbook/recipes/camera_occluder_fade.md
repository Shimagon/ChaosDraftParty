# Recipe: Camera Occluder Dither Fade (3D TPS / 3rd person)

**確定**: 2026-05-06、8 反復で完成
**用途**: camera と player の間の壁を **dither (screen-door transparency)** で透過。Genshin / Hat in Time / モンハン Wilds defacto。
**前提**: Unity 6 + URP + Cinemachine 3.x

---

## ⚡ 完成形パラメータ (体感最良)

### CameraOccluderFader.cs (C# component)

```yaml
# Detection
sphereRadius:        0.6   # 経路振動防止 (0.3 だと hit/no-hit 振動)
distanceShorten:     0.2   # 低い天井 (0.7m 等) でも届く
targetUpOffset:      1.0   # SphereCast 終点を player 頭上 (CORE FIX、TPS 必須)
maxOccluders:        8

# Dither
maxDither:           0.75  # 完全消失せず網戸残し (酔い対策、業界 defacto)

# Timing — Anticipation Tick + Smooth Tail
anticipationStart:   0.3   # 入り瞬間 maxDither × 30% 即時表示 (精度感の core)
fadeOutTime:         0.15  # anticipationStart → maxDither ぬるっと上昇
fadeInTime:          0.30  # 戻りぬるっと (1:2 非対称、Cinemachine Deoccluder 公式準拠)
holdTime:            0.25  # hit 振動を hysteresis で吸収 (精度の core)
```

### Shader (URP Lit + Bayer 4x4 dither + Alpha Clip)

- **Surface Type**: Opaque (影・Z 正常)
- **Alpha Clipping**: ON
- **Bayer 4x4 matrix** で screen-space dither
- **ForwardLit pass のみ自前 fragment**、shadow/depth/meta は UsePass で URP Lit 再利用
- **Unity 6 / URP 17 では `LitPassFragment` は void + out param 形式** ([gotchas](../gotchas/unity6_urp17_pitfalls.md) 参照)

---

## 🛠️ コード骨格

```csharp
[SerializeField] Transform target;
[SerializeField] Transform cameraTransform;
[SerializeField] LayerMask occlusionMask;
[SerializeField] Material ditherMaterial;
[SerializeField] float maxDither = 0.75f;
[SerializeField] float anticipationStart = 0.3f;
[SerializeField] float fadeOutTime = 0.15f;
[SerializeField] float fadeInTime = 0.30f;
[SerializeField] float holdTime = 0.25f;
[SerializeField] float sphereRadius = 0.6f;
[SerializeField] float distanceShorten = 0.2f;
[SerializeField] float targetUpOffset = 1.0f;

class FadeState {
    public float ditherAmount;
    public float velocity;          // SmoothDamp ref
    public float lastHitTime;
    public Material[] originals;
    public Material[] ditherInstances;
    public bool swapped;
}

readonly Dictionary<Renderer, FadeState> states = new();
readonly HashSet<Renderer> hitThisFrame = new();
readonly List<Renderer> removeList = new();
RaycastHit[] hitBuffer = new RaycastHit[8];

void LateUpdate() {
    if (target == null || cameraTransform == null || ditherMaterial == null) return;
    hitThisFrame.Clear();

    // CORE FIX: SphereCast 終点を player 頭上に上げる
    var aimPoint = target.position + Vector3.up * targetUpOffset;
    var diff = aimPoint - cameraTransform.position;
    float dist = diff.magnitude - distanceShorten;
    if (dist > 0.05f) {
        int count = Physics.SphereCastNonAlloc(cameraTransform.position, sphereRadius,
            diff.normalized, hitBuffer, dist, occlusionMask, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++) {
            var rend = hitBuffer[i].collider.GetComponentInParent<Renderer>();
            if (rend == null) continue;
            hitThisFrame.Add(rend);
            if (!states.ContainsKey(rend)) {
                // Anticipation Tick: 即時 30% 表示で精度感
                states[rend] = new FadeState { ditherAmount = maxDither * anticipationStart };
            }
            states[rend].lastHitTime = Time.time;
        }
    }

    // 各 state を update
    removeList.Clear();
    foreach (var kv in states) {
        var rend = kv.Key; var st = kv.Value;
        if (rend == null) { removeList.Add(rend); continue; }
        bool active = hitThisFrame.Contains(rend) || (Time.time - st.lastHitTime) < holdTime;
        float targetDither = active ? maxDither : 0f;
        float smoothTime = active ? fadeOutTime : fadeInTime;
        st.ditherAmount = Mathf.SmoothDamp(st.ditherAmount, targetDither, ref st.velocity, smoothTime);
        if (st.ditherAmount > 0.005f) {
            EnsureSwapped(rend, st);
            ApplyDitherAmount(st, st.ditherAmount);
        } else {
            if (st.swapped) RestoreOriginal(rend, st);
            removeList.Add(rend);
        }
    }
    foreach (var r in removeList) states.Remove(r);
}

// EnsureSwapped: 元 sharedMaterials を保存、各 submesh 毎に dither material instance に swap
// RestoreOriginal: instance Destroy + 元 material 復帰
// ApplyDitherAmount: 各 instance に SetFloat("_DitherAmount", amount)
// CopyMaterialProperties: 元 material の _BaseMap/_BaseColor/_Metallic/_Smoothness/_BumpMap 等を dither instance にコピー
```

---

## 📋 新規プロジェクトでの実装手順 (7 step)

1. **URP Lit shader を HLSL or Shader Graph で作成** (Bayer 4x4 + Alpha Clip + URP Lit ベース)
2. **Material 1 個作成** (Custom/OcclusionDither shader 使用)
3. **CameraOccluderFader.cs を player に attach**
4. **OcclusionFade Layer (Unity Layer 8 等) を追加** + 透過対象 object に設定
5. **CinemachineDeoccluder の CollideAgainst から OcclusionFade Layer を除外** (camera 寄せ過ぎ防止、必須)
6. **上記パラメータで初期値設定** (anticipationStart=0.3 等)
7. **Play test → Inspector で微調整**

---

## 🧠 学んだ核心原則 (なぜこの値か)

### 原則 1: 「精度感」と「ぬるっと感」は SmoothDamp 単独では両立不可
- 即時 max 開始 → ぱっと感 (精度 OK、滑らかでない)
- 0 から SmoothDamp → 滑らかだが入り遅延 (精度悪化)
- **解**: Anticipation Tick + Smooth Tail (即時 30% + 残り SmoothDamp) → [pattern](../patterns/anticipation_tick_smooth_tail.md) 参照

### 原則 2: Cinemachine Deoccluder defacto = 入り:出 = 1:2 非対称
- 入り (occluded): 0.15-0.20s (速め)
- 出 (recovery): 0.30-0.40s (ぬるっと)
- 関数: 指数減衰 `1 - e^(-4.605 * dt / dampTime)` (`Damper.Damp()`)
- **公式 Cinemachine も同じ** (DampingWhenOccluded 0.2 / Damping 0.4)

### 原則 3: SphereCast 4 点セットは連動
- distanceShorten + targetUpOffset + sphereRadius + holdTime
- **1 個だけ調整しても効かない罠**

### 原則 4: SphereCast 経路の罠 (TPS 特有)
- TPS の camera は player より高い → camera→target line は player の中心を通る
- 低い天井 (Y < player.Y + 1) は line の **下** に落ちる → SphereCast 当たらない
- **解**: `targetUpOffset = +1.0m` で line を player 頭上を通す

### 原則 5: hit 振動の罠
- player or camera が動くと SphereCast 経路微変動
- 同じ壁でも 1 frame ごとに hit/no-hit 振動 → SmoothDamp 減衰 → max まで届かず
- **解**: `holdTime = 0.25s` で active 維持 (AAA defacto 0.20-0.30 帯)

### 原則 6: SphereCast radius の罠
- radius 0.3 だと経路ギリギリで hit/no-hit 振動
- radius 0.6 で経路に余裕 → 安定 hit
- 大きすぎ (1.0+) は誤検知

---

## ⚠️ 試行錯誤の時系列 (8 反復で確定)

| 反復 | 変更 | 結果 |
|---|---|---|
| 1 | distanceShorten 0.5 → 0.2 | 低い天井に届かない解消 |
| 2 | targetUpOffset 0 → 1.0 | **CORE FIX**: 頭上 SphereCast で天井検出 |
| 3 | sphereRadius 0.3 → 0.6 | 経路振動 hit/no-hit 防止 |
| 4 | holdTime 0.10 → 0.25 | hit 振動 active 維持 |
| 5 | 新規 state ditherAmount = maxDither | 即時 max、精度 OK だが「ぱっと」感 |
| 6 | fadeInTime 0.25 → 0.35 | 戻りぬるっと |
| 7 | (失敗) 新規 state = 0 + fadeOutTime 0.20 | ぬるっと出るが精度悪化 |
| 8 | **anticipationStart 0.3 + fadeOutTime 0.15 + fadeInTime 0.30** | **完成: 精度感 + ぬるっと両立** |

---

## 🔗 関連

- [Anticipation Tick + Smooth Tail pattern](../patterns/anticipation_tick_smooth_tail.md) — この pattern の汎用化
- [Unity 6 / URP 17 罠](../gotchas/unity6_urp17_pitfalls.md) — shader / Cinemachine 罠

## 📚 参考リンク

- [Cinemachine Deoccluder 3.1.6](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineDeoccluder.html)
- [Easings cheat sheet](https://easings.net/) — OutExpo / OutQuart 比較
- [Daniel Ilett URP dither transparency](https://danielilett.com/2020-04-19-tut5-5-urp-dither-transparency/)
- [Hat in Time camera dither (UE4)](https://forums.unrealengine.com/t/a-hat-in-time-camera-dithering-object-fade-unreal-engine-4-tutorial/101909)
