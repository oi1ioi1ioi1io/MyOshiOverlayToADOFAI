using UnityEngine;
using System.Collections;
using System.IO;

namespace MyOshiOverlay
{
    public class Overlay : MonoBehaviour
    {
        public string filePath; // 불러올 이미지 경로
        public Texture2D texture; // 불러온 이미지(Texture2D)
        public Rect rect = new Rect(100, 100, 200, 200); // 오버레이 위치와 크기

        // 최대 크기 제한 Main.cs랑 똑같음
        public int maxWidth = 500;
        public int maxHeight = 500;

        public bool isDragging = false; // 드래그 중인지 아닌지 여부
        public bool isTyping = false; // 텍스트 입력 중 여부 (UI 입력 감지 방지용)

        private Vector2 dragOffset; // 드래그할 때 클릭 위치와 이미지 위치 간의 거리

        public void LoadImage()
        {
            // 경로 유효성 검사
            if (!File.Exists(filePath))
            {
                Debug.LogWarning("[MyOshiOverlay] File not found: " + filePath);
                return;
            }

            // 이미지 데이터를 바이트로 읽고 Texture2D로 변환
            byte[] data = File.ReadAllBytes(filePath);
            texture = new Texture2D(2, 2);
            texture.LoadImage(data); // PNG, JPG 등 일반 이미지 로드

            UpdateImageSize(); // 불러온 뒤 해상도 조정
        }

        public void UpdateImageSize()
        {
            if (texture == null) return;

            float aspect = (float)texture.width / texture.height;
            float width = texture.width;
            float height = texture.height;

            // 가로 제한
            if (width > maxWidth)
            {
                width = maxWidth;
                height = width / aspect;
            }

            // 세로 제한
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
            if (isDragging) // 드래그 중일 때 Unity Input을 잠시 리셋해서 게임 내 입력을 무시함
            {
                UnityEngine.Input.ResetInputAxes();
            }
        }

        private void OnGUI()
        {
            if (texture == null) return; // 이미지가 없으면 아무 사진도 불러오지 않음

            GUI.DrawTexture(rect, texture, ScaleMode.ScaleToFit); // rect 위치에 이미지 그리기 (비율 유지)

            HandleDrag(); // 마우스 입력 처리
        }

        private void HandleDrag()
        {
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && rect.Contains(e.mousePosition)) // 왼쪽 클릭으로 이미지 클릭 시작 시
            {
                isDragging = true; // 드래그 시작
                isTyping = true; // 입력 중 상태로 전환
                dragOffset = e.mousePosition - new Vector2(rect.x, rect.y); // 클릭 오프셋 계산
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                isDragging = false;
                isTyping = false;
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && isDragging) // 드래그 하고 있을 때 이미지 위치 갱신
            {
                rect.position = e.mousePosition - dragOffset;
                e.Use();
            }
        }
    }
}