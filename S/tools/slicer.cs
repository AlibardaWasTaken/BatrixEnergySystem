using ENSYS;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ENSYS.ENSYSCore;
using static Utility.UtilityMethods;
using Random = UnityEngine.Random;

namespace Utility
{
    public static class TextureAutoSlicerRuntime
    {
        public static Dictionary<Texture2D, List<Sprite>> CachedText = new Dictionary<Texture2D, List<Sprite>>();


        private static Dictionary<Texture2D, Texture2D> TrimmedTextureCash = new Dictionary<Texture2D, Texture2D>();

        
        public static Texture2D TrimTexture(Texture2D texture1, Texture2D texture2)
        {
            if (TrimmedTextureCash.TryGetValue(texture2, out Texture2D cachedTexture))
            {
                return cachedTexture;
            }

            if (texture2.width <= texture1.width && texture2.height <= texture1.height)
            {
                
                TrimmedTextureCash[texture2] = texture2;
                return texture2;
            }

         
            Texture2D trimmedTexture = new Texture2D(texture1.width, texture1.height);

      
            Color[] pixels = texture2.GetPixels(0, 0, texture1.width, texture1.height);
            trimmedTexture.SetPixels(pixels);
            trimmedTexture.Apply();

            
            TrimmedTextureCash[texture2] = trimmedTexture;

            return trimmedTexture;
        }


        /// <summary>
        /// Automatically slices a texture into sprites by detecting non-transparent regions at runtime.
        /// </summary>
        /// <param name="texture">The Texture2D to slice (must be readable).</param>
        /// <param name="alphaTolerance">The alpha tolerance to consider a pixel as transparent (0-1).</param>
        /// <param name="minSpriteSize">Minimum size of sprites to detect (in pixels).</param>
        /// <param name="padding">Padding around each sprite (in pixels).</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the created sprites.</param>
        /// <returns>A list of Sprites created from the texture.</returns>
        public static List<Sprite> AutoSliceTexture(
            Texture2D texture,
            float pixelsPerUnit = 35.0f,
            float alphaTolerance = 0.1f,
            int minSpriteSize = 4,
            int padding = 0
            )
        {
            if (texture == null)
            {
                Debug.LogError("Texture is null.");
                return null;
            }

            // Ensure the texture is readable
            if (!texture.isReadable)
            {
                Debug.LogError("Texture is not readable. Please set the texture's Read/Write Enabled flag in the import settings.");
                return null;
            }

            if (CachedText.TryGetValue(texture, out var list))
            {
                return list;
            }

            Color[] pixels = texture.GetPixels();
            int width = texture.width;
            int height = texture.height;

            // Keep track of visited pixels
            bool[] visited = new bool[pixels.Length];

            List<Sprite> sprites = new List<Sprite>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (visited[index])
                        continue;

                    if (pixels[index].a > alphaTolerance)
                    {
                        // Start a new sprite detection
                        Rect spriteRect = GetSpriteRect(pixels, visited, width, height, x, y, alphaTolerance);

                        // Check if the sprite size meets the minimum size requirement
                        if (spriteRect.width >= minSpriteSize && spriteRect.height >= minSpriteSize)
                        {
                            // Apply padding
                            spriteRect.xMin = Mathf.Max(0, spriteRect.xMin - padding);
                            spriteRect.yMin = Mathf.Max(0, spriteRect.yMin - padding);
                            spriteRect.xMax = Mathf.Min(width, spriteRect.xMax + padding);
                            spriteRect.yMax = Mathf.Min(height, spriteRect.yMax + padding);

                            // Create a new sprite from the defined rect with the specified pixels per unit
                            Sprite sprite = Sprite.Create(texture, Rect(spriteRect), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                            sprites.Add(sprite);
                        }
                    }
                    else
                    {
                        visited[index] = true;
                    }
                }
            }

            Debug.Log("Automatic texture slicing completed successfully. Slices created: " + sprites.Count);
            CachedText.Add(texture, sprites);
            return sprites;
        }

        private static Rect GetSpriteRect(Color[] pixels, bool[] visited, int width, int height, int startX, int startY, float alphaTolerance)
        {
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(startX, startY));

            int xMin = startX;
            int xMax = startX;
            int yMin = startY;
            int yMax = startY;

            while (queue.Count > 0)
            {
                Vector2Int pixel = queue.Dequeue();
                int x = pixel.x;
                int y = pixel.y;

                if (x < 0 || x >= width || y < 0 || y >= height)
                    continue;

                int index = y * width + x;

                if (visited[index])
                    continue;

                if (pixels[index].a <= alphaTolerance)
                {
                    visited[index] = true;
                    continue;
                }

                visited[index] = true;

                // Update bounds
                xMin = Mathf.Min(xMin, x);
                xMax = Mathf.Max(xMax, x);
                yMin = Mathf.Min(yMin, y);
                yMax = Mathf.Max(yMax, y);

                // Enqueue neighboring pixels
                queue.Enqueue(new Vector2Int(x + 1, y));
                queue.Enqueue(new Vector2Int(x - 1, y));
                queue.Enqueue(new Vector2Int(x, y + 1));
                queue.Enqueue(new Vector2Int(x, y - 1));
            }

