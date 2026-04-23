#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace ChaosDraftParty.EditorTools
{
    [InitializeOnLoad]
    static class CharacterLabAutoBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/CharacterLab.unity";
        const string FlagKey = "ChaosDraftParty.CharacterLab.AutoBuilt";

        static CharacterLabAutoBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (SessionState.GetBool(FlagKey, false)) return;
            SessionState.SetBool(FlagKey, true);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null) return;
            Debug.Log("[CharacterLab] Scene not found, auto-building on first compile...");
            CharacterLabBuilder.Build();
        }
    }

    /// <summary>
    /// EGGYBOT 専用の character 検証シーンを生成する。
    /// - Studio ニュートラル背景 + 3-point lighting
    /// - Turntable camera (回転 pivot + Cinemachine 代替の簡易 orbit)
    /// - 4 color variants (pink/blue/yellow/green) を横一列に並べる
    /// - Glass shader tuning 用 HDR rim light
    /// ユーザーが model を iterate する時はこのシーンだけ開いて作業、TitleMenu に影響しない。
    /// </summary>
    public static class CharacterLabBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/CharacterLab.unity";
        const string EGGYBOT_FBX = "Assets/ThirdParty/Meshy/Characters/EGGYBOT/eggybot_base.fbx";

        [MenuItem("Tools/ChaosDraftParty/Build Character Lab Scene")]
        public static void Build()
        {
            // New empty scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Floor ---
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(2, 1, 2);
            var floorMat = LoadOrCreateMat("Assets/_Project/Art/Materials/CharLab_Floor.mat",
                new Color(0.42f, 0.40f, 0.38f, 1f), 0.25f);
            floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;

            // --- Backdrop cyclorama ---
            var backdrop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backdrop.name = "Backdrop";
            backdrop.transform.position = new Vector3(0, 4, 8);
            backdrop.transform.localScale = new Vector3(30, 10, 0.1f);
            var backdropMat = LoadOrCreateMat("Assets/_Project/Art/Materials/CharLab_Backdrop.mat",
                new Color(0.28f, 0.31f, 0.38f, 1f), 0.1f);
            backdrop.GetComponent<MeshRenderer>().sharedMaterial = backdropMat;

            // --- 3-point lighting ---
            var keyGo = new GameObject("KeyLight");
            var key = keyGo.AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1.0f, 0.96f, 0.88f, 1f);
            key.intensity = 1.05f;
            key.shadows = LightShadows.Soft;
            keyGo.transform.rotation = Quaternion.Euler(40, 35, 0);

            var fillGo = new GameObject("FillLight");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.72f, 0.80f, 0.95f, 1f);
            fill.intensity = 0.45f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(30, -135, 0);

            var rimGo = new GameObject("RimLight");
            var rim = rimGo.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(1.0f, 0.72f, 0.88f, 1f);
            rim.intensity = 0.95f;
            rim.shadows = LightShadows.None;
            rimGo.transform.rotation = Quaternion.Euler(-10, 185, 0);

            // Accent top light — crisp specular highlight from above
            var topGo = new GameObject("TopAccent");
            var top = topGo.AddComponent<Light>();
            top.type = LightType.Spot;
            top.color = new Color(1.0f, 0.96f, 0.90f, 1f);
            top.intensity = 8.0f;
            top.range = 10f;
            top.spotAngle = 80f;
            top.innerSpotAngle = 40f;
            top.shadows = LightShadows.None;
            topGo.transform.position = new Vector3(0, 5, -0.5f);
            topGo.transform.rotation = Quaternion.Euler(88, 0, 0);

            // Back kick light — warm rear rim for silhouette separation
            var kickGo = new GameObject("BackKick");
            var kick = kickGo.AddComponent<Light>();
            kick.type = LightType.Directional;
            kick.color = new Color(1.0f, 0.85f, 0.6f, 1f);
            kick.intensity = 0.8f;
            kick.shadows = LightShadows.None;
            kickGo.transform.rotation = Quaternion.Euler(15, 165, 0);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.58f, 0.66f, 0.80f, 1f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.50f, 0.56f, 1f);
            RenderSettings.ambientGroundColor = new Color(0.30f, 0.30f, 0.34f, 1f);
            RenderSettings.ambientIntensity = 0.75f;

            // --- Camera ---
            var camGo = new GameObject("Main Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.17f, 0.22f, 1f);
            cam.fieldOfView = 35f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            // Enable post-processing on the camera (URP)
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;

            // --- Post-processing Volume (global) ---
            var volGo = new GameObject("PostProcessingVolume");
            var vol = volGo.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 1;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = "CharLab_PostFx";
            vol.sharedProfile = profile;

            var bloom = profile.Add<UnityEngine.Rendering.Universal.Bloom>();
            bloom.threshold.overrideState = true; bloom.threshold.value = 1.0f;
            bloom.intensity.overrideState = true; bloom.intensity.value = 0.35f;
            bloom.scatter.overrideState = true; bloom.scatter.value = 0.75f;
            bloom.tint.overrideState = true; bloom.tint.value = new Color(1f, 0.95f, 0.92f, 1f);

            var color = profile.Add<UnityEngine.Rendering.Universal.ColorAdjustments>();
            color.postExposure.overrideState = true; color.postExposure.value = 0.4f;
            color.contrast.overrideState = true; color.contrast.value = 12f;
            color.saturation.overrideState = true; color.saturation.value = 8f;
            color.colorFilter.overrideState = true;
            color.colorFilter.value = new Color(1.02f, 0.98f, 0.96f, 1f);

            var tone = profile.Add<UnityEngine.Rendering.Universal.Tonemapping>();
            tone.mode.overrideState = true; tone.mode.value = UnityEngine.Rendering.Universal.TonemappingMode.ACES;

            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>();
            vignette.intensity.overrideState = true; vignette.intensity.value = 0.28f;
            vignette.smoothness.overrideState = true; vignette.smoothness.value = 0.45f;
            vignette.color.overrideState = true; vignette.color.value = new Color(0.05f, 0.05f, 0.08f, 1f);

            // Depth of Field — gentle background blur (Gaussian mode for simplicity)
            var dof = profile.Add<UnityEngine.Rendering.Universal.DepthOfField>();
            dof.mode.overrideState = true; dof.mode.value = UnityEngine.Rendering.Universal.DepthOfFieldMode.Gaussian;
            dof.gaussianStart.overrideState = true; dof.gaussianStart.value = 3.5f;
            dof.gaussianEnd.overrideState = true;   dof.gaussianEnd.value = 12f;
            dof.gaussianMaxRadius.overrideState = true; dof.gaussianMaxRadius.value = 0.9f;

            // Save profile
            AssetDatabase.CreateAsset(profile, "Assets/_Project/Art/CharLab_PostFx.asset");

            // --- Studio gradient skybox ---
            var skyShader = Shader.Find("ChaosDraftParty/CharLabSkybox");
            if (skyShader != null)
            {
                var skyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/CharLab_Skybox.mat");
                if (skyMat == null)
                {
                    skyMat = new Material(skyShader);
                    AssetDatabase.CreateAsset(skyMat, "Assets/_Project/Art/CharLab_Skybox.mat");
                }
                skyMat.shader = skyShader;
                skyMat.SetColor("_TopColor", new Color(0.20f, 0.24f, 0.32f, 1f));
                skyMat.SetColor("_EquatorColor", new Color(0.52f, 0.52f, 0.56f, 1f));
                skyMat.SetColor("_GroundColor", new Color(0.14f, 0.15f, 0.18f, 1f));
                skyMat.SetFloat("_TopExponent", 1.4f);
                skyMat.SetFloat("_GroundExponent", 2.0f);
                skyMat.SetFloat("_Intensity", 1.0f);
                EditorUtility.SetDirty(skyMat);
                AssetDatabase.SaveAssets();
                RenderSettings.skybox = skyMat;
            }

            // --- Reflection probe for glass reflections ---
            var probeGo = new GameObject("ReflectionProbe");
            probeGo.transform.position = new Vector3(0, 0.6f, 0);
            var probe = probeGo.AddComponent<ReflectionProbe>();
            probe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            probe.refreshMode = UnityEngine.Rendering.ReflectionProbeRefreshMode.OnAwake;
            probe.resolution = 128;
            probe.size = new Vector3(20, 10, 20);
            probe.nearClipPlane = 0.1f;
            probe.farClipPlane = 50f;
            probe.RenderProbe();

            // --- Turntable Pivot (camera orbits around this) ---
            var pivot = new GameObject("TurntablePivot");
            pivot.transform.position = new Vector3(0, 0.6f, 0);
            camGo.transform.SetParent(pivot.transform, true);
            camGo.transform.localPosition = new Vector3(0, 0.4f, -2.8f);
            camGo.transform.LookAt(pivot.transform.position);

            var orbit = pivot.AddComponent<ChaosDraftParty.UI.TurntableRotator>();

            // --- Spawn 4 EGGYBOT variants in a line ---
            var eggybotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EGGYBOT_FBX);
            if (eggybotPrefab == null)
            {
                Debug.LogError($"[CharacterLab] EGGYBOT FBX not found at {EGGYBOT_FBX}. Run Blender build script first.");
                return;
            }

            string[] names = { "Pink", "Blue", "Yellow", "Green" };
            Color[] colors = {
                new Color(0.98f, 0.62f, 0.72f, 1f),
                new Color(0.45f, 0.72f, 0.96f, 1f),
                new Color(0.98f, 0.88f, 0.45f, 1f),
                new Color(0.55f, 0.90f, 0.52f, 1f),
            };
            float[] xs = { -1.5f, -0.5f, 0.5f, 1.5f };

            for (int i = 0; i < 4; i++)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(eggybotPrefab);
                inst.name = "EGGYBOT_" + names[i];
                inst.transform.position = new Vector3(xs[i], 0, 0);
                inst.transform.localScale = Vector3.one * 0.9f;
                inst.transform.rotation = Quaternion.Euler(0, 180, 0);

                var clearMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/EGGYBOT_Clear.mat");
                var eyeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Project/Art/Materials/EGGYBOT_Eye.mat");
                var shineMat = LoadOrCreateMat("Assets/_Project/Art/Materials/EGGYBOT_EyeShine.mat",
                    new Color(1f, 1f, 1f, 1f), 0.9f);
                // Head: matte, 反射ゼロ
                var headMat = LoadOrCreateSoftPlasticMat(
                    $"Assets/_Project/Art/Materials/EGGYBOT_{names[i]}_HeadMatte.mat",
                    colors[i], smoothness: 0.03f, spec: 0.0f, rim: 0.15f);
                // Body + limbs: テカテカ gloss
                var bodyMat = LoadOrCreateSoftPlasticMat(
                    $"Assets/_Project/Art/Materials/EGGYBOT_{names[i]}_BodyGloss.mat",
                    colors[i], smoothness: 0.82f, spec: 1.10f, rim: 0.55f);
                // Blush: same matte-ish as head for consistency
                var blushCol = Color.Lerp(colors[i], new Color(0.9f, 0.30f, 0.40f, 1f), 0.55f);
                var blushMat = LoadOrCreateSoftPlasticMat(
                    $"Assets/_Project/Art/Materials/EGGYBOT_{names[i]}_Blush.mat",
                    blushCol, smoothness: 0.05f, spec: 0.05f, rim: 0.15f);

                foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var n = smr.gameObject.name;
                    if (n == "BodyUpperClear") smr.sharedMaterial = clearMat;
                    else if (n == "Eye_L" || n == "Eye_R") smr.sharedMaterial = eyeMat;
                    else if (n == "EyeShine_L" || n == "EyeShine_R") smr.sharedMaterial = shineMat;
                    else if (n == "Blush_L" || n == "Blush_R") smr.sharedMaterial = blushMat;
                    else if (n == "Head") smr.sharedMaterial = headMat;
                    else smr.sharedMaterial = bodyMat;
                }
                foreach (var mr in inst.GetComponentsInChildren<MeshRenderer>())
                {
                    if (mr.gameObject.name == "Mouth") mr.sharedMaterial = eyeMat;
                    else if (mr.gameObject.name == "Blush_L" || mr.gameObject.name == "Blush_R") mr.sharedMaterial = blushMat;
                }

                var bobber = inst.AddComponent<ChaosDraftParty.UI.PropAnimator>();
                var so = new SerializedObject(bobber);
                so.FindProperty("bobAmplitude").floatValue = 0.03f;
                so.FindProperty("bobFrequency").floatValue = 0.6f + i * 0.12f;
                so.FindProperty("swayAmplitudeDeg").floatValue = 2.5f;
                so.FindProperty("swayFrequency").floatValue = 0.4f;
                so.FindProperty("swayAxis").vector3Value = new Vector3(0, 1, 0);
                so.FindProperty("phaseOffset").floatValue = i * 0.7f;
                so.ApplyModifiedProperties();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log($"[CharacterLab] Scene built and saved to {ScenePath}");
        }

        static Material LoadOrCreateMat(string path, Color color, float smoothness)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material LoadOrCreateMetalMat(string path, Color color)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.72f);
            mat.SetFloat("_Metallic", 0.88f);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        static Material LoadOrCreateSoftPlasticMat(string path, Color baseCol,
            float smoothness = 0.12f, float spec = 0.18f, float rim = 0.30f)
        {
            var shader = Shader.Find("ChaosDraftParty/EGGYBOTSoftPlastic");
            if (shader == null)
            {
                return LoadOrCreateMat(path, baseCol, smoothness);
            }
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.shader = shader;
            mat.SetColor("_BaseColor", baseCol);
            var ambient = Color.Lerp(baseCol, new Color(0.7f, 0.7f, 0.72f, 1f), 0.35f);
            mat.SetColor("_AmbientTint", ambient);
            var sss = Color.Lerp(baseCol, Color.white, -0.15f);
            sss.a = 1f;
            mat.SetColor("_SSSColor", sss);
            mat.SetFloat("_SSSDistortion", 0.30f);
            mat.SetFloat("_SSSPower", 2.2f);
            mat.SetFloat("_SSSScale", 1.05f);
            mat.SetFloat("_WrapFactor", 0.60f);
            mat.SetColor("_RimColor", Color.Lerp(baseCol, Color.white, 0.45f));
            mat.SetFloat("_RimPower", 5.0f);
            mat.SetFloat("_RimIntensity", rim);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_SpecularIntensity", spec);
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }
    }
}
#endif
