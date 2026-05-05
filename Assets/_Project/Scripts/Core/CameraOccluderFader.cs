using System.Collections.Generic;
using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// 原神風: Camera と Player の間にある障害物を半透明化する (M1 簡易版)。
    ///
    /// 仕組み:
    /// 1. LateUpdate で Camera→Player の方向に <see cref="UnityEngine.Physics.SphereCastNonAlloc"/>
    /// 2. <see cref="occlusionMask"/> に該当する Renderer を集めて、fade material に swap
    /// 3. 障害物が外れたら元の material 配列に restore
    ///
    /// 制約 (M1 → M3 で改善):
    /// - Material swap 方式: 全 sharedMaterials が一括で swap される (per-submesh fade 不可)
    /// - 4 player の場合は本コンポーネントを各 player camera に attach (player 1 つだけなら十分)
    /// - 本格 dither (screen-door transparency) は M3 で URP Shader Graph に置換予定
    ///
    /// 推奨 setup:
    /// - 障害物 (壁、天井) を Layer "OcclusionFade" に登録
    /// - <see cref="occlusionMask"/> = OcclusionFade のみ
    /// - <see cref="fadeMaterial"/> = URP Lit Surface Type Transparent、alpha 0.4 程度
    /// </summary>
    [DefaultExecutionOrder(60)] // PlayerController(50) の後
    public class CameraOccluderFader : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("追跡対象 (player の体中央、または CameraTarget)。")]
        [SerializeField] Transform target;

        [Tooltip("カメラ Transform (CinemachineBrain 付き Main Camera 推奨、null=Camera.main)。")]
        [SerializeField] Transform cameraTransform;

        [Header("Detection")]
        [Tooltip("障害物 layer mask。OcclusionFade レイヤーのみ推奨。")]
        [SerializeField] LayerMask occlusionMask = ~0;

        [Tooltip("SphereCast の半径 (m)。0.3 推奨 (CC 半径より大きく)。")]
        [SerializeField] float sphereRadius = 0.3f;

        [Tooltip("camera→target の距離より少し短くして自分自身は当てない。0.5 推奨。")]
        [SerializeField] float distanceShorten = 0.5f;

        [Tooltip("最大同時 fade 数。SphereCastNonAlloc バッファサイズ。8 で十分。")]
        [SerializeField] int maxOccluders = 8;

        [Header("Fade")]
        [Tooltip("障害物に適用する透過 material。URP Lit Transparent + alpha 0.4 推奨。")]
        [SerializeField] Material fadeMaterial;

        readonly Dictionary<Renderer, Material[]> originalMaterials = new();
        readonly HashSet<Renderer> currentlyFaded = new();
        readonly HashSet<Renderer> hitThisFrame = new();
        RaycastHit[] hitBuffer;

        void Awake()
        {
            hitBuffer = new RaycastHit[maxOccluders];
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (target == null || cameraTransform == null || fadeMaterial == null) return;
            hitThisFrame.Clear();

            var diff = target.position - cameraTransform.position;
            float dist = diff.magnitude - distanceShorten;
            if (dist <= 0.05f)
            {
                RestoreAll();
                return;
            }

            int count = Physics.SphereCastNonAlloc(cameraTransform.position, sphereRadius, diff.normalized, hitBuffer, dist, occlusionMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var rend = hitBuffer[i].collider.GetComponentInParent<Renderer>();
                if (rend == null) continue;
                hitThisFrame.Add(rend);
                if (!currentlyFaded.Contains(rend))
                {
                    if (!originalMaterials.ContainsKey(rend))
                        originalMaterials[rend] = rend.sharedMaterials;
                    var mats = new Material[rend.sharedMaterials.Length];
                    for (int j = 0; j < mats.Length; j++) mats[j] = fadeMaterial;
                    rend.sharedMaterials = mats;
                    currentlyFaded.Add(rend);
                }
            }

            // Restore renderers that are no longer hit
            // (HashSet enumeration during modification 不可、temp list 使用)
            using var tmp = ListPool<Renderer>.Get(out var toRestore);
            foreach (var r in currentlyFaded)
                if (!hitThisFrame.Contains(r)) toRestore.Add(r);
            foreach (var r in toRestore)
            {
                if (r != null && originalMaterials.TryGetValue(r, out var orig))
                    r.sharedMaterials = orig;
                currentlyFaded.Remove(r);
            }
        }

        void RestoreAll()
        {
            foreach (var r in currentlyFaded)
            {
                if (r != null && originalMaterials.TryGetValue(r, out var orig))
                    r.sharedMaterials = orig;
            }
            currentlyFaded.Clear();
        }

        void OnDisable() { RestoreAll(); }
        void OnDestroy() { RestoreAll(); }

        // Mini object pool for List<T> to avoid GC alloc each frame
        static class ListPool<T>
        {
            static readonly Stack<List<T>> pool = new();
            public static PooledList Get(out List<T> list)
            {
                list = pool.Count > 0 ? pool.Pop() : new List<T>();
                return new PooledList(list);
            }
            public struct PooledList : System.IDisposable
            {
                readonly List<T> list;
                public PooledList(List<T> l) { list = l; }
                public void Dispose() { list.Clear(); pool.Push(list); }
            }
        }
    }
}