            // Convert to Rect
            Rect rect = new Rect(xMin, yMin, xMax - xMin + 1, yMax - yMin + 1);
            return rect;
        }


        private static Dictionary<string, List<Sprite>> spriteCache = new Dictionary<string, List<Sprite>>();

        /// <summary>
        /// Slices a texture into sprites of specified width and height, with caching.
        /// </summary>
        /// <param name="texture">The texture to slice.</param>
        /// <param name="sliceWidth">The width of each slice.</param>
        /// <param name="sliceHeight">The height of each slice.</param>
        /// <param name="pixelsPerUnit">Pixels per unit for the sprites.</param>
        /// <returns>A list of sprites generated from the texture.</returns>
        public static List<Sprite> ResolutionSliceTexture(Texture2D texture, int sliceWidth = 12, int sliceHeight = 12, float pixelsPerUnit = 35)
        {
            Vector2 pivot = new Vector2(0.5f, 0.5f);

            if (texture == null)
            {
                Debug.LogError("Texture is null.");
                return new List<Sprite>();
            }

            if (sliceWidth <= 0 || sliceHeight <= 0)
            {
                Debug.LogError("Slice width and height must be positive integers.");
                return new List<Sprite>();
            }

            // Generate a unique cache key based on texture and parameters
            string cacheKey = GenerateCacheKey(texture, sliceWidth, sliceHeight, pixelsPerUnit);

            // Check if the sprites are already in the cache
            if (spriteCache.ContainsKey(cacheKey))
            {
                return spriteCache[cacheKey];
            }

            List<Sprite> sprites = new List<Sprite>();
            int textureWidth = texture.width;
            int textureHeight = texture.height;

            // Calculate the number of slices needed in each direction
            int slicesX = Mathf.CeilToInt((float)textureWidth / sliceWidth);
            int slicesY = Mathf.CeilToInt((float)textureHeight / sliceHeight);

            // Loop over the texture to create sprites
            for (int y = 0; y < slicesY; y++)
            {
                int yPos = textureHeight - (y + 1) * sliceHeight;
                int currentSliceHeight = sliceHeight;

                // Adjust for edges
                if (yPos < 0)
                {
                    currentSliceHeight += yPos;
                    yPos = 0;
                }
                if (yPos + currentSliceHeight > textureHeight)
                {
                    currentSliceHeight = textureHeight - yPos;
                }

                for (int x = 0; x < slicesX; x++)
                {
                    int xPos = x * sliceWidth;
                    int currentSliceWidth = sliceWidth;

                    if (xPos + currentSliceWidth > textureWidth)
                    {
                        currentSliceWidth = textureWidth - xPos;
                    }

                    // Create a new Texture2D for the slice
                    Texture2D slice = new Texture2D(currentSliceWidth, currentSliceHeight);
                    slice.SetPixels(texture.GetPixels(xPos, yPos, currentSliceWidth, currentSliceHeight));
                    slice.filterMode = FilterMode.Point;
                    slice.Apply();

                    // Create a sprite from the slice
                    Sprite sprite = Sprite.Create(slice, new Rect(0, 0, currentSliceWidth, currentSliceHeight), pivot, pixelsPerUnit);
                    sprites.Add(sprite);
                }
            }

            // Store the generated sprites in the cache
            spriteCache[cacheKey] = sprites;

            return sprites;
        }

        /// <summary>
        /// Generates a unique cache key based on texture and parameters.
        /// </summary>
        /// <param name="texture">The texture being sliced.</param>
        /// <param name="sliceWidth">Slice width.</param>
        /// <param name="sliceHeight">Slice height.</param>
        /// <param name="pixelsPerUnit">Pixels per unit.</param>
        /// <returns>A unique string key for caching.</returns>
        private static string GenerateCacheKey(Texture2D texture, int sliceWidth, int sliceHeight, float pixelsPerUnit)
        {
            int textureID = texture.GetInstanceID();
            string key = $"{textureID}_{sliceWidth}_{sliceHeight}_{pixelsPerUnit}";
            return key;
        }


        private static Rect Rect(Rect rect)
        {
            int x = Mathf.RoundToInt(rect.x);
            int y = Mathf.RoundToInt(rect.y);
            int width = Mathf.RoundToInt(rect.width);
            int height = Mathf.RoundToInt(rect.height);
            return new Rect(x, y, width, height);
        }
    }

    public static class JointController
    {
        public static void RemakeConnectedAnchor(GameObject obj, Vector2 Canchor)
        {
            UtilityMethods.DelayedInvoke(0.05f, () =>
            {
                if (obj.TryGetComponent<HingeJoint2D>(out var hj))
                {
                    hj.connectedAnchor = Canchor;
                }
            });


        }
    }


}
