using UnityEngine;
using System.Collections;
using System.IO;

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

        public void LoadImage()
        {
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("[MyOshiOverlay] File not found: " + filePath);
                return;
            }

            byte[] data = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(data);

            UpdateImageSize();
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