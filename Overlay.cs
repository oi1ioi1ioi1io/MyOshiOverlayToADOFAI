using UnityEngine;
using System.IO;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace MyOshiOverlay
{
    public class Overlay : MonoBehaviour
    {
        public string filePath;
        public Texture2D texture;
        public Rect rect = new Rect(100, 100, 200, 200);

        public int maxWidth = 500;
        public int maxHeight = 500;

        public bool isDragging = false;
        public bool isTyping = false;

        private Vector2 dragOffset;

        private bool isGif = false;

        private List<Texture2D> gifFrames = new List<Texture2D>();
        private List<float> gifDelays = new List<float>();

        private const int MaxGifFrames = 420;
        private const int MaxGifMemoryMB = 128;

        private const int MaxGifWidth = 1000;
        private const int MaxGifHeight = 1000;

        private int currentFrame = 0;
        private float gifTimer = 0f;

        private void ClearTexture()
        {
            bool textureIsGifFrame = gifFrames.Contains(texture);

            foreach (Texture2D frame in gifFrames)
            {
                if (frame != null)
                {
                    Destroy(frame);
                }
            }

            gifFrames.Clear();
            gifDelays.Clear();

            if (texture != null && !textureIsGifFrame)
            {
                Destroy(texture);
            }

            texture = null;
            isGif = false;

            currentFrame = 0;
            gifTimer = 0f;
        }

        public void LoadImage()
        {
            ClearTexture();

            if (!File.Exists(filePath))
            {
                Debug.LogWarning("[MyOshiOverlay] File not found: " + filePath);
                return;
            }

            string ext = Path.GetExtension(filePath).ToLower();

            if (ext == ".gif")
            {
                isGif = true;
                LoadGif(filePath);
            }
            else
            {
                isGif = false;
                LoadStaticImage(filePath);
            }
        }

        private void LoadStaticImage(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            UpdateImageSize();
        }

        private void LoadGif(string path)
        {
            currentFrame = 0;
            gifTimer = 0f;

            FileInfo info = new FileInfo(path);

            const long maxGifSize = 128L * 1024L * 1024L;

            if (info.Length > maxGifSize)
            {
                Debug.LogWarning(
                    "[MyOshiOverlay] GIF file too large: "
                    + (info.Length / 1024 / 1024)
                    + "MB"
                );

                return;
            }

            try
            {
                using (Image<Rgba32> gif = Image.Load<Rgba32>(path))
                {
                    if (gif.Width > 2000 || gif.Height > 2000)
                    {
                        Debug.LogWarning(
                            "[MyOshiOverlay] GIF resolution is very large: "
                            + gif.Width + "x" + gif.Height
                        );
                    }

                    int frameCount = gif.Frames.Count;

                    Debug.Log("[MyOshiOverlay] GIF Frames: " + frameCount);

                    for (int i = 0; i < frameCount; i++)
                    {
                        if (gifFrames.Count >= MaxGifFrames)
                        {
                            Debug.LogWarning(
                                "[MyOshiOverlay] GIF frame limit reached. Only the first "
                                + MaxGifFrames + " frames were loaded."
                            );
                            break;
                        }

                        using (Image<Rgba32> frame = gif.Frames.CloneFrame(i))
                        {
                            Image<Rgba32> finalFrame = frame.Clone();

                            // 메모리가 초과할 경우에만 리사이즈
                            if (gif.Width * gif.Height * 4L * frameCount > MaxGifMemoryMB * 1024L * 1024L)
                            {
                                finalFrame = ResizeIfNeeded(frame);
                            }

                            Texture2D tex = ConvertToTexture(finalFrame);

                            gifFrames.Add(tex);

                            float delay = 0.1f;

                            try
                            {
                                var metadata = gif.Frames[i].Metadata.GetGifMetadata();

                                if (metadata.FrameDelay > 0)
                                {
                                    delay = metadata.FrameDelay / 100f;
                                }
                            }
                            catch
                            {
                                // delay 정보가 없으면 기본값 사용
                            }

                            delay = Mathf.Max(delay, 1f / 60f);

                            gifDelays.Add(delay);
                        }
                    }
                }

                if (gifFrames.Count > 0)
                {
                    texture = gifFrames[0];
                    UpdateImageSize();

                    Debug.Log("[MyOshiOverlay] GIF Loaded!");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[MyOshiOverlay] GIF Load Failed: " + e);
            }
        }

        private Image<Rgba32> ResizeIfNeeded(Image<Rgba32> image)
        {
            Image<Rgba32> result = image.Clone();

            if (result.Width <= MaxGifWidth &&
                result.Height <= MaxGifHeight)
            {
                return result;
            }

            float aspect = (float)result.Width / result.Height;

            int width = result.Width;
            int height = result.Height;


            if (width > MaxGifWidth)
            {
                width = MaxGifWidth;
                height = (int)(width / aspect);
            }

            if (height > MaxGifHeight)
            {
                height = MaxGifHeight;
                width = (int)(height * aspect);
            }


            result.Mutate(x => x.Resize(width, height));

            return result;
        }

        private Texture2D ConvertToTexture(Image<Rgba32> image)
        {
            Texture2D tex = new Texture2D(
                image.Width,
                image.Height,
                TextureFormat.RGBA32,
                false
            );

            byte[] pixels = new byte[image.Width * image.Height * 4];

            image.CopyPixelDataTo(pixels);

            byte[] flippedPixels = new byte[pixels.Length];

            int rowSize = image.Width * 4;

            for (int y = 0; y < image.Height; y++)
            {
                int sourceIndex = y * rowSize;
                int targetIndex = (image.Height - y - 1) * rowSize;

                System.Array.Copy(
                    pixels,
                    sourceIndex,
                    flippedPixels,
                    targetIndex,
                    rowSize
                );
            }

            tex.LoadRawTextureData(flippedPixels);
            tex.Apply(false, true);

            return tex;
        }

        private void Update()
        {
            if (!isGif || gifFrames.Count == 0)
                return;

            gifTimer += Time.deltaTime;

            while (gifTimer >= gifDelays[currentFrame])
            {
                gifTimer -= gifDelays[currentFrame];

                currentFrame++;

                if (currentFrame >= gifFrames.Count)
                    currentFrame = 0;

                texture = gifFrames[currentFrame];
            }
        }

        public void UpdateImageSize()
        {
            if (texture == null) return;

            float aspect = (float)texture.width / texture.height;
            float width = texture.width;
            float height = texture.height;

            if (width > maxWidth)
            {
                width = maxWidth;
                height = width / aspect;
            }

            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspect;
            }

            rect.width = width;
            rect.height = height;

            Debug.Log(
                "[MyOshiOverlay] Texture: "
                + texture.width + "x" + texture.height
                + " / Rect: "
                + rect.width + "x" + rect.height
                + " / Max: "
                + maxWidth + "x" + maxHeight
            );
        }

        public void CustomUpdate()
        {
            if (isDragging)
            {
                UnityEngine.Input.ResetInputAxes();
            }
        }

        private void OnGUI()
        {
            if (texture == null) return;
            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit);
            HandleDrag();
        }

        private void OnDestroy()
        {
            ClearTexture();
        }

        private void HandleDrag()
        {
            Event e = Event.current;    // 현재 이벤트 가져오기

            // 오버레이 위치가 고정되어 있으면 드래그하지 않음
            if (Main.settings != null && Main.settings.overlayLocked)
            {
                return;
            }

            // 마우스 이벤트 처리
            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                isDragging = true;
                isTyping = true;

                Input.ResetInputAxes();

                dragOffset = e.mousePosition - new Vector2(rect.x, rect.y);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                isTyping = false;

                Main.settings.overlayX = rect.x;
                Main.settings.overlayY = rect.y;

                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
            {
                Input.ResetInputAxes();

                rect.position = e.mousePosition - dragOffset;
                e.Use();
            }
        }
    }
}
