using UnityEditor;
using UnityEngine;
using System.IO;

public class SVGAsTextAssetImporter : AssetPostprocessor
{
    static void OnPostprocessAllAssets(
        string[] importedAssets, string[] _, string[] __, string[] ___)
    {
        foreach (var assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".svg"))
            {
                // Check if Unity treated this as a Sprite
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                if (asset is not TextAsset)
                {
                    string fullPath = Path.Combine(Application.dataPath, assetPath.Replace("Assets/", ""));
                    if (File.Exists(fullPath))
                    {
                        string content = File.ReadAllText(fullPath);
                        string textAssetPath = Path.ChangeExtension(assetPath, ".svg.txt");

                        File.WriteAllText(textAssetPath, content);
                        Debug.Log($"Converted SVG to TextAsset: {textAssetPath}");

                        AssetDatabase.Refresh();
                    }
                }
            }
        }
    }
}
