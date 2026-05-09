using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bob.SharedMobility.Editor
{
    public static class ProjectIndustrializationValidator
    {
        private const string ProjectRoot = "Assets/_Project";
        private const int LargeScriptWarningLines = 450;
        private const long LargePngWarningBytes = 8L * 1024L * 1024L;
        private const long LargeVideoWarningBytes = 50L * 1024L * 1024L;

        [MenuItem("Tools/Bob Shared Mobility/Validate Project Industrialization")]
        public static void ValidateProjectIndustrialization()
        {
            if (!Directory.Exists(ProjectRoot))
            {
                Debug.LogError($"Project root is missing: {ProjectRoot}");
                return;
            }

            List<string> warnings = new List<string>();

            ValidateSourceGovernance(warnings);
            ValidateAssetGovernance(warnings);

            if (warnings.Count == 0)
            {
                Debug.Log("Project industrialization validation passed.");
                return;
            }

            foreach (string warning in warnings)
            {
                Debug.LogWarning(warning);
            }

            Debug.LogWarning($"Project industrialization validation completed with {warnings.Count} warning(s).");
        }

        private static void ValidateSourceGovernance(List<string> warnings)
        {
            foreach (string path in Directory.GetFiles($"{ProjectRoot}/Source", "*.cs", SearchOption.AllDirectories))
            {
                string unityPath = NormalizePath(path);
                string[] lines = File.ReadAllLines(path);

                if (lines.Length > LargeScriptWarningLines)
                {
                    warnings.Add($"Large script needs ownership review ({lines.Length} lines): {unityPath}");
                }

                string text = string.Join("\n", lines);
                bool isEditorScript = unityPath.Contains("/Editor/");

                if (!isEditorScript && !unityPath.EndsWith("/ProjectInput.cs") && text.Contains("Input.Get"))
                {
                    warnings.Add($"Direct Unity input read should route through ProjectInput: {unityPath}");
                }

                if (!isEditorScript && !IsKeyboardInputOwner(unityPath) && text.Contains("Keyboard.current"))
                {
                    warnings.Add($"Direct keyboard device read should route through ProjectInput: {unityPath}");
                }

                if (!isEditorScript && !IsGamepadInputOwner(unityPath) && text.Contains("Gamepad.current"))
                {
                    warnings.Add($"Direct gamepad device read should route through GamepadButtonReader or ProjectInput: {unityPath}");
                }

                if (!IsSceneWideDiscoveryAllowed(unityPath) && ContainsSceneWideDiscovery(text))
                {
                    warnings.Add($"Scene-wide discovery should be explicit wiring or bootstrap-only fallback: {unityPath}");
                }

                if (!unityPath.EndsWith("/BobTargetIds.cs") && ContainsRawBobTargetLiteral(text))
                {
                    warnings.Add($"Raw Bob target ID literal should go through BobTargetIds or authored scene data: {unityPath}");
                }
            }
        }

        private static void ValidateAssetGovernance(List<string> warnings)
        {
            List<FileInfo> largePngs = new List<FileInfo>();

            foreach (string path in Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".meta")) continue;

                string unityPath = NormalizePath(path);
                FileInfo file = new FileInfo(path);
                string extension = file.Extension.ToLowerInvariant();

                if (!UsesExpectedPrefix(unityPath, extension))
                {
                    warnings.Add($"Asset name does not match project prefix rules: {unityPath}");
                }

                if (extension == ".png" && file.Length > LargePngWarningBytes)
                {
                    largePngs.Add(file);
                }

                if (extension == ".png")
                {
                    ValidateTextureImportPolicy(warnings, unityPath);
                }

                if (extension == ".mp4" && file.Length > LargeVideoWarningBytes)
                {
                    warnings.Add($"Large video should have an explicit compression/encoding pass: {FormatSize(file.Length)} {unityPath}");
                }
            }

            foreach (FileInfo png in largePngs.OrderByDescending(file => file.Length).Take(12))
            {
                warnings.Add($"Large PNG should be reviewed for import size or UI slicing: {FormatSize(png.Length)} {NormalizePath(png.FullName)}");
            }

            if (largePngs.Count > 12)
            {
                warnings.Add($"Large PNG backlog continues: {largePngs.Count - 12} additional PNG file(s) exceed {FormatSize(LargePngWarningBytes)}.");
            }
        }

        private static void ValidateTextureImportPolicy(List<string> warnings, string unityPath)
        {
            if (!ProjectAssetImportPolicy.TryResolveTexturePolicy(unityPath, out TextureImportPolicy policy)) return;

            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer == null) return;

            List<string> issues = new List<string>();

            if (importer.textureType != TextureImporterType.Sprite)
            {
                issues.Add("textureType");
            }

            if (importer.mipmapEnabled)
            {
                issues.Add("mipmaps");
            }

            if (importer.isReadable)
            {
                issues.Add("read/write");
            }

            if (importer.maxTextureSize != policy.maxTextureSize)
            {
                issues.Add($"maxTextureSize {importer.maxTextureSize}->{policy.maxTextureSize}");
            }

            if (importer.textureCompression != policy.compression)
            {
                issues.Add($"compression {importer.textureCompression}->{policy.compression}");
            }

            if (importer.compressionQuality != policy.compressionQuality)
            {
                issues.Add($"quality {importer.compressionQuality}->{policy.compressionQuality}");
            }

            if (issues.Count > 0)
            {
                warnings.Add($"Texture import policy mismatch ({string.Join(", ", issues)}): {unityPath}. Run Tools/Bob Shared Mobility/Assets/Apply Texture Import Policy.");
            }
        }

        private static bool UsesExpectedPrefix(string unityPath, string extension)
        {
            if (unityPath.Contains("/Documentation/")) return true;

            string fileName = Path.GetFileName(unityPath);
            if (string.IsNullOrEmpty(fileName)) return true;

            switch (extension)
            {
                case ".prefab":
                    return fileName.StartsWith("PF_");
                case ".fbx":
                    return fileName.StartsWith("MODEL_");
                case ".png":
                    return unityPath.Contains("/References/")
                        ? fileName.StartsWith("REF_")
                        : fileName.StartsWith("TEX_");
                case ".mat":
                    return fileName.StartsWith("MAT_");
                case ".shadergraph":
                    return fileName.StartsWith("SG_");
                case ".rendertexture":
                    return fileName.StartsWith("RT_");
                case ".mp3":
                    return fileName.StartsWith("VO_");
                case ".mp4":
                    return fileName.StartsWith("VID_");
                default:
                    return true;
            }
        }

        private static bool ContainsSceneWideDiscovery(string text)
        {
            return text.Contains("FindObjectOfType")
                || text.Contains("FindObjectsOfType")
                || text.Contains("Resources.FindObjectsOfTypeAll")
                || text.Contains("GameObject.Find");
        }

        private static bool ContainsRawBobTargetLiteral(string text)
        {
            return text.Contains("\"Mapfull\"")
                || text.Contains("targetID == \"Map\"")
                || text.Contains("targetID == \"map\"")
                || text.Contains("targetID.Equals(\"Map\"")
                || text.Contains("targetID.Equals(\"map\"");
        }

        private static bool IsSceneWideDiscoveryAllowed(string unityPath)
        {
            return unityPath.Contains("/Editor/")
                || unityPath.EndsWith("/AppNavigationService.Registry.cs")
                || unityPath.EndsWith("/RuntimeDiagnosticsHub.Diagnostics.cs")
                || unityPath.EndsWith("/ScenePointerRouting.cs")
                || unityPath.EndsWith("/SceneWorldPointerRouter.cs");
        }

        private static bool IsKeyboardInputOwner(string unityPath)
        {
            return unityPath.EndsWith("/ProjectInput.cs");
        }

        private static bool IsGamepadInputOwner(string unityPath)
        {
            return unityPath.EndsWith("/ProjectInput.cs")
                || unityPath.EndsWith("/GamepadButtonReader.cs")
                || unityPath.EndsWith("/GamepadInputDebugger.cs");
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }

        private static string FormatSize(long bytes)
        {
            return $"{bytes / (1024f * 1024f):0.##} MB";
        }
    }
}
