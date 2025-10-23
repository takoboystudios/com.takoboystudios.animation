using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Color = UnityEngine.Color;
using Graphics = System.Drawing.Graphics;

namespace TakoBoyStudios.Animation.Editor
{
    /// <summary>
    /// Editor utility for converting GIF files to sprite sheets with frame timing data.
    /// </summary>
    /// <remarks>
    /// This tool converts animated GIFs into sprite sheets compatible with the TakoBoy animation workflow.
    /// 
    /// Naming Convention:
    /// - Input: AssetName@AnimationName.gif
    /// - Output: AssetName@AnimationName_WxH.png (sprite sheet)
    /// - Output: AssetName@AnimationName.json (frame timing)
    /// 
    /// Features:
    /// - Extracts all frames from GIF
    /// - Preserves frame timing (in milliseconds)
    /// - Creates optimized sprite sheet (max 1024px wide, multiple rows if needed)
    /// - Generates JSON timing file
    /// - Integrates with existing animation workflow
    /// </remarks>
    public static class GifToSpriteSheet
    {
        private const int MAX_SHEET_WIDTH = 1024;
        private const string MENU_PATH = "Assets/TakoBoy Studios/Animation/Convert GIF to Sprite Sheet";
        private const int MENU_PRIORITY = 50;

        /// <summary>
        /// Converts selected GIF files to sprite sheets with timing data.
        /// </summary>
        [MenuItem(MENU_PATH, false, MENU_PRIORITY)]
        public static void ConvertSelectedGifs()
        {
            string[] guids = Selection.assetGUIDs;

            if (guids == null || guids.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "No Selection",
                    "Please select one or more GIF files to convert.",
                    "OK"
                );
                return;
            }

            int successCount = 0;
            int failCount = 0;
            List<string> failedFiles = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                
                // Show progress
                float progress = (float)i / guids.Length;
                if (EditorUtility.DisplayCancelableProgressBar(
                    "Converting GIFs",
                    $"Processing: {Path.GetFileName(path)}",
                    progress))
                {
                    break;
                }

                // Only process GIF files
                if (!path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"[GifToSpriteSheet] Skipping non-GIF file: {path}");
                    continue;
                }

                try
                {
                    if (ConvertGifToSpriteSheet(path))
                    {
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        failedFiles.Add(Path.GetFileName(path));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GifToSpriteSheet] Error converting {path}: {e.Message}");
                    failCount++;
                    failedFiles.Add(Path.GetFileName(path));
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // Show results
            string message = $"GIF Conversion Complete!\n\n" +
                           $"Successfully converted: {successCount}\n" +
                           $"Failed: {failCount}";

            if (failedFiles.Count > 0)
            {
                message += "\n\nFailed files:\n" + string.Join("\n", failedFiles);
            }

            EditorUtility.DisplayDialog("Conversion Complete", message, "OK");
        }

