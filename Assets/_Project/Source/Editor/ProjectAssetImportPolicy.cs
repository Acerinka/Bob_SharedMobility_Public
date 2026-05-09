using UnityEditor;

namespace Bob.SharedMobility.Editor
{
    internal static class ProjectAssetImportPolicy
    {
        public static bool TryResolveTexturePolicy(string unityPath, out TextureImportPolicy policy)
        {
            if (unityPath.Contains("/Art/Textures/References/"))
            {
                policy = new TextureImportPolicy(1024, TextureImporterCompression.Compressed, 50);
                return true;
            }

            if (unityPath.Contains("/Art/Textures/Onboarding/")
                || unityPath.Contains("/Art/Textures/VehicleControls/"))
            {
                policy = new TextureImportPolicy(2048, TextureImporterCompression.CompressedHQ, 60);
                return true;
            }

            policy = new TextureImportPolicy();
            return false;
        }
    }

    internal struct TextureImportPolicy
    {
        public readonly int maxTextureSize;
        public readonly TextureImporterCompression compression;
        public readonly int compressionQuality;

        public TextureImportPolicy(
            int maxTextureSize,
            TextureImporterCompression compression,
            int compressionQuality)
        {
            this.maxTextureSize = maxTextureSize;
            this.compression = compression;
            this.compressionQuality = compressionQuality;
        }
    }
}
