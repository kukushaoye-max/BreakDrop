using UnityEditor;
namespace BreakDrop.Editor
{
    public sealed class CandyTextureImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/BreakDrop/Resources/Candy/Approved/")) return;
            var t = (TextureImporter)assetImporter;
            t.isReadable = true;
            t.mipmapEnabled = false;
            t.alphaIsTransparency = true;
            t.npotScale = TextureImporterNPOTScale.None;
            t.maxTextureSize = 4096;
            t.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