        /// <summary>
        /// Validates if the menu item should be enabled (only if GIF files are selected).
        /// </summary>
        [MenuItem(MENU_PATH, true)]
        private static bool ValidateConvertGifs()
        {
            if (Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0)
                return false;

            // Check if any selected file is a GIF
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Converts a single GIF file to a sprite sheet and timing data.
        /// </summary>
        /// <param name="gifPath">Path to the GIF file</param>
        /// <returns>True if conversion succeeded</returns>
        private static bool ConvertGifToSpriteSheet(string gifPath)
        {
            string fileName = Path.GetFileNameWithoutExtension(gifPath);
            string directory = Path.GetDirectoryName(gifPath);

            // Validate naming convention
            if (!fileName.Contains("@"))
            {
                Debug.LogError(
                    $"[GifToSpriteSheet] File '{fileName}' doesn't follow naming convention.\n" +
                    "Expected: AssetName@AnimationName.gif"
                );
                return false;
            }

            // Get absolute path
            string absolutePath = Path.Combine(Application.dataPath, "..", gifPath);

            // Extract frames and timing
            List<Bitmap> frames;
            List<int> frameDelays;

            try
            {
                ExtractFramesFromGif(absolutePath, out frames, out frameDelays);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GifToSpriteSheet] Failed to extract frames from '{fileName}': {e.Message}");
                return false;
            }

            if (frames.Count == 0)
            {
                Debug.LogError($"[GifToSpriteSheet] No frames found in '{fileName}'");
                return false;
            }

            Debug.Log($"[GifToSpriteSheet] Extracted {frames.Count} frames from '{fileName}'");

            // Get frame dimensions (assuming all frames are same size)
            int frameWidth = frames[0].Width;
            int frameHeight = frames[0].Height;

            // Calculate sprite sheet layout
            int framesPerRow = Mathf.Min(frames.Count, MAX_SHEET_WIDTH / frameWidth);
            int rows = Mathf.CeilToInt((float)frames.Count / framesPerRow);
            int sheetWidth = framesPerRow * frameWidth;
            int sheetHeight = rows * frameHeight;

            Debug.Log(
                $"[GifToSpriteSheet] Creating sprite sheet: {sheetWidth}x{sheetHeight} " +
                $"({framesPerRow} frames per row, {rows} rows)"
            );

            // Create sprite sheet
            Bitmap spriteSheet = new Bitmap(sheetWidth, sheetHeight, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(spriteSheet))
            {
                g.Clear(System.Drawing.Color.Transparent);

                for (int i = 0; i < frames.Count; i++)
                {
                    int col = i % framesPerRow;
                    int row = i / framesPerRow;
                    int x = col * frameWidth;
                    int y = row * frameHeight;

                    g.DrawImage(frames[i], x, y, frameWidth, frameHeight);
                }
            }

            // Save sprite sheet
            string outputFileName = $"{fileName}_{frameWidth}x{frameHeight}.png";
            string outputPath = Path.Combine(directory, outputFileName);
            string absoluteOutputPath = Path.Combine(Application.dataPath, "..", outputPath);

            try
            {
                spriteSheet.Save(absoluteOutputPath, ImageFormat.Png);
                Debug.Log($"[GifToSpriteSheet] Saved sprite sheet: {outputPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GifToSpriteSheet] Failed to save sprite sheet: {e.Message}");
                
                // Cleanup
                spriteSheet.Dispose();
                foreach (var frame in frames)
                    frame.Dispose();
                
                return false;
            }

            // Save JSON timing data
            string jsonFileName = $"{fileName}.json";
            string jsonPath = Path.Combine(directory, jsonFileName);
            string absoluteJsonPath = Path.Combine(Application.dataPath, "..", jsonPath);

            try
            {
                GifTimingData timingData = new GifTimingData();
                timingData.frames = frameDelays.Select(d => (float)d).ToList();

                string json = JsonUtility.ToJson(timingData, true);
                File.WriteAllText(absoluteJsonPath, json);
                
                Debug.Log($"[GifToSpriteSheet] Saved timing data: {jsonPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GifToSpriteSheet] Failed to save JSON timing: {e.Message}");
            }

            // Cleanup
            spriteSheet.Dispose();
            foreach (var frame in frames)
                frame.Dispose();

            Debug.Log($"[GifToSpriteSheet] Successfully converted: {fileName}");
            return true;
        }

        /// <summary>
        /// Extracts frames and timing information from a GIF file.
        /// </summary>
        /// <param name="gifPath">Absolute path to the GIF file</param>
        /// <param name="frames">Output list of frame bitmaps</param>
        /// <param name="frameDelays">Output list of frame delays in milliseconds</param>
        private static void ExtractFramesFromGif(string gifPath, out List<Bitmap> frames, out List<int> frameDelays)
        {
            frames = new List<Bitmap>();
            frameDelays = new List<int>();

            using (Image gifImage = Image.FromFile(gifPath))
            {
                // Get frame dimension for animation
                FrameDimension dimension = new FrameDimension(gifImage.FrameDimensionsList[0]);
                int frameCount = gifImage.GetFrameCount(dimension);

                // Property item for frame delay (ID = 0x5100)
                const int PropertyTagFrameDelay = 0x5100;
                PropertyItem frameDelayItem = null;

                try
                {
                    frameDelayItem = gifImage.GetPropertyItem(PropertyTagFrameDelay);
                }
                catch
                {
                    Debug.LogWarning("[GifToSpriteSheet] Could not read frame delays, using default 100ms");
                }

                for (int i = 0; i < frameCount; i++)
                {
                    // Select frame
                    gifImage.SelectActiveFrame(dimension, i);

                    // Clone frame to avoid issues with GIF disposal
                    Bitmap frameBitmap = new Bitmap(gifImage.Width, gifImage.Height, PixelFormat.Format32bppArgb);
                    using (Graphics g = Graphics.FromImage(frameBitmap))
                    {
                        g.DrawImage(gifImage, 0, 0, gifImage.Width, gifImage.Height);
                    }

                    frames.Add(frameBitmap);

                    // Get frame delay (in 1/100th of a second)
                    int delay = 100; // Default 100ms
                    if (frameDelayItem != null)
                    {
                        // Delays are stored as array of 4-byte integers
                        byte[] delayBytes = frameDelayItem.Value;
                        int delayIndex = i * 4;
                        
                        if (delayIndex + 3 < delayBytes.Length)
                        {
                            int delayIn100th = BitConverter.ToInt32(delayBytes, delayIndex);
                            delay = delayIn100th * 10; // Convert to milliseconds
                            
                            // Some GIFs have 0 delay, use minimum of 10ms
                            if (delay < 10)
                                delay = 10;
                        }
                    }

                    frameDelays.Add(delay);
                }
            }

            Debug.Log($"[GifToSpriteSheet] Extracted {frames.Count} frames with timing data");
        }

        /// <summary>
        /// JSON structure for frame timing data.
        /// </summary>
        [Serializable]
        private class GifTimingData
        {
            public List<float> frames;
        }
    }
}
