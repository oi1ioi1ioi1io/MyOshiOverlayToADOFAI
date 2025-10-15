using UnityModManagerNet;
using UnityEngine;
using System.Collections.Generic;

namespace MyOshiOverlay
{
    public class Settings : UnityModManager.ModSettings
    {
        // 오버레이 최대 크기 설정
        public int maxWidth = 500;
        public int maxHeight = 500;

        public string lastImagePath = ""; // 마지막으로 설정한 사진 경로를 저장

        // 유니티모드매니저가 설정 저장할 때 호출함
        public override void Save(UnityModManager.ModEntry modEntry)
        {
            Save(this, modEntry);
        }
    }

    // 언어요 랭귀지
    public enum Language
    {
        English,
        Korean
    }

    public static class Main
    {
        public static bool enabled; // 모드 활성화 여부
        public static Overlay overlay; // Overlay 객체(실제 오버레이 동작 담당 스크립트)
        public static Settings settings; // 설정 인스턴스

        // UI 입력 임시 값 (문자열)
        public static string tempMaxWidth;
        public static string tempMaxHeight;

        // 마지막으로 유효했던 값 저장 (숫자 아닌 값 방지용)
        private static string lastValidMaxWidth;
        private static string lastValidMaxHeight;

        // 현재 언어요
        public static Language currentLanguage = Language.Korean;

        // 언어 텍스트 딕셔너리
        public static readonly Dictionary<Language, Dictionary<string, string>> languageTexts =
            new Dictionary<Language, Dictionary<string, string>>()
        {
            {
                Language.English, new Dictionary<string, string>()
                {
                    { "PhotoPath", "Enter photo path:" },
                    { "GIFWarn", "GIFs are supported, but the game may briefly freeze when applied. This is due to the GIF loading process.\nAlso, because they can be a strain on the CPU, we recommend not using GIFs on low-spec PC." },
                    { "ApplyImage", "Apply" },
                    { "ResolutionSettings", "Overlay Max Size Settings" },
                    { "MaxWidth", "Max Width:" },
                    { "MaxHeight", "Max Height:" },
                    { "ApplyResolution", "Apply" },
                }
            },
            {
                Language.Korean, new Dictionary<string, string>()
                {
                    { "PhotoPath", "사진 경로 입력:" },
                    { "GIFWarn", "GIF를 지원하지만 적용 시 GIF 로딩 때문에 게임이 잠시 멈출 수 있습니다.\n또한, CPU에 부담이 있을 수 있으니 저사양 컴퓨터에서는 GIF 적용을 하지 않는 걸 추천합니다." },
                    { "ApplyImage", "적용" },
                    { "ResolutionSettings", "오버레이 최대 크기 설정" },
                    { "MaxWidth", "최대 너비:" },
                    { "MaxHeight", "최대 높이:" },
                    { "ApplyResolution", "적용" },
                }
            }
        };

        // 모드 로드 시 1회 실행
        public static bool Load(UnityModManager.ModEntry modEntry)
        {
            // 기존 설정 불러오기
            settings = UnityModManager.ModSettings.Load<Settings>(modEntry);

            // 불러온 값을 문자열로 변환 (GUI 입력용)
            tempMaxWidth = settings.maxWidth.ToString();
            tempMaxHeight = settings.maxHeight.ToString();

            lastValidMaxWidth = tempMaxWidth;
            lastValidMaxHeight = tempMaxHeight;

            // 유니티모드매니저 이벤트 등록
            modEntry.OnToggle = OnToggle;   // 모드 활성/비활성 토글 시
            modEntry.OnGUI = OnGUI;         // 설정창 GUI 표시 시
            modEntry.OnSaveGUI = OnSaveGUI; // 설정창 닫을 때 저장 시
            modEntry.OnUpdate = OnUpdate;   // 매 프레임 업데이트 시

            return true;
        }

        private static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
        {
            enabled = value;

            if (enabled && overlay == null)
            {
                // 오버레이 GameObject 생성 및 설정
                var go = new GameObject("MyOshiOverlay");
                overlay = go.AddComponent<Overlay>();
                Object.DontDestroyOnLoad(go);

                overlay.maxWidth = settings.maxWidth;
                overlay.maxHeight = settings.maxHeight;

                // 저장된 경로가 있다면 자동으로 로드
                if (!string.IsNullOrEmpty(settings.lastImagePath))
                {
                    overlay.filePath = settings.lastImagePath;
                    overlay.LoadImage();
                }
            }
            else if (!enabled && overlay != null)
            {
                // 비활성화 시 오브젝트 제거
                Object.Destroy(overlay.gameObject);
                overlay = null;
            }

            return true;
        }


        private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
        {
            if (overlay != null)
            {
                overlay.CustomUpdate(); // Overlay.cs 내부의 CustomUpdate() 호출
            }
        }

