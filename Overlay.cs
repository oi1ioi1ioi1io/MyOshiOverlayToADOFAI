using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

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

        // --- GIF 관련 변수 ---
        private bool isGif = false;
        private List<Texture2D> gifFrames = new List<Texture2D>();
        private List<float> gifDelays = new List<float>();
        private int currentFrame = 0;
        private float timer = 0f;

        public void LoadImage()
        {
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
                UpdateImageSize();
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
            gifFrames.Clear();
            gifDelays.Clear();

            using (Image gif = Image.FromFile(path))
            {
                FrameDimension dimension = new FrameDimension(gif.FrameDimensionsList[0]);
                int frameCount = gif.GetFrameCount(dimension);

                // 프레임별로 분리
                for (int i = 0; i < frameCount; i++)
                {
                    gif.SelectActiveFrame(dimension, i);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        gif.Save(ms, ImageFormat.Png);
                        Texture2D frameTex = new Texture2D(2, 2);
                        frameTex.LoadImage(ms.ToArray());
                        gifFrames.Add(frameTex);
                    }

                    // 프레임 딜레이 (기본적으로 0x5100 속성)
                    try
                    {
                        PropertyItem item = gif.GetPropertyItem(0x5100);
                        int delay = System.BitConverter.ToInt32(item.Value, i * 4) * 10;
                        if (delay <= 0) delay = 100; // 최소 100ms 보정
                        gifDelays.Add(delay / 1000f);
                    }
                    catch
                    {
                        gifDelays.Add(0.1f); // 속성 없을 경우 기본값 0.1초
                    }
                }
            }

            // 첫 프레임 표시
            if (gifFrames.Count > 0)
            {
                texture = gifFrames[0];
            }
        }

        private void Update()
        {
            if (isGif && gifFrames.Count > 0)
            {
                timer += Time.deltaTime;
                if (timer >= gifDelays[currentFrame])
                {
                    timer = 0f;
                    currentFrame = (currentFrame + 1) % gifFrames.Count;
                    texture = gifFrames[currentFrame];
                }
            }

            CustomUpdate();
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

        private void HandleDrag()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition))
            {
                isDragging = true;
                isTyping = true;
                dragOffset = e.mousePosition - new Vector2(rect.x, rect.y);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                isTyping = false;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging)
            {
                rect.position = e.mousePosition - dragOffset;
                e.Use();
            }
        }
    }
}