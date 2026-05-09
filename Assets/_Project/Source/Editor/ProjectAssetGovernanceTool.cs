using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Bob.SharedMobility.Editor
{
    public static class ProjectAssetGovernanceTool
    {
        private const string ProjectRoot = "Assets/_Project";
        private const long LargePngWarningBytes = 8L * 1024L * 1024L;
        private const long LargeVideoWarningBytes = 50L * 1024L * 1024L;

        [MenuItem("Tools/Bob Shared Mobility/Assets/Log Large Media Backlog")]
        public static void LogLargeMediaBacklog()
        {
            List<MediaRecord> largeMedia = EnumerateLargeMedia().ToList();
            if (largeMedia.Count == 0)
            {
                Debug.Log("No large media assets found under Assets/_Project.");
                return;
            }

            foreach (MediaRecord record in largeMedia.OrderByDescending(record => record.Bytes))
            {
                Debug.LogWarning(
                    $"{record.Kind}: {FormatSize(record.Bytes)} {record.Path} | {record.Recommendation}");
            }

            Debug.LogWarning($"Large media backlog contains {largeMedia.Count} asset(s).");
        }

        [MenuItem("Tools/Bob Shared Mobility/Assets/Apply Texture Import Policy")]
        public static void ApplyTextureImportPolicy()
        {
            List<string> changedAssets = new List<string>();

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (string path in Directory.GetFiles(ProjectRoot, "*.png", SearchOption.AllDirectories))
                {
                    string unityPath = NormalizePath(path);
                    if (!ProjectAssetImportPolicy.TryResolveTexturePolicy(unityPath, out TextureImportPolicy policy)) continue;

                    TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                    if (importer == null) continue;

                    if (ApplyPolicy(importer, policy))
                    {
                        changedAssets.Add(unityPath);
                        importer.SaveAndReimport();
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            if (changedAssets.Count == 0)
            {
                Debug.Log("Texture import policy is already satisfied.");
                return;
            }

            foreach (string path in changedAssets)
            {
                Debug.Log($"Applied texture import policy: {path}");
            }

            Debug.Log($"Texture import policy updated {changedAssets.Count} asset(s).");
        }

        private static bool ApplyPolicy(TextureImporter importer, TextureImportPolicy policy)
        {
            bool changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.isReadable)
            {
                importer.isReadable = false;
                changed = true;
            }

            if (importer.maxTextureSize != policy.maxTextureSize)
            {
                importer.maxTextureSize = policy.maxTextureSize;
                changed = true;
            }

            if (importer.textureCompression != policy.compression)
            {
                importer.textureCompression = policy.compression;
                changed = true;
            }

            if (importer.compressionQuality != policy.compressionQuality)
            {
                importer.compressionQuality = policy.compressionQuality;
                changed = true;
            }

            return changed;
        }

        private static IEnumerable<MediaRecord> EnumerateLargeMedia()
        {
            foreach (string path in Directory.GetFiles(ProjectRoot, "*", SearchOption.AllDirectories))
            {
                if (path.EndsWith(".meta")) continue;

                string unityPath = NormalizePath(path);
                FileInfo file = new FileInfo(path);
                string extension = file.Extension.ToLowerInvariant();

                if (extension == ".png" && file.Length > LargePngWarningBytes)
                {
                    bool hasPolicy = ProjectAssetImportPolicy.TryResolveTexturePolicy(unityPath, out TextureImportPolicy policy);
                    yield return new MediaRecord(
                        "Large PNG",
                        unityPath,
                        file.Length,
                        hasPolicy
                            ? $"managed import policy: max {policy.maxTextureSize}px, {policy.compression}"
                            : "review whether this belongs in runtime UI or reference-only art");
                }
                else if (extension == ".mp4" && file.Length > LargeVideoWarningBytes)
                {
                    yield return new MediaRecord(
                        "Large MP4",
                        unityPath,
                        file.Length,
                        "re-encode to Unity-friendly H.264 baseline/profile-constrained playback asset");
                }
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace("\\", "/");
        }

        private static string FormatSize(long bytes)
        {
            return $"{bytes / (1024f * 1024f):0.##} MB";
        }

        private struct MediaRecord
        {
            public readonly string Kind;
            public readonly string Path;
            public readonly long Bytes;
            public readonly string Recommendation;

            public MediaRecord(string kind, string path, long bytes, string recommendation)
            {
                Kind = kind;
                Path = path;
                Bytes = bytes;
                Recommendation = recommendation;
            }
        }
    }
}
