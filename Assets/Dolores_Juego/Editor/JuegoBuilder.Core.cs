#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dolores1810.EditorTools
{
    /// <summary>
    /// Construye desde cero MainMenu, Level1, Level2 y Level3 y crea los datos del juego (armas, tienda, enemigos).
    /// Menú: Tools > Dolores 1810 > Construir juego completo (menú + 3 niveles).
    /// Es seguro ejecutarlo varias veces. Antes de sobrescribir guarda copias de tus escenas anteriores.
    /// </summary>
    public static partial class JuegoBuilder
    {
        const string Root = "Assets/Dolores_Juego";
        const string ArtDir = Root + "/Art/";
        const string AudioDir = Root + "/Audio/";
        const string ScenesDir = "Assets/Scenes";

        static Material litMat, unlitMat;
        static int layerGround, layerPlayer, layerEnemy;
        static System.Random rnd;
        static readonly CultureInfo CI = CultureInfo.InvariantCulture;
        static List<SpriteRenderer> glows;
        static List<Light2D> lamps;

        [MenuItem("Tools/Dolores 1810/Construir juego completo (menu + 3 niveles)")]
        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            try
            {
                rnd = new System.Random(1810);
                EditorUtility.DisplayProgressBar("1810", "Importando recursos...", 0.03f);
                AssetDatabase.Refresh();
                BackupScenes();
                EnsureLayers();
                EditorUtility.DisplayProgressBar("1810", "Configurando sprites...", 0.1f);
                ConfigureSprites();
                EnsureMaterials();
                EditorUtility.DisplayProgressBar("1810", "Creando datos del juego...", 0.3f);
                CreateDataAssets();
                if (!Directory.Exists(Path.Combine(Application.dataPath, "Scenes"))) Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
                EditorUtility.DisplayProgressBar("1810", "Construyendo el menú principal...", 0.4f);
                BuildMenuScene();
                for (int i = 1; i <= 3; i++)
                {
                    EditorUtility.DisplayProgressBar("1810", "Construyendo el nivel " + i + "...", 0.45f + i * 0.15f);
                    BuildLevel(i);
                }
                SetBuildSettings();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                string report = ValidateRules();
                EditorSceneManager.OpenScene(ScenesDir + "/MainMenu.unity");
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Listo",
                    "Se crearon MainMenu, Level1, Level2 y Level3 y se agregaron al Build Settings.\n\n" + report +
                    "\nAbre MainMenu y presiona Play.", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[1810] Error al construir: " + e);
                EditorUtility.DisplayDialog("Error al construir", e.Message + "\n\nRevisa la consola para más detalles.", "OK");
            }
        }

        [MenuItem("Tools/Dolores 1810/Validar reglas del juego")]
        public static void ValidateMenu()
        {
            string r = ValidateRules();
            Debug.Log("[1810] " + r);
            EditorUtility.DisplayDialog("Validación", r, "OK");
        }

        // ================================================================== respaldo y capas
        static void BackupScenes()
        {
            string dir = ScenesDir + "/Respaldo_Anterior";
            string[] names = { "MainMenu", "Level1", "Level2", "Level3" };
            foreach (string n in names)
            {
                string src = ScenesDir + "/" + n + ".unity";
                if (!File.Exists(src)) continue;
                string dst = dir + "/" + n + "_anterior.unity";
                if (File.Exists(dst)) continue;
                if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder(ScenesDir, "Respaldo_Anterior");
                AssetDatabase.CopyAsset(src, dst);
            }
        }

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

        // ================================================================== sprites y materiales
        static void ConfigureSprites()
        {
            string manifest = Path.Combine(Application.dataPath, "Dolores_Juego/Art/art_manifest.txt");
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
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Dolores_Juego/Materials"));
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

        // ================================================================== utilidades
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

        static float R(float a, float b) { return a + (float)rnd.NextDouble() * (b - a); }

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

        static Transform Group(string name) { return new GameObject(name).transform; }

        static Light2D AddGlobalLight(Color color, float intensity)
        {
            var gl = new GameObject("Global Light 2D");
            var l = gl.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.color = color;
            l.intensity = intensity;
            return l;
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

        static List<SpriteRenderer> LayerSprites(GameObject layer)
        {
            var list = new List<SpriteRenderer>();
            foreach (var sr in layer.GetComponentsInChildren<SpriteRenderer>()) list.Add(sr);
            return list;
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
            add(SfxId.Jump, "sfx_jump.wav", 0.7f); add(SfxId.Land, "sfx_land.wav", 0.8f); add(SfxId.Swing, "sfx_swing.wav", 0.8f);
            add(SfxId.HitEnemy, "sfx_hit_enemy.wav", 0.9f); add(SfxId.PlayerHurt, "sfx_player_hurt.wav", 1f); add(SfxId.EnemyDie, "sfx_enemy_die.wav", 0.9f);
            add(SfxId.PlayerDie, "sfx_player_die.wav", 1f); add(SfxId.Coin, "sfx_coin.wav", 0.7f); add(SfxId.Life, "sfx_life.wav", 0.9f);
            add(SfxId.Heal, "sfx_heal.wav", 0.9f); add(SfxId.Gem, "sfx_gem.wav", 0.9f); add(SfxId.Checkpoint, "sfx_checkpoint.wav", 0.9f);
            add(SfxId.Bell, "sfx_bell.wav", 1f); add(SfxId.Break, "sfx_break.wav", 0.9f); add(SfxId.Spike, "sfx_spike.wav", 0.8f);
            add(SfxId.Click, "sfx_click.wav", 0.8f); add(SfxId.Hover, "sfx_hover.wav", 0.6f); add(SfxId.Pause, "sfx_pause.wav", 0.8f);
            add(SfxId.Step, "sfx_step.wav", 0.6f); add(SfxId.Alert, "sfx_alert.wav", 0.7f); add(SfxId.EnemySwing, "sfx_enemy_swing.wav", 0.7f);
            add(SfxId.Victory, "sfx_victory.wav", 1f); add(SfxId.GameOver, "sfx_gameover.wav", 1f);
            add(SfxId.ChestOpen, "sfx_chest_open.wav", 0.9f); add(SfxId.Interact, "sfx_interact.wav", 0.7f); add(SfxId.Buy, "sfx_buy.wav", 0.9f);
            add(SfxId.Error, "sfx_error.wav", 0.8f); add(SfxId.WeaponSwitch, "sfx_weapon_switch.wav", 0.8f); add(SfxId.Climb, "sfx_climb.wav", 0.7f);
            add(SfxId.Secret, "sfx_secret.wav", 0.9f); add(SfxId.Whoosh, "sfx_whoosh.wav", 0.8f); add(SfxId.BossAlert, "sfx_boss_alert.wav", 1f);
            add(SfxId.Shockwave, "sfx_shockwave.wav", 1f); add(SfxId.Firework, "sfx_firework.wav", 0.8f); add(SfxId.Splash, "sfx_splash.wav", 0.9f);
            add(SfxId.ShopOpen, "sfx_shop_open.wav", 0.8f); add(SfxId.LevelComplete, "sfx_level_complete.wav", 1f); add(SfxId.Breath, "sfx_breath.wav", 0.8f);
            add(SfxId.Lever, "sfx_lever.wav", 0.9f);
            return map.ToArray();
        }

        static void SetBuildSettings()
        {
            string[] names = { "MainMenu", "Level1", "Level2", "Level3" };
            var list = new List<EditorBuildSettingsScene>();
            var used = new HashSet<string>();
            foreach (string n in names)
            {
                string p = ScenesDir + "/" + n + ".unity";
                list.Add(new EditorBuildSettingsScene(p, true));
                used.Add(p);
            }
            foreach (var s in EditorBuildSettings.scenes)
                if (!used.Contains(s.path) && File.Exists(s.path) && !s.path.Contains("Respaldo_Anterior")) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
#endif
