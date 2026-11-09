#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Dolores.EditorTools
{
    /// <summary>
    /// Construye desde cero las escenas MainMenu y Level1 ("1810: El Grito de la Libertad").
    /// Menú: Tools > Dolores 1810 > Construir Nivel 1 y menú principal.
    /// Es seguro ejecutarlo varias veces: vuelve a generar ambas escenas.
    /// </summary>
    public static class Nivel1Builder
    {
        const string Root = "Assets/Dolores_Nivel1";
        const string ArtDir = Root + "/Art/";
        const string AudioDir = Root + "/Audio/";
        const string ScenesDir = "Assets/Scenes";
        const float LevelEnd = 880f;

        static Material litMat, unlitMat;
        static int layerGround, layerPlayer, layerEnemy;
        static System.Random rnd;
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;

        class Seg { public float x0, x1, top; public bool grass; }
        static List<Seg> segs;

        static List<SpriteRenderer> glows;
        static List<Light2D> lamps;

        // ================================================================== punto de entrada
        [MenuItem("Tools/Dolores 1810/Construir Nivel 1 y menu principal")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                rnd = new System.Random(1810);
                EditorUtility.DisplayProgressBar("1810", "Importando recursos...", 0.05f);
                AssetDatabase.Refresh();
                EnsureLayers();
                EditorUtility.DisplayProgressBar("1810", "Configurando sprites...", 0.15f);
                ConfigureSprites();
                EnsureMaterials();
                if (!Directory.Exists(Path.Combine(Application.dataPath, "Scenes"))) Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
                EditorUtility.DisplayProgressBar("1810", "Construyendo el menú principal...", 0.4f);
                BuildMenuScene();
                EditorUtility.DisplayProgressBar("1810", "Construyendo el Nivel 1...", 0.6f);
                BuildLevelScene();
                SetBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Listo",
                    "Se crearon las escenas MainMenu y Level1 y se agregaron al Build Settings.\n\n" +
                    "Abre MainMenu y presiona Play para probar todo el juego.", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[1810] Error al construir: " + e);
                EditorUtility.DisplayDialog("Error al construir", e.Message + "\n\nRevisa la consola para más detalles.", "OK");
            }
        }

        // ================================================================== preparación
        static int EnsureLayer(string name)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            var tm = new SerializedObject(assets[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 8; i < 32; i++)
                if (layers.GetArrayElementAtIndex(i).stringValue == name) return i;
            for (int i = 8; i < 32; i++)
            {
                var p = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(p.stringValue))
                {
                    p.stringValue = name;
                    tm.ApplyModifiedProperties();
                    return i;
                }
            }
            throw new Exception("No hay capas libres para '" + name + "'. Libera una en Project Settings > Tags and Layers.");
        }

        static void EnsureLayers()
        {
            layerGround = EnsureLayer("Ground");
            layerPlayer = EnsureLayer("Player");
            layerEnemy = EnsureLayer("Enemy");
        }

        static void ConfigureSprites()
        {
            string manifest = Path.Combine(Application.dataPath, "Dolores_Nivel1/Art/art_manifest.txt");
            if (!File.Exists(manifest)) throw new Exception("No se encontró " + manifest);
            string[] lines = File.ReadAllLines(manifest);
            int changed = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string raw in lines)
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    string[] p = line.Split('|');
                    string path = ArtDir + p[0];
                    var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                    if (imp == null) { Debug.LogWarning("[1810] Sin importador para " + path); continue; }
                    float ppu = float.Parse(p[1], CI);
                    Vector2 pivot = new Vector2(float.Parse(p[2], CI), float.Parse(p[3], CI));
                    bool full = p[4] == "1";
                    bool point = p[5] == "1";
                    string[] b = p[6].Split(',');
                    Vector4 border = new Vector4(float.Parse(b[0], CI), float.Parse(b[1], CI), float.Parse(b[2], CI), float.Parse(b[3], CI));
                    FilterMode fm = point ? FilterMode.Point : FilterMode.Bilinear;

                    var st = new TextureImporterSettings();
                    imp.ReadTextureSettings(st);
                    bool ok = imp.textureType == TextureImporterType.Sprite
                              && st.spriteMode == (int)SpriteImportMode.Single
                              && Mathf.Abs(st.spritePixelsPerUnit - ppu) < 0.01f
                              && st.spriteAlignment == (int)SpriteAlignment.Custom
                              && Vector2.Distance(st.spritePivot, pivot) < 0.002f
                              && (st.spriteMeshType == SpriteMeshType.FullRect) == full
                              && imp.filterMode == fm
                              && imp.spriteBorder == border;
                    if (ok) continue;

                    imp.textureType = TextureImporterType.Sprite;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    imp.spritePixelsPerUnit = ppu;
                    imp.mipmapEnabled = false;
                    imp.alphaIsTransparency = true;
                    imp.filterMode = fm;
                    imp.wrapMode = TextureWrapMode.Clamp;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.maxTextureSize = 4096;
                    imp.ReadTextureSettings(st);
                    st.spriteAlignment = (int)SpriteAlignment.Custom;
                    st.spritePivot = pivot;
                    st.spriteMeshType = full ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                    imp.SetTextureSettings(st);
                    imp.spriteBorder = border;
                    imp.SaveAndReimport();
                    changed++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();
            Debug.Log("[1810] Sprites configurados: " + changed);
        }

        static Material GetMat(string name, string shaderName)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m != null) return m;
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Dolores_Nivel1/Materials"));
            var sh = Shader.Find(shaderName);
            if (sh == null) sh = Shader.Find("Sprites/Default");
            m = new Material(sh);
            AssetDatabase.CreateAsset(m, path);
            return m;
        }

        static void EnsureMaterials()
        {
            litMat = GetMat("Sprite_Lit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            unlitMat = GetMat("Sprite_Unlit", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
        }

        // ================================================================== utilidades de carga
        static Sprite S(string rel)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + rel);
            if (s == null) Debug.LogWarning("[1810] Falta el sprite: " + rel);
            return s;
        }

        static Sprite[] Seq(string folder, string anim, int count)
        {
            var arr = new Sprite[count];
            for (int i = 0; i < count; i++) arr[i] = S(folder + "/" + anim + "_" + i.ToString("00") + ".png");
            return arr;
        }

        static SpriteClip Clip(string name, Sprite[] frames, float fps, bool loop)
        {
            var c = new SpriteClip();
            c.name = name; c.frames = frames; c.fps = fps; c.loop = loop;
            return c;
        }

        static AudioClip A(string file)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>(AudioDir + file);
            if (c == null) Debug.LogWarning("[1810] Falta el audio: " + file);
            return c;
        }

        static float R(float a, float b)
        {
            return a + (float)rnd.NextDouble() * (b - a);
        }

        static SpriteRenderer SR(string name, Sprite sp, Vector3 pos, int order, Transform parent, bool unlit, bool local)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            if (local) go.transform.localPosition = pos; else go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.sortingOrder = order;
            sr.sharedMaterial = unlit ? unlitMat : litMat;
            return sr;
        }

        static SpriteRenderer Tiled(string name, Sprite sp, Vector3 center, Vector2 size, int order, Transform parent, Color? color)
        {
            var sr = SR(name, sp, center, order, parent, false, false);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = size;
            if (color.HasValue) sr.color = color.Value;
            return sr;
        }

        static Transform Group(string name)
        {
            return new GameObject(name).transform;
        }

        // ================================================================== escena: menú
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            glows = new List<SpriteRenderer>();
            lamps = new List<Light2D>();

            // cámara
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5.4f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            cam.nearClipPlane = -20f;
            cam.farClipPlane = 60f;
            camGO.transform.position = new Vector3(0f, 3.4f, -10f);
            camGO.AddComponent<AudioListener>();

            AddGlobalLight(new Color(0.85f, 0.82f, 1f), 0.95f);

            // fondo
            var bg = Group("Fondo");
            var sky = new GameObject("Cielo");
            sky.transform.SetParent(bg);
            var skyNight = SR("Noche", S("Background/sky_night.png"), Vector3.zero, -100, sky.transform, true, true);
            var skyDusk = SR("Atardecer", S("Background/sky_dusk.png"), Vector3.zero, -99, sky.transform, true, true);
            skyDusk.color = new Color(1f, 1f, 1f, 0.55f);
            var sf = sky.AddComponent<SkyFollow>();
            sf.cam = cam; sf.reference = skyNight;

            var starsRoot = new GameObject("Estrellas");
            starsRoot.transform.SetParent(bg);
            var stars = SR("Estrellas", S("Background/stars.png"), Vector3.zero, -97, starsRoot.transform, true, true);
            var sf2 = starsRoot.AddComponent<SkyFollow>();
            sf2.cam = cam; sf2.reference = stars;

            var moon = SR("Luna", S("Background/moon.png"), new Vector3(6.2f, 8.2f, 0f), -96, bg, true, false);
            moon.transform.localScale = Vector3.one * 1.6f;

            Color far = new Color(0.55f, 0.58f, 0.85f, 1f);
            Layer("MontanasLejanas", S("Background/mountains_far.png"), 0f, 0f, -1.5f, -80, far, bg, -0.12f);
            Layer("MontanasCercanas", S("Background/mountains_near.png"), 0f, 0f, -1.5f, -75, new Color(0.48f, 0.5f, 0.78f, 1f), bg, -0.25f);
            Layer("Lomas", S("Background/hills_green.png"), 0f, 0f, -1.4f, -70, new Color(0.45f, 0.55f, 0.6f, 1f), bg, -0.4f);
            Layer("Pueblo", S("Background/town_far.png"), 0f, 0f, -1.4f, -65, new Color(0.5f, 0.5f, 0.78f, 1f), bg, -0.6f);
            Layer("PuebloLuces", S("Background/town_lights.png"), 0f, 0f, -1.4f, -64, new Color(1f, 1f, 1f, 1f), bg, -0.6f);

            // suelo y personajes
            var world = Group("Escenario");
            Tiled("Calle", S("Tiles/ground_cobble_top.png"), new Vector3(0f, -0.5f, 0f), new Vector2(40f, 1f), 1, world, null);
            Tiled("Tierra", S("Tiles/ground_dirt.png"), new Vector3(0f, -4f, 0f), new Vector2(40f, 6f), 0, world, null);

            var hero = Character("Body_A", new Vector3(-6.4f, 0f, 0f), 20, false);
            var heroAnim = hero.GetComponentInChildren<FrameAnimator>();
            var heroIdle = Clip("idle", Seq("Player", "Idle", 4), 6f, true);
            var holder = hero.AddComponent<MenuActor>();
            holder.animator = heroAnim; holder.clip = heroIdle; holder.flip = false;

            var knight = Character("Soldado", new Vector3(6.6f, 0f, 0f), 20, true);
            var kAnim = knight.GetComponentInChildren<FrameAnimator>();
            var kHolder = knight.AddComponent<MenuActor>();
            kHolder.animator = kAnim; kHolder.clip = Clip("idle", Seq("Enemies/Knight1", "Idle", 10), 8f, true); kHolder.flip = true;

            // audio + interfaz
            var audio = new GameObject("Audio");
            var hub = audio.AddComponent<AudioHub>();
            hub.musicClip = A("music_menu.ogg");
            hub.musicVolume = 0.5f;
            hub.sfx = SfxEntries();

            var ui = new GameObject("MenuUI");
            var mm = ui.AddComponent<MainMenuUI>();
            mm.panelSprite = S("UI/panel.png");
            mm.buttonSprite = S("UI/button.png");
            mm.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/TitleFont.otf");
            mm.levelScene = SceneNames.Level1;

            EditorSceneManager.SaveScene(scene, ScenesDir + "/MainMenu.unity");
        }

        static GameObject Character(string name, Vector3 pos, int order, bool flipX)
        {
            var root = new GameObject(name);
            root.transform.position = pos;
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            sr.sharedMaterial = litMat;
            sr.flipX = flipX;
            var an = vis.AddComponent<FrameAnimator>();
            an.target = sr;
            var sh = SR("Sombra", S("Fx/shadow.png"), new Vector3(0f, 0.03f, 0f), order - 1, root.transform, true, true);
            sh.transform.localScale = new Vector3(1.3f, 1f, 1f);
            return root;
        }

        static void AddGlobalLight(Color color, float intensity)
        {
            var gl = new GameObject("Global Light 2D");
            var l = gl.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.color = color;
            l.intensity = intensity;
        }

        static GameObject Layer(string name, Sprite sp, float speedX, float speedY, float y, int order, Color col, Transform parent, float drift)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(0f, y, 0f);
            float w = sp.bounds.size.x;
            for (int i = -1; i <= 1; i++)
            {
                var sr = SR(name + "_" + i, sp, new Vector3(i * w, 0f, 0f), order, root.transform, true, true);
                sr.color = col;
            }
            var pl = root.AddComponent<ParallaxLayer>();
            pl.speedX = speedX; pl.speedY = speedY; pl.tileWidth = w; pl.drift = drift;
            return root;
        }

        static AudioHub.Entry[] SfxEntries()
        {
            var map = new List<AudioHub.Entry>();
            Action<SfxId, string, float> add = (id, file, vol) =>
            {
                var e = new AudioHub.Entry();
                e.id = id; e.clip = A(file); e.volume = vol;
                map.Add(e);
            };
            add(SfxId.Jump, "sfx_jump.wav", 0.7f);
            add(SfxId.Land, "sfx_land.wav", 0.8f);
            add(SfxId.Swing, "sfx_swing.wav", 0.8f);
            add(SfxId.HitEnemy, "sfx_hit_enemy.wav", 0.9f);
            add(SfxId.PlayerHurt, "sfx_player_hurt.wav", 1f);
            add(SfxId.EnemyDie, "sfx_enemy_die.wav", 0.9f);
            add(SfxId.PlayerDie, "sfx_player_die.wav", 1f);
            add(SfxId.Coin, "sfx_coin.wav", 0.7f);
            add(SfxId.Life, "sfx_life.wav", 0.9f);
            add(SfxId.Heal, "sfx_heal.wav", 0.9f);
            add(SfxId.Gem, "sfx_gem.wav", 0.9f);
            add(SfxId.Checkpoint, "sfx_checkpoint.wav", 0.9f);
            add(SfxId.Bell, "sfx_bell.wav", 1f);
            add(SfxId.Break, "sfx_break.wav", 0.9f);
            add(SfxId.Spike, "sfx_spike.wav", 0.8f);
            add(SfxId.Click, "sfx_click.wav", 0.8f);
            add(SfxId.Hover, "sfx_hover.wav", 0.6f);
            add(SfxId.Pause, "sfx_pause.wav", 0.8f);
            add(SfxId.Step, "sfx_step.wav", 0.6f);
            add(SfxId.Alert, "sfx_alert.wav", 0.7f);
            add(SfxId.EnemySwing, "sfx_enemy_swing.wav", 0.7f);
            add(SfxId.Victory, "sfx_victory.wav", 1f);
            add(SfxId.GameOver, "sfx_gameover.wav", 1f);
            return map.ToArray();
        }

        // ================================================================== escena: nivel 1
        static void BuildLevelScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            glows = new List<SpriteRenderer>();
            lamps = new List<Light2D>();
            DefineSegments();

            var gGround = Group("Suelo");
            var gHouses = Group("Casas");
            var gProps = Group("Decorado");
            var gEnemies = Group("Enemigos");
            var gItems = Group("Coleccionables");
            var gCheck = Group("PuntosDeControl");
            var gLights = Group("Luces");
            var gBg = Group("Fondo");
            var gMgr = Group("Gestores");

            // cámara
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            cam.nearClipPlane = -20f;
            cam.farClipPlane = 60f;
            camGO.transform.position = new Vector3(3f, 2.8f, -10f);
            camGO.AddComponent<AudioListener>();
            var fcam = camGO.AddComponent<FollowCamera>();
            fcam.minX = -6f; fcam.maxX = 878f;

            var globalLight = AddGlobalLightReturn();

            // mundo
            foreach (var s in segs) BuildGround(s, gGround);
            BuildWalls(gGround);
            BuildPits(gGround);
            BuildPlatforms(gGround);
            BuildHouses(gHouses);
            BuildProps(gProps);
            BuildHazards(gProps);
            BuildLamps(gLights);
            BuildBreakables(gProps);

            // jugador
            var player = BuildPlayer();
            fcam.target = player.transform;

            // enemigos, objetos, checkpoints, meta
            BuildEnemies(gEnemies);
            BuildPickups(gItems);
            BuildCheckpoints(gCheck);
            BuildZones(gCheck);
            BuildGoal(gCheck);

            // fondo
            var dn = BuildBackground(gBg, cam, globalLight, player.transform);

            // gestores
            var audio = new GameObject("Audio");
            audio.transform.SetParent(gMgr);
            var hub = audio.AddComponent<AudioHub>();
            hub.musicClip = A("music_level.ogg");
            hub.musicVolume = 0.42f;
            hub.sfx = SfxEntries();

            var sessGO = new GameObject("GameSession");
            sessGO.transform.SetParent(gMgr);
            var gs = sessGO.AddComponent<GameSession>();
            gs.player = player;
            gs.cam = fcam;
            var start = new GameObject("PuntoDeInicio");
            start.transform.SetParent(gMgr);
            start.transform.position = new Vector3(3f, 0.1f, 0f);
            gs.startPoint = start.transform;
            gs.sparkSprite = S("Fx/spark.png");
            gs.dustSprite = S("Fx/dust.png");
            gs.coinSprites = new Sprite[6];
            for (int i = 0; i < 6; i++) gs.coinSprites[i] = S("Pickups/coin_" + i + ".png");
            gs.appleSprite = S("Pickups/apple.png");
            gs.heartSprite = S("Pickups/heart.png");
            gs.litMaterial = litMat;
            gs.unlitMaterial = unlitMat;
            gs.menuScene = SceneNames.Menu;
            gs.nextScene = SceneNames.Level2;

            var uiGO = new GameObject("GameUI");
            uiGO.transform.SetParent(gMgr);
            var ui = uiGO.AddComponent<GameUI>();
            ui.heartSprite = S("Pickups/heart.png");
            ui.coinSprite = S("Pickups/coin_0.png");
            ui.panelSprite = S("UI/panel.png");
            ui.buttonSprite = S("UI/button.png");
            ui.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/TitleFont.otf");

            EditorSceneManager.SaveScene(scene, ScenesDir + "/Level1.unity");
        }

        static Light2D AddGlobalLightReturn()
        {
            var gl = new GameObject("Global Light 2D");
            var l = gl.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.color = Color.white;
            l.intensity = 1f;
            return l;
        }

        // ------------------------------------------------------------------ terreno
        static void DefineSegments()
        {
            segs = new List<Seg>();
            Action<float, float, float, bool> add = (a, b, t, g) =>
            {
                var s = new Seg(); s.x0 = a; s.x1 = b; s.top = t; s.grass = g; segs.Add(s);
            };
            add(-6f, 168f, 0f, false);
            add(171f, 190f, 0f, false);
            add(190f, 200f, 1f, true);
            add(200f, 330f, 0f, false);
            add(333.5f, 470f, 0f, false);
            add(470f, 480f, 1f, true);
            add(480f, 510f, 2f, true);
            add(510f, 520f, 1f, true);
            add(520f, 560f, 0f, false);
            add(563.5f, 606f, 0f, false);
            add(609.5f, 716f, 0f, false);
            add(719.5f, LevelEnd, 0f, false);
        }

        static bool TryGroundTop(float x, out float top)
        {
            foreach (var s in segs)
            {
                if (x >= s.x0 && x <= s.x1) { top = s.top; return true; }
            }
            top = 0f;
            return false;
        }

        static float GroundTop(float x)
        {
            float t;
            TryGroundTop(x, out t);
            return t;
        }

        static bool OnGround(float x, float margin)
        {
            float t;
            return TryGroundTop(x - margin, out t) && TryGroundTop(x + margin, out t);
        }

        static void BuildGround(Seg s, Transform parent)
        {
            float w = s.x1 - s.x0;
            float depth = 8f;
            var go = new GameObject("Suelo_" + (int)s.x0);
            go.layer = layerGround;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(s.x0 + w / 2f, s.top, 0f);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, depth);
            bc.offset = new Vector2(0f, -depth / 2f);

            bool capL = true, capR = true;
            foreach (var o in segs)
            {
                if (Mathf.Approximately(o.x1, s.x0) && Mathf.Approximately(o.top, s.top)) capL = false;
                if (Mathf.Approximately(o.x0, s.x1) && Mathf.Approximately(o.top, s.top)) capR = false;
            }
            Sprite topTile = S(s.grass ? "Tiles/ground_grass_top.png" : "Tiles/ground_cobble_top.png");
            float a = s.x0 + (capL ? 1f : 0f);
            float b = s.x1 - (capR ? 1f : 0f);
            Tiled("Superficie", topTile, new Vector3((a + b) / 2f, s.top - 0.5f, 0f), new Vector2(b - a, 1f), 1, go.transform, null);
            if (capL) SR("BordeIzq", S("Tiles/ground_corner_l.png"), new Vector3(s.x0 + 0.5f, s.top - 0.5f, 0f), 1, go.transform, false, false);
            if (capR) SR("BordeDer", S("Tiles/ground_corner_r.png"), new Vector3(s.x1 - 0.5f, s.top - 0.5f, 0f), 1, go.transform, false, false);
            Tiled("Tierra", S("Tiles/ground_dirt.png"), new Vector3(s.x0 + w / 2f, s.top - 1f - (depth - 1f) / 2f, 0f), new Vector2(w, depth - 1f), 0, go.transform, null);
        }

        static void BuildWalls(Transform parent)
        {
            float[] xs = { -6.5f, LevelEnd + 0.5f };
            foreach (float x in xs)
            {
                var w = new GameObject("MuroInvisible");
                w.layer = layerGround;
                w.transform.SetParent(parent);
                w.transform.position = new Vector3(x, 10f, 0f);
                var c = w.AddComponent<BoxCollider2D>();
                c.size = new Vector2(1f, 60f);
            }
        }

        static void BuildPits(Transform parent)
        {
            // Fondo oscuro visible dentro de los huecos del suelo
            var s = Tiled("FondoDeFosas", S("Tiles/ground_dirt.png"), new Vector3(LevelEnd / 2f - 3f, -9f, 0f), new Vector2(LevelEnd + 40f, 14f), -55, parent, new Color(0.32f, 0.26f, 0.3f, 1f));
            s.gameObject.name = "FondoDeFosas";
        }

        static void BuildPlatforms(Transform parent)
        {
            float[,] pl = {
                { 60f, 2.0f, 4f }, { 67f, 3.3f, 3f },
                { 169.5f, 0.8f, 2.2f },
                { 216f, 2.3f, 6f },
                { 298f, 2.2f, 5f }, { 306f, 3.5f, 4f }, { 314f, 2.2f, 5f }, { 331.7f, 0.8f, 2f },
                { 492f, 4.3f, 3.5f }, { 499f, 5.4f, 3f },
                { 562f, 0.8f, 2f }, { 578f, 2.3f, 6f }, { 608f, 0.8f, 2f },
                { 690f, 2.3f, 5f }, { 698f, 3.6f, 3f }, { 717.7f, 0.8f, 2f }
            };
            int n = pl.GetLength(0);
            for (int i = 0; i < n; i++) BuildPlatform(pl[i, 0], pl[i, 1], pl[i, 2], parent);
        }

        static void BuildPlatform(float x, float top, float w, Transform parent)
        {
            const float h = 0.34375f;
            var go = new GameObject("Plataforma");
            go.layer = layerGround;
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, top - h / 2f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = S("Props/platform_mid.png");
            sr.sharedMaterial = litMat;
            sr.sortingOrder = 2;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(w, h);
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(w, h);
            bc.usedByEffector = true;
            var eff = go.AddComponent<PlatformEffector2D>();
            eff.useOneWay = true;
            eff.surfaceArc = 170f;
        }

        // ------------------------------------------------------------------ casas y decorado
        static void BuildHouses(Transform parent)
        {
            var names = new List<string>();
            for (int i = 0; i < 10; i++) names.Add("Buildings/casa_" + i.ToString("00") + ".png");
            names.Add("Buildings/pink_arches.png");
            names.Add("Buildings/tile_house.png");
            names.Add("Buildings/stone_house.png");

            // fila trasera (más pequeña y azulada)
            string prev = "";
            float x = -8f;
            while (x < 876f)
            {
                string n = names[rnd.Next(names.Count)];
                if (n == prev) continue;
                prev = n;
                var sp = S(n);
                float sc = 0.56f;
                float w = sp.bounds.size.x * sc;
                float cx = x + w / 2f;
                x += w + R(0.4f, 3.2f);
                if (cx > 820f && cx < 842f) continue;
                var sr = SR("CasaFondo", sp, new Vector3(cx, 0.35f, 0f), -48, parent, false, false);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                sr.color = new Color(0.78f, 0.8f, 0.95f, 1f);
            }

            // fila delantera
            prev = "";
            x = -4f;
            while (x < 876f)
            {
                string n = names[rnd.Next(names.Count)];
                if (n == prev) continue;
                prev = n;
                var sp = S(n);
                float sc = R(0.72f, 0.82f);
                float w = sp.bounds.size.x * sc;
                float cx = x + w / 2f;
                x += w + R(0.1f, 1.6f);
                if (cx + w / 2f > 822f && cx - w / 2f < 838f) { x = Mathf.Max(x, 838.5f); continue; }
                var sr = SR("Casa", sp, new Vector3(cx, -0.05f, 0f), -40, parent, false, false);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                if (rnd.NextDouble() < 0.75)
                {
                    var g = SR("LuzVentana", S("Pickups/glow.png"), new Vector3(cx + R(-w * 0.3f, w * 0.3f), R(0.85f, 1.2f), 0f), -39, parent, true, false);
                    g.color = new Color(1f, 0.8f, 0.4f, 0f);
                    g.transform.localScale = Vector3.one * R(0.9f, 1.3f);
                    glows.Add(g);
                }
            }

            // parroquia
            var ch = SR("Parroquia", S("Buildings/parroquia.png"), new Vector3(830f, -0.05f, 0f), -30, parent, false, false);
            ch.name = "Parroquia";
            ch.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            for (int i = 0; i < 4; i++)
            {
                var g = SR("LuzParroquia", S("Pickups/glow.png"), new Vector3(826.6f + i * 2.1f, 1.1f, 0f), -29, parent, true, false);
                g.color = new Color(1f, 0.8f, 0.4f, 0f);
                g.transform.localScale = Vector3.one * 1.2f;
                glows.Add(g);
            }
        }

        static void Prop(string rel, float x, int order, Transform parent, bool flip)
        {
            float t;
            if (!TryGroundTop(x, out t)) return;
            var sp = S(rel);
            var sr = SR(System.IO.Path.GetFileNameWithoutExtension(rel), sp, new Vector3(x, t - 0.02f, 0f), order, parent, false, false);
            sr.flipX = flip;
        }

        static void BuildProps(Transform parent)
        {
            // decorado repartido por todo el recorrido
            string[] low = { "Props/fence_1.png", "Props/fence_2.png", "Props/fence_3.png", "Props/grass_1.png", "Props/grass_2.png", "Props/rock_1.png", "Props/rock_2.png", "Props/rock_5.png", "Props/sign_1.png", "Props/sack.png", "Props/bag.png", "Props/stone.png" };
            for (float x = 8f; x < 870f; x += R(5f, 11f))
            {
                if (!OnGround(x, 1.5f)) continue;
                if (x > 819f && x < 842f) continue;
                string n = low[rnd.Next(low.Length)];
                Prop(n, x, rnd.NextDouble() < 0.4 ? 30 : -20, parent, rnd.NextDouble() < 0.5);
            }
            // primeros pasos (tutorial)
            Prop("Props/quest_board.png", 9f, -20, parent, false);
            Prop("Props/sign_2.png", 14f, -20, parent, false);
            Prop("Props/target_1.png", 24f, -20, parent, false);
            Prop("Props/target_2.png", 28f, -20, parent, false);
            // plaza del mercado
            for (float x = 270f; x < 420f; x += R(13f, 19f))
            {
                string stall = rnd.NextDouble() < 0.5 ? "Buildings/stall_open.png" : "Buildings/stall_counter.png";
                float t;
                if (!TryGroundTop(x, out t)) continue;
                SR("Puesto", S(stall), new Vector3(x, t - 0.05f, 0f), -25, parent, false, false);
                Prop("Props/table.png", x + 3.2f, -22, parent, false);
                Prop("Props/chair.png", x + 4.6f, -22, parent, true);
            }
            // barricadas
            string[] bar = { "Props/barrier_1.png", "Props/barrier_2.png" };
            float[] bx = { 440f, 456f, 520f, 543f, 590f, 622f };
            for (int i = 0; i < bx.Length; i++) Prop(bar[i % 2], bx[i], -18, parent, false);
            // árboles secos en el camino nocturno
            for (float x = 650f; x < 800f; x += R(9f, 15f))
                Prop(rnd.NextDouble() < 0.5 ? "Props/tree_1.png" : "Props/tree_2.png", x, -26, parent, rnd.NextDouble() < 0.5);
            // zona de la parroquia
            Prop("Props/shield_2.png", 806f, -20, parent, false);
            Prop("Props/fence_3.png", 812f, -20, parent, false);
        }

        static void BuildHazards(Transform parent)
        {
            float[,] sp = { { 214f, 4f }, { 575f, 6f }, { 645f, 4f }, { 758f, 4f } };
            for (int k = 0; k < sp.GetLength(0); k++)
            {
                float x0 = sp[k, 0];
                float w = sp[k, 1];
                var root = new GameObject("Pinchos");
                root.transform.SetParent(parent);
                root.transform.position = new Vector3(x0 + w / 2f, GroundTop(x0), 0f);
                for (int i = 0; i < (int)w; i++)
                {
                    SR("Pico", S("Props/spikes.png"), new Vector3(-w / 2f + 0.5f + i, -0.02f, 0f), 5, root.transform, false, true);
                }
                var c = root.AddComponent<BoxCollider2D>();
                c.isTrigger = true;
                c.size = new Vector2(w - 0.3f, 0.45f);
                c.offset = new Vector2(0f, 0.25f);
                root.AddComponent<Hazard>();
            }
        }

        static void BuildLamps(Transform parent)
        {
            for (float x = 96f; x < 830f; x += 24f)
            {
                if (!OnGround(x, 1f)) continue;
                float t = GroundTop(x);
                var root = new GameObject("Farol");
                root.transform.SetParent(parent);
                root.transform.position = new Vector3(x, t, 0f);
                SR("Poste", S("Props/lamp.png"), new Vector3(0f, -0.02f, 0f), -12, root.transform, false, true);
                var g = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 2.05f, 0f), -11, root.transform, true, true);
                g.color = new Color(1f, 0.74f, 0.35f, 0f);
                g.transform.localScale = Vector3.one * 3.2f;
                glows.Add(g);
                if (x > 540f)
                {
                    var lg = new GameObject("Luz");
                    lg.transform.SetParent(root.transform, false);
                    lg.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                    var l = lg.AddComponent<Light2D>();
                    l.lightType = Light2D.LightType.Point;
                    l.color = new Color(1f, 0.72f, 0.4f);
                    l.intensity = 0f;
                    l.pointLightOuterRadius = 6f;
                    l.pointLightInnerRadius = 0.6f;
                    lamps.Add(l);
                }
            }
        }

        static void BuildBreakables(Transform parent)
        {
            float[] xs = { 30f, 54f, 88f, 125f, 140f, 175f, 205f, 240f, 270f, 276f, 285f, 320f, 345f, 355f, 372f, 392f, 405f, 415f,
                           440f, 452f, 466f, 530f, 550f, 570f, 600f, 650f, 665f, 680f, 700f, 730f, 745f, 765f, 780f };
            foreach (float x in xs)
            {
                if (!OnGround(x, 0.8f)) continue;
                if ((x > 213f && x < 219f) || (x > 574f && x < 582f) || (x > 644f && x < 650f)) continue;
                bool barrel = rnd.NextDouble() < 0.55;
                var sp = S(barrel ? "Props/barrel.png" : "Props/crate.png");
                float t = GroundTop(x);
                var sr = SR(barrel ? "Barril" : "Caja", sp, new Vector3(x, t - 0.02f, 0f), -5, parent, false, false);
                var bc = sr.gameObject.AddComponent<BoxCollider2D>();
                Vector2 sz = sp.bounds.size;
                bc.size = new Vector2(sz.x * 0.85f, sz.y * 0.95f);
                bc.offset = new Vector2(0f, sz.y / 2f);
                sr.gameObject.layer = layerEnemy;
                var br = sr.gameObject.AddComponent<Breakable>();
                br.hp = barrel ? 2 : 1;
                br.coins = barrel ? 3 : 2;
                br.appleChance = 0.12f;
            }
        }

        // ------------------------------------------------------------------ personajes
        static PlatformerPlayer BuildPlayer()
        {
            var root = new GameObject("Jugador");
            root.layer = layerPlayer;
            root.transform.position = new Vector3(3f, 0.05f, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.gravityScale = 3.6f;
            var bc = root.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(0.55f, 1.35f);
            bc.offset = new Vector2(0f, 0.675f);
            var pl = root.AddComponent<PlatformerPlayer>();

            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
            sr.sharedMaterial = litMat;
            var an = vis.AddComponent<FrameAnimator>();
            an.target = sr;
            var shadow = SR("Sombra", S("Fx/shadow.png"), new Vector3(0f, 0.03f, 0f), 19, root.transform, true, true);
            shadow.transform.localScale = new Vector3(0.9f, 1f, 1f);

            pl.visual = sr;
            pl.animator = an;
            pl.idle = Clip("idle", Seq("Player", "Idle", 4), 6f, true);
            pl.run = Clip("run", Seq("Player", "Run", 6), 12f, true);
            pl.jump = Clip("jump", Seq("Player", "Jump", 1), 1f, true);
            pl.fall = Clip("fall", Seq("Player", "Fall", 1), 1f, true);
            pl.attack = Clip("attack", Seq("Player", "Attack", 8), 17f, false);
            pl.hit = Clip("hit", Seq("Player", "Hit", 4), 10f, false);
            pl.death = Clip("death", Seq("Player", "Death", 8), 9f, false);
            return pl;
        }

        struct KnightDef
        {
            public string folder; public int hp; public float patrol, chase, aggro, range, cooldown, stagger;
            public int damage, score, coins; public Vector2 boxSize, boxOffset; public float colW, colH; public float apple; public bool heart; public float barY;
        }

        static KnightDef Def(int type)
        {
            var d = new KnightDef();
            if (type == 1)
            {
                d.folder = "Knight1"; d.hp = 2; d.patrol = 1.6f; d.chase = 3.2f; d.aggro = 9f; d.range = 2.5f; d.cooldown = 1.6f; d.stagger = 1f;
                d.damage = 1; d.score = 50; d.coins = 2; d.boxSize = new Vector2(2.7f, 1.3f); d.boxOffset = new Vector2(1.7f, 0.9f);
                d.colW = 0.9f; d.colH = 1.75f; d.apple = 0.1f; d.heart = false; d.barY = 2.15f;
            }
            else if (type == 2)
            {
                d.folder = "Knight2"; d.hp = 4; d.patrol = 1.2f; d.chase = 2.7f; d.aggro = 8f; d.range = 2.3f; d.cooldown = 2.0f; d.stagger = 0.5f;
                d.damage = 1; d.score = 120; d.coins = 4; d.boxSize = new Vector2(2.4f, 1.5f); d.boxOffset = new Vector2(1.5f, 0.95f);
                d.colW = 1.1f; d.colH = 2.0f; d.apple = 0.3f; d.heart = false; d.barY = 2.5f;
            }
            else
            {
                d.folder = "Knight3"; d.hp = 9; d.patrol = 1.8f; d.chase = 3.7f; d.aggro = 13f; d.range = 2.2f; d.cooldown = 1.25f; d.stagger = 0.2f;
                d.damage = 2; d.score = 500; d.coins = 12; d.boxSize = new Vector2(2.2f, 1.5f); d.boxOffset = new Vector2(1.4f, 0.95f);
                d.colW = 1.0f; d.colH = 1.85f; d.apple = 0f; d.heart = true; d.barY = 2.4f;
            }
            return d;
        }

        static void BuildKnight(int type, float x, float radius, Transform parent)
        {
            float t;
            if (!TryGroundTop(x, out t)) { Debug.LogWarning("[1810] Enemigo fuera del suelo en x=" + x); return; }
            var d = Def(type);
            var root = new GameObject("Soldado" + type + "_" + (int)x);
            root.layer = layerEnemy;
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(x, t + 0.05f, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 5f;
            var bc = root.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(d.colW, d.colH);
            bc.offset = new Vector2(0f, d.colH / 2f);
            var e = root.AddComponent<EnemyKnight>();
            e.maxHp = d.hp; e.patrolSpeed = d.patrol; e.chaseSpeed = d.chase; e.aggroRange = d.aggro; e.patrolRadius = radius;
            e.attackStartRange = d.range; e.damage = d.damage; e.attackCooldown = d.cooldown; e.attackBoxSize = d.boxSize; e.attackBoxOffset = d.boxOffset;
            e.staggerChance = d.stagger; e.scoreValue = d.score; e.coinDrops = d.coins; e.appleChance = d.apple; e.dropsHeart = d.heart;
            e.hitStartFrame = 5; e.hitEndFrame = 7;

            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.sharedMaterial = litMat;
            var an = vis.AddComponent<FrameAnimator>();
            an.target = sr;
            var shadow = SR("Sombra", S("Fx/shadow.png"), new Vector3(0f, 0.03f, 0f), 9, root.transform, true, true);
            shadow.transform.localScale = new Vector3(1.5f, 1f, 1f);

            string f = "Enemies/" + d.folder;
            e.visual = sr; e.animator = an;
            e.idle = Clip("idle", Seq(f, "Idle", 10), 8f, true);
            e.walk = Clip("walk", Seq(f, "Walk", 10), 10f, true);
            e.run = Clip("run", Seq(f, "Run", 10), 14f, true);
            e.attack = Clip("attack", Seq(f, "Attack", 10), 12f, false);
            e.hurt = Clip("hurt", Seq(f, "Hurt", 10), 16f, false);
            e.die = Clip("die", Seq(f, "Die", 10), 12f, false);

            // barra de vida
            var bar = new GameObject("BarraVida");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(-0.6f, d.barY, 0f);
            var back = SR("Fondo", S("Fx/pixel.png"), new Vector3(-0.03f, 0f, 0f), 40, bar.transform, true, true);
            back.transform.localScale = new Vector3(1.26f, 0.2f, 1f);
            back.color = new Color(0.08f, 0.03f, 0.03f, 0.9f);
            var fill = SR("Vida", S("Fx/pixel.png"), Vector3.zero, 41, bar.transform, true, true);
            fill.transform.localScale = new Vector3(1.2f, 0.14f, 1f);
            fill.color = new Color(0.35f, 0.9f, 0.2f, 1f);
            e.hpBar = bar.transform; e.hpFill = fill; e.hpBarWidth = 1.2f;
        }

        static void BuildEnemies(Transform parent)
        {
            // tipo, x, radio de patrulla
            float[,] list = {
                // Plaza del Pueblo
                { 1, 46, 3 }, { 1, 78, 3 }, { 1, 100, 4 },
                // Calle Real
                { 1, 130, 3 }, { 1, 150, 3 }, { 1, 182, 3 }, { 1, 196, 2 }, { 2, 232, 3 }, { 1, 248, 3 },
                // El Mercado
                { 1, 280, 3 }, { 2, 312, 4 }, { 1, 345, 4 }, { 1, 365, 3 }, { 2, 378, 4 }, { 1, 398, 3 }, { 1, 410, 3 },
                // Las Barricadas
                { 1, 445, 3 }, { 2, 462, 3 }, { 1, 495, 2 }, { 1, 530, 3 }, { 2, 545, 3 }, { 1, 575, 3 }, { 1, 590, 3 }, { 2, 620, 4 },
                // Camino a la Parroquia
                { 1, 660, 3 }, { 2, 672, 3 }, { 1, 712, 3 }, { 2, 735, 3 }, { 1, 752, 3 }, { 2, 770, 3 },
                // Oficial realista
                { 3, 796, 4 }
            };
            int n = list.GetLength(0);
            for (int i = 0; i < n; i++) BuildKnight((int)list[i, 0], list[i, 1], list[i, 2], parent);
        }

        // ------------------------------------------------------------------ objetos
        static void Pick(Pickup.Kind kind, float x, float y, Transform parent)
        {
            string sprite;
            switch (kind)
            {
                case Pickup.Kind.Apple: sprite = "Pickups/apple.png"; break;
                case Pickup.Kind.Heart: sprite = "Pickups/heart.png"; break;
                case Pickup.Kind.Diamond: sprite = "Pickups/diamond.png"; break;
                case Pickup.Kind.Star: sprite = "Pickups/star.png"; break;
                case Pickup.Kind.Chest: sprite = "Pickups/chest_closed.png"; break;
                default: sprite = "Pickups/coin_0.png"; break;
            }
            var sr = SR(kind.ToString(), S(sprite), new Vector3(x, y, 0f), 15, parent, false, false);
            var c = sr.gameObject.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = kind == Pickup.Kind.Chest ? 0.9f : 0.45f;
            if (kind == Pickup.Kind.Chest) c.offset = new Vector2(0f, 0.5f);
            var p = sr.gameObject.AddComponent<Pickup>();
            p.kind = kind;
            if (kind == Pickup.Kind.Coin)
            {
                p.coinSprites = new Sprite[6];
                for (int i = 0; i < 6; i++) p.coinSprites[i] = S("Pickups/coin_" + i + ".png");
            }
            else if (kind == Pickup.Kind.Chest)
            {
                p.openSprite = S("Pickups/chest_open.png");
                p.bonusCoins = 12;
            }
            p.staticSprite = sr.sprite;
        }

        static void CoinLine(float x0, float x1, float y, int n, Transform parent)
        {
            for (int i = 0; i < n; i++)
            {
                float x = n == 1 ? x0 : Mathf.Lerp(x0, x1, i / (float)(n - 1));
                Pick(Pickup.Kind.Coin, x, y, parent);
            }
        }

        static void CoinArc(float xc, float y0, float w, float h, int n, Transform parent)
        {
            for (int i = 0; i < n; i++)
            {
                float u = n == 1 ? 0.5f : i / (float)(n - 1);
                float x = xc + (u - 0.5f) * w;
                float y = y0 + Mathf.Sin(u * Mathf.PI) * h;
                Pick(Pickup.Kind.Coin, x, y, parent);
            }
        }

        static void BuildPickups(Transform parent)
        {
            // monedas sueltas por el camino (grupos cada cierto tramo)
            float x = 14f;
            int toggle = 0;
            while (x < 860f)
            {
                float t;
                if (OnGround(x, 3f) && TryGroundTop(x, out t))
                {
                    bool nearSpike = (x > 208f && x < 224f) || (x > 570f && x < 586f) || (x > 640f && x < 652f) || (x > 752f && x < 764f);
                    if (!nearSpike)
                    {
                        if (toggle % 2 == 0) CoinLine(x - 2f, x + 2f, t + 1.0f, 5, parent);
                        else CoinArc(x, t + 0.8f, 5f, 2.0f, 6, parent);
                    }
                    toggle++;
                }
                x += R(24f, 34f);
            }
            // sobre los huecos del suelo (ayudan a calcular el salto)
            float[] pits = { 169.5f, 331.75f, 561.75f, 607.75f, 717.75f };
            foreach (float px in pits) CoinArc(px, 1.2f, 4.2f, 1.6f, 5, parent);
            // sobre plataformas
            CoinLine(58.5f, 61.5f, 3.2f, 4, parent);
            CoinLine(66f, 68f, 4.5f, 3, parent);
            CoinLine(213.5f, 218.5f, 3.4f, 5, parent);
            CoinLine(296f, 300f, 3.3f, 4, parent);
            CoinLine(312f, 316f, 3.3f, 4, parent);
            CoinLine(575.5f, 580.5f, 3.4f, 5, parent);
            CoinLine(688f, 692f, 3.4f, 4, parent);
            CoinLine(491f, 493f, 5.4f, 3, parent);
            // manzanas (curan)
            float[] apples = { 120f, 236f, 300f, 440f, 548f, 625f, 700f, 790f };
            foreach (float ax in apples)
            {
                float t;
                if (TryGroundTop(ax, out t)) Pick(Pickup.Kind.Apple, ax, t + 0.8f, parent);
            }
            // corazones (vida extra) escondidos arriba
            Pick(Pickup.Kind.Heart, 306f, 4.6f, parent);
            Pick(Pickup.Kind.Heart, 698f, 4.7f, parent);
            // diamantes y estrellas
            Pick(Pickup.Kind.Diamond, 216f, 3.4f, parent);
            Pick(Pickup.Kind.Diamond, 314f, 3.4f, parent);
            Pick(Pickup.Kind.Diamond, 578f, 3.4f, parent);
            Pick(Pickup.Kind.Diamond, 690f, 3.4f, parent);
            Pick(Pickup.Kind.Star, 499f, 6.5f, parent);
            // cofre del tesoro sobre la escalera de las barricadas
            Pick(Pickup.Kind.Chest, 505f, 2.0f, parent);
        }

        static void BuildCheckpoints(Transform parent)
        {
            float[] xs = { 255f, 420f, 632f };
            for (int i = 0; i < xs.Length; i++)
            {
                float t = GroundTop(xs[i]);
                var root = new GameObject("Banderines_" + (i + 1));
                root.transform.SetParent(parent);
                root.transform.position = new Vector3(xs[i], t, 0f);
                var flag = SR("Banderines", S("Props/flag.png"), new Vector3(0f, -0.02f, 0f), -8, root.transform, false, true);
                var glow = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 1.3f, 0f), -7, root.transform, true, true);
                glow.color = new Color(1f, 0.9f, 0.5f, 0f);
                glow.transform.localScale = Vector3.one * 3.4f;
                var spawn = new GameObject("Reaparicion");
                spawn.transform.SetParent(root.transform, false);
                spawn.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                var bc = root.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(3.2f, 4f);
                bc.offset = new Vector2(0f, 2f);
                var cp = root.AddComponent<Checkpoint>();
                cp.index = i; cp.flag = flag; cp.glow = glow; cp.spawn = spawn.transform;
            }
        }

        static void BuildZones(Transform parent)
        {
            string[,] z = {
                { "3", "PLAZA DEL PUEBLO", "Dolores, Guanajuato · Septiembre de 1810" },
                { "112", "CALLE REAL", "Los soldados vigilan cada esquina" },
                { "262", "EL MERCADO", "Entre puestos y rumores, la noche se acerca" },
                { "432", "LAS BARRICADAS", "El camino hacia la parroquia está bloqueado" },
                { "642", "CAMINO A LA PARROQUIA", "Cae la noche sobre Dolores" },
                { "800", "LA PARROQUIA DE DOLORES", "Toca la campana" }
            };
            for (int i = 0; i < z.GetLength(0); i++)
            {
                float x = float.Parse(z[i, 0], CI);
                var go = new GameObject("Zona_" + z[i, 1]);
                go.transform.SetParent(parent);
                go.transform.position = new Vector3(x, 4f, 0f);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(2f, 16f);
                var zt = go.AddComponent<ZoneTrigger>();
                zt.title = z[i, 1];
                zt.subtitle = z[i, 2];
            }
        }

        static void BuildGoal(Transform parent)
        {
            float x = 856f;
            float t = GroundTop(x);
            var root = new GameObject("CampanaDeDolores");
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(x, t, 0f);
            SR("Estructura", S("Props/bell_frame.png"), new Vector3(0f, -0.02f, 0f), -10, root.transform, false, true);
            var bell = SR("Campana", S("Props/bell.png"), new Vector3(0f, 2.85f, 0f), -9, root.transform, false, true);
            var glow = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 1.8f, 0f), -11, root.transform, true, true);
            glow.color = new Color(1f, 0.85f, 0.45f, 0.5f);
            glow.transform.localScale = Vector3.one * 4.5f;
            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(3.4f, 5f);
            bc.offset = new Vector2(0f, 2.5f);
            var goal = root.AddComponent<LevelGoal>();
            goal.bell = bell.transform;
        }

        // ------------------------------------------------------------------ fondo
        static DayNightCycle BuildBackground(Transform parent, Camera cam, Light2D globalLight, Transform player)
        {
            var dn = parent.gameObject.AddComponent<DayNightCycle>();
            var sky = new GameObject("Cielo");
            sky.transform.SetParent(parent);
            var skyNight = SR("Noche", S("Background/sky_night.png"), Vector3.zero, -100, sky.transform, true, true);
            var skyDusk = SR("Atardecer", S("Background/sky_dusk.png"), Vector3.zero, -99, sky.transform, true, true);
            var skyDay = SR("Dia", S("Background/sky_day.png"), Vector3.zero, -98, sky.transform, true, true);
            var sf = sky.AddComponent<SkyFollow>();
            sf.cam = cam; sf.reference = skyDay;

            var starsRoot = new GameObject("Estrellas");
            starsRoot.transform.SetParent(parent);
            var stars = SR("Estrellas", S("Background/stars.png"), Vector3.zero, -97, starsRoot.transform, true, true);
            stars.color = new Color(1f, 1f, 1f, 0f);
            var sf2 = starsRoot.AddComponent<SkyFollow>();
            sf2.cam = cam; sf2.reference = stars;

            var sun = SR("Sol", S("Background/sun.png"), new Vector3(8f, 6f, 0f), -96, parent, true, false);
            sun.transform.localScale = Vector3.one * 1.5f;
            var moon = SR("Luna", S("Background/moon.png"), new Vector3(-6f, 8f, 0f), -95, parent, true, false);
            moon.transform.localScale = Vector3.one * 1.5f;
            moon.color = new Color(1f, 1f, 1f, 0f);

            var tinted = new List<SpriteRenderer>();
            // nubes (tres grupos con distinto parallax)
            for (int g = 0; g < 3; g++)
            {
                var root = new GameObject("Nubes_" + g);
                root.transform.SetParent(parent);
                float y0 = 5.2f + g * 1.1f;
                root.transform.position = new Vector3(3f, y0, 0f);
                float tile = 48f;
                for (int j = 0; j < 4; j++)
                {
                    var sp = S("Background/cloud_" + ((j + g) % 4) + ".png");
                    float cx = (j + 0.5f) * (tile / 4f) + R(-2f, 2f) - tile / 2f;
                    float cy = R(-1f, 1.2f);
                    float s = R(0.8f, 1.4f) * (1f - g * 0.15f);
                    for (int copy = -1; copy <= 1; copy++)
                    {
                        var sr = SR("Nube", sp, new Vector3(copy * tile + cx, cy, 0f), -90 + g, root.transform, true, true);
                        sr.transform.localScale = new Vector3(s, s, 1f);
                        sr.color = new Color(1f, 1f, 1f, 0.92f - g * 0.12f);
                        tinted.Add(sr);
                    }
                }
                var pl = root.AddComponent<ParallaxLayer>();
                pl.speedX = 0.06f + g * 0.05f; pl.speedY = 0.5f; pl.tileWidth = tile; pl.drift = 0.15f + g * 0.1f;
            }

            Color white = Color.white;
            tinted.AddRange(LayerSprites(Layer("MontanasLejanas", S("Background/mountains_far.png"), 0.10f, 0.45f, -1.2f, -80, white, parent, 0f)));
            tinted.AddRange(LayerSprites(Layer("MontanasCercanas", S("Background/mountains_near.png"), 0.20f, 0.5f, -1.2f, -75, white, parent, 0f)));
            tinted.AddRange(LayerSprites(Layer("Lomas", S("Background/hills_green.png"), 0.34f, 0.55f, -1.2f, -70, white, parent, 0f)));
            tinted.AddRange(LayerSprites(Layer("PuebloLejano", S("Background/town_far.png"), 0.50f, 0.6f, -1.2f, -65, white, parent, 0f)));
            var lightsLayer = Layer("PuebloLuces", S("Background/town_lights.png"), 0.50f, 0.6f, -1.2f, -64, new Color(1f, 1f, 1f, 0f), parent, 0f);
            var lightRenderers = LayerSprites(lightsLayer);

            dn.player = player;
            dn.cam = cam;
            dn.startX = 0f;
            dn.endX = 840f;
            dn.globalLight = globalLight;
            dn.skyDay = skyDay; dn.skyDusk = skyDusk; dn.skyNight = skyNight;
            dn.stars = stars; dn.sun = sun; dn.moon = moon;
            dn.townLights = lightRenderers.ToArray();
            dn.tinted = tinted.ToArray();
            dn.glows = glows.ToArray();
            dn.lamps = lamps.ToArray();
            return dn;
        }

        static List<SpriteRenderer> LayerSprites(GameObject layer)
        {
            var list = new List<SpriteRenderer>();
            foreach (var sr in layer.GetComponentsInChildren<SpriteRenderer>()) list.Add(sr);
            return list;
        }

        static GameObject Layer(string name, Sprite sp, float speedX, float speedY, float y, int order, Color col, Transform parent)
        {
            return Layer(name, sp, speedX, speedY, y, order, col, parent, 0f);
        }

        // ================================================================== build settings
        static void SetBuildSettings()
        {
            string menu = ScenesDir + "/MainMenu.unity";
            string level = ScenesDir + "/Level1.unity";
            var list = new List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene(menu, true));
            list.Add(new EditorBuildSettingsScene(level, true));
            foreach (var s in EditorBuildSettings.scenes)
            {
                if (s.path != menu && s.path != level && File.Exists(s.path)) list.Add(s);
            }
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
