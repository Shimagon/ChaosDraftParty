# Spring-Damper 物理で Squash & Stretch を作る — 普遍レシピ

**実証**: 2026-05-19 ChaosDraftParty EGGYBOT Squash 移行  
**症状**: coroutine + SmoothDamp の「戻り方が変、戻り中再 trigger で急に通常に戻ってブニブニ」  
**真因**: 位置駆動 (`localScale` 直書き) は discontinuous、velocity 継承なし  
**解決**: Spring-Damper 物理シミュ (2 変数 state、Update 単一駆動、AAA defacto)

User 評価: **「Claude いいね。天才。素晴らしい。記録！」**

---

## なぜ Spring 物理が「連続的で自然」になるのか

3 つの本質:

1. **velocity 継承**
   - 状態を (currentY, velocityY) の 2 変数で持つ
   - target が変わっても velocity は据え置き → 戻り途中で再 trigger しても scale も velocity も飛ばない
   - 新 impulse は `velocityY += impulse` で **加算** → 現状から滑らかに加速

2. **指数減衰**
   - damping で振動が `e^(-ζωt)` で指数的に減衰
   - tail が自然に消える (SmoothDamp の固定時間 tail とは別物)

3. **impulse 加算性**
   - `velocity += impulse` だけで衝撃を追加
   - 重ねがけが物理的に正しい (連続再衝撃で振幅増大、減衰中なら自然に上書き)

---

## 数式 (semi-implicit Euler、Allen Chou defacto)

```
omega     = frequency * 2π
stiffness = omega²
damping   = 2 * dampingRatio * omega

accel = (target - current) * stiffness - velocity * damping
velocity += accel * dt    ← Euler step 1
current  += velocity * dt ← Euler step 2 (semi-implicit、stability 保証)
```

**安定条件** (Allen Chou):
- ζ < 1 (under-damped)
- ω < 10π (~5 Hz)
- dt < 1/30 (60fps なら自然に満たす、frame drop 対策で clamp 推奨)

---

## defacto 値 (Daniel Holden / Maxime Heckel / 業界)

| 用途 | frequency | damping ratio (ζ) | 振動回数 |
|---|---|---|---|
| UI bouncy spring | 1.5-3 Hz | 0.3-0.5 | 2-3 |
| **Character squash (Yoshi/Kirby/LocoRoco 系)** | **2-4 Hz** | **0.3-0.6** | 2 |
| Critical damp (現 SmoothDamp 相当) | - | 1.0 | 0 |
| Suspension (車・台車) | 1-2 Hz | 0.6-0.8 | 1 |

EGGYBOT 採用値: **freq 2.5 Hz / ζ 0.35** (under-damped、ジェリー感)

---

## コード骨格 (新規プロジェクト即適用)

```csharp
using UnityEngine;

[DefaultExecutionOrder(70)]
public class SpringSquash : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Spring-Damper")]
    [Range(0.5f, 8f)]   [SerializeField] float frequency = 2.5f;
    [Range(0.05f, 1.5f)] [SerializeField] float dampingRatio = 0.35f;

    [Header("Impulse (Event 発火用)")]
    [Range(-30f, 0f)] [SerializeField] float landingImpulseMax = -12f;

    [Header("Safety")]
    [Range(0.05f, 0.5f)] [SerializeField] float currentYMinClamp = 0.2f;
    [Range(1f/120f, 1f/10f)] [SerializeField] float dtMaxClamp = 1f/30f;

    Vector3 baseScale;
    float currentY = 1f, velocityY = 0f, targetY = 1f;

    void Awake()
    {
        if (target == null) target = transform;
        baseScale = target.localScale;
    }

    void Update()
    {
        // target 決定 (外部から SetTarget で変更、event ベース)
        // 例: airborne 中は target = 1.10、地上で 1.0

        // semi-implicit Euler spring step
        float omega = frequency * 2f * Mathf.PI;
        float stiffness = omega * omega;
        float damping = 2f * dampingRatio * omega;
        float dt = Mathf.Min(Time.deltaTime, dtMaxClamp);
        float accel = (targetY - currentY) * stiffness - velocityY * damping;
        velocityY += accel * dt;
        currentY  += velocityY * dt;
        currentY = Mathf.Max(currentY, currentYMinClamp); // sqrt(0) 防止

        // 体積保存 + 適用
        float xz = 1f / Mathf.Sqrt(currentY);
        target.localScale = new Vector3(baseScale.x * xz, baseScale.y * currentY, baseScale.z * xz);
    }

    public void AddImpulse(float strength)
    {
        velocityY += strength; // Landing/Hit/Damage 等で呼出
    }

    public void SetTarget(float y) => targetY = y;
}
```

---

## NG パターン

| NG | 真因 |
|---|---|
| **coroutine + SmoothDamp** で squash 復元 | 位置駆動で discontinuous、戻り中再 trigger で reset が見える |
| **explicit Euler** で物理 step | ω·dt > 1 で発散、frame drop で不安定 (semi-implicit 必須) |
| **dampingRatio > 1** で over-damped | 振動なし、現 SmoothDamp と同じ「カクッ」感、ジェリー感 NG |
| **dampingRatio < 0.2** で under-damped | 5 回以上振動、気持ち悪い、ζ 0.3-0.5 sweet spot |
| **target に sin wobble + spring freq 同じ** | ビート / 干渉ノイズ、wobble freq は spring freq の 2x 以上離す |
| **currentY 0 以下に到達** | `sqrt(0)` で xz が ∞、体積保存破綻 (Mathf.Max(0.2, ...) で clamp) |
| **`Time.deltaTime` spike** | 1 frame drop で積分誤差大、`dt = Min(dt, 1/30)` で安全 |

---

## 拡張アイデア

- **複数 target broadcast** (頭 + 手足 + 体): `Transform[] additionalTargets` で同じ currentY を broadcast
- **Spring 2 軸** (Y + XZ 個別): 「縦に潰れる + 横に膨らむ」を別 spring で
- **Event API 階層化**: Landing / Hit / Damage / Pickup / KO で impulse 強度・symbol 切替
- **Network 同期** (M3 オンライン): RPC で impulse broadcast、各 client local で Spring 駆動

---

## 関連 memory / docs

- [[research-psychology-industry-pro-view-mandatory]] — 物理 / 心理学 / 業界 defacto を並列調査
- [[research-first-then-root-solution]] — 表層修正より根本解決
- [[mesh-pivot-offset-scale-correction]] — Blink での pivot offset 補正 (同 session 学習)
- `Assets/_Project/Scripts/Core/EggybotSquashStretch.cs` — 本作の実装

## 参考文献

- [Allen Chou - Numeric Springing](https://allenchou.net/2015/04/game-math-more-on-numeric-springing/)
- [Daniel Holden - Spring-Roll-Call](https://theorangeduck.com/page/spring-roll-call)
- [Alexis Bacot - The Art of Damping](https://www.alexisbacot.com/blog/the-art-of-damping)
- [Ryan Juckett - Damped Springs](https://www.ryanjuckett.com/damped-springs/)
- [Maxime Heckel - Spring Physics](https://blog.maximeheckel.com/posts/the-physics-behind-spring-animations/)

---

**作成**: 2026-05-19  
**根拠**: User 高評価「ちゃんと調べる」獲得、Spring 物理移行成功で記録レシピ化
