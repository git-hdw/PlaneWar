using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
static class ReimportPixelArt
{
    static ReimportPixelArt()
    {
        if (SessionState.GetBool("PlaneWarArtImported", false)) return;
        SessionState.SetBool("PlaneWarArtImported", true);
        if (AssetDatabase.IsValidFolder("Assets/Resources/Art"))
            AssetDatabase.ImportAsset("Assets/Resources/Art", ImportAssetOptions.ImportRecursive);
    }
}

public class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.spritePixelsPerUnit = assetPath.Contains("texture_pack") ? 32 : 16;
    }
}
