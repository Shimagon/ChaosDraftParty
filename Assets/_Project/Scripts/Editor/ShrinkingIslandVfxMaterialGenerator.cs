// SPDX-License-Identifier: Proprietary
// Issue #116 — Editor 手動 Phase 1: VFX Material 7 個一括生成
//
// 使い方:
//   Unity Editor メニュー: Tools / ChaosDraftParty / Generate VFX Materials (#116)
//
// 出力先: Assets/_Project/Art/Materials/ShrinkingIsland/
//   1. M_BouncePad_Ring.mat        (Custom shader)
//   2. M_TileWarning.mat            (Custom shader)
//   3. M_TileDissolve.mat           (Custom shader)
//   4. M_TileDust_Particle.mat      (URP Particles/Unlit, Transparent + Premultiply)
//   5. M_TileChip.mat               (URP Lit, Opaque, GPU Instancing ON)
//   6. M_BouncePad_Pillar.mat       (URP Lit, Opaque)
//   7. M_BouncePad_Top.mat          (URP Lit, Opaque, Emission ON)
//
// HDR 値 (>1) も Color 構造体でそのまま設定可能。
// 既存 .mat が存在する場合は overwrite。

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ChaosDraftParty.EditorTools
{
    internal static class ShrinkingIslandVfxMaterialGenerator
    {
        private const string TARGET_FOLDER = "Assets/_Project/Art/Materials/ShrinkingIsland";
        private const string KENNEY_SMOKE_GUID_SEARCH = "whitePuff04 t:Texture2D";
        private const string KENNEY_SMOKE_SEARCH_ROOT = "Assets/_Project/ThirdParty/Kenney";

        [MenuItem("Tools/ChaosDraftParty/Generate VFX Materials (#116)")]
        private static void Generate()
        {
            EnsureFolder(TARGET_FOLDER);

            var results = new List<string>();
            int errors = 0;

            // --- 1. M_BouncePad_Ring (Custom shader, HDR rim) ---
            {
                var path = $"{TARGET_FOLDER}/M_BouncePad_Ring.mat";
                var shader = Shader.Find("ChaosDraftParty/ShrinkingIsland/BouncePadRing");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: ChaosDraftParty/ShrinkingIsland/BouncePadRing");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_BouncePad_Ring" };
                    mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
                    // HDR gold base (Intensity は _EmissionMul と shader 側で増幅)
                    mat.SetColor("_RimColor", new Color(1.0f, 0.85f, 0.35f, 1f));
                    mat.SetFloat("_InnerRadius", 0.35f);
                    mat.SetFloat("_OuterRadius", 0.50f);
                    mat.SetFloat("_RingWidth", 0.08f);
                    mat.SetFloat("_Progress", 0f);
                    mat.SetFloat("_EmissionMul", 3.5f);
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 2. M_TileWarning (Custom shader) ---
            {
                var path = $"{TARGET_FOLDER}/M_TileWarning.mat";
                var shader = Shader.Find("ChaosDraftParty/ShrinkingIsland/TileWarning");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: ChaosDraftParty/ShrinkingIsland/TileWarning");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_TileWarning" };
                    mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
                    mat.SetFloat("_WarningProgress", 0f);
                    mat.SetColor("_WarningColor", new Color(1.0f, 0.4f, 0.3f, 1f));
                    mat.SetFloat("_WarningTintLerp", 0.4f);
                    mat.SetFloat("_EdgeEmissionMul", 2.5f);
                    mat.SetFloat("_PulseFrequency", 2.5f);
                    mat.SetFloat("_EdgeWidth", 0.1f);
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 3. M_TileDissolve (Custom shader, HDR edge) ---
            {
                var path = $"{TARGET_FOLDER}/M_TileDissolve.mat";
                var shader = Shader.Find("ChaosDraftParty/ShrinkingIsland/TileDissolve");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: ChaosDraftParty/ShrinkingIsland/TileDissolve");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_TileDissolve" };
                    mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
                    mat.SetFloat("_DissolveProgress", 0f);
                    mat.SetFloat("_DissolveScale", 8.0f);
                    mat.SetFloat("_DissolveEdgeWidth", 0.08f);
                    mat.SetColor("_DissolveEdgeColor", new Color(1.0f, 0.6f, 0.2f, 1f));
                    mat.SetFloat("_EdgeEmissionMul", 4.0f);
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 4. M_TileDust_Particle (URP Particles/Unlit, Transparent + Premultiply) ---
            {
                var path = $"{TARGET_FOLDER}/M_TileDust_Particle.mat";
                var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: Universal Render Pipeline/Particles/Unlit");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_TileDust_Particle" };
                    mat.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));

                    // Surface Type = Transparent (1), Blend Mode = Premultiply (3)
                    mat.SetFloat("_Surface", 1f);
                    mat.SetFloat("_Blend", 3f);
                    mat.SetFloat("_AlphaClip", 0f);
                    mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                    mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetFloat("_ZWrite", 0f);
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHAMODULATE_ON");
                    mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    mat.EnableKeyword("_ALPHAPREMULTIPLY_ON");

                    // Soft Particles ON (URP particle keyword)
                    mat.EnableKeyword("_SOFTPARTICLES_ON");
                    mat.SetFloat("_SoftParticlesEnabled", 1f);
                    mat.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f, 0f, 0f));

                    // Kenney smoke texture を検索 (whitePuff04.png)
                    var smokeTex = FindKenneySmoke();
                    if (smokeTex != null)
                    {
                        mat.SetTexture("_BaseMap", smokeTex);
                    }
                    else
                    {
                        Debug.LogWarning($"[#116] Kenney smoke texture not found at {KENNEY_SMOKE_SEARCH_ROOT}. Material created without BaseMap.");
                    }

                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 5. M_TileChip (URP Lit, Opaque, GPU Instancing) ---
            {
                var path = $"{TARGET_FOLDER}/M_TileChip.mat";
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: Universal Render Pipeline/Lit");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_TileChip" };
                    mat.SetColor("_BaseColor", new Color(0.85f, 0.75f, 0.55f, 1f)); // sandstone
                    mat.SetFloat("_Smoothness", 0.2f);
                    mat.SetFloat("_Metallic", 0f);
                    SetUrpLitOpaque(mat);
                    mat.enableInstancing = true; // 64 chip 同時生成
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 6. M_BouncePad_Pillar (URP Lit, Opaque) ---
            {
                var path = $"{TARGET_FOLDER}/M_BouncePad_Pillar.mat";
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: Universal Render Pipeline/Lit");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_BouncePad_Pillar" };
                    mat.SetColor("_BaseColor", new Color(0.4f, 0.5f, 0.6f, 1f)); // cool blue-grey
                    mat.SetFloat("_Smoothness", 0.4f);
                    mat.SetFloat("_Metallic", 0f);
                    SetUrpLitOpaque(mat);
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            // --- 7. M_BouncePad_Top (URP Lit, Opaque, Emission ON, HDR) ---
            {
                var path = $"{TARGET_FOLDER}/M_BouncePad_Top.mat";
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    Debug.LogError("[#116] Shader not found: Universal Render Pipeline/Lit");
                    errors++;
                }
                else
                {
                    var mat = new Material(shader) { name = "M_BouncePad_Top" };
                    mat.SetColor("_BaseColor", new Color(1.0f, 0.85f, 0.35f, 1f)); // gold accent (ring 同系)
                    mat.SetFloat("_Smoothness", 0.6f);
                    mat.SetFloat("_Metallic", 0f);
                    SetUrpLitOpaque(mat);
                    // Emission ON, HDR base color
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", new Color(0.8f, 0.65f, 0.25f, 1f));
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    SaveMaterial(mat, path);
                    results.Add(path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[#116] Generated {results.Count}/7 materials. Errors: {errors}\n" + string.Join("\n", results));

            if (errors == 0)
            {
                EditorUtility.DisplayDialog(
                    "Generate VFX Materials",
                    $"Success: {results.Count}/7 materials generated.\n\nSee Console for paths.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Generate VFX Materials",
                    $"Completed with {errors} error(s). See Console for details.",
                    "OK");
            }
        }

        private static void SetUrpLitOpaque(Material mat)
        {
            mat.SetFloat("_Surface", 0f); // 0=Opaque
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_AlphaClip", 0f);
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
            mat.SetFloat("_ZWrite", 1f);
            mat.SetOverrideTag("RenderType", "Opaque");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
            mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.DisableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        }

        private static Texture2D FindKenneySmoke()
        {
            var guids = AssetDatabase.FindAssets(KENNEY_SMOKE_GUID_SEARCH, new[] { KENNEY_SMOKE_SEARCH_ROOT });
            if (guids != null && guids.Length > 0)
            {
                var p = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }

            // フォールバック: 任意の whitePuff*.png を拾う
            var fallback = AssetDatabase.FindAssets("whitePuff t:Texture2D", new[] { KENNEY_SMOKE_SEARCH_ROOT });
            if (fallback != null && fallback.Length > 0)
            {
                var p = AssetDatabase.GUIDToAssetPath(fallback[0]);
                return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
            }
            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }
            var parts = folderPath.Split('/');
            var current = parts[0]; // "Assets"
            for (int i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }
                current = next;
            }
        }

        private static void SaveMaterial(Material mat, string path)
        {
            // 既存 .mat が存在する場合は overwrite (削除してから CreateAsset)
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(mat, path);
        }
    }
}
#endif
