using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace TakoBoyStudios.Animation.Editor
{
    /// <summary>
    /// Editor utility for processing sprite textures with proper import settings and slicing.
    /// Unity 6+ compatible using ISpriteEditorDataProvider.
    /// </summary>
    public static class SpriteProcessor
    {
        private const string PPU_PREF_KEY = "TakoBoy_SpritePPU";
        private const int DEFAULT_PPU = 1;

        public static int PixelsPerUnit => EditorPrefs.GetInt(PPU_PREF_KEY, DEFAULT_PPU);

        [MenuItem("Assets/TakoBoy Studios/Sprites/Process Sprite", false, 0)]
        public static void ProcessSprite()
        {
            ProcessSprites(Selection.assetGUIDs, skipIfAlreadyProcessed: false);
        }

        [MenuItem("Assets/TakoBoy Studios/Sprites/Process Sprite (Skip if Processed)", false, 1)]
        public static void ProcessSpriteSkipProcessed()
        {
            ProcessSprites(Selection.assetGUIDs, skipIfAlreadyProcessed: true);
        }

        [MenuItem("Assets/TakoBoy Studios/Sprites/Slice Sprite Sheet", false, 2)]
        public static void SliceSprite()
        {
            ProcessSprites(Selection.assetGUIDs, skipIfAlreadyProcessed: false);
            SliceSprites(Selection.assetGUIDs);
        }

        private static void ProcessSprites(string[] guids, bool skipIfAlreadyProcessed)
        {
            if (guids == null || guids.Length == 0) return;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null) continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                if (skipIfAlreadyProcessed && Math.Abs(importer.spritePixelsPerUnit - PixelsPerUnit) < float.Epsilon)
                    continue;

                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.textureType = TextureImporterType.Sprite;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.filterMode = FilterMode.Point;
                importer.isReadable = true;
                importer.mipmapEnabled = false;

                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                settings.spriteExtrude = 0;
                settings.spriteGenerateFallbackPhysicsShape = false;
                settings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(settings);

                importer.isReadable = false;
                importer.SaveAndReimport();
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
        }

        private static void SliceSprites(string[] guids)
        {
            if (guids == null || guids.Length == 0) return;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null || !texture.name.Contains("_")) continue;

                string[] split = texture.name.Split('_');
                if (split.Length < 2 || !split[split.Length - 1].Contains("x")) continue;

                string sizeStr = split[split.Length - 1];
                string[] sizeParts = sizeStr.Split('x');
                if (sizeParts.Length != 2 || !int.TryParse(sizeParts[0], out int width) || !int.TryParse(sizeParts[1], out int height))
                    continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Set to Multiple sprite mode and make readable
                importer.isReadable = true;
                importer.spriteImportMode = SpriteImportMode.Multiple;
                importer.SaveAndReimport();

                // Reload texture
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                int rows = texture.height / height;
                int columns = texture.width / width;

                // Create sprite rects using Unity 6 API
                List<SpriteRect> spriteRects = new List<SpriteRect>();
                int frame = 0;

                for (int r = rows - 1; r >= 0; r--)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        Color[] colors = texture.GetPixels(c * width, r * height, width, height);
                        if (colors.All(m => Mathf.Approximately(m.a, 0))) continue;

                        SpriteRect spriteRect = new SpriteRect
                        {
                            rect = new Rect(c * width, r * height, width, height),
                            name = $"{texture.name}_{frame}",
                            alignment = SpriteAlignment.BottomCenter,
                            pivot = new Vector2(0.5f, 0f),
                            spriteID = GUID.Generate()
                        };
                        spriteRects.Add(spriteRect);
                        frame++;
                    }
                }

                // Use ISpriteEditorDataProvider (Unity 6 API)
                var dataProviderFactories = new SpriteDataProviderFactories();
                dataProviderFactories.Init();
                var dataProvider = dataProviderFactories.GetSpriteEditorDataProviderFromObject(importer);
                
                if (dataProvider != null)
                {
                    dataProvider.InitSpriteEditorDataProvider();
                    dataProvider.SetSpriteRects(spriteRects.ToArray());
                    dataProvider.Apply();

                    // Save changes
                    var assetImporter = dataProvider.targetObject as AssetImporter;
                    assetImporter?.SaveAndReimport();
                }

                importer.isReadable = false;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
            AssetDatabase.Refresh();
        }
    }

    /// <summary>
    /// Simple input dialog helper for editor.
    /// In production, replace with EditorUtility.DisplayDialog or a proper custom window.
    /// </summary>
    public static class EditorInputDialog
    {
        public static string Show(string title, string message, string defaultValue = "")
        {
            // For now, just return default value
            // TODO: Implement proper input dialog using EditorWindow
            return defaultValue;
        }
    }
}