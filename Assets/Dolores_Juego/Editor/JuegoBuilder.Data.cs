#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Dolores1810.EditorTools
{
    public static partial class JuegoBuilder
    {
        static GameDatabase database;
        static readonly Dictionary<string, EnemyConfig> enemyAssets = new Dictionary<string, EnemyConfig>();

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        static T Asset<T>(string path) where T : ScriptableObject
        {
            var a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            EditorUtility.SetDirty(a);
            return a;
        }

        // ================================================================== datos
        static void CreateDataAssets()
        {
            EnsureFolder(Root + "/Resources");
            EnsureFolder(Root + "/Data");
            EnsureFolder(Root + "/Data/Enemies");
            EnsureFolder(Root + "/Data/Weapons");
            EnsureFolder(Root + "/Data/Shop");

            // ---- armas (la última comprable hace exactamente el doble que la más fuerte anterior)
            var weapons = new List<WeaponData>();
            weapons.Add(MakeWeapon("hacha", "Hacha de leñador", "El arma con la que empiezas. Golpe pesado y firme.", 10, 1.0f, 1.0f, 0, 3, 5, "hacha"));
            weapons.Add(MakeWeapon("espada", "Espada de caballería", "Hoja de acero más larga y rápida. Alcanza un poco más lejos.", 15, 1.1f, 1.15f, 120, 3, 5, "espada"));
            weapons.Add(MakeWeapon("machete", "Machete insurgente", "Corta con gran velocidad. Ideal para pelear sin parar.", 20, 1.35f, 1.0f, 250, 3, 5, "machete"));
            weapons.Add(MakeWeapon("lanza", "Lanza de caballería", "Largo alcance: golpea a los soldados antes de que te alcancen.", 25, 0.9f, 1.55f, 450, 3, 6, "lanza"));
            weapons.Add(MakeWeapon("estoque", "Estoque de honor", "El arma final. Hace exactamente el doble de daño que la lanza.", 50, 1.0f, 1.3f, 800, 3, 5, "estoque"));

            // ---- tienda
            var items = new List<ShopItemData>();
            for (int i = 1; i < weapons.Count; i++)
            {
                var w = weapons[i];
                items.Add(MakeItem("arma_" + w.id, ShopKind.Weapon, w.displayName, w.description, w.icon, new int[] { w.price }, w.id));
            }
            items.Add(MakeItem("mejora_corazon", ShopKind.Hearts, "Corazón adicional", "Aumenta tu vida máxima en 1 corazón. Solo se puede comprar 2 veces.", S("Pickups/heart.png"), new int[] { 150, 300 }, ""));
            items.Add(MakeItem("mejora_res_cap", ShopKind.StaminaCapacity, "Resistencia máxima", "Aumenta 25% tu barra de resistencia: corres más tiempo.", S("UI/icon_resistencia.png"), new int[] { 60, 120, 200 }, ""));
            items.Add(MakeItem("mejora_res_rec", ShopKind.StaminaRecovery, "Recuperación de aliento", "Recuperas la resistencia 30% más rápido.", S("UI/icon_resistencia.png"), new int[] { 80, 150, 250 }, ""));
            items.Add(MakeItem("mejora_vel", ShopKind.Speed, "Botas veloces", "Caminas y corres 6% más rápido por nivel.", S("UI/icon_velocidad.png"), new int[] { 100, 200, 320 }, ""));
            items.Add(MakeItem("mejora_atq", ShopKind.Attack, "Fuerza de ataque", "Todas tus armas hacen 15% más de daño por nivel.", S("UI/icon_ataque.png"), new int[] { 120, 240, 400 }, ""));

            // ---- enemigos (la resistencia fija golpes y daño: 2/1, 4/2, 6/3, 8/4)
            enemyAssets.Clear();
            var enemies = new List<EnemyConfig>();
            enemies.Add(MakeEnemy("soldado", "Soldado realista", EnemyTier.Debil, 1, Color.white, 1f, 1.6f, 3.2f, 9f, 15f, 2.5f, 1.6f, 1f,
                new Vector2(2.7f, 1.3f), new Vector2(1.7f, 0.9f), 0.9f, 1.75f, 3, 50, 0.10f, false, 0));
            enemies.Add(MakeEnemy("granadero", "Granadero", EnemyTier.Intermedio, 2, Color.white, 1f, 1.2f, 2.8f, 8.5f, 14f, 2.3f, 2.0f, 0.5f,
                new Vector2(2.4f, 1.5f), new Vector2(1.5f, 0.95f), 1.1f, 2.0f, 6, 120, 0.25f, false, 0));
            enemies.Add(MakeEnemy("sargento", "Sargento", EnemyTier.Resistente, 2, new Color(0.78f, 0.82f, 1f, 1f), 1.12f, 1.3f, 3.0f, 9f, 15f, 2.5f, 1.8f, 0.35f,
                new Vector2(2.4f, 1.5f), new Vector2(1.5f, 0.95f), 1.1f, 2.0f, 10, 220, 0.25f, false, 0));
            enemies.Add(MakeEnemy("oficial", "Oficial realista", EnemyTier.MuyResistente, 3, Color.white, 1.1f, 1.8f, 3.6f, 11f, 17f, 2.2f, 1.4f, 0.2f,
                new Vector2(2.2f, 1.5f), new Vector2(1.4f, 0.95f), 1.0f, 1.85f, 16, 400, 0.2f, false, 0));
            enemies.Add(MakeEnemy("capitan", "Capitán realista", EnemyTier.MuyResistente, 2, new Color(1f, 0.85f, 0.7f, 1f), 1.45f, 1.5f, 3.0f, 12f, 20f, 2.6f, 1.8f, 0f,
                new Vector2(2.6f, 1.6f), new Vector2(1.6f, 1.0f), 1.1f, 2.0f, 40, 800, 0f, true, 14));
            enemies.Add(MakeEnemy("coronel", "Coronel realista", EnemyTier.MuyResistente, 3, new Color(1f, 0.8f, 0.8f, 1f), 1.5f, 1.6f, 3.4f, 14f, 22f, 2.6f, 1.6f, 0f,
                new Vector2(2.6f, 1.6f), new Vector2(1.5f, 1.0f), 1.0f, 1.85f, 60, 1500, 0f, true, 22));

            // ---- niveles
            var levels = new List<LevelInfo>();
            levels.Add(MakeLevel(1, "Nivel 1: El Pueblo de Dolores", "Septiembre de 1810. Cruza el pueblo de día y de noche, aprende a pelear y busca tesoros escondidos.", "Un pueblo tranquilo que pronto cambiará la historia."));
            levels.Add(MakeLevel(2, "Nivel 2: El Camino de la Insurgencia", "Un camino nocturno entre ríos, bosques y haciendas. Al amanecer te espera un capitán realista.", "La noche cubre el camino. Hay que seguir adelante."));
            levels.Add(MakeLevel(3, "Nivel 3: El Grito de Independencia", "La gran plaza de Dolores. Derrota al coronel y toca la campana para dar el Grito.", "La plaza espera. Ha llegado el momento."));

            database = Asset<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            database.weapons = weapons.ToArray();
            database.shopItems = items.ToArray();
            database.enemies = enemies.ToArray();
            database.levels = levels.ToArray();
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
        }

        static WeaponData MakeWeapon(string id, string name, string desc, int dmg, float speed, float reach, int price, int hs, int he, string icon)
        {
            var w = Asset<WeaponData>(Root + "/Data/Weapons/" + id + ".asset");
            w.id = id; w.displayName = name; w.description = desc; w.damage = dmg; w.attackSpeed = speed; w.reach = reach; w.price = price;
            w.hitStartFrame = hs; w.hitEndFrame = he;
            w.icon = S("UI/icon_" + icon + ".png");
            w.attackClip = Clip("ataque_" + id, Seq("Player", "Attack_" + id, 8), 17f, false);
            w.swingSfx = SfxId.Swing;
            return w;
        }

        static ShopItemData MakeItem(string id, ShopKind kind, string name, string desc, Sprite icon, int[] prices, string weaponId)
        {
            var it = Asset<ShopItemData>(Root + "/Data/Shop/" + id + ".asset");
            it.id = id; it.kind = kind; it.displayName = name; it.description = desc; it.icon = icon; it.prices = prices; it.weaponId = weaponId;
            return it;
        }

        static EnemyConfig MakeEnemy(string file, string display, EnemyTier tier, int skin, Color tint, float scale, float patrol, float chase, float aggro, float lose,
            float range, float cooldown, float stagger, Vector2 hbSize, Vector2 hbOff, float colW, float colH, int coins, int score, float apple, bool boss, int bossHits)
        {
            var e = Asset<EnemyConfig>(Root + "/Data/Enemies/" + file + ".asset");
            e.displayName = display; e.tier = tier; e.skin = skin; e.tint = tint; e.scale = scale; e.isBoss = boss;
            e.patrolSpeed = patrol; e.chaseSpeed = chase; e.aggroRange = aggro; e.loseRange = lose; e.attackRange = range; e.attackCooldown = cooldown; e.staggerChance = stagger;
            e.hitBoxSize = hbSize; e.hitBoxOffset = hbOff; e.colliderWidth = colW; e.colliderHeight = colH;
            e.coinReward = coins; e.scoreValue = score; e.appleChance = apple;
            e.ApplyTierDefaults();
            if (boss)
            {
                e.hitsToKill = bossHits;
                e.heartsDamage = file == "coronel" ? 4 : 3;
            }
            enemyAssets[file] = e;
            return e;
        }

        static LevelInfo MakeLevel(int index, string title, string desc, string intro)
        {
            var l = new LevelInfo();
            l.index = index; l.sceneName = "Level" + index; l.title = title; l.description = desc; l.introLine = intro;
            l.thumbnail = S("UI/level_" + index + ".png");
            return l;
        }

        // ================================================================== validación
        static string ValidateRules()
        {
            var db = AssetDatabase.LoadAssetAtPath<GameDatabase>(Root + "/Resources/GameDatabase.asset");
            var sb = new StringBuilder();
            bool ok = true;
            if (db == null) return "No se encontró la base de datos del juego.";

            // armas
            WeaponData last = null;
            foreach (var w in db.weapons) if (w != null && (last == null || w.price > last.price)) last = w;
            int prevMax = 0;
            foreach (var w in db.weapons) if (w != null && w != last && w.damage > prevMax) prevMax = w.damage;
            if (last != null && last.damage == prevMax * 2) sb.AppendLine("OK  Última arma (" + last.displayName + ") hace " + last.damage + " = el doble de " + prevMax + ".");
            else { ok = false; sb.AppendLine("ERROR  La última arma no hace el doble de daño que la anterior."); }

            // corazones
            var hearts = db.FindUpgrade(ShopKind.Hearts);
            if (hearts != null && hearts.MaxLevel == GameRules.MaxHeartUpgrades) sb.AppendLine("OK  Mejora de corazones: " + hearts.MaxLevel + " compras como máximo.");
            else { ok = false; sb.AppendLine("ERROR  La mejora de corazones no tiene exactamente 2 niveles."); }

            // enemigos
            foreach (var e in db.enemies)
            {
                if (e == null || e.isBoss) continue;
                int t = (int)e.tier;
                if (e.hitsToKill != GameRules.TierHits[t] || e.heartsDamage != GameRules.TierHearts[t])
                { ok = false; sb.AppendLine("ERROR  " + e.displayName + " no cumple la tabla de resistencia."); }
            }
            sb.AppendLine("OK  Tabla de enemigos (2/1, 4/2, 6/3, 8/4) revisada.");

            // escenas
            foreach (string n in new[] { "MainMenu", "Level1", "Level2", "Level3" })
                if (!File.Exists(ScenesDir + "/" + n + ".unity")) { ok = false; sb.AppendLine("ERROR  Falta la escena " + n); }

            // tienda
            var ids = new HashSet<string>();
            foreach (var it in db.shopItems)
            {
                if (it == null) continue;
                if (!ids.Add(it.id)) { ok = false; sb.AppendLine("ERROR  Id repetido en la tienda: " + it.id); }
                if (it.icon == null) { ok = false; sb.AppendLine("ERROR  Sin icono: " + it.id); }
            }
            sb.Insert(0, ok ? "Reglas verificadas: TODO OK\n" : "Reglas verificadas: HAY ERRORES\n");
            return sb.ToString();
        }

        // ================================================================== escena: menú principal
        static void BuildMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            glows = new List<SpriteRenderer>();
            lamps = new List<Light2D>();

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

            AddGlobalLight(new Color(0.78f, 0.8f, 1f), 0.95f);

            var bg = Group("Fondo");
            var sky = new GameObject("Cielo");
            sky.transform.SetParent(bg);
            var skyNight = SR("Noche", S("Background/sky_night.png"), Vector3.zero, -100, sky.transform, true, true);
            var skyDusk = SR("Atardecer", S("Background/sky_dusk.png"), Vector3.zero, -99, sky.transform, true, true);
            skyDusk.color = new Color(1f, 1f, 1f, 0.35f);
            var sf = sky.AddComponent<SkyFollow>();
            sf.cam = cam; sf.reference = skyNight;
            var starsRoot = new GameObject("Estrellas");
            starsRoot.transform.SetParent(bg);
            var stars = SR("Estrellas", S("Background/stars.png"), Vector3.zero, -97, starsRoot.transform, true, true);
            var sf2 = starsRoot.AddComponent<SkyFollow>();
            sf2.cam = cam; sf2.reference = stars;
            var moon = SR("Luna", S("Background/moon.png"), new Vector3(-7.2f, 8.0f, 0f), -96, bg, true, false);
            moon.transform.localScale = Vector3.one * 1.6f;

            Layer("MontanasLejanas", S("Background/mountains_far.png"), 0f, 0f, -1.5f, -80, new Color(0.45f, 0.48f, 0.78f, 1f), bg, -0.1f);
            Layer("MontanasCercanas", S("Background/mountains_near.png"), 0f, 0f, -1.5f, -75, new Color(0.38f, 0.4f, 0.7f, 1f), bg, -0.2f);
            Layer("Lomas", S("Background/hills_green.png"), 0f, 0f, -1.4f, -70, new Color(0.35f, 0.45f, 0.55f, 1f), bg, -0.32f);
            Layer("Pueblo", S("Background/town_far.png"), 0f, 0f, -1.4f, -65, new Color(0.42f, 0.42f, 0.7f, 1f), bg, -0.45f);
            Layer("PuebloLuces", S("Background/town_lights.png"), 0f, 0f, -1.4f, -64, Color.white, bg, -0.45f);

            var world = Group("Escenario");
            Tiled("Calle", S("Tiles/ground_cobble_top.png"), new Vector3(0f, -0.5f, 0f), new Vector2(44f, 1f), 1, world, null);
            Tiled("Tierra", S("Tiles/ground_dirt.png"), new Vector3(0f, -4f, 0f), new Vector2(44f, 6f), 0, world, null);
            var church = SR("Parroquia", S("Buildings/parroquia.png"), new Vector3(0f, -0.05f, 0f), -30, world, false, false);
            church.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            for (int i = 0; i < 4; i++)
            {
                var g = SR("Luz", S("Pickups/glow.png"), new Vector3(-3.4f + i * 2.3f, 1.2f, 0f), -29, world, true, false);
                g.color = new Color(1f, 0.8f, 0.4f, 0.55f);
                g.transform.localScale = Vector3.one * 1.3f;
            }
            foreach (float lx in new[] { -4.4f, 4.4f })
            {
                var lamp = new GameObject("Farol"); lamp.transform.SetParent(world); lamp.transform.position = new Vector3(lx, 0f, 0f);
                SR("Poste", S("Props/lamp.png"), new Vector3(0f, -0.02f, 0f), -12, lamp.transform, false, true);
                var g = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 2.05f, 0f), -11, lamp.transform, true, true);
                g.color = new Color(1f, 0.74f, 0.35f, 0.6f); g.transform.localScale = Vector3.one * 3.2f;
                var lg = new GameObject("Luz"); lg.transform.SetParent(lamp.transform, false); lg.transform.localPosition = new Vector3(0f, 2.05f, 0f);
                var l = lg.AddComponent<Light2D>(); l.lightType = Light2D.LightType.Point; l.color = new Color(1f, 0.72f, 0.4f); l.intensity = 1.1f; l.pointLightOuterRadius = 6f; l.pointLightInnerRadius = 0.6f;
            }
            var frame = SR("MarcoCampana", S("Props/bell_frame.png"), new Vector3(8.2f, -0.02f, 0f), -10, world, false, false);
            frame.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var bell = SR("Campana", S("Props/bell.png"), new Vector3(8.2f, 2.5f, 0f), -9, world, false, false);
            bell.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            bell.gameObject.AddComponent<BellSway>();
            var pp1 = SR("PapelPicado", S("Props/papel_picado.png"), new Vector3(-6.5f, 7.6f, 0f), 25, world, false, false);
            pp1.transform.localScale = Vector3.one * 1.3f;
            var pp2 = SR("PapelPicado", S("Props/papel_picado.png"), new Vector3(6.5f, 7.4f, 0f), 25, world, false, false);
            pp2.transform.localScale = new Vector3(-1.3f, 1.3f, 1f);

            var hero = Character("Body_A", new Vector3(-6.6f, 0f, 0f), 20, false);
            var holder = hero.AddComponent<MenuActor>();
            holder.animator = hero.GetComponentInChildren<FrameAnimator>();
            holder.clip = Clip("idle", Seq("Player", "Idle", 4), 6f, true); holder.flip = false;
            var knight = Character("Soldado", new Vector3(6.4f, 0f, 0f), 20, true);
            var kh = knight.AddComponent<MenuActor>();
            kh.animator = knight.GetComponentInChildren<FrameAnimator>();
            kh.clip = Clip("idle", Seq("Enemies/Knight1", "Idle", 10), 8f, true); kh.flip = true;

            var life = new GameObject("VidaAmbiental");
            var al = life.AddComponent<AmbientLife>();
            al.glowSprite = S("Pickups/glow.png"); al.unlit = unlitMat; al.cam = cam; al.forceNight = true; al.fireflyCount = 22;
            al.birdFrames = new Sprite[0];

            var audio = new GameObject("Audio");
            var hub = audio.AddComponent<AudioHub>();
            hub.sfx = SfxEntries();

            var ui = new GameObject("MenuUI");
            var mm = ui.AddComponent<MainMenuUI>();
            mm.panelSprite = S("UI/panel.png");
            mm.buttonSprite = S("UI/button.png");
            mm.coinSprite = S("Pickups/coin_0.png");
            mm.lockSprite = S("UI/icon_lock.png");
            mm.checkSprite = S("UI/icon_check.png");
            mm.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/TitleFont.otf");
            mm.menuMusic = A("music_menu.ogg");

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
    }
}
#endif
