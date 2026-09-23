using System.Collections.Generic;
using System.IO;
using System.Linq;
using EchoesOfSteal.Combat;
using EchoesOfSteal.Enemy;
using EchoesOfSteal.Player;
using EchoesOfSteal.Systems;
using EchoesOfSteal.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace EchoesOfSteal.EditorTools
{
    /// <summary>
    /// Terapkan visual game dari aset Ninja Adventure (CC0): slice sprite sheet, bangun arena
    /// Tilemap + dinding, ganti sprite player/enemy, pasang VFX tebasan, dan restyle UI
    /// (avatar faceset, ikon slash pada tombol, joystick ring pixel-art).
    /// </summary>
    public static class EchoesVisualBuilder
    {
        private const int ArenaWidth = 40;
        private const int ArenaHeight = 24;

        [MenuItem("Tools/Echoes of Steal/Apply Game Visuals")]
        public static void ApplyGameVisuals()
        {
            EditorApplication.ExecuteMenuItem("Tools/Echoes of Steal/Repair Scene UI");

            SliceAllSpriteSheets();

            Sprite ninjaWalk = LoadSprite("Assets/Art/Characters/NinjaGreen/SeparateAnim/Walk.png", "Walk_0");
            Sprite demonWalk = LoadSprite("Assets/Art/Characters/DemonGreen/SeparateAnim/Walk.png", "Walk_0");
            Sprite ninjaFace = LoadSprite("Assets/Art/Characters/NinjaGreen/Faceset.png", "Faceset");
            Sprite demonFace = LoadSprite("Assets/Art/Characters/DemonGreen/Faceset.png", "Faceset");
            Sprite slashIcon = LoadSprite("Assets/Art/Effects/CircularSlash/SpriteSheet.png", "SpriteSheet_3");
            Sprite[] slashFrames =
            {
                LoadSprite("Assets/Art/Effects/CircularSlash/SpriteSheet.png", "SpriteSheet_0"),
                LoadSprite("Assets/Art/Effects/CircularSlash/SpriteSheet.png", "SpriteSheet_1"),
                LoadSprite("Assets/Art/Effects/CircularSlash/SpriteSheet.png", "SpriteSheet_2"),
                slashIcon
            };

            if (ninjaWalk == null || demonWalk == null || ninjaFace == null || demonFace == null || slashIcon == null)
            {
                Debug.LogError("[EchoesVisualBuilder] Sprite wajib tidak ditemukan — slicing gagal. Cek Console.");
                return;
            }

            List<Tile> floorTiles = BuildFloorTiles();
            BuildArena(floorTiles);
            ApplyCharacterSprites(ninjaWalk, demonWalk);
            WireSlashEffect(slashFrames);
            GenerateJoystickSprites();
            GenerateAttackButtonSprite();
            GenerateXpOrbSprite();
            GenerateFrameSprite();
            WireAnimations();
            WireCamera();
            List<UpgradeDefinition> upgrades = BuildUpgradeAssets();
            GameObject[] enemyPrefabs = BuildEnemyVariants();
            WireProgression(upgrades, enemyPrefabs);
            RestyleUi(ninjaFace, demonFace, slashIcon);

            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[EchoesVisualBuilder] VISUAL SELESAI — arena, karakter, VFX, dan UI bertema diterapkan.");
        }

        private static void SliceAllSpriteSheets()
        {
            SliceSheet("Assets/Art/Characters/NinjaGreen/SeparateAnim/Walk.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Characters/NinjaGreen/SeparateAnim/Attack.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Characters/NinjaGreen/SeparateAnim/Idle.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Characters/DemonGreen/SeparateAnim/Walk.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Characters/DemonGreen/SeparateAnim/Idle.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Effects/CircularSlash/SpriteSheet.png", 32, 32, 32f);
            SliceSheet("Assets/Art/Tilesets/TilesetFloor.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Tilesets/TilesetFloorDetail.png", 16, 16, 16f);
            SliceSheet("Assets/Art/Tilesets/TilesetNature.png", 16, 16, 16f);
            SliceSingle("Assets/Art/Characters/NinjaGreen/Faceset.png", 32f);
            SliceSingle("Assets/Art/Characters/DemonGreen/Faceset.png", 32f);
        }

        private static void SliceSheet(string path, int frameW, int frameH, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[EchoesVisualBuilder] Tidak menemukan {path}");
                return;
            }

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            int cols = texture.width / frameW;
            int rows = texture.height / frameH;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 2048;

            var rects = new List<SpriteMetaData>();
            string baseName = Path.GetFileNameWithoutExtension(path);
            int index = 0;
            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    rects.Add(new SpriteMetaData
                    {
                        name = $"{baseName}_{index}",
                        rect = new Rect(col * frameW, (rows - 1 - row) * frameH, frameW, frameH),
                        alignment = (int)SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f)
                    });
                    index++;
                }
            }

            importer.spritesheet = rects.ToArray();
            importer.SaveAndReimport();
        }

        private static void SliceSingle(string path, float ppu)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        /// <summary>Membuat sprite latar tombol attack pixel-art: lingkaran gelap dengan ring merah coral + outline.</summary>
        private static void GenerateAttackButtonSprite()
        {
            const string path = "Assets/Art/Generated/attack_button.png";
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            var fill = new Color32(52, 20, 28, 235);
            var ring = new Color32(232, 76, 61, 255);
            var outline = new Color32(24, 10, 14, 255);
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (distance > center + 2f)
                        color = new Color32(0, 0, 0, 0);
                    else if (distance > center)
                        color = outline;
                    else if (distance > center - 8f)
                        color = ring;
                    else if (distance > center - 10f)
                        color = outline;
                    else
                        color = fill;
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        /// <summary>Memasang SpriteAnimator (idle+walk) pada Player dan Enemy prefab + wiring referensinya.</summary>
        private static void WireAnimations()
        {
            Sprite[] playerWalk = LoadFrames("Assets/Art/Characters/NinjaGreen/SeparateAnim/Walk.png", "Walk", 4);
            Sprite[] playerIdle = LoadFrames("Assets/Art/Characters/NinjaGreen/SeparateAnim/Idle.png", "Idle", 4);
            Sprite[] enemyWalk = LoadFrames("Assets/Art/Characters/DemonGreen/SeparateAnim/Walk.png", "Walk", 4);

            GameObject player = GameObject.Find("Player");
            SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>(true);

            SpriteAnimator playerAnimator = player.GetComponent<SpriteAnimator>();
            if (playerAnimator == null)
                playerAnimator = player.AddComponent<SpriteAnimator>();
            SerializedObject animatorSo = new SerializedObject(playerAnimator);
            animatorSo.FindProperty("_renderer").objectReferenceValue = playerRenderer;
            SetSpriteArray(animatorSo.FindProperty("_idleFrames"), playerIdle);
            SetSpriteArray(animatorSo.FindProperty("_walkFrames"), playerWalk);
            animatorSo.FindProperty("_secondsPerFrame").floatValue = 0.14f;
            animatorSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(playerAnimator);

            PlayerController controller = player.GetComponent<PlayerController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_animator").objectReferenceValue = playerAnimator;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            SpriteRenderer prefabRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            SpriteAnimator enemyAnimator = prefab.GetComponent<SpriteAnimator>();
            if (enemyAnimator == null)
                enemyAnimator = prefab.AddComponent<SpriteAnimator>();
            SerializedObject enemyAnimatorSo = new SerializedObject(enemyAnimator);
            enemyAnimatorSo.FindProperty("_renderer").objectReferenceValue = prefabRenderer;
            SetSpriteArray(enemyAnimatorSo.FindProperty("_walkFrames"), enemyWalk);
            enemyAnimatorSo.FindProperty("_secondsPerFrame").floatValue = 0.16f;
            enemyAnimatorSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject aiSo = new SerializedObject(prefab.GetComponent<EnemyAI>());
            aiSo.FindProperty("_animator").objectReferenceValue = enemyAnimator;
            aiSo.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject healthSo = new SerializedObject(prefab.GetComponent<EnemyHealth>());
            healthSo.FindProperty("_sprite").objectReferenceValue = prefabRenderer;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SavePrefabAsset(prefab);
        }

        private static void SetSpriteArray(SerializedProperty property, Sprite[] frames)
        {
            property.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        private static Sprite LoadSprite(string path, string spriteName = null)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
                if (asset is Sprite sprite && (spriteName == null || sprite.name == spriteName))
                    return sprite;
            return null;
        }

        private static Sprite[] LoadFrames(string path, string prefix, int count)
        {
            var frames = new List<Sprite>();
            for (int i = 0; i < count; i++)
            {
                Sprite frame = LoadSprite(path, $"{prefix}_{i}");
                if (frame == null)
                {
                    Debug.LogError($"[EchoesVisualBuilder] Frame {prefix}_{i} tidak ditemukan di {path}");
                    return frames.ToArray();
                }
                frames.Add(frame);
            }
            return frames.ToArray();
        }

        /// <summary>Memasang CameraController (follow + screen shake FR-7) pada Main Camera.</summary>
        private static void WireCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                Debug.LogError("[EchoesVisualBuilder] Main Camera tidak ditemukan.");
                return;
            }

            CameraController controller = camera.GetComponent<CameraController>();
            if (controller == null)
                controller = camera.gameObject.AddComponent<CameraController>();

            GameObject player = GameObject.Find("Player");
            SerializedObject so = new SerializedObject(controller);
            so.FindProperty("_target").objectReferenceValue = player.transform;
            so.FindProperty("_attackArea").objectReferenceValue = player.transform.Find("AttackArea").GetComponent<AttackArea>();
            so.FindProperty("_arenaHalfWidth").floatValue = ArenaWidth / 2f;
            so.FindProperty("_arenaHalfHeight").floatValue = ArenaHeight / 2f;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        /// <summary>Tile "datar" (varians warna terendah & opaque penuh) dari TilesetFloor — kandidat lantai arena.</summary>
        private static List<Tile> BuildFloorTiles()
        {
            const string sheetPath = "Assets/Art/Tilesets/TilesetFloor.png";
            EnsureFolder("Assets/Art/Generated");
            EnsureFolder("Assets/Art/Generated/Tiles");

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(sheetPath);
            importer.isReadable = true;
            importer.SaveAndReimport();

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath);
            Color32[] pixels = texture.GetPixels32();
            int width = texture.width;

            var candidates = new List<(Sprite sprite, float variance)>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(sheetPath))
            {
                if (!(asset is Sprite sprite))
                    continue;
                Rect r = sprite.textureRect;
                int x0 = (int)r.x, y0 = (int)r.y;
                long sumR = 0, sumG = 0, sumB = 0, sumSq = 0;
                int opaque = 0;
                int total = (int)r.width * (int)r.height;
                for (int y = 0; y < (int)r.height; y++)
                {
                    for (int x = 0; x < (int)r.width; x++)
                    {
                        Color32 c = pixels[(y0 + y) * width + x0 + x];
                        if (c.a > 128)
                        {
                            opaque++;
                            int lum = (c.r + c.g + c.b) / 3;
                            sumR += c.r; sumG += c.g; sumB += c.b;
                            sumSq += lum * (long)lum;
                        }
                    }
                }

                if (total == 0 || opaque < total * 9 / 10)
                    continue;

                float mean = (sumR + sumG + sumB) / (3f * opaque);
                float variance = sumSq / (float)opaque - mean * mean;
                candidates.Add((sprite, variance));
            }

            importer.isReadable = false;
            importer.SaveAndReimport();

            List<Tile> tiles = new List<Tile>();
            foreach ((Sprite sprite, _) in candidates.OrderBy(c => c.variance).Take(6))
            {
                string tilePath = $"Assets/Art/Generated/Tiles/{sprite.name}.asset";
                Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }
                tile.sprite = sprite;
                EditorUtility.SetDirty(tile);
                tiles.Add(tile);
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[EchoesVisualBuilder] Tile lantai dibuat: {tiles.Count} ({string.Join(", ", tiles.Select(t => t.name))})");
            return tiles;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Membuat sprite joystick pixel-art (ring luar + knob dalam) sebagai PNG di folder Generated.</summary>
        private static void GenerateJoystickSprites()
        {
            EnsureFolder("Assets/Art/Generated");
            GenerateRingSprite("Assets/Art/Generated/joystick_ring.png", 128, 52f, 60f);
            GenerateKnobSprite("Assets/Art/Generated/joystick_knob.png", 72);
        }

        private static void GenerateRingSprite(string path, int size, float innerRadius, float outerRadius)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (distance >= innerRadius - 1.5f && distance <= outerRadius)
                        color = new Color32(255, 255, 255, 175);
                    else if (distance > outerRadius && distance <= outerRadius + 1.5f)
                        color = new Color32(20, 20, 30, 200);
                    else if (distance >= innerRadius - 3f && distance < innerRadius - 1.5f)
                        color = new Color32(255, 255, 255, 90);
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void GenerateKnobSprite(string path, int size)
        {
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size * 0.5f;
            Color32[] pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (distance <= center - 6f)
                        color = new Color32(240, 240, 245, 235);
                    else if (distance <= center - 3f)
                        color = new Color32(60, 60, 75, 235);
                    else if (distance <= center - 0.5f)
                        color = new Color32(30, 30, 40, 200);
                    pixels[y * size + x] = color;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 36f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static void BuildArena(List<Tile> floorTiles)
        {
            Camera camera = Camera.main;
            if (camera != null)
            {
                camera.orthographicSize = 8f;
                camera.backgroundColor = new Color(0.07f, 0.07f, 0.1f);
                camera.transform.position = new Vector3(0f, 0f, -10f);
            }

            GameObject arenaGo = GameObject.Find("Arena");
            if (arenaGo == null)
                arenaGo = new GameObject("Arena", typeof(Grid));
            else if (arenaGo.GetComponent<Grid>() == null)
                arenaGo.AddComponent<Grid>();

            if (floorTiles.Count > 0)
            {
                Tilemap floorMap = GetOrCreateTilemap(arenaGo.transform, "Floor", -10);
                floorMap.ClearAllTiles();
                for (int x = -ArenaWidth / 2; x < ArenaWidth / 2; x++)
                    for (int y = -ArenaHeight / 2; y < ArenaHeight / 2; y++)
                        floorMap.SetTile(new Vector3Int(x, y, 0), floorTiles[Random.Range(0, floorTiles.Count)]);
            }

            GameObject boundsGo = GameObject.Find("ArenaBounds");
            if (boundsGo == null)
                boundsGo = new GameObject("ArenaBounds");
            if (boundsGo.GetComponents<BoxCollider2D>().Length == 0)
            {
                float halfW = ArenaWidth / 2f;
                float halfH = ArenaHeight / 2f;
                MakeWall(boundsGo.transform, "WallTop", new Vector2(0f, halfH + 0.5f), new Vector2(ArenaWidth + 4f, 1f));
                MakeWall(boundsGo.transform, "WallBottom", new Vector2(0f, -halfH - 0.5f), new Vector2(ArenaWidth + 4f, 1f));
                MakeWall(boundsGo.transform, "WallLeft", new Vector2(-halfW - 0.5f, 0f), new Vector2(1f, ArenaHeight + 4f));
                MakeWall(boundsGo.transform, "WallRight", new Vector2(halfW + 0.5f, 0f), new Vector2(1f, ArenaHeight + 4f));
            }

            GameObject player = GameObject.Find("Player");
            if (player != null)
                player.transform.position = Vector3.zero;
        }

        private static Tilemap GetOrCreateTilemap(Transform parent, string name, int sortingOrder)
        {
            Transform existing = parent.Find(name);
            Tilemap map;
            if (existing != null)
            {
                map = existing.GetComponent<Tilemap>();
            }
            else
            {
                GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(parent, false);
                map = go.GetComponent<Tilemap>();
            }
            map.GetComponent<TilemapRenderer>().sortingOrder = sortingOrder;
            return map;
        }

        private static void MakeWall(Transform parent, string name, Vector2 center, Vector2 size)
        {
            GameObject wall = new GameObject(name, typeof(BoxCollider2D));
            wall.transform.SetParent(parent, false);
            wall.transform.position = center;
            BoxCollider2D box = wall.GetComponent<BoxCollider2D>();
            box.size = size;
        }

        private static void ApplyCharacterSprites(Sprite ninjaWalk, Sprite demonWalk)
        {
            GameObject player = GameObject.Find("Player");
            SpriteRenderer playerSprite = player != null ? player.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (playerSprite != null)
            {
                playerSprite.sprite = ninjaWalk;
                playerSprite.color = Color.white;
                playerSprite.flipX = false;
                playerSprite.sortingOrder = 0;
                playerSprite.transform.localScale = Vector3.one;
                EditorUtility.SetDirty(playerSprite);
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            SpriteRenderer prefabSprite = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (prefabSprite != null)
            {
                SerializedObject so = new SerializedObject(prefabSprite);
                so.FindProperty("m_Sprite").objectReferenceValue = demonWalk;
                so.FindProperty("m_Color").colorValue = Color.white;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SavePrefabAsset(prefab);
            }
        }

        private static void WireSlashEffect(Sprite[] frames)
        {
            GameObject player = GameObject.Find("Player");
            Transform slashTransform = player.transform.Find("SlashEffect");
            GameObject slashGo;
            if (slashTransform != null)
            {
                slashGo = slashTransform.gameObject;
            }
            else
            {
                slashGo = new GameObject("SlashEffect");
                slashGo.transform.SetParent(player.transform, false);
            }

            SlashEffect slash = slashGo.GetComponent<SlashEffect>();
            if (slash == null)
                slash = slashGo.AddComponent<SlashEffect>();

            SerializedObject so = new SerializedObject(slash);
            SerializedProperty framesProperty = so.FindProperty("_frames");
            framesProperty.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                framesProperty.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(slash);

            PlayerController controller = player.GetComponent<PlayerController>();
            SerializedObject controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("_slashEffect").objectReferenceValue = slash;
            controllerSo.FindProperty("_spriteRenderer").objectReferenceValue = player.GetComponentInChildren<SpriteRenderer>(true);
            controllerSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void RestyleUi(Sprite ninjaFace, Sprite demonFace, Sprite slashIcon)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            Transform canvasTransform = canvas.transform;

            Transform joystick = canvasTransform.Find("Joystick");
            if (joystick != null)
            {
                Image joystickImage = joystick.GetComponent<Image>();
                joystickImage.color = new Color(1f, 1f, 1f, 0.9f);
                Transform handle = joystick.Find("Handle");
                if (handle != null)
                {
                    Image handleImage = handle.GetComponent<Image>();
                    handleImage.color = new Color(1f, 1f, 1f, 0.95f);
                    if (handleImage.sprite == null || handleImage.sprite.name == "Knob")
                        handleImage.sprite = LoadSprite("Assets/Art/Generated/joystick_knob.png");
                }
                if (joystickImage.sprite == null || joystickImage.sprite.name == "Knob")
                    joystickImage.sprite = LoadSprite("Assets/Art/Generated/joystick_ring.png");
            }

            Transform attackButton = canvasTransform.Find("AttackButton");
            if (attackButton != null)
            {
                Image buttonImage = attackButton.GetComponent<Image>();
                Sprite buttonBg = LoadSprite("Assets/Art/Generated/attack_button.png");
                buttonImage.sprite = buttonBg != null ? buttonBg : slashIcon;
                buttonImage.color = Color.white;
                buttonImage.preserveAspect = true;
                buttonImage.raycastTarget = true;
                attackButton.GetComponent<Button>().targetGraphic = buttonImage;

                Transform legacyLabel = attackButton.Find("Text");
                if (legacyLabel != null)
                    Object.DestroyImmediate(legacyLabel.gameObject);

                Transform icon = attackButton.Find("Icon");
                GameObject iconGo;
                if (icon != null)
                {
                    iconGo = icon.gameObject;
                }
                else
                {
                    iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(attackButton.transform, false);
                }

                Image iconImage = iconGo.GetComponent<Image>();
                iconImage.sprite = slashIcon;
                iconImage.color = new Color(1f, 0.95f, 0.78f);
                iconImage.preserveAspect = true;
                iconImage.raycastTarget = false;
                SetRect(iconGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 150f));
                EditorUtility.SetDirty(iconImage);
            }

            Transform hud = canvasTransform.Find("HUD");
            if (hud != null)
            {
                Transform avatar = hud.Find("HeroAvatar");
                GameObject avatarGo;
                if (avatar != null)
                {
                    avatarGo = avatar.gameObject;
                }
                else
                {
                    avatarGo = new GameObject("HeroAvatar", typeof(RectTransform), typeof(Image));
                    avatarGo.transform.SetParent(hud, false);
                }
                Image avatarImage = avatarGo.GetComponent<Image>();
                avatarImage.sprite = ninjaFace;
                avatarImage.preserveAspect = true;
                avatarImage.raycastTarget = false;
                SetRect(avatarGo.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(44f, -44f), new Vector2(56f, 56f));

                Transform bg = hud.Find("HealthBarBG");
                if (bg != null)
                    SetRect(bg.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(306f, -44f), new Vector2(440f, 44f));
            }

            Transform startContent = canvasTransform.Find("StartPanel/Content");
            if (startContent != null)
            {
                AddPanelAvatar(startContent, "Avatar", ninjaFace, new Vector2(0f, 320f), 150f);
                StyleText(startContent, "TitleText", new Color(1f, 0.84f, 0.35f), 84, true);
                StyleText(startContent, "SubtitleText", new Color(0.8f, 0.8f, 0.85f), 36, false);
                Transform begin = startContent.Find("BeginButton");
                if (begin != null)
                {
                    Image beginImage = begin.GetComponent<Image>();
                    beginImage.type = Image.Type.Sliced;
                    beginImage.color = new Color(0.22f, 0.6f, 0.3f);
                }
            }

            Transform gameOverContent = canvasTransform.Find("GameOverPanel/Content");
            if (gameOverContent != null)
            {
                AddPanelAvatar(gameOverContent, "Avatar", demonFace, new Vector2(0f, 320f), 150f);
                StyleText(gameOverContent, "TitleText", new Color(0.95f, 0.32f, 0.32f), 84, true);
                StyleText(gameOverContent, "FinalScoreText", new Color(1f, 0.84f, 0.35f), 52, true);
                Transform restart = gameOverContent.Find("RestartButton");
                if (restart != null)
                {
                    Image restartImage = restart.GetComponent<Image>();
                    restartImage.type = Image.Type.Sliced;
                    restartImage.color = new Color(0.75f, 0.22f, 0.22f);
                }
            }
        }

        private static void AddPanelAvatar(Transform content, string name, Sprite face, Vector2 position, float size)
        {
            Transform existing = content.Find(name);
            GameObject avatarGo;
            if (existing != null)
            {
                avatarGo = existing.gameObject;
            }
            else
            {
                avatarGo = new GameObject(name, typeof(RectTransform), typeof(Image));
                avatarGo.transform.SetParent(content, false);
            }
            Image image = avatarGo.GetComponent<Image>();
            image.sprite = face;
            image.preserveAspect = true;
            image.raycastTarget = false;
            SetRect(avatarGo.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        private static void StyleText(Transform parent, string name, Color color, float size, bool bold)
        {
            Transform found = parent.Find(name);
            if (found == null)
                return;
            TMP_Text text = found.GetComponent<TMP_Text>();
            if (text == null)
                return;
            text.color = color;
            text.fontSize = size;
            if (bold)
                text.fontStyle = FontStyles.Bold;
            EditorUtility.SetDirty(text);
        }

        /// <summary>Sprite XP orb: diamond kuning dengan highlight (16x16 pixel-art).</summary>
        private static void GenerateXpOrbSprite()
        {
            const string path = "Assets/Art/Generated/xp_orb.png";
            const int size = 16;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            var gem = new Color32(255, 214, 64, 255);
            var gemDark = new Color32(200, 150, 20, 255);
            var shine = new Color32(255, 250, 200, 255);
            var outline = new Color32(90, 60, 10, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Abs(x - 7) + Mathf.Abs(y - 7);
                    Color32 color = new Color32(0, 0, 0, 0);
                    if (dx <= 5)
                    {
                        color = dx == 5 ? outline : gem;
                        if (dx == 5 && (x + y) % 2 == 0)
                            color = gemDark;
                        if (x >= 5 && x <= 6 && y >= 8 && y <= 9)
                            color = shine;
                    }
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        /// <summary>Sprite frame 9-slice pixel-art (48x48, border 8px) untuk panel/bar/kartu UI.</summary>
        private static void GenerateFrameSprite()
        {
            const string path = "Assets/Art/Generated/frame_9slice.png";
            const int size = 48;
            const int border = 8;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color32[] pixels = new Color32[size * size];
            var corner = new Color32(210, 215, 230, 255);
            var edgeLight = new Color32(120, 126, 150, 255);
            var edgeDark = new Color32(45, 48, 62, 255);
            var fill = new Color32(30, 32, 44, 255);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool inBorder = x < border || x >= size - border || y < border || y >= size - border;
                    Color32 color = fill;
                    if (inBorder)
                    {
                        bool isCorner = (x < border || x >= size - border) && (y < border || y >= size - border);
                        if (isCorner)
                            color = corner;
                        else if (y >= size - border)
                            color = edgeLight;
                        else if (x < border)
                            color = edgeLight;
                        else
                            color = edgeDark;
                    }
                    pixels[y * size + x] = color;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 48f;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        /// <summary>Membuat 7 asset UpgradeDefinition (SO) bila belum ada — balancing tanpa sentuh kode.</summary>
        private static List<UpgradeDefinition> BuildUpgradeAssets()
        {
            EnsureFolder("Assets/ScriptableObjects/Upgrades");
            return new List<UpgradeDefinition>
            {
                EnsureUpgrade("UpMoveSpeed", UpgradeDefinition.StatType.MoveSpeed, 0.6f, "Swift Boots", "+0.6 kecepatan gerak."),
                EnsureUpgrade("UpDamage", UpgradeDefinition.StatType.Damage, 1f, "Sharpened Blade", "+1 damage serangan."),
                EnsureUpgrade("UpAttackSpeed", UpgradeDefinition.StatType.AttackSpeed, 0.12f, "Quick Hands", "Serangan 12% lebih cepat."),
                EnsureUpgrade("UpKnockback", UpgradeDefinition.StatType.Knockback, 4f, "Heavy Strikes", "+4 gaya knockback."),
                EnsureUpgrade("UpMagnet", UpgradeDefinition.StatType.MagnetRadius, 1.2f, "Soul Magnet", "+1.2 radius tarik XP."),
                EnsureUpgrade("UpMaxHealth", UpgradeDefinition.StatType.MaxHealth, 3f, "Vitality", "+3 HP maksimum & heal."),
                EnsureUpgrade("UpAttackSize", UpgradeDefinition.StatType.AttackSize, 0.25f, "Great Cleaver", "Hitbox serangan 25% lebih besar.")
            };
        }

        private static UpgradeDefinition EnsureUpgrade(string fileName, UpgradeDefinition.StatType type, float value, string title, string description)
        {
            string path = $"Assets/ScriptableObjects/Upgrades/{fileName}.asset";
            UpgradeDefinition existing = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path);
            if (existing != null)
                return existing;

            UpgradeDefinition upgrade = ScriptableObject.CreateInstance<UpgradeDefinition>();
            AssetDatabase.CreateAsset(upgrade, path);
            SerializedObject so = new SerializedObject(upgrade);
            so.FindProperty("_type").enumValueIndex = (int)type;
            so.FindProperty("_value").floatValue = value;
            so.FindProperty("_title").stringValue = title;
            so.FindProperty("_description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrade);
            return upgrade;
        }

        /// <summary>Membuat 3 tipe musuh (SO stats + prefab variant dengan tint/scale beda) — index 0=normal, 1=fast, 2=tank.</summary>
        private static GameObject[] BuildEnemyVariants()
        {
            EnemyData normal = EnsureEnemyData("EnemyNormal", 3f, 2f, 1f, 5f, 2f, 0.2f);
            EnemyData fast = EnsureEnemyData("EnemyFast", 2f, 3.4f, 1f, 5f, 2f, 0.14f);
            EnemyData tank = EnsureEnemyData("EnemyTank", 8f, 1.1f, 2f, 8f, 6f, 0.3f);

            GameObject normalPrefab = EnsureEnemyPrefab("Assets/Prefabs/Enemy.prefab", normal, new Color(1f, 1f, 1f), 1f);
            GameObject fastPrefab = EnsureEnemyPrefab("Assets/Prefabs/EnemyFast.prefab", fast, new Color(0.6f, 0.9f, 1f), 0.8f);
            GameObject tankPrefab = EnsureEnemyPrefab("Assets/Prefabs/EnemyTank.prefab", tank, new Color(0.85f, 0.55f, 1f), 1.3f);
            return new[] { normalPrefab, fastPrefab, tankPrefab };
        }

        private static EnemyData EnsureEnemyData(string fileName, float hp, float speed, float damage, float knockback, float xp, float hitReact)
        {
            string path = $"Assets/ScriptableObjects/{fileName}.asset";
            EnemyData existing = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
            if (existing != null)
                return existing;

            EnemyData data = ScriptableObject.CreateInstance<EnemyData>();
            AssetDatabase.CreateAsset(data, path);
            SerializedObject so = new SerializedObject(data);
            so.FindProperty("_maxHealth").floatValue = hp;
            so.FindProperty("_moveSpeed").floatValue = speed;
            so.FindProperty("_contactDamage").floatValue = damage;
            so.FindProperty("_contactKnockback").floatValue = knockback;
            so.FindProperty("_xpReward").floatValue = xp;
            so.FindProperty("_hitReactDuration").floatValue = hitReact;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(data);
            return data;
        }

        private static GameObject EnsureEnemyPrefab(string path, EnemyData data, Color tint, float scale)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            if (source == null)
            {
                Debug.LogError("[EchoesVisualBuilder] Enemy.prefab dasar tidak ditemukan.");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            instance.transform.position = Vector3.zero;

            SpriteRenderer sprite = instance.GetComponentInChildren<SpriteRenderer>(true);
            sprite.color = tint;
            sprite.transform.localScale = Vector3.one * scale;

            SerializedObject aiSo = new SerializedObject(instance.GetComponent<EnemyAI>());
            aiSo.FindProperty("_data").objectReferenceValue = data;
            aiSo.ApplyModifiedPropertiesWithoutUndo();
            SerializedObject healthSo = new SerializedObject(instance.GetComponent<EnemyHealth>());
            healthSo.FindProperty("_data").objectReferenceValue = data;
            healthSo.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return prefab;
        }

        /// <summary>Wiring sistem progression: PlayerLevel/PlayerUpgrader di Player, UpgradePanel UI, FeedbackSpawner, HUD refs, prefab array WaveSpawner.</summary>
        private static void WireProgression(List<UpgradeDefinition> upgrades, GameObject[] enemyPrefabs)
        {
            GameObject player = GameObject.Find("Player");

            PlayerLevel level = player.GetComponent<PlayerLevel>();
            if (level == null)
                level = player.AddComponent<PlayerLevel>();

            PlayerUpgrader upgrader = player.GetComponent<PlayerUpgrader>();
            if (upgrader == null)
                upgrader = player.AddComponent<PlayerUpgrader>();

            SerializedObject upgraderSo = new SerializedObject(upgrader);
            SerializedProperty poolProperty = upgraderSo.FindProperty("_pool");
            poolProperty.arraySize = upgrades.Count;
            for (int i = 0; i < upgrades.Count; i++)
                poolProperty.GetArrayElementAtIndex(i).objectReferenceValue = upgrades[i];
            upgraderSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(upgrader);

            GameObject feedbackGo = GameObject.Find("FeedbackSpawner");
            if (feedbackGo == null)
                feedbackGo = new GameObject("FeedbackSpawner");
            FeedbackSpawner feedback = feedbackGo.GetComponent<FeedbackSpawner>();
            if (feedback == null)
                feedback = feedbackGo.AddComponent<FeedbackSpawner>();
            SerializedObject feedbackSo = new SerializedObject(feedback);
            feedbackSo.FindProperty("_orbSprite").objectReferenceValue = LoadSprite("Assets/Art/Generated/xp_orb.png");
            feedbackSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(feedback);

            GameObject spawnerGo = GameObject.Find("WaveSpawner");
            WaveSpawner spawner = spawnerGo != null ? spawnerGo.GetComponent<WaveSpawner>() : null;
            if (spawner != null)
            {
                SerializedObject spawnerSo = new SerializedObject(spawner);
                SerializedProperty prefabsProperty = spawnerSo.FindProperty("_enemyPrefabs");
                prefabsProperty.arraySize = enemyPrefabs.Length;
                for (int i = 0; i < enemyPrefabs.Length; i++)
                    prefabsProperty.GetArrayElementAtIndex(i).objectReferenceValue = enemyPrefabs[i] != null ? enemyPrefabs[i].GetComponent<EnemyAI>() : null;
                spawnerSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(spawner);
            }

            Transform hud = GameObject.Find("Canvas").transform.Find("HUD");
            HUDController hudController = hud != null ? hud.GetComponent<HUDController>() : null;
            if (hudController != null)
            {
                SerializedObject hudSo = new SerializedObject(hudController);
                hudSo.FindProperty("_playerLevel").objectReferenceValue = level;
                hudSo.FindProperty("_xpBarFill").objectReferenceValue = hud.Find("XPBarBG/XPBarFill")?.GetComponent<Image>();
                hudSo.FindProperty("_levelText").objectReferenceValue = hud.Find("LevelText")?.GetComponent<TMP_Text>();
                hudSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(hudController);
            }

            WireUpgradePanel(player, level, upgrader);
        }

        private static void WireUpgradePanel(GameObject player, PlayerLevel level, PlayerUpgrader upgrader)
        {
            Transform panelTransform = GameObject.Find("Canvas").transform.Find("UpgradePanel");
            if (panelTransform == null)
            {
                Debug.LogError("[EchoesVisualBuilder] UpgradePanel belum dibuat — jalankan Repair Scene UI dulu.");
                return;
            }

            UpgradePanel panel = panelTransform.GetComponent<UpgradePanel>();
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            SerializedObject so = new SerializedObject(panel);
            so.FindProperty("_gameManager").objectReferenceValue = gameManager;
            so.FindProperty("_upgrader").objectReferenceValue = upgrader;
            so.FindProperty("_playerLevel").objectReferenceValue = level;
            so.FindProperty("_content").objectReferenceValue = panelTransform.Find("Content").gameObject;
            so.FindProperty("_levelTitle").objectReferenceValue = panelTransform.Find("Content/TitleText").GetComponent<TMP_Text>();

            SerializedProperty cardsProperty = so.FindProperty("_cards");
            string[] cardNames = { "CardA", "CardB", "CardC" };
            cardsProperty.arraySize = cardNames.Length;
            for (int i = 0; i < cardNames.Length; i++)
            {
                Transform card = panelTransform.Find($"Content/{cardNames[i]}");
                if (card == null)
                    continue;
                SerializedProperty cardProperty = cardsProperty.GetArrayElementAtIndex(i);
                cardProperty.FindPropertyRelative("Root").objectReferenceValue = card.gameObject;
                cardProperty.FindPropertyRelative("Button").objectReferenceValue = card.GetComponent<Button>();
                cardProperty.FindPropertyRelative("Title").objectReferenceValue = card.Find("TitleText").GetComponent<TMP_Text>();
                cardProperty.FindPropertyRelative("Description").objectReferenceValue = card.Find("DescText").GetComponent<TMP_Text>();
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(panel);
            panelTransform.Find("Content").gameObject.SetActive(false);
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = anchor;
            rectTransform.anchorMax = anchor;
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }
    }
}
