using EchoesOfSteal.Combat;
using EchoesOfSteal.Enemy;
using EchoesOfSteal.Player;
using EchoesOfSteal.Systems;
using EchoesOfSteal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace EchoesOfSteal.EditorTools
{
    /// <summary>
    /// Verifikasi wiring scene via batch mode: membuka SampleScene, memeriksa semua
    /// referensi serialized lewat SerializedObject (tanpa mengubah kode runtime),
    /// dan mencetak laporan PASS/FAIL per item.
    /// </summary>
    public static class EchoesSceneVerifier
    {
        private static int _pass;
        private static int _fail;

        [MenuItem("Tools/Echoes of Steal/Verify Scene")]
        public static void VerifyScene()
        {
            _pass = 0;
            _fail = 0;
            EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);

            GameObject player = GameObject.Find("Player");
            Check("Player ada", player != null);
            if (player != null)
            {
                PlayerController controller = player.GetComponent<PlayerController>();
                Check("PlayerController ada", controller != null);
                Check("PlayerController._joystick wired", RefWired(controller, "_joystick"));
                Check("PlayerController._attackArea wired", RefWired(controller, "_attackArea"));
                Check("PlayerHealth ada", player.GetComponent<PlayerHealth>() != null);
                Check("Player layer = Player", player.layer == LayerMask.NameToLayer("Player"));

                Transform attackArea = player.transform.Find("AttackArea");
                Component area = attackArea != null ? attackArea.GetComponent<AttackArea>() : null;
                Check("AttackArea ada", area != null);
                Check("AttackArea._targetLayers = Enemy", IntVal(area, "_targetLayers") == LayerMask.GetMask("Enemy"));
            }

            GameObject spawnerGo = GameObject.Find("WaveSpawner");
            WaveSpawner spawner = spawnerGo != null ? spawnerGo.GetComponent<WaveSpawner>() : null;
            Check("WaveSpawner ada", spawner != null);
            Check("WaveSpawner._waves = 3", ArraySize(spawner, "_waves") == 3);
            Check("WaveSpawner._playerTransform wired", RefWired(spawner, "_playerTransform"));
            Check("WaveSpawner._enemyPrefabs[0] wired", ArraySize(spawner, "_enemyPrefabs") >= 1 && RefWired(spawner, "_enemyPrefabs.Array.data[0]"));
            Check("WaveSpawner._autoStart = false", BoolVal(spawner, "_autoStart") == false);

            GameObject managerGo = GameObject.Find("GameManager");
            GameManager manager = managerGo != null ? managerGo.GetComponent<GameManager>() : null;
            Check("GameManager ada", manager != null);
            Check("GameManager._waveSpawner wired", RefWired(manager, "_waveSpawner"));
            Check("GameManager._playerHealth wired", RefWired(manager, "_playerHealth"));

            Canvas[] allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas bestCanvas = null;
            foreach (Canvas candidate in allCanvases)
            {
                Debug.Log($"[EchoesSceneVerifier] Canvas '{candidate.name}' aktif={candidate.gameObject.activeInHierarchy} children={candidate.transform.childCount}");
                if (bestCanvas == null || candidate.transform.childCount > bestCanvas.transform.childCount)
                    bestCanvas = candidate;
            }
            Check("Canvas ada", bestCanvas != null);
            Transform canvas = bestCanvas != null ? bestCanvas.transform : null;
            if (canvas != null)
            {
                foreach (Transform child in canvas)
                    Debug.Log($"[EchoesSceneVerifier] child canvas utama: {child.name}");
                Check("Joystick ada", canvas.Find("Joystick") != null);

                Transform attackButton = canvas.Find("AttackButton");
                Button attackBtn = attackButton != null ? attackButton.GetComponent<Button>() : null;
                Check("AttackButton ada + 1 listener", attackBtn != null && attackBtn.onClick.GetPersistentEventCount() == 1);

                Transform hud = canvas.Find("HUD");
                HUDController hudController = hud != null ? hud.GetComponent<HUDController>() : null;
                Check("HUD + HUDController ada", hudController != null);
                Check("HUD._healthBarFill wired", RefWired(hudController, "_healthBarFill"));
                Check("HUD._scoreText wired", RefWired(hudController, "_scoreText"));
                Check("HUD._waveText wired", RefWired(hudController, "_waveText"));

                Transform startPanel = canvas.Find("StartPanel");
                Check("StartPanel ada", startPanel != null);
                Transform beginButton = startPanel != null ? startPanel.Find("Content/BeginButton") : null;
                Button beginBtn = beginButton != null ? beginButton.GetComponent<Button>() : null;
                Check("BeginButton ada + 1 listener", beginBtn != null && beginBtn.onClick.GetPersistentEventCount() == 1);

                Transform gameOverPanel = canvas.Find("GameOverPanel");
                Check("GameOverPanel ada", gameOverPanel != null);
                Transform restartButton = gameOverPanel != null ? gameOverPanel.Find("Content/RestartButton") : null;
                Button restartBtn = restartButton != null ? restartButton.GetComponent<Button>() : null;
                Check("RestartButton ada + 1 listener", restartBtn != null && restartBtn.onClick.GetPersistentEventCount() == 1);
            }

            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            Check("Enemy prefab ada", enemyPrefab != null);
            if (enemyPrefab != null)
            {
                Check("Prefab EnemyAI._data wired", RefWired(enemyPrefab.GetComponent("EnemyAI"), "_data"));
                Check("Prefab EnemyAI._playerLayers = Player", IntVal(enemyPrefab.GetComponent("EnemyAI"), "_playerLayers") == LayerMask.GetMask("Player"));
                Check("Prefab EnemyHealth._data wired", RefWired(enemyPrefab.GetComponent("EnemyHealth"), "_data"));
                Collider2D prefabCollider = enemyPrefab.GetComponent<Collider2D>();
                Check("Prefab collider isTrigger", prefabCollider != null && prefabCollider.isTrigger);
            }

            Debug.Log($"[EchoesSceneVerifier] HASIL: {_pass} PASS, {_fail} FAIL");
        }

        private static Component GetComponent(this GameObject go, string typeName)
        {
            foreach (Component component in go.GetComponents<Component>())
                if (component != null && component.GetType().Name == typeName)
                    return component;
            return null;
        }

        private static bool RefWired(Component component, string field)
        {
            if (component == null)
                return false;
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            return property != null && property.objectReferenceValue != null;
        }

        private static int IntVal(Component component, string field)
        {
            if (component == null)
                return 0;
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            return property != null ? property.intValue : 0;
        }

        private static bool BoolVal(Component component, string field)
        {
            if (component == null)
                return false;
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            return property != null && property.boolValue;
        }

        private static int ArraySize(Component component, string field)
        {
            if (component == null)
                return 0;
            SerializedProperty property = new SerializedObject(component).FindProperty(field);
            return property != null && property.isArray ? property.arraySize : 0;
        }

        private static void Check(string label, bool condition)
        {
            if (condition)
            {
                _pass++;
                Debug.Log($"[EchoesSceneVerifier] PASS: {label}");
            }
            else
            {
                _fail++;
                Debug.LogWarning($"[EchoesSceneVerifier] FAIL: {label}");
            }
        }
    }
}
