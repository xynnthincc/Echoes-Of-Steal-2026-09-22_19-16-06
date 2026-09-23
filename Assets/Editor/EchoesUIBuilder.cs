using EchoesOfSteal.Systems;
using EchoesOfSteal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfSteal.EditorTools
{
    /// <summary>
    /// Builder otomatis untuk panel UI (dijalankan sekali dari menu Tools).
    /// Membuat hierarki GameOverPanel, melengkapi referensi serialized, dan
    /// memasang OnClick listener tombol Restart â€” tanpa wiring manual.
    /// </summary>
    public static class EchoesUIBuilder
    {
        [MenuItem("Tools/Echoes of Steal/Create Game Over Panel")]
        public static void CreateGameOverPanel()
        {
            Canvas canvas = FindRootCanvas();
            if (canvas == null)
            {
                Debug.LogError("[EchoesUIBuilder] Tidak ada Canvas di scene.");
                return;
            }

            if (canvas.transform.Find("GameOverPanel") != null)
            {
                Debug.LogError("[EchoesUIBuilder] GameOverPanel sudah ada di bawah Canvas â€” tidak dibuat ulang.");
                return;
            }

            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager == null)
            {
                Debug.LogError("[EchoesUIBuilder] GameManager tidak ditemukan di scene â€” buat GameObject GameManager (step 3) dulu, lalu jalankan lagi.");
                return;
            }

            TMP_FontAsset font = TMP_Settings.defaultFontAsset;
            if (font == null)
                Debug.LogWarning("[EchoesUIBuilder] TMP default font tidak ada â€” import TMP Essentials (Window â†’ TextMeshPro â†’ Import TMP Essential Resources), lalu set font manual pada text.");

            GameObject root = new GameObject("GameOverPanel", typeof(RectTransform), typeof(GameOverPanel));
            root.transform.SetParent(canvas.transform, false);
            SetFullStretch(root.GetComponent<RectTransform>());

            GameOverPanel panel = root.GetComponent<GameOverPanel>();

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            SetFullStretch(content.GetComponent<RectTransform>());
            content.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            CreateText("TitleText", content.transform, font, "GAME OVER", 80f, new Vector2(0f, 80f), new Vector2(800f, 120f));
            TMP_Text finalScore = CreateText("FinalScoreText", content.transform, font, "Score: 0", 50f, new Vector2(0f, -40f), new Vector2(600f, 80f));

            GameObject buttonGo = new GameObject("RestartButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGo.transform.SetParent(content.transform, false);
            RectTransform buttonRt = buttonGo.GetComponent<RectTransform>();
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0.5f);
            buttonRt.anchoredPosition = new Vector2(0f, -170f);
            buttonRt.sizeDelta = new Vector2(320f, 90f);

            Image buttonImage = buttonGo.GetComponent<Image>();
            buttonImage.sprite = EditorGUIUtility.Load("UI/Skin/Background.psd") as Sprite;
            buttonImage.color = new Color(0.85f, 0.25f, 0.25f);

            Button button = buttonGo.GetComponent<Button>();
            button.targetGraphic = buttonImage;
            CreateText("Text", buttonGo.transform, font, "RESTART", 40f, Vector2.zero, new Vector2(320f, 90f));

            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_gameManager").objectReferenceValue = gameManager;
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_finalScoreText").objectReferenceValue = finalScore;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(button.onClick, panel.Restart);

            content.SetActive(false);
            root.transform.SetAsLastSibling();

            Undo.RegisterCreatedObjectUndo(root, "Create Game Over Panel");
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[EchoesUIBuilder] GameOverPanel dibuat & scene disimpan. Restart button + semua referensi sudah ter-wire. Content disembunyikan (aktif saat mati).", root);
        }

        private static Canvas FindRootCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas candidate in canvases)
                if (candidate.transform.parent == null && candidate.isRootCanvas)
                    return candidate;
            return null;
        }

        private static void SetFullStretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string text, float size, Vector2 position, Vector2 dimensions)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = dimensions;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (font != null)
                tmp.font = font;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.text = text;
            return tmp;
        }
    }
}
