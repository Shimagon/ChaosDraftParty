#if UNITY_EDITOR
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ChaosDraftParty.EditorTools
{
    /// <summary>
    /// 罠 #41 の恒久対策。CreateFontAsset で作った TMP_FontAsset に atlas Texture2D と Material を
    /// sub-asset として明示 AddObjectToAsset しないと Unity 再起動で m_AtlasTextures が null になる。
    /// このスクリプトはメニューから呼ぶ再生成ユーティリティ。
    /// </summary>
    public static class TMPFontAssetRepair
    {
        const string KosugiMaruAssetPath = "Assets/_Project/Art/Fonts/KosugiMaru_Dynamic.asset";
        const string KosugiMaruTTFSourceGuid = "d9996220fc68d4ad18d9e426e6822acb";

        const string PreloadCharacters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "abcdefghijklmnopqrstuvwxyz" +
            "0123456789" +
            "!\"#$%&'()*+,-./:;<=>?@[\\]^_`{|}~ " +
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
            "がぎぐげござじずぜぞだぢづでどばびぶべぼぱぴぷぺぽ" +
            "ぁぃぅぇぉっゃゅょー" +
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
            "ガギグゲゴザジズゼゾダヂヅデドバビブベボパピプペポ" +
            "ァィゥェォッャュョー" +
            "カオスドラフトパーティー勝負試合戦闘準備開始終了勝利敗北参加退出設定戻進次前選択決定取消確認" +
            "…―「」『』、。！？　";

        [MenuItem("Tools/ChaosDraftParty/Repair TMP Font Asset (Kosugi Maru)")]
        public static void RepairKosugiMaru()
        {
            var sourcePath = AssetDatabase.GUIDToAssetPath(KosugiMaruTTFSourceGuid);
            if (string.IsNullOrEmpty(sourcePath))
            {
                EditorUtility.DisplayDialog("Repair failed",
                    "Source font (Kosugi Maru TTF) not found by GUID. Check Assets/_Project/Art/Fonts/", "OK");
                return;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
            if (sourceFont == null)
            {
                EditorUtility.DisplayDialog("Repair failed",
                    $"Could not load Font at {sourcePath}", "OK");
                return;
            }

            Rebuild(sourceFont, KosugiMaruAssetPath, "KosugiMaru_Dynamic");
        }

        static void Rebuild(Font sourceFont, string assetPath, string assetName)
        {
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (existing != null)
            {
                AssetDatabase.DeleteAsset(assetPath);
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                samplingPointSize: 90,
                atlasPadding: 9,
                renderMode: GlyphRenderMode.SDFAA,
                atlasWidth: 1024,
                atlasHeight: 1024,
                atlasPopulationMode: AtlasPopulationMode.Dynamic,
                enableMultiAtlasSupport: true);

            fontAsset.name = assetName;
            fontAsset.hideFlags = HideFlags.None;

            AssetDatabase.CreateAsset(fontAsset, assetPath);

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{assetName} Material";
                fontAsset.material.hideFlags = HideFlags.HideInHierarchy;
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            if (fontAsset.atlasTextures != null)
            {
                for (int i = 0; i < fontAsset.atlasTextures.Length; i++)
                {
                    var tex = fontAsset.atlasTextures[i];
                    if (tex == null) continue;
                    tex.name = $"{assetName} Atlas {i}";
                    tex.hideFlags = HideFlags.HideInHierarchy;
                    AssetDatabase.AddObjectToAsset(tex, fontAsset);
                }
            }

            var missing = new StringBuilder();
            if (!fontAsset.TryAddCharacters(PreloadCharacters, out var missingChars))
            {
                missing.Append(missingChars);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

            int glyphCount = fontAsset.characterTable?.Count ?? 0;
            int atlasCount = fontAsset.atlasTextures?.Count(t => t != null) ?? 0;
            var msg =
                $"TMP Font Asset rebuilt: {assetName}\n" +
                $" characters: {glyphCount}\n" +
                $" atlas textures: {atlasCount}\n" +
                $" material sub-asset: {(fontAsset.material != null)}\n" +
                (missing.Length > 0 ? $" missing glyphs: {missing}\n" : " all preload glyphs baked\n");
            Debug.Log(msg);
            EditorUtility.DisplayDialog("TMP Font Repaired", msg, "OK");
        }

        [MenuItem("Tools/ChaosDraftParty/Verify TMP Font Asset State")]
        public static void Verify()
        {
            var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KosugiMaruAssetPath);
            if (fa == null)
            {
                Debug.LogError("KosugiMaru_Dynamic.asset not found");
                return;
            }

            var atlasOk = fa.atlasTextures != null && fa.atlasTextures.Length > 0 && fa.atlasTextures[0] != null;
            var matOk = fa.material != null;
            var subAssets = AssetDatabase.LoadAllAssetsAtPath(KosugiMaruAssetPath);
            var subNames = string.Join(", ", subAssets.Select(s => $"{s.GetType().Name}:{s.name}"));
            Debug.Log(
                $"[TMP Font Verify] {fa.name}\n" +
                $" atlas ok: {atlasOk}\n" +
                $" material ok: {matOk}\n" +
                $" character count: {fa.characterTable?.Count}\n" +
                $" sub-assets: {subNames}");
        }
    }
}
#endif
