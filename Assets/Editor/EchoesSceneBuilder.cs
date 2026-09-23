using System.Collections.Generic;
using System.IO;
using EchoesOfSteal.Combat;
using EchoesOfSteal.Enemy;
using EchoesOfSteal.Player;
using EchoesOfSteal.Systems;
using EchoesOfSteal.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace EchoesOfSteal.EditorTools
{
    /// <summary>
    /// Builder scene otomatis: Player, joystick, tombol attack, WaveSpawner, GameManager,
    /// HUD, Start & Game Over panel, Enemy prefab, dan asset WaveData/EnemyData.
    /// Idempotent: objek yang sudah ada dilengkapi/di-wire ulang, tidak diduplikasi.
    /// </summary>
    public static class EchoesSceneBuilder
    {
        private const int PlayerLayer = 8;
        private const int EnemyLayer = 9;

        private static Sprite _square;
        private static Sprite _circle;
        private static TMP_FontAsset _font;
        private static GameObject _canvas;

        [MenuItem("Tools/Echoes of Steal/Build Full Gameplay Scene")]
        public static void BuildFullScene()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            CreateRequiredFolders();
            LoadBuiltInAssets();

            GameObject player = EnsurePlayer();
            GameObject waveSpawner = EnsureWaveSpawner();
            GameObject gameManager = EnsureGameManager();
            EnsureCanvasAndEventSystem();
            EnsureJoystick();
            EnsureAttackButton(player);
            EnsureHUD();
            EnsureStartPanel();
            EnsureGameOverPanel();

            EnemyData enemyData = EnsureEnemyData();
            WaveData[] waves = EnsureWaveDataAssets();
            GameObject enemyPrefab = EnsureEnemyPrefab(enemyData);

            WireWaveSpawner(waveSpawner, player, enemyPrefab, waves);
            WireGameManager(gameManager, waveSpawner, player);
            WirePlayer(player);
            WireHud(gameManager, waveSpawner);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();

            Debug.Log("[EchoesSceneBuilder] BUILD SELESAI â€” buka Unity dan tekan Play.");
        }

        private static void CreateRequiredFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects"))
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        }

        private static void LoadBuiltInAssets()
        {
            _square = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            _circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            _font = TMP_Settings.defaultFontAsset;
            if (_font == null)
                Debug.LogWarning("[EchoesSceneBuilder] TMP default font tidak ditemukan. Import: Window â†’ TextMeshPro â†’ Import TMP Essential Resources.");
        }

        private static GameObject EnsurePlayer()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                player = new GameObject("Player");
                player.AddComponent<Rigidbody2D>().gravityScale = 0f;
                player.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeRotation;
                player.AddComponent<CircleCollider2D>();
                SpriteRenderer sr = player.AddComponent<SpriteRenderer>();
                sr.sprite = _square;
                sr.color = new Color(0.35f, 0.85f, 0.4f);
                player.AddComponent<PlayerController>();
            }

            player.layer = PlayerLayer;
            if (player.GetComponent<PlayerHealth>() == null)
                player.AddComponent<PlayerHealth>();

            if (player.transform.Find("AttackArea") == null)
            {
                GameObject attackArea = new GameObject("AttackArea");
                attackArea.transform.SetParent(player.transform, false);
                attackArea.AddComponent<AttackArea>();
            }

            return player;
        }

        private static GameObject EnsureWaveSpawner()
        {
            GameObject spawner = GameObject.Find("WaveSpawner");
            if (spawner == null)
                spawner = new GameObject("WaveSpawner");
            if (spawner.GetComponent<WaveSpawner>() == null)
                spawner.AddComponent<WaveSpawner>();
            return spawner;
        }

        private static GameObject EnsureGameManager()
        {
            GameObject manager = GameObject.Find("GameManager");
            if (manager == null)
                manager = new GameObject("GameManager");
            if (manager.GetComponent<GameManager>() == null)
                manager.AddComponent<GameManager>();
            return manager;
        }

        private static void EnsureCanvasAndEventSystem()
        {
            Canvas existingCanvas = Object.FindAnyObjectByType<Canvas>();
            if (existingCanvas == null)
            {
                _canvas = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                existingCanvas = _canvas.GetComponent<Canvas>();
                existingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                _canvas = existingCanvas.gameObject;
            }

            CanvasScaler scaler = _canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = _canvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            }
            if (eventSystem.GetComponent<StandaloneInputModule>() != null)
                Object.DestroyImmediate(eventSystem.GetComponent<StandaloneInputModule>());
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private static void EnsureJoystick()
        {
            Transform existing = _canvas.transform.Find("Joystick");
            if (existing != null)
                return;

            GameObject joystick = new GameObject("Joystick", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
            joystick.transform.SetParent(_canvas.transform, false);
            RectTransform joystickRt = joystick.GetComponent<RectTransform>();
            joystickRt.anchorMin = joystickRt.anchorMax = new Vector2(0f, 0f);
            joystickRt.anchoredPosition = new Vector2(200f, 200f);
            joystickRt.sizeDelta = new Vector2(300f, 300f);

            Image joystickImage = joystick.GetComponent<Image>();
            joystickImage.sprite = _circle;
            joystickImage.color = new Color(1f, 1f, 1f, 0.15f);
            joystickImage.raycastTarget = true;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handle.transform.SetParent(joystick.transform, false);
            RectTransform handleRt = handle.GetComponent<RectTransform>();
            handleRt.anchorMin = handleRt.anchorMax = new Vector2(0.5f, 0.5f);
            handleRt.anchoredPosition = Vector2.zero;
            handleRt.sizeDelta = new Vector2(120f, 120f);

            Image handleImage = handle.GetComponent<Image>();
            handleImage.sprite = _circle;
            handleImage.color = new Color(1f, 1f, 1f, 0.35f);
            handleImage.raycastTarget = false;

            SerializedObject so = new SerializedObject(joystick.GetComponent<VirtualJoystick>());
            so.FindProperty("_background").objectReferenceValue = joystick.GetComponent<RectTransform>();
            so.FindProperty("_handle").objectReferenceValue = handle.GetComponent<RectTransform>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureAttackButton(GameObject player)
        {
            Button button;
            Transform existing = _canvas.transform.Find("AttackButton");
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
            else
            {
                GameObject buttonGo = new GameObject("AttackButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(_canvas.transform, false);
                RectTransform rt = buttonGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-200f, 200f);
                rt.sizeDelta = new Vector2(200f, 200f);

                Image image = buttonGo.GetComponent<Image>();
                image.sprite = _circle;
                image.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
                button = buttonGo.GetComponent<Button>();
                button.targetGraphic = image;
                CreateText("Text", buttonGo.transform, "ATTACK", 32f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f), TextAlignmentOptions.Center);
            }

            if (button != null && button.onClick.GetPersistentEventCount() == 0)
                UnityEventTools.AddPersistentListener(button.onClick, player.GetComponent<PlayerController>().TryAttack);
        }

        private static void EnsureHUD()
        {
            Transform hud = _canvas.transform.Find("HUD");
            if (hud == null)
            {
                GameObject hudGo = new GameObject("HUD", typeof(RectTransform));
                hudGo.transform.SetParent(_canvas.transform, false);
                SetFullStretch(hudGo.GetComponent<RectTransform>());
                hudGo.AddComponent<HUDController>();
                hud = hudGo.transform;
            }
            else if (hud.GetComponent<HUDController>() == null)
            {
                hud.gameObject.AddComponent<HUDController>();
            }

            Transform bg = hud.Find("HealthBarBG");
            if (bg == null)
                bg = _canvas.transform.Find("HealthBarBG");
            if (bg == null)
            {
                GameObject bgGo = new GameObject("HealthBarBG", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(hud, false);
                RectTransform rt = bgGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(210f, -30f);
                rt.sizeDelta = new Vector2(400f, 40f);
                bgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                bg = bgGo.transform;
            }
            else if (bg.parent != hud)
            {
                bg.SetParent(hud, true);
            }

            Transform fill = bg.Find("HealthBarFill");
            if (fill == null)
            {
                GameObject fillGo = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(bg, false);
                RectTransform rt = fillGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(4f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);
                fill = fillGo.transform;
            }

            Image fillImage = fill.GetComponent<Image>();
            if (fillImage == null)
                fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.sprite = _square;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.color = Color.red;
            EditorUtility.SetDirty(fillImage);

            EnsureHudText("ScoreText", hud, "Score: 0", 50f, new Vector2(1f, 1f), new Vector2(-220f, -40f), new Vector2(400f, 60f), TextAlignmentOptions.TopRight);
            EnsureHudText("WaveText", hud, "Wave 1", 50f, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 60f), TextAlignmentOptions.Top);

            hud.SetAsFirstSibling();
        }

        private static void EnsureHudText(string name, Transform hud, string text, float size, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            Transform existing = hud.Find(name);
            if (existing == null)
                existing = _canvas.transform.Find(name);
            if (existing == null)
            {
                CreateText(name, hud, text, size, anchor, position, dimensions, alignment);
            }
            else if (existing.parent != hud)
            {
                existing.SetParent(hud, true);
            }
        }

        private static GameObject EnsureStartPanel()
        {
            Transform existing = _canvas.transform.Find("StartPanel");
            if (existing != null)
            {
                StartPanel existingPanel = existing.GetComponent<StartPanel>();
                if (existingPanel != null)
                {
                    Transform existingContent = existing.Find("Content");
                    if (existingContent != null && existingContent.Find("BeginButton") == null)
                        CreatePanelButton(existing.gameObject, "BEGIN", "BeginButton", new Vector2(0f, -120f), existingPanel.BeginGame);
                }
                return existing.gameObject;
            }

            GameObject root = new GameObject("StartPanel", typeof(RectTransform), typeof(StartPanel));
            root.transform.SetParent(_canvas.transform, false);
            SetFullStretch(root.GetComponent<RectTransform>());

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            SetFullStretch(content.GetComponent<RectTransform>());
            content.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            CreateText("TitleText", content.transform, "ECHOES OF STEAL", 80f, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 140f), TextAlignmentOptions.Center);
            CreateText("SubtitleText", content.transform, "Survive the waves", 40f, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1000f, 80f), TextAlignmentOptions.Center);

            CreatePanelButton(root, "BEGIN", "BeginButton", new Vector2(0f, -120f), root.GetComponent<StartPanel>().BeginGame);

            StartPanel panel = root.GetComponent<StartPanel>();
            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_gameManager").objectReferenceValue = GameObject.Find("GameManager").GetComponent<GameManager>();
            so.FindProperty("_content").objectReferenceValue = content;
            so.ApplyModifiedPropertiesWithoutUndo();

            return root;
        }

        private static GameObject EnsureGameOverPanel()
        {
            Transform existing = _canvas.transform.Find("GameOverPanel");
            if (existing != null)
                return existing.gameObject;

            GameObject root = new GameObject("GameOverPanel", typeof(RectTransform), typeof(GameOverPanel));
            root.transform.SetParent(_canvas.transform, false);
            SetFullStretch(root.GetComponent<RectTransform>());

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
            content.transform.SetParent(root.transform, false);
            SetFullStretch(content.GetComponent<RectTransform>());
            content.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);

            CreateText("TitleText", content.transform, "GAME OVER", 80f, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 120f), TextAlignmentOptions.Center);
            TMP_Text finalScore = CreateText("FinalScoreText", content.transform, "Score: 0", 50f, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(600f, 80f), TextAlignmentOptions.Center);

            CreatePanelButton(root, "RESTART", "RestartButton", new Vector2(0f, -170f), root.GetComponent<GameOverPanel>().Restart, content.transform);

            GameOverPanel panel = root.GetComponent<GameOverPanel>();
            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_gameManager").objectReferenceValue = GameObject.Find("GameManager").GetComponent<GameManager>();
            so.FindProperty("_content").objectReferenceValue = content;
            so.FindProperty("_finalScoreText").objectReferenceValue = finalScore;
            so.ApplyModifiedPropertiesWithoutUndo();

            content.SetActive(false);
            root.transform.SetAsLastSibling();
            return root;
        }

        private static void CreatePanelButton(GameObject root, string label, string name, Vector2 position, UnityAction action, Transform parentOverride = null)
        {
            Transform parent = parentOverride != null ? parentOverride : root.transform.Find("Content");
            if (parent == null)
                parent = root.transform;

            Transform existing = parent.Find(name);
            Button button;
            if (existing != null)
            {
                button = existing.GetComponent<Button>();
            }
            else
            {
                GameObject buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(parent, false);
                RectTransform rt = buttonGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = position;
                rt.sizeDelta = new Vector2(320f, 90f);

                Image image = buttonGo.GetComponent<Image>();
                image.sprite = _square;
                image.color = new Color(0.2f, 0.55f, 0.3f);
                button = buttonGo.GetComponent<Button>();
                button.targetGraphic = image;
                CreateText("Text", buttonGo.transform, label, 40f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320f, 90f), TextAlignmentOptions.Center);
            }

            if (button != null && button.onClick.GetPersistentEventCount() == 0)
                UnityEventTools.AddPersistentListener(button.onClick, action);
        }

        private static EnemyData EnsureEnemyData()
        {
            const string path = "Assets/ScriptableObjects/EnemyData.asset";
            EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null)
                return existing;

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            return data;
        }

        private static WaveData[] EnsureWaveDataAssets()
        {
            return new[]
            {
                LoadOrCreateWaveData("Assets/ScriptableObjects/Wave1.asset", 5, 1f, 3f),
                LoadOrCreateWaveData("Assets/ScriptableObjects/Wave2.asset", 8, 0.75f, 3f),
                LoadOrCreateWaveData("Assets/ScriptableObjects/Wave3.asset", 12, 0.5f, 3f)
            };
        }

        private static WaveData LoadOrCreateWaveData(string path, int enemyCount, float spawnInterval, float delayAfterWave)
        {
            WaveData existing = AssetDatabase.LoadAssetAtPath<WaveData>(path);
            if (existing != null)
                return existing;

            WaveData wave = ScriptableObject.CreateInstance<WaveData>();
            AssetDatabase.CreateAsset(wave, path);
            SerializedObject so = new SerializedObject(wave);
            so.FindProperty("_enemyCount").intValue = enemyCount;
            so.FindProperty("_spawnInterval").floatValue = spawnInterval;
            so.FindProperty("_delayAfterWave").floatValue = delayAfterWave;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
            return wave;
        }

        private static GameObject EnsureEnemyPrefab(EnemyData enemyData)
        {
            const string path = "Assets/Prefabs/Enemy.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null)
            {
                GameObject temp = new GameObject("Enemy");
                temp.layer = EnemyLayer;
                Rigidbody2D rb = temp.AddComponent<Rigidbody2D>();
                rb.gravityScale = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                rb.linearDamping = 4f;
                temp.AddComponent<CircleCollider2D>().isTrigger = true;
                SpriteRenderer sr = temp.AddComponent<SpriteRenderer>();
                sr.sprite = _square;
                sr.color = new Color(0.85f, 0.3f, 0.3f);
                temp.AddComponent<EnemyAI>();
                temp.AddComponent<EnemyHealth>();
                prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
                Object.DestroyImmediate(temp);
            }

            SerializedObject aiSo = new SerializedObject(prefab.GetComponent<EnemyAI>());
            aiSo.FindProperty("_data").objectReferenceValue = enemyData;
            aiSo.FindProperty("_playerLayers").intValue = LayerMask.GetMask("Player");
            aiSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject healthSo = new SerializedObject(prefab.GetComponent<EnemyHealth>());
            healthSo.FindProperty("_data").objectReferenceValue = enemyData;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SavePrefabAsset(prefab);
            return prefab;
        }

        private static void WireWaveSpawner(GameObject spawnerGo, GameObject player, GameObject enemyPrefab, WaveData[] waves)
        {
            SerializedObject so = new SerializedObject(spawnerGo.GetComponent<WaveSpawner>());
            so.FindProperty("_enemyPrefab").objectReferenceValue = enemyPrefab.GetComponent<EnemyAI>();
            so.FindProperty("_playerTransform").objectReferenceValue = player.transform;
            so.FindProperty("_autoStart").boolValue = false;

            SerializedProperty wavesProperty = so.FindProperty("_waves");
            wavesProperty.arraySize = waves.Length;
            for (int i = 0; i < waves.Length; i++)
                wavesProperty.GetArrayElementAtIndex(i).objectReferenceValue = waves[i];

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireGameManager(GameObject gameManagerGo, GameObject waveSpawnerGo, GameObject player)
        {
            SerializedObject so = new SerializedObject(gameManagerGo.GetComponent<GameManager>());
            so.FindProperty("_waveSpawner").objectReferenceValue = waveSpawnerGo.GetComponent<WaveSpawner>();
            so.FindProperty("_playerHealth").objectReferenceValue = player.GetComponent<PlayerHealth>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WirePlayer(GameObject player)
        {
            PlayerController controller = player.GetComponent<PlayerController>();
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_attackArea").objectReferenceValue = player.transform.Find("AttackArea").GetComponent<AttackArea>();
            so.FindProperty("_joystick").objectReferenceValue = _canvas.transform.Find("Joystick").GetComponent<VirtualJoystick>();
            so.ApplyModifiedPropertiesWithoutUndo();

            AttackArea attackArea = player.transform.Find("AttackArea").GetComponent<AttackArea>();
            SerializedObject areaSo = new SerializedObject(attackArea);
            areaSo.FindProperty("_targetLayers").intValue = LayerMask.GetMask("Enemy");
            areaSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void WireHud(GameObject gameManagerGo, GameObject waveSpawnerGo)
        {
            Transform hud = _canvas.transform.Find("HUD");
            HUDController hudController = hud.GetComponent<HUDController>();
            SerializedObject so = new SerializedObject(hudController);
            so.FindProperty("_gameManager").objectReferenceValue = gameManagerGo.GetComponent<GameManager>();
            so.FindProperty("_waveSpawner").objectReferenceValue = waveSpawnerGo.GetComponent<WaveSpawner>();
            so.FindProperty("_healthBarFill").objectReferenceValue = hud.Find("HealthBarBG/HealthBarFill").GetComponent<Image>();
            so.FindProperty("_scoreText").objectReferenceValue = hud.Find("ScoreText").GetComponent<TMP_Text>();
            so.FindProperty("_waveText").objectReferenceValue = hud.Find("WaveText").GetComponent<TMP_Text>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TMP_Text CreateText(string name, Transform parent, string text, float size, Vector2 anchor, Vector2 position, Vector2 dimensions, TextAlignmentOptions alignment)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = position;
            rt.sizeDelta = dimensions;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (_font != null)
                tmp.font = _font;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.raycastTarget = false;
            tmp.text = text;
            return tmp;
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Perbaikan struktur UI hasil setup manual/parcial: konsolidasi semua UI di satu Canvas,
        /// bungkus stray Content menjadi StartPanel, pindahkan GameOverPanel dari root ke Canvas,
        /// re-wire semua referensi & listener, lalu simpan dengan dirty flag yang benar.
        /// Semua pencarian berbasis komponen (bukan nama) agar tahan salah penamaan.
        /// </summary>
        [MenuItem("Tools/Echoes of Steal/Repair Scene UI")]
        public static void RepairSceneUI()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
            LoadBuiltInAssets();

            GameObject player = EnsurePlayer();
            GameObject waveSpawner = EnsureWaveSpawner();
            GameObject gameManager = EnsureGameManager();

            Canvas primary = SelectPrimaryCanvas();
            _canvas = primary.gameObject;
            NormalizeCanvas(primary);
            EnsureEventSystem();

            EnsureJoystickComponent(primary);
            EnsureAttackButtonComponent(primary, player);
            Transform hud = EnsureHudComponent(primary);
            EnsureHealthBar(hud);
            EnsureHudTexts(hud);
            EnsureStartPanelComponent(primary);
            EnsureGameOverPanelComponent(primary);
            OrderCanvasChildren(primary);
            NormalizeUILayout(primary);

            WireWaveSpawner(waveSpawner, player,
                EnsureEnemyPrefab(EnsureEnemyData()), EnsureWaveDataAssets());
            WireGameManager(gameManager, waveSpawner, player);
            WirePlayer(player);
            WireHud(gameManager, waveSpawner);
            WirePanelRefs();

            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[EchoesSceneBuilder] REPAIR SELESAI — struktur UI dinormalisasi & scene disimpan.");
        }

        private static Canvas SelectPrimaryCanvas()
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas best = null;
            foreach (Canvas candidate in canvases)
                if (best == null || candidate.transform.childCount > best.transform.childCount)
                    best = candidate;
            if (best == null)
            {
                GameObject canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                best = canvasGo.GetComponent<Canvas>();
                best.renderMode = RenderMode.ScreenSpaceOverlay;
            }
            return best;
        }

        private static void NormalizeCanvas(Canvas primary)
        {
            primary.renderMode = RenderMode.ScreenSpaceOverlay;
            primary.name = "Canvas";
            CanvasScaler scaler = primary.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = primary.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (primary.GetComponent<GraphicRaycaster>() == null)
                primary.gameObject.AddComponent<GraphicRaycaster>();
            EditorUtility.SetDirty(primary);
        }

        private static void EnsureEventSystem()
        {
            EventSystem eventSystem = Object.FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
                eventSystem = new GameObject("EventSystem", typeof(EventSystem)).GetComponent<EventSystem>();
            if (eventSystem.GetComponent<StandaloneInputModule>() != null)
                Object.DestroyImmediate(eventSystem.GetComponent<StandaloneInputModule>());
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Transform EnsureJoystickComponent(Canvas primary)
        {
            VirtualJoystick joystick = Object.FindAnyObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
            if (joystick == null)
            {
                GameObject joystickGo = new GameObject("Joystick", typeof(RectTransform), typeof(Image), typeof(VirtualJoystick));
                joystickGo.transform.SetParent(primary.transform, false);
                RectTransform joystickRt = joystickGo.GetComponent<RectTransform>();
                joystickRt.anchorMin = joystickRt.anchorMax = new Vector2(0f, 0f);
                joystickRt.anchoredPosition = new Vector2(200f, 200f);
                joystickRt.sizeDelta = new Vector2(300f, 300f);

                Image joystickImage = joystickGo.GetComponent<Image>();
                joystickImage.sprite = _circle;
                joystickImage.color = new Color(1f, 1f, 1f, 0.15f);
                joystickImage.raycastTarget = true;
                joystick = joystickGo.GetComponent<VirtualJoystick>();

                GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handleGo.transform.SetParent(joystickGo.transform, false);
                RectTransform handleRt = handleGo.GetComponent<RectTransform>();
                handleRt.anchorMin = handleRt.anchorMax = new Vector2(0.5f, 0.5f);
                handleRt.anchoredPosition = Vector2.zero;
                handleRt.sizeDelta = new Vector2(120f, 120f);
                Image handleImage = handleGo.GetComponent<Image>();
                handleImage.sprite = _circle;
                handleImage.color = new Color(1f, 1f, 1f, 0.35f);

                SerializedObject so = new SerializedObject(joystick);
                so.FindProperty("_background").objectReferenceValue = joystickGo.GetComponent<RectTransform>();
                so.FindProperty("_handle").objectReferenceValue = handleGo.GetComponent<RectTransform>();
                so.ApplyModifiedProperties();
                return joystickGo.transform;
            }

            joystick.transform.SetParent(primary.transform, false);
            joystick.gameObject.name = "Joystick";
            Image background = joystick.GetComponent<Image>();
            if (background != null && background.sprite == null)
                background.sprite = _circle;

            Transform handle = joystick.transform.Find("Handle");
            if (handle == null)
            {
                GameObject handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
                handleGo.transform.SetParent(joystick.transform, false);
                RectTransform handleRt = handleGo.GetComponent<RectTransform>();
                handleRt.anchorMin = handleRt.anchorMax = new Vector2(0.5f, 0.5f);
                handleRt.anchoredPosition = Vector2.zero;
                handleRt.sizeDelta = new Vector2(120f, 120f);
                Image handleImage = handleGo.GetComponent<Image>();
                handleImage.sprite = _circle;
                handleImage.color = new Color(1f, 1f, 1f, 0.35f);

                SerializedObject so = new SerializedObject(joystick);
                so.FindProperty("_handle").objectReferenceValue = handleGo.GetComponent<RectTransform>();
                so.ApplyModifiedProperties();
            }
            return joystick.transform;
        }

        private static void EnsureAttackButtonComponent(Canvas primary, GameObject player)
        {
            Button attackButton = null;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.name == "AttackButton")
                {
                    attackButton = button;
                    break;
                }
            }
            if (attackButton == null)
            {
                GameObject buttonGo = new GameObject("AttackButton", typeof(RectTransform), typeof(Image), typeof(Button));
                buttonGo.transform.SetParent(primary.transform, false);
                RectTransform rt = buttonGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-200f, 200f);
                rt.sizeDelta = new Vector2(200f, 200f);
                Image image = buttonGo.GetComponent<Image>();
                image.sprite = _circle;
                image.color = new Color(0.85f, 0.2f, 0.2f, 0.9f);
                attackButton = buttonGo.GetComponent<Button>();
                attackButton.targetGraphic = image;
                CreateText("Text", buttonGo.transform, "ATTACK", 32f, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 200f), TextAlignmentOptions.Center);
            }
            else
            {
                attackButton.transform.SetParent(primary.transform, true);
            }

            if (player != null && attackButton.onClick.GetPersistentEventCount() == 0)
                UnityEventTools.AddPersistentListener(attackButton.onClick, player.GetComponent<PlayerController>().TryAttack);
        }

        private static Transform EnsureHudComponent(Canvas primary)
        {
            HUDController hud = Object.FindAnyObjectByType<HUDController>(FindObjectsInactive.Include);
            if (hud == null)
            {
                GameObject hudGo = new GameObject("HUD", typeof(RectTransform));
                hudGo.transform.SetParent(primary.transform, false);
                SetFullStretch(hudGo.GetComponent<RectTransform>());
                hud = hudGo.AddComponent<HUDController>();
            }
            else
            {
                hud.transform.SetParent(primary.transform, false);
            }
            hud.gameObject.name = "HUD";
            return hud.transform;
        }

        private static void EnsureHealthBar(Transform hud)
        {
            Transform bg = hud.Find("HealthBarBG");
            if (bg == null)
                bg = _canvas.transform.Find("HealthBarBG");
            if (bg == null)
            {
                GameObject bgGo = new GameObject("HealthBarBG", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(hud, false);
                RectTransform rt = bgGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
                rt.anchoredPosition = new Vector2(210f, -30f);
                rt.sizeDelta = new Vector2(400f, 40f);
                bgGo.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                bg = bgGo.transform;
            }
            else
            {
                bg.SetParent(hud, true);
            }

            Transform fill = bg.Find("HealthBarFill");
            if (fill == null)
            {
                GameObject fillGo = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(bg, false);
                RectTransform rt = fillGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(4f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);
                fill = fillGo.transform;
            }

            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = _square;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.color = Color.red;
            EditorUtility.SetDirty(fillImage);
        }

        private static void EnsureHudTexts(Transform hud)
        {
            ReparentInto(hud, "ScoreText");
            ReparentInto(hud, "WaveText");
        }

        private static void ReparentInto(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
                return;

            existing = _canvas.transform.Find(childName);
            if (existing == null)
            {
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
                {
                    Transform candidate = canvas.transform.Find(childName);
                    if (candidate != null)
                    {
                        existing = candidate;
                        break;
                    }
                }
            }
            if (existing == null)
            {
                foreach (Transform root in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
                {
                    if (root.parent == null && root.name == childName)
                    {
                        existing = root;
                        break;
                    }
                }
            }

            if (existing != null)
                existing.SetParent(parent, true);
        }

        private static void EnsureStartPanelComponent(Canvas primary)
        {
            StartPanel panel = Object.FindAnyObjectByType<StartPanel>(FindObjectsInactive.Include);
            if (panel != null && !(panel.transform is RectTransform))
            {
                Debug.LogWarning("[EchoesSceneBuilder] StartPanel dibuat via Create Empty (Transform biasa, bukan RectTransform) — struktur di-rebuild.", panel);
                panel = RebuildBrokenStartPanel(panel, primary);
            }

            if (panel == null)
            {
                GameObject root = new GameObject("StartPanel", typeof(RectTransform), typeof(StartPanel));
                root.transform.SetParent(primary.transform, false);
                SetFullStretch(root.GetComponent<RectTransform>());
                panel = root.GetComponent<StartPanel>();

                Transform content = FindStrayContent(primary);
                if (content != null)
                {
                    content.SetParent(root.transform, false);
                    SetFullStretch(content as RectTransform);
                }
                else
                {
                    GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(Image));
                    contentGo.transform.SetParent(root.transform, false);
                    SetFullStretch(contentGo.GetComponent<RectTransform>());
                    contentGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
                    CreateText("TitleText", contentGo.transform, "ECHOES OF STEAL", 80f, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 140f), TextAlignmentOptions.Center);
                    content = contentGo.transform;
                    CreatePanelButton(root, "BEGIN", "BeginButton", new Vector2(0f, -120f), panel.BeginGame);
                }
            }
            else
            {
                panel.transform.SetParent(primary.transform, false);
            }

            panel.gameObject.name = "StartPanel";
            Transform panelContent = panel.transform.Find("Content");
            if (panelContent != null && panelContent.Find("BeginButton") == null)
                CreatePanelButton(panel.gameObject, "BEGIN", "BeginButton", new Vector2(0f, -120f), panel.BeginGame);
        }

        /// <summary>
        /// Rebuild StartPanel yang transform-nya bukan RectTransform (dibuat via Create Empty,
        /// biasanya berisi Canvas nested otomatis buatan Unity). Konten user (Content + anak-anaknya)
        /// dipindahkan ke panel baru yang valid, listener tombol di-rewire, lalu GO lama dihapus.
        /// </summary>
        private static StartPanel RebuildBrokenStartPanel(StartPanel oldPanel, Canvas primary)
        {
            Transform content = oldPanel.transform.Find("Content");
            if (content == null)
            {
                foreach (Transform child in oldPanel.transform)
                {
                    if (child.GetComponent<Canvas>() != null)
                    {
                        content = child.Find("Content");
                        break;
                    }
                }
            }

            GameObject root = new GameObject("StartPanel", typeof(RectTransform), typeof(StartPanel));
            root.transform.SetParent(primary.transform, false);
            SetFullStretch(root.GetComponent<RectTransform>());
            StartPanel newPanel = root.GetComponent<StartPanel>();

            if (content != null)
            {
                content.SetParent(root.transform, false);
                SetFullStretch(content as RectTransform);

                Transform beginButton = content.Find("BeginButton");
                if (beginButton != null)
                {
                    Button button = beginButton.GetComponent<Button>();
                    if (button != null)
                    {
                        while (button.onClick.GetPersistentEventCount() > 0)
                            UnityEventTools.RemovePersistentListener(button.onClick, 0);
                        UnityEventTools.AddPersistentListener(button.onClick, newPanel.BeginGame);
                    }
                }
            }

            Object.DestroyImmediate(oldPanel.gameObject);
            return newPanel;
        }

        private static Transform FindStrayContent(Canvas primary)
        {
            foreach (Transform child in primary.transform)
            {
                if (child.name == "Content")
                    return child;
            }
            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                foreach (Transform child in canvas.transform)
                {
                    if (child.name == "Content" && child.GetComponentInParent<StartPanel>() == null && child.GetComponentInParent<GameOverPanel>() == null)
                        return child;
                }
            }
            return null;
        }

        private static void EnsureGameOverPanelComponent(Canvas primary)
        {
            GameOverPanel panel = Object.FindAnyObjectByType<GameOverPanel>(FindObjectsInactive.Include);
            if (panel == null)
            {
                GameObject root = new GameObject("GameOverPanel", typeof(RectTransform), typeof(GameOverPanel));
                root.transform.SetParent(primary.transform, false);
                SetFullStretch(root.GetComponent<RectTransform>());
                panel = root.GetComponent<GameOverPanel>();

                GameObject content = new GameObject("Content", typeof(RectTransform), typeof(Image));
                content.transform.SetParent(root.transform, false);
                SetFullStretch(content.GetComponent<RectTransform>());
                content.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.8f);
                CreateText("TitleText", content.transform, "GAME OVER", 80f, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 120f), TextAlignmentOptions.Center);
                TMP_Text finalScore = CreateText("FinalScoreText", content.transform, "Score: 0", 50f, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(600f, 80f), TextAlignmentOptions.Center);
                CreatePanelButton(root, "RESTART", "RestartButton", new Vector2(0f, -170f), panel.Restart, content.transform);

                SerializedObject so = new SerializedObject(panel);
                so.FindProperty("_content").objectReferenceValue = content;
                so.FindProperty("_finalScoreText").objectReferenceValue = finalScore;
                so.ApplyModifiedProperties();
                content.SetActive(false);
            }
            else
            {
                panel.transform.SetParent(primary.transform, false);
            }
            panel.gameObject.name = "GameOverPanel";
        }

        private static void OrderCanvasChildren(Canvas primary)
        {
            Transform hud = primary.transform.Find("HUD");
            if (hud != null)
                hud.SetAsFirstSibling();

            Transform startPanel = primary.transform.Find("StartPanel");
            if (startPanel != null)
                startPanel.SetAsLastSibling();
            Transform gameOverPanel = primary.transform.Find("GameOverPanel");
            if (gameOverPanel != null)
                gameOverPanel.SetAsLastSibling();

            foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (canvas != primary && canvas.transform.parent == null && canvas.transform.childCount == 0)
                    Object.DestroyImmediate(canvas.gameObject);
            }
        }

        private static void WirePanelRefs()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            StartPanel startPanel = Object.FindAnyObjectByType<StartPanel>(FindObjectsInactive.Include);
            if (gameManager != null && startPanel != null)
            {
                SerializedObject so = new SerializedObject(startPanel);
                so.FindProperty("_gameManager").objectReferenceValue = gameManager;
                if (so.FindProperty("_content").objectReferenceValue == null)
                    so.FindProperty("_content").objectReferenceValue = startPanel.transform.Find("Content") != null ? startPanel.transform.Find("Content").gameObject : null;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(startPanel);
            }

            GameOverPanel gameOverPanel = Object.FindAnyObjectByType<GameOverPanel>(FindObjectsInactive.Include);
            if (gameManager != null && gameOverPanel != null)
            {
                SerializedObject so = new SerializedObject(gameOverPanel);
                so.FindProperty("_gameManager").objectReferenceValue = gameManager;
                Transform content = gameOverPanel.transform.Find("Content");
                if (so.FindProperty("_content").objectReferenceValue == null && content != null)
                    so.FindProperty("_content").objectReferenceValue = content.gameObject;
                if (so.FindProperty("_finalScoreText").objectReferenceValue == null && content != null)
                    so.FindProperty("_finalScoreText").objectReferenceValue = content.Find("FinalScoreText") != null ? content.Find("FinalScoreText").GetComponent<TMP_Text>() : null;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(gameOverPanel);
                if (content != null)
                    content.gameObject.SetActive(false);
            }

            Transform beginButton = startPanel != null ? startPanel.transform.Find("Content/BeginButton") : null;
            if (beginButton != null)
            {
                Button button = beginButton.GetComponent<Button>();
                if (button != null && button.onClick.GetPersistentEventCount() == 0)
                    UnityEventTools.AddPersistentListener(button.onClick, startPanel.BeginGame);
            }

            Transform restartButton = gameOverPanel != null ? gameOverPanel.transform.Find("Content/RestartButton") : null;
            if (restartButton != null)
            {
                Button button = restartButton.GetComponent<Button>();
                if (button != null && button.onClick.GetPersistentEventCount() == 0)
                    UnityEventTools.AddPersistentListener(button.onClick, gameOverPanel.Restart);
            }
        }

        /// <summary>
        /// Normalisasi total layout UI: enforce rect/font/posisi tiap elemen,
        /// deduplikasi objek UI kembar, bersihkan listener basi, rapikan sibling order.
        /// Idempotent & korektif — aman dijalankan berulang.
        /// </summary>
        private static void NormalizeUILayout(Canvas primary)
        {
            MergeExtraCanvases(primary);

            Transform hud = primary.transform.Find("HUD");
            if (hud != null)
                NormalizeHudVisuals(hud);
            NormalizeJoystickVisual(primary);
            NormalizeAttackButtonVisual(primary);
            NormalizeStartPanelVisual(primary);
            NormalizeGameOverPanelVisual(primary);

            if (hud != null)
                hud.SetAsFirstSibling();
            Transform startPanel = primary.transform.Find("StartPanel");
            if (startPanel != null)
                startPanel.SetAsLastSibling();
            Transform gameOverPanel = primary.transform.Find("GameOverPanel");
            if (gameOverPanel != null)
                gameOverPanel.SetAsLastSibling();
        }

        private static void MergeExtraCanvases(Canvas primary)
        {
            Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            foreach (Canvas canvas in canvases)
            {
                if (canvas == primary || canvas.transform.parent != null)
                    continue;
                List<Transform> children = new List<Transform>();
                foreach (Transform child in canvas.transform)
                    children.Add(child);
                foreach (Transform child in children)
                    child.SetParent(primary.transform, false);
                Debug.Log($"[EchoesSceneBuilder] Menghapus Canvas duplikat kosong: {canvas.name}", canvas);
                Object.DestroyImmediate(canvas.gameObject);
            }
        }

        private static void SetRect(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        private static void SetFullRect(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>Cari TMP_Text berdasar nama; kalau tidak ada, adopsi TMP tak-bernama yang belum diklaim; kalau tetap tidak ada, buat baru.</summary>
        private static TMP_Text EnsureTmp(Transform parent, string name, HashSet<Transform> claimed, string text, float size, TextAlignmentOptions align, Vector2 anchor, Vector2 pos, Vector2 dim)
        {
            Transform found = parent.Find(name);
            TMP_Text tmp = found != null ? found.GetComponent<TMP_Text>() : null;
            if (tmp == null)
            {
                foreach (Transform child in parent)
                {
                    if (claimed.Contains(child))
                        continue;
                    TMP_Text candidate = child.GetComponent<TMP_Text>();
                    if (candidate != null && child.GetComponent<Button>() == null)
                    {
                        tmp = candidate;
                        tmp.gameObject.name = name;
                        found = child;
                        break;
                    }
                }
            }
            if (tmp == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
                go.transform.SetParent(parent, false);
                tmp = go.GetComponent<TMP_Text>();
                found = go.transform;
            }
            claimed.Add(found);
            SetRect(tmp.GetComponent<RectTransform>(), anchor, pos, dim);
            if (_font != null && tmp.font == null)
                tmp.font = _font;
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.text = text;
            tmp.raycastTarget = false;
            tmp.enabled = true;
            EditorUtility.SetDirty(tmp);
            return tmp;
        }

        private static Button EnsureUiButton(Transform parent, string name, string label, float labelSize, Color bgColor, Vector2 anchor, Vector2 pos, Vector2 dim, UnityAction onClick)
        {
            Transform found = parent.Find(name);
            Button button = found != null ? found.GetComponent<Button>() : null;
            if (button == null)
            {
                foreach (Transform child in parent)
                {
                    Button candidate = child.GetComponent<Button>();
                    if (candidate == null)
                        continue;
                    TMP_Text childLabel = child.GetComponentInChildren<TMP_Text>(true);
                    if ((child.name == name) || (childLabel != null && childLabel.text == label))
                    {
                        button = candidate;
                        button.gameObject.name = name;
                        found = child;
                        break;
                    }
                }
            }
            if (button == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                go.transform.SetParent(parent, false);
                button = go.GetComponent<Button>();
                found = go.transform;
            }
            SetRect(button.GetComponent<RectTransform>(), anchor, pos, dim);
            Image image = button.GetComponent<Image>();
            if (image == null)
                image = button.gameObject.AddComponent<Image>();
            if (image.sprite == null)
                image.sprite = _square;
            image.color = bgColor;
            image.raycastTarget = true;
            button.targetGraphic = image;

            TMP_Text labelTmp = button.GetComponentInChildren<TMP_Text>(true);
            GameObject labelGo;
            if (labelTmp == null)
            {
                labelGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelGo.transform.SetParent(button.transform, false);
                labelTmp = labelGo.GetComponent<TMP_Text>();
            }
            else
            {
                labelGo = labelTmp.gameObject;
                labelGo.name = "Text";
            }
            SetFullRect(labelTmp.GetComponent<RectTransform>());
            if (_font != null && labelTmp.font == null)
                labelTmp.font = _font;
            labelTmp.fontSize = labelSize;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.text = label;
            labelTmp.raycastTarget = false;

            while (button.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(button.onClick, 0);
            if (onClick != null)
                UnityEventTools.AddPersistentListener(button.onClick, onClick);
            EditorUtility.SetDirty(button);
            return button;
        }

        private static void NormalizeHudVisuals(Transform hud)
        {
            HashSet<Transform> claimed = new HashSet<Transform>();

            Transform bg = hud.Find("HealthBarBG");
            if (bg == null)
            {
                GameObject bgGo = new GameObject("HealthBarBG", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(hud, false);
                bg = bgGo.transform;
            }
            SetRect(bg.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(210f, -30f), new Vector2(400f, 40f));
            Image bgImage = bg.GetComponent<Image>();
            if (bgImage == null)
                bgImage = bg.gameObject.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            bgImage.raycastTarget = false;
            claimed.Add(bg);

            Transform fill = bg.Find("HealthBarFill");
            if (fill == null)
            {
                GameObject fillGo = new GameObject("HealthBarFill", typeof(RectTransform), typeof(Image));
                fillGo.transform.SetParent(bg, false);
                fill = fillGo.transform;
            }
            RectTransform fillRt = fill.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(4f, 4f);
            fillRt.offsetMax = new Vector2(-4f, -4f);
            Image fillImage = fill.GetComponent<Image>();
            if (fillImage == null)
                fillImage = fill.gameObject.AddComponent<Image>();
            if (fillImage.sprite == null)
                fillImage.sprite = _square;
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount = 1f;
            fillImage.color = Color.red;
            fillImage.raycastTarget = false;
            claimed.Add(fill);
            bg.SetAsFirstSibling();

            EnsureTmp(hud, "ScoreText", claimed, "Score: 0", 50f, TextAlignmentOptions.TopRight, new Vector2(1f, 1f), new Vector2(-220f, -40f), new Vector2(400f, 60f));
            EnsureTmp(hud, "WaveText", claimed, "Wave 1", 50f, TextAlignmentOptions.Top, new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(400f, 60f));
        }

        private static void NormalizeJoystickVisual(Canvas primary)
        {
            VirtualJoystick joystick = Object.FindAnyObjectByType<VirtualJoystick>(FindObjectsInactive.Include);
            if (joystick == null)
                return;
            joystick.transform.SetParent(primary.transform, false);
            SetRect(joystick.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(200f, 200f), new Vector2(300f, 300f));
            Transform handle = joystick.transform.Find("Handle");
            if (handle != null)
                SetRect(handle.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 120f));
        }

        private static void NormalizeAttackButtonVisual(Canvas primary)
        {
            PlayerController player = Object.FindAnyObjectByType<PlayerController>();
            EnsureUiButton(primary.transform, "AttackButton", "ATTACK", 32f, new Color(0.85f, 0.2f, 0.2f, 0.9f), new Vector2(1f, 0f), new Vector2(-200f, 200f), new Vector2(200f, 200f), player != null ? (UnityAction)player.TryAttack : null);
        }

        private static Transform NormalizePanelRoot(Canvas primary, string name)
        {
            Transform found = primary.transform.Find(name);
            if (found != null && !(found is RectTransform))
            {
                Debug.LogWarning($"[EchoesSceneBuilder] {name} bukan RectTransform — dihapus & dibuat ulang.", found);
                Object.DestroyImmediate(found.gameObject);
                found = null;
            }
            if (found == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform));
                go.transform.SetParent(primary.transform, false);
                SetFullRect(go.GetComponent<RectTransform>());
                found = go.transform;
            }
            else
            {
                SetFullRect(found.GetComponent<RectTransform>());
            }
            return found;
        }

        private static Transform NormalizePanelContent(Transform panelRoot)
        {
            Transform content = panelRoot.Find("Content");
            if (content == null)
            {
                GameObject go = new GameObject("Content", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(panelRoot, false);
                content = go.transform;
            }
            SetFullRect(content.GetComponent<RectTransform>());
            Image image = content.GetComponent<Image>();
            if (image == null)
                image = content.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.8f);
            image.raycastTarget = true;
            return content;
        }

        private static void NormalizeStartPanelVisual(Canvas primary)
        {
            Transform root = NormalizePanelRoot(primary, "StartPanel");
            StartPanel panel = root.GetComponent<StartPanel>();
            if (panel == null)
                panel = root.gameObject.AddComponent<StartPanel>();
            Transform content = NormalizePanelContent(root);

            HashSet<Transform> claimed = new HashSet<Transform> { content };
            EnsureTmp(content, "TitleText", claimed, "ECHOES OF STEAL", 80f, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 150f), new Vector2(1400f, 140f));
            EnsureTmp(content, "SubtitleText", claimed, "Survive the waves", 40f, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(1000f, 80f));
            EnsureUiButton(content, "BeginButton", "BEGIN", 40f, new Color(0.2f, 0.55f, 0.3f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(320f, 90f), (UnityAction)panel.BeginGame);

            root.gameObject.SetActive(true);
            content.gameObject.SetActive(true);
            EditorUtility.SetDirty(panel);
        }

        private static void NormalizeGameOverPanelVisual(Canvas primary)
        {
            Transform root = NormalizePanelRoot(primary, "GameOverPanel");
            GameOverPanel panel = root.GetComponent<GameOverPanel>();
            if (panel == null)
                panel = root.gameObject.AddComponent<GameOverPanel>();
            Transform content = NormalizePanelContent(root);

            HashSet<Transform> claimed = new HashSet<Transform> { content };
            EnsureTmp(content, "TitleText", claimed, "GAME OVER", 80f, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, 80f), new Vector2(800f, 120f));
            EnsureTmp(content, "FinalScoreText", claimed, "Score: 0", 50f, TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(600f, 80f));
            EnsureUiButton(content, "RestartButton", "RESTART", 40f, new Color(0.85f, 0.25f, 0.25f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(320f, 90f), (UnityAction)panel.Restart);

            root.gameObject.SetActive(true);
            content.gameObject.SetActive(false);
            EditorUtility.SetDirty(panel);
        }
    }
}
