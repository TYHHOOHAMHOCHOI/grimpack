using UnityEditor;
using UnityEngine;

namespace Project05.Editor
{
    /// <summary>
    /// Automatically enforces 2D Pixel-Perfect import settings (FilterMode.Point, Uncompressed, No Mipmaps)
    /// on all textures under Assets/_Project/.
    /// </summary>
    public class PixelArtImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/_Project/")) return;

            TextureImporter importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true;
        }
    }
}
