#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ChaosDraftParty.Editor
{
    /// <summary>
    /// #116 VFX Prefab generator (BouncePadAaa + SI_TileDustVfx).
    /// 前提: Materials Phase 完了 (M_BouncePad_Ring / M_BouncePad_Pillar / M_BouncePad_Top /
    ///       M_TileDust_Particle / M_TileChip など 7 個生成済)。
    /// 仕様: Synthesizer Phase 3 + handoff doc Section A / B 準拠。
    /// </summary>
    internal static class ShrinkingIslandVfxPrefabGenerator
    {
        const string PrefabFolder = "Assets/_Project/Prefabs/Minigames/ShrinkingIsland";
        const string MatFolder = "Assets/_Project/Art/Materials/ShrinkingIsland";

        [MenuItem("Tools/ChaosDraftParty/Generate VFX Prefabs (#116)")]
        public static void GeneratePrefabs()
        {
            int success = 0;
            int failed = 0;
            var log = new System.Text.StringBuilder();

            EnsureFolder(PrefabFolder);

            try
            {
                BuildBouncePadAaa(log);
                success++;
            }
            catch (System.Exception ex)
            {
                failed++;
                log.AppendLine($"BouncePadAaa FAILED: {ex.Message}");
                Debug.LogError($"[#116] BouncePadAaa: {ex}");
            }

            try
            {
                BuildTileDustVfx(log);
                success++;
            }
            catch (System.Exception ex)
            {
                failed++;
                log.AppendLine($"SI_TileDustVfx FAILED: {ex.Message}");
                Debug.LogError($"[#116] SI_TileDustVfx: {ex}");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string msg = $"Prefab generation: {success}/2 success\n\n{log}";
            EditorUtility.DisplayDialog("VFX Prefab Generator (#116)", msg, "OK");
            Debug.Log($"[#116] {msg}");
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }

        static Material LoadMat(string name)
        {
            string path = $"{MatFolder}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) throw new System.IO.FileNotFoundException($"Material not found: {path}");
            return m;
        }

        // -----------------------------------------------------------------
        // BouncePadAaa.prefab
        // -----------------------------------------------------------------
        static void BuildBouncePadAaa(System.Text.StringBuilder log)
        {
            var matRing = LoadMat("M_BouncePad_Ring");
            var matPillar = LoadMat("M_BouncePad_Pillar");
            var matTop = LoadMat("M_BouncePad_Top");

            var root = new GameObject("BouncePadAaa");
            root.transform.position = Vector3.zero;

            // Components on root
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.isTrigger = true;
            capsule.radius = 0.6f;
            capsule.height = 1.4f;
            capsule.center = new Vector3(0, 0.7f, 0);

            var rb = root.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            // CinemachineImpulseSource (reflection)
            Component impulseSrc = null;
            var impulseType = System.Type.GetType("Unity.Cinemachine.CinemachineImpulseSource, Unity.Cinemachine");
            if (impulseType != null)
            {
                impulseSrc = root.AddComponent(impulseType);
            }
            else
            {
                log.AppendLine("WARN: CinemachineImpulseSource type not found");
            }

            // BouncePad.cs
            var bpType = System.Type.GetType("ChaosDraftParty.Minigames.ShrinkingIsland.BouncePad, ChaosDraftParty.Minigames");
            Component bouncePad = null;
            if (bpType != null)
            {
                bouncePad = root.AddComponent(bpType);
            }
            else
            {
                log.AppendLine("WARN: BouncePad type not found");
            }

            // Child: Pillar
            var pillar = MakeCylinder("Pillar", root.transform, new Vector3(0, 0.4f, 0), new Vector3(0.8f, 0.4f, 0.8f), matPillar);

            // Child: TopDisc
            var topDisc = MakeCylinder("TopDisc", root.transform, new Vector3(0, 0.85f, 0), new Vector3(1.2f, 0.05f, 1.2f), matTop);

            // Child: RingMesh (Quad rotated)
            var ringMesh = GameObject.CreatePrimitive(PrimitiveType.Quad);
            ringMesh.name = "RingMesh";
            ringMesh.transform.SetParent(root.transform, false);
            ringMesh.transform.localPosition = new Vector3(0, 0.9f, 0);
            ringMesh.transform.localRotation = Quaternion.Euler(90, 0, 0);
            ringMesh.transform.localScale = Vector3.one;
            Object.DestroyImmediate(ringMesh.GetComponent<Collider>());
            var ringRenderer = ringMesh.GetComponent<MeshRenderer>();
            ringRenderer.sharedMaterial = matRing;
            ringRenderer.enabled = false; // 初期非表示

            // PS_Streak
            var streakGo = new GameObject("PS_Streak");
            streakGo.transform.SetParent(root.transform, false);
            streakGo.transform.localPosition = new Vector3(0, 0.9f, 0);
            var streakPs = streakGo.AddComponent<ParticleSystem>();
            ConfigStreak(streakPs);

            // PS_Spark
            var sparkGo = new GameObject("PS_Spark");
            sparkGo.transform.SetParent(root.transform, false);
            sparkGo.transform.localPosition = new Vector3(0, 0.9f, 0);
            var sparkPs = sparkGo.AddComponent<ParticleSystem>();
            ConfigSpark(sparkPs);

            // Streak Sub-Emitter -> Spark (Death)
            var sub = streakPs.subEmitters;
            sub.enabled = true;
            sub.AddSubEmitter(sparkPs, ParticleSystemSubEmitterType.Death, ParticleSystemSubEmitterProperties.InheritNothing);

            // PS_IdleGlow (looping)
            var idleGo = new GameObject("PS_IdleGlow");
            idleGo.transform.SetParent(root.transform, false);
            idleGo.transform.localPosition = new Vector3(0, 0.9f, 0);
            var idlePs = idleGo.AddComponent<ParticleSystem>();
            ConfigIdleGlow(idlePs);

            // BouncePad.cs SerializeField wiring
            if (bouncePad != null)
            {
                var so = new SerializedObject(bouncePad);
                SetFloat(so, "_launchHeight", 6.0f);
                SetFloat(so, "_launchDuration", 0.55f);
                SetFloat(so, "_targetXZMagnetism", 0.8f);
                SetFloat(so, "_anticipationDuration", 0.07f);
                SetFloat(so, "_punchDuration", 0.12f);
                SetFloat(so, "_settleDuration", 0.38f);
                SetFloat(so, "_anticipationScaleY", 0.65f);
                SetFloat(so, "_anticipationScaleXZ", 1.30f);
                SetFloat(so, "_punchScaleY", 1.18f);
                SetFloat(so, "_punchScaleXZ", 0.90f);
                SetFloat(so, "_idleFrequency", 0.8f);
                SetFloat(so, "_idleEmissiveAmplitude", 0.25f);
                SetFloat(so, "_ringDuration", 0.15f);
                SetFloat(so, "_ringScaleStart", 0.3f);
                SetFloat(so, "_ringScaleEnd", 2.5f);
                SetFloat(so, "_impulseAmplitude", 0.18f);
                SetFloat(so, "_impulseDuration", 0.35f);
                SetFloat(so, "_reuseCooldown", 1.5f);
                SetFloat(so, "_upwardSpeed", 10.5f);
                SetFloat(so, "_horizontalSpeed", 7.5f);

                SetObj(so, "_visualRenderer", pillar.GetComponent<MeshRenderer>());
                SetObj(so, "_visualRoot", pillar.transform);
                SetObj(so, "_ringMesh", ringRenderer);
                SetObj(so, "_streakParticle", streakPs);
                SetObj(so, "_sparkParticle", sparkPs);
                SetObj(so, "_idleGlowParticle", idlePs);
                SetObj(so, "_impulseSource", impulseSrc);

                so.ApplyModifiedProperties();
            }

            // Save
            string path = $"{PrefabFolder}/BouncePadAaa.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            log.AppendLine($"BouncePadAaa saved: {path}");
        }

        static GameObject MakeCylinder(string name, Transform parent, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = localScale;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static void ConfigStreak(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.12f;
            main.startSpeed = 8.0f;
            main.startSize = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.1f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 4.0f;
            renderer.velocityScale = 0;
        }

        static void ConfigSpark(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 0.5f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.3f;
            main.startSpeed = 3.0f;
            main.startSize = 0.08f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 64;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;
        }

        static void ConfigIdleGlow(ParticleSystem ps)
        {
            var main = ps.main;
            main.duration = 2.0f;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = 1.5f;
            main.startSpeed = 0.3f;
            main.startSize = 0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 32;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 8;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);
        }

        // -----------------------------------------------------------------
        // SI_TileDustVfx.prefab
        // -----------------------------------------------------------------
        static void BuildTileDustVfx(System.Text.StringBuilder log)
        {
            var matDust = LoadMat("M_TileDust_Particle");
            var matChip = LoadMat("M_TileChip");

            var root = new GameObject("SI_TileDustVfx");
            root.transform.position = Vector3.zero;

            // PS_Dust
            var dustGo = new GameObject("PS_Dust");
            dustGo.transform.SetParent(root.transform, false);
            var dustPs = dustGo.AddComponent<ParticleSystem>();
            ConfigDust(dustPs, matDust);

            // PS_Chip
            var chipGo = new GameObject("PS_Chip");
            chipGo.transform.SetParent(root.transform, false);
            var chipPs = chipGo.AddComponent<ParticleSystem>();
            ConfigChip(chipPs, matChip);

            string path = $"{PrefabFolder}/SI_TileDustVfx.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            log.AppendLine($"SI_TileDustVfx saved: {path}");
        }

        static void ConfigDust(ParticleSystem ps, Material mat)
        {
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 0.55f;
            main.startSpeed = 1.2f;
            main.startSize = 0.3f;
            main.startRotation = new ParticleSystem.MinMaxCurve(-30f * Mathf.Deg2Rad, 30f * Mathf.Deg2Rad);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 32;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 45f;
            shape.radius = 0.05f;
            shape.rotation = new Vector3(-90f, 0, 0);
            shape.position = new Vector3(0, 0.02f, 0);

            // ColorOverLifetime (alpha 末端 0 必須)
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(new Color(0.82f, 0.71f, 0.55f, 1f), 0.3f),
                    new GradientColorKey(new Color(0.82f, 0.71f, 0.55f, 1f), 1f)
                },
                new[] {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(0.7f, 0.6f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            col.color = new ParticleSystem.MinMaxGradient(grad);

            // Texture Sheet Animation (4x4 flipbook)
            var tsa = ps.textureSheetAnimation;
            tsa.enabled = true;
            tsa.numTilesX = 4;
            tsa.numTilesY = 4;
            tsa.animation = ParticleSystemAnimationType.WholeSheet;
            tsa.timeMode = ParticleSystemAnimationTimeMode.Lifetime;
            tsa.frameOverTime = new ParticleSystem.MinMaxCurve(1f);
            tsa.startFrame = new ParticleSystem.MinMaxCurve(0f, 3f);
            tsa.cycleCount = 1;

            // SizeOverLifetime
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            var curve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1.5f));
            sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // Renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial = mat;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            renderer.sortingFudge = -0.5f;
            renderer.alignment = ParticleSystemRenderSpace.View;
            renderer.minParticleSize = 0;
            renderer.maxParticleSize = 0.5f;
        }

        static void ConfigChip(ParticleSystem ps, Material mat)
        {
            var main = ps.main;
            main.duration = 0.6f;
            main.loop = false;
            main.playOnAwake = true;
            main.startLifetime = 0.7f;
            main.startSpeed = 3.5f;
            main.startSize = 0.08f;
            main.gravityModifier = 4.0f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            main.maxParticles = 32;
            main.startRotation3D = true;
            main.startRotationX = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startRotationY = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.startRotationZ = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.1f;
            shape.position = new Vector3(0, 0.05f, 0);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Mesh;
            // Cube mesh (built-in)
            var cubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            if (cubeMesh != null) renderer.mesh = cubeMesh;
            renderer.sharedMaterial = mat;
        }

        static void SetFloat(SerializedObject so, string field, float v)
        {
            var p = so.FindProperty(field);
            if (p != null && p.propertyType == SerializedPropertyType.Float) p.floatValue = v;
        }

        static void SetObj(SerializedObject so, string field, Object obj)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = obj;
        }
    }
}
#endif