        private static void OnGUI(UnityModManager.ModEntry modEntry)
        {
            // 언어 전환 버튼
            GUILayout.BeginHorizontal();

            // 언어 고르면 그 버튼 볼드체
            GUIStyle englishStyle = new GUIStyle(GUI.skin.button);
            GUIStyle koreanStyle = new GUIStyle(GUI.skin.button);

            if (currentLanguage == Language.English)
            {
                englishStyle.fontStyle = FontStyle.Bold;
            }
            else
            {
                englishStyle.fontStyle = FontStyle.Normal;
            }

            if (currentLanguage == Language.Korean)
            {
                koreanStyle.fontStyle = FontStyle.Bold;
            }
            else
            {
                koreanStyle.fontStyle = FontStyle.Normal;
            }
            
            if (GUILayout.Button("English", englishStyle, GUILayout.Width(100)))
            {
                currentLanguage = Language.English;
            }

            if (GUILayout.Button("한국어", koreanStyle, GUILayout.Width(100)))
            {
                currentLanguage = Language.Korean;
            }
            GUILayout.EndHorizontal();

            // 오버레이가 켜져있을 때만 설정 표시
            if (overlay != null)
            {
                GUIStyle GIFWarnStyle = new GUIStyle(GUI.skin.label);
                GIFWarnStyle.normal.textColor = new Color(1f, 0.3f, 0f);
                GIFWarnStyle.fontSize = 12;

                if (overlay.isDragging || overlay.isTyping)
                {
                    GUIUtility.hotControl = 0;
                    Event.current.Use();
                }

                // 파일 경로 입력
                GUILayout.Label(languageTexts[currentLanguage]["PhotoPath"]);

                GUI.SetNextControlName("FilePathInput");
                overlay.filePath = GUILayout.TextField(overlay.filePath ?? "", GUILayout.Width(300));
                GUILayout.Label(languageTexts[currentLanguage]["GIFWarn"], GIFWarnStyle);

                // 적용 버튼
                if (GUILayout.Button(languageTexts[currentLanguage]["ApplyImage"], GUILayout.Width(100)))
                {
                    // 쌍따옴표와 공백 자동 제거
                    if (!string.IsNullOrEmpty(overlay.filePath))
                    overlay.filePath = overlay.filePath.Trim().Trim('"');

                    overlay.LoadImage();    // Overlay.cs 쪽에서 이미지 로드

                    settings.lastImagePath = overlay.filePath; // 껏다 켜도 저장
                    settings.Save(modEntry);
                }

                GUILayout.Space(20);

                // 오버레이 최대 해상도 설정
                GUILayout.Label(languageTexts[currentLanguage]["ResolutionSettings"]);

                GUILayout.BeginHorizontal();
                GUILayout.Label(languageTexts[currentLanguage]["MaxWidth"], GUILayout.Width(100));
                GUI.SetNextControlName("MaxWidthInput");
                tempMaxWidth = GUILayout.TextField(tempMaxWidth, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                GUILayout.Label(languageTexts[currentLanguage]["MaxHeight"], GUILayout.Width(100));
                GUI.SetNextControlName("MaxHeightInput");
                tempMaxHeight = GUILayout.TextField(tempMaxHeight, GUILayout.Width(100));
                GUILayout.EndHorizontal();

                // 적용 버튼
                if (GUILayout.Button(languageTexts[currentLanguage]["ApplyResolution"], GUILayout.Width(200)))
                {
                    ApplyResolutionSettings(modEntry);
                }
            }
        }

        // 사진 크기 적용 및 저장 함수
        private static void ApplyResolutionSettings(UnityModManager.ModEntry modEntry)
        {
            bool applied = false;

            // 숫자 입력만 받고 입력 잘못하면 이전 값 유지
            if (int.TryParse(tempMaxWidth, out int w))
            {
                settings.maxWidth = Mathf.Clamp(w, 100, Screen.width);
                lastValidMaxWidth = settings.maxWidth.ToString();
                tempMaxWidth = lastValidMaxWidth;
                applied = true;
            }
            else
            {
                tempMaxWidth = lastValidMaxWidth;
            }

            if (int.TryParse(tempMaxHeight, out int h))
            {
                settings.maxHeight = Mathf.Clamp(h, 100, Screen.height);
                lastValidMaxHeight = settings.maxHeight.ToString();
                tempMaxHeight = lastValidMaxHeight;
                applied = true;
            }
            else
            {
                tempMaxHeight = lastValidMaxHeight;
            }

            // 설정 저장
            settings.Save(modEntry);

            // 오버레이 크기 갱신
            if (overlay != null && applied)
            {
                overlay.maxWidth = settings.maxWidth;
                overlay.maxHeight = settings.maxHeight;
                overlay.UpdateImageSize();
            }
        }

        private static void OnSaveGUI(UnityModManager.ModEntry modEntry)
        {
            settings.Save(modEntry);
        }
    }
}