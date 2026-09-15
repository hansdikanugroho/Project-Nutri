using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

internal static class ExportGaeguTmpFonts
{
    private const string SourceFolder = "Assets/Fonts/Gaegu";
    private const string OutputFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";

    [InitializeOnLoadMethod]
    private static void ExportMissingFontsOnLoad()
    {
        EditorApplication.delayCall += Export;
    }

    [MenuItem("Tools/Fonts/Export Gaegu TMP Fonts")]
    public static void Export()
    {
        ExportFont("Gaegu-Regular.ttf", "Gaegu-Regular SDF.asset");
        ExportFont("Gaegu-Light.ttf", "Gaegu-Light SDF.asset");
        ExportFont("Gaegu-Bold.ttf", "Gaegu-Bold SDF.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Gaegu TMP font assets exported to {OutputFolder}.");
    }

    private static void ExportFont(string sourceName, string outputName)
    {
        string sourcePath = $"{SourceFolder}/{sourceName}";
        string outputPath = $"{OutputFolder}/{outputName}";

        if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath) != null)
        {
            Debug.Log($"Skipped existing TMP font asset: {outputPath}");
            return;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourcePath);
        if (sourceFont == null)
        {
            Debug.LogError($"Source font was not found: {sourcePath}");
            return;
        }

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
        fontAsset.name = Path.GetFileNameWithoutExtension(outputName);
        fontAsset.material.name = $"{fontAsset.name} Material";
        fontAsset.atlasTexture.name = $"{fontAsset.name} Atlas";

        AssetDatabase.CreateAsset(fontAsset, outputPath);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        EditorUtility.SetDirty(fontAsset);
    }
}
