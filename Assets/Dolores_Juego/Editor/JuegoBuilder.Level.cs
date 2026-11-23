#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Dolores1810.EditorTools
{
    public static partial class JuegoBuilder
    {
        class Seg { public float x0, x1, top; public bool grass; }

        class LevelMeta
        {
            public string title, subtitle, objective;
            public Mission[] missions;
            public float todStart, todEnd;
            public AudioClip explore;
            public bool isFinal;
        }

        static List<Seg> segs;
        static List<float[]> reserved;
        static float levelEnd;
        static int theme;           // 0 pueblo, 1 camino, 2 plaza
        static int levelIdx;
        static Transform gGround, gHouses, gProps, gEnemies, gItems, gCheck, gLights, gBg, gMgr;
        static Camera mainCam;
        static FollowCamera fcam;
        static Light2D globalLight;
        static PlatformerPlayer player;

        // ================================================================== estructura general
        static void BuildLevel(int index)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            glows = new List<SpriteRenderer>();
            lamps = new List<Light2D>();
            segs = new List<Seg>();
            reserved = new List<float[]>();
            levelIdx = index;
            theme = index == 2 ? 1 : (index == 3 ? 2 : 0);

            gGround = Group("Suelo"); gHouses = Group("Casas"); gProps = Group("Decorado"); gEnemies = Group("Enemigos");
            gItems = Group("Coleccionables"); gCheck = Group("PuntosDeControl"); gLights = Group("Luces"); gBg = Group("Fondo"); gMgr = Group("Gestores");

            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            mainCam.orthographic = true;
            mainCam.orthographicSize = 4.5f;
            mainCam.clearFlags = CameraClearFlags.SolidColor;
            mainCam.backgroundColor = new Color(0.04f, 0.05f, 0.12f);
            mainCam.nearClipPlane = -20f;
            mainCam.farClipPlane = 60f;
            camGO.transform.position = new Vector3(3f, 2.8f, -10f);
            camGO.AddComponent<AudioListener>();
            fcam = camGO.AddComponent<FollowCamera>();
            globalLight = AddGlobalLight(Color.white, 1f);

            LevelMeta meta = index == 1 ? Level1() : (index == 2 ? Level2() : Level3());
            FinishLevel(meta);
            EditorSceneManager.SaveScene(scene, ScenesDir + "/Level" + index + ".unity");
        }

        static void FinishLevel(LevelMeta meta)
        {
            fcam.minX = -6f;
            fcam.maxX = levelEnd - 1f;
            player = BuildPlayer();
            fcam.target = player.transform;

            var dn = BuildBackground(meta);

            var audio = new GameObject("Audio");
            audio.transform.SetParent(gMgr);
            var hub = audio.AddComponent<AudioHub>();
            hub.sfx = SfxEntries();

            var amb = new GameObject("Ambiente");
            amb.transform.SetParent(gMgr);
            var asnd = amb.AddComponent<AmbientSound>();
            asnd.dayLoop = A("amb_day.ogg");
            asnd.nightLoop = A("amb_night.ogg");
            asnd.distantBell = theme == 1 ? null : A("sfx_bell.wav");
            asnd.cycle = dn;
            asnd.volume = 0.55f;
            var life = amb.AddComponent<AmbientLife>();
            life.glowSprite = S("Pickups/glow.png");
            life.unlit = unlitMat;
            life.cycle = dn;
            life.cam = mainCam;
            life.birdFrames = new Sprite[] { S("Fx/bird_0.png"), S("Fx/bird_1.png") };

            var sessGO = new GameObject("GameSession");
            sessGO.transform.SetParent(gMgr);
            var gs = sessGO.AddComponent<GameSession>();
            gs.player = player;
            gs.cam = fcam;
            var start = new GameObject("PuntoDeInicio");
            start.transform.SetParent(gMgr);
            start.transform.position = new Vector3(3f, 0.1f, 0f);
            gs.startPoint = start.transform;
            gs.levelIndex = levelIdx;
            gs.levelTitle = meta.title;
            gs.levelSubtitle = meta.subtitle;
            gs.startObjective = meta.objective;
            gs.missions = meta.missions;
            gs.sparkSprite = S("Fx/spark.png");
            gs.dustSprite = S("Fx/dust.png");
            gs.confettiSprite = S("Fx/confetti.png");
            gs.coinSprites = new Sprite[6];
            for (int i = 0; i < 6; i++) gs.coinSprites[i] = S("Pickups/coin_" + i + ".png");
            gs.appleSprite = S("Pickups/apple.png");
            gs.heartSprite = S("Pickups/heart.png");
            gs.litMaterial = litMat;
            gs.unlitMaterial = unlitMat;
            gs.menuScene = SceneNames.Menu;
            gs.exploreClip = meta.explore;
            gs.combatClip = A("music_combat.ogg");
            gs.bossClip = A("music_boss.ogg");
            gs.victoryClip = A("music_victory.ogg");
            gs.finalClip = A("music_final.ogg");

            var uiGO = new GameObject("GameUI");
            uiGO.transform.SetParent(gMgr);
            var ui = uiGO.AddComponent<GameUI>();
            ui.heartSprite = S("Pickups/heart.png");
            ui.shieldSprite = S("Props/shield_2.png");
            ui.scrollSprite = S("UI/icon_pergamino.png");
            ui.coinSprite = S("Pickups/coin_0.png");
            ui.panelSprite = S("UI/panel.png");
            ui.buttonSprite = S("UI/button.png");
            ui.titleFont = AssetDatabase.LoadAssetAtPath<Font>(Root + "/Fonts/TitleFont.otf");
        }

        // ================================================================== terreno
        static void AddSeg(float x0, float x1, float top, bool grass)
        {
            var s = new Seg(); s.x0 = x0; s.x1 = x1; s.top = top; s.grass = grass; segs.Add(s);
        }

        static bool TryGroundTop(float x, out float top)
        {
            foreach (var s in segs)
                if (x >= s.x0 && x <= s.x1) { top = s.top; return true; }
            top = 0f;
            return false;
        }

        static float GroundTop(float x) { float t; TryGroundTop(x, out t); return t; }
        static bool OnGround(float x, float margin) { float t; return TryGroundTop(x - margin, out t) && TryGroundTop(x + margin, out t); }

        static void Reserve(float x, float half) { reserved.Add(new float[] { x - half, x + half }); }
        static bool Free(float x, float margin)
        {
            foreach (var r in reserved) if (x > r[0] - margin && x < r[1] + margin) return false;
            return true;
        }

        static void BuildTerrain()
        {
            foreach (var s in segs) BuildGround(s, gGround);
            float[] xs = { -6.5f, levelEnd + 0.5f };
            foreach (float x in xs)
            {
                var w = new GameObject("MuroInvisible");
                w.layer = layerGround;
                w.transform.SetParent(gGround);
                w.transform.position = new Vector3(x, 10f, 0f);
                var c = w.AddComponent<BoxCollider2D>();
                c.size = new Vector2(1f, 60f);
            }
            Tiled("FondoDeFosas", S("Tiles/ground_dirt.png"), new Vector3(levelEnd / 2f - 3f, -9f, 0f), new Vector2(levelEnd + 60f, 14f), -55, gGround, new Color(0.3f, 0.25f, 0.28f, 1f));
            // huecos entre segmentos
            var sorted = new List<Seg>(segs);
            sorted.Sort((a, b) => a.x0.CompareTo(b.x0));
            for (int i = 0; i + 1 < sorted.Count; i++)
            {
                float a = sorted[i].x1, b = sorted[i + 1].x0;
                if (b - a < 0.6f) continue;
                BuildPit(a, b);
            }
        }

        static void BuildPit(float a, float b)
        {
            if (theme != 1) return;
            float w = b - a;
            float cx = (a + b) / 2f;
            Tiled("Agua", S("Props/water.png"), new Vector3(cx, -2.1f, 0f), new Vector2(w + 1.6f, 3.4f), -2, gGround, null);
            var kz = new GameObject("Rio");
            kz.transform.SetParent(gGround);
            kz.transform.position = new Vector3(cx, -1.6f, 0f);
            var bc = kz.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(w + 1.0f, 3.0f);
            kz.AddComponent<KillZone>();
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

        static void BuildPlatforms(float[,] pl)
        {
            for (int i = 0; i < pl.GetLength(0); i++) BuildPlatform(pl[i, 0], pl[i, 1], pl[i, 2], gGround);
        }

        static void BuildBridge(float x0, float x1, float top)
        {
            float w = x1 - x0, cx = (x0 + x1) / 2f;
            BuildPlatform(cx, top, w, gGround);
            var rail = Tiled("BarandalPuente", S("Props/bridge_rail.png"), new Vector3(cx, top - 0.02f, 0f), new Vector2(w, 0.6f), 3, gProps, null);
            rail.gameObject.name = "BarandalPuente";
        }

        static void BuildLadder(float x, float bottom, float top, float exitDir)
        {
            var root = new GameObject("Escalera");
            root.transform.SetParent(gProps);
            root.transform.position = new Vector3(x, bottom, 0f);
            float h = top - bottom + 0.5f;
            var sr = SR("Peldanos", S("Props/ladder.png"), new Vector3(0f, h / 2f, 0f), -6, root.transform, false, true);
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.size = new Vector2(0.75f, h);
            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.3f, top - bottom + 1.5f);
            bc.offset = new Vector2(0f, (top - bottom) / 2f + 0.25f);
            var z = root.AddComponent<ClimbZone>();
            z.TopY = top; z.BottomY = bottom; z.exitDirection = exitDir;
            Reserve(x, 1.5f);
        }

        // ================================================================== casas y decorado
        static List<float[]> houseSkips = new List<float[]>();

        static void AddHouseSkip(float a, float b) { houseSkips.Add(new float[] { a, b }); }

        static void BuildHouseRow(string[] names, float x0, float x1, float sMin, float sMax, float gMin, float gMax, int order, float y, Color tint, float glowChance, string groupName)
        {
            string prev = "";
            float x = x0;
            int guard = 0;
            while (x < x1 && guard++ < 3000)
            {
                string n = names[rnd.Next(names.Length)];
                if (n == prev && names.Length > 1) continue;
                prev = n;
                var sp = S(n);
                float sc = R(sMin, sMax);
                float w = sp.bounds.size.x * sc;
                float cx = x + w / 2f;
                x += w + R(gMin, gMax);
                if (cx - w / 2f > x1) break;
                bool skip = false;
                foreach (var r in houseSkips) if (cx + w / 2f > r[0] && cx - w / 2f < r[1]) { skip = true; if (r[1] + 0.5f > x) x = r[1] + 0.5f; }
                if (skip) continue;
                float gy = 0f;
                float tt;
                if (TryGroundTop(cx, out tt)) gy = tt;
                var sr = SR(groupName, sp, new Vector3(cx, y + gy, 0f), order, gHouses, false, false);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                sr.color = tint;
                if (rnd.NextDouble() < glowChance)
                {
                    var g = SR("LuzVentana", S("Pickups/glow.png"), new Vector3(cx + R(-w * 0.3f, w * 0.3f), y + gy + R(0.85f, 1.2f), 0f), order + 1, gHouses, true, false);
                    g.color = new Color(1f, 0.8f, 0.4f, 0f);
                    g.transform.localScale = Vector3.one * R(0.9f, 1.3f);
                    glows.Add(g);
                }
            }
        }

        /// <summary>Casa destacada (con una azotea a la que se sube). Devuelve {x0, x1, y de la azotea}.</summary>
        static float[] FeatureHouse(string rel, float cx, float scale)
        {
            var sp = S(rel);
            float gy = GroundTop(cx);
            var sr = SR("CasaDestacada", sp, new Vector3(cx, gy - 0.05f, 0f), -40, gHouses, false, false);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            float w = sp.bounds.size.x * scale, h = sp.bounds.size.y * scale;
            AddHouseSkip(cx - w / 2f - 0.3f, cx + w / 2f + 0.3f);
            var g = SR("LuzVentana", S("Pickups/glow.png"), new Vector3(cx, gy + 1.0f, 0f), -39, gHouses, true, false);
            g.color = new Color(1f, 0.8f, 0.4f, 0f);
            glows.Add(g);
            return new float[] { cx - w / 2f, cx + w / 2f, gy - 0.05f + h };
        }

        /// <summary>Plataforma sobre una casa destacada. Devuelve la altura de la plataforma.</summary>
        static float RoofPlatform(float[] house)
        {
            float top = house[2] - 0.1f;
            float w = house[1] - house[0] - 0.6f;
            BuildPlatform((house[0] + house[1]) / 2f, top, w, gGround);
            return top;
        }

        static void BackTrees(string[] names, float x0, float x1, float stepMin, float stepMax, int order, float sMin, float sMax, Color tint)
        {
            for (float x = x0; x < x1; x += R(stepMin, stepMax))
            {
                float t;
                if (!TryGroundTop(x, out t)) continue;
                var sr = SR("ArbolFondo", S(names[rnd.Next(names.Length)]), new Vector3(x, t + 0.1f, 0f), order, gProps, false, false);
                float sc = R(sMin, sMax);
                sr.transform.localScale = new Vector3(sc, sc, 1f);
                sr.flipX = rnd.NextDouble() < 0.5;
                sr.color = tint;
            }
        }

        static SpriteRenderer Prop(string rel, float x, int order, bool flip, float scale)
        {
            float t;
            if (!TryGroundTop(x, out t)) return null;
            var sp = S(rel);
            var sr = SR(System.IO.Path.GetFileNameWithoutExtension(rel), sp, new Vector3(x, t - 0.02f, 0f), order, gProps, false, false);
            sr.flipX = flip;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        static void Scatter(string[] names, float x0, float x1, float stepMin, float stepMax, int order, float scaleMin, float scaleMax, float margin)
        {
            for (float x = x0; x < x1; x += R(stepMin, stepMax))
            {
                if (!OnGround(x, 1.2f) || !Free(x, margin)) continue;
                Prop(names[rnd.Next(names.Length)], x, order, rnd.NextDouble() < 0.5, R(scaleMin, scaleMax));
            }
        }

        static void BuildLamp(float x, bool withLight)
        {
            if (!OnGround(x, 1f) || !Free(x, 0.8f)) return;
            float t = GroundTop(x);
            var root = new GameObject("Farol");
            root.transform.SetParent(gLights);
            root.transform.position = new Vector3(x, t, 0f);
            SR("Poste", S("Props/lamp.png"), new Vector3(0f, -0.02f, 0f), -12, root.transform, false, true);
            var g = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 2.05f, 0f), -11, root.transform, true, true);
            g.color = new Color(1f, 0.74f, 0.35f, 0f);
            g.transform.localScale = Vector3.one * 3.2f;
            glows.Add(g);
            if (withLight) AddPointLight(root.transform, new Vector3(0f, 2.05f, 0f), 6f);
        }

        static void BuildTorch(float x, bool withLight)
        {
            if (!OnGround(x, 0.8f)) return;
            float t = GroundTop(x);
            var root = new GameObject("Antorcha");
            root.transform.SetParent(gLights);
            root.transform.position = new Vector3(x, t, 0f);
            SR("Palo", S("Props/torch.png"), new Vector3(0f, -0.02f, 0f), -12, root.transform, false, true);
            var g = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 1.95f, 0f), -11, root.transform, true, true);
            g.color = new Color(1f, 0.65f, 0.25f, 0f);
            g.transform.localScale = Vector3.one * 2.6f;
            glows.Add(g);
            if (withLight) AddPointLight(root.transform, new Vector3(0f, 1.95f, 0f), 5f);
        }

        static void AddPointLight(Transform parent, Vector3 localPos, float radius)
        {
            var lg = new GameObject("Luz");
            lg.transform.SetParent(parent, false);
            lg.transform.localPosition = localPos;
            var l = lg.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.72f, 0.4f);
            l.intensity = 0f;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = 0.6f;
            lamps.Add(l);
        }

        static void BuildCampfire(float x)
        {
            if (!OnGround(x, 1f)) return;
            float t = GroundTop(x);
            var root = new GameObject("Fogata");
            root.transform.SetParent(gProps);
            root.transform.position = new Vector3(x, t, 0f);
            SR("Piedras", S("Props/fire_ring.png"), new Vector3(0f, -0.02f, 0f), -5, root.transform, false, true).transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var flame = SR("Llama", S("Props/fire_0.png"), new Vector3(0f, 0.12f, 0f), -4, root.transform, true, true);
            flame.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            var g = SR("Brillo", S("Pickups/glow.png"), new Vector3(0f, 0.9f, 0f), -6, root.transform, true, true);
            g.color = new Color(1f, 0.62f, 0.25f, 0.55f);
            g.transform.localScale = Vector3.one * 4.2f;
            var lg = new GameObject("Luz");
            lg.transform.SetParent(root.transform, false);
            lg.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            var l = lg.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = new Color(1f, 0.6f, 0.3f);
            l.intensity = 1.1f;
            l.pointLightOuterRadius = 7f;
            l.pointLightInnerRadius = 0.7f;
            var cf = root.AddComponent<CampFire>();
            cf.target = flame; cf.fireLight = l;
            cf.frames = new Sprite[] { S("Props/fire_0.png"), S("Props/fire_1.png"), S("Props/fire_2.png"), S("Props/fire_3.png") };
            Reserve(x, 2f);
        }

        static void BuildTent(float x, float scale)
        {
            float t;
            if (!TryGroundTop(x, out t)) return;
            var sr = SR("Carpa", S("Buildings/tent.png"), new Vector3(x, t - 0.05f, 0f), -24, gProps, false, false);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            Reserve(x, 1.8f * scale + 0.5f);
        }

        static void AddGlow(GameObject go, Color c)
        {
            var g = go.AddComponent<InteractGlow>();
            g.glowColor = c;
            g.haloSprite = S("Pickups/glow.png");
        }

        static void BuildBreakables(float[] xs)
        {
            foreach (float x in xs)
            {
                if (!OnGround(x, 0.8f) || !Free(x, 0.6f)) continue;
                bool barrel = rnd.NextDouble() < 0.55;
                var sp = S(barrel ? "Props/barrel.png" : "Props/crate.png");
                float t = GroundTop(x);
                var sr = SR(barrel ? "Barril" : "Caja", sp, new Vector3(x, t - 0.02f, 0f), -5, gProps, false, false);
                var bc = sr.gameObject.AddComponent<BoxCollider2D>();
                Vector2 sz = sp.bounds.size;
                bc.size = new Vector2(sz.x * 0.85f, sz.y * 0.95f);
                bc.offset = new Vector2(0f, sz.y / 2f);
                sr.gameObject.layer = layerEnemy;
                var br = sr.gameObject.AddComponent<Breakable>();
                br.hits = barrel ? 2 : 1;
                br.coinValue = barrel ? 5 : 3;
                br.appleChance = 0.12f;
                AddGlow(sr.gameObject, new Color(1f, 0.85f, 0.4f, 1f));
            }
        }

        static void BuildSpikes(float x0, float w)
        {
            float top;
            if (!TryGroundTop(x0, out top)) return;
            var root = new GameObject("Pinchos");
            root.transform.SetParent(gProps);
            root.transform.position = new Vector3(x0 + w / 2f, top, 0f);
            for (int i = 0; i < (int)w; i++)
                SR("Pico", S("Props/spikes.png"), new Vector3(-w / 2f + 0.5f + i, -0.02f, 0f), 5, root.transform, false, true);
            var c = root.AddComponent<BoxCollider2D>();
            c.isTrigger = true;
            c.size = new Vector2(w - 0.3f, 0.45f);
            c.offset = new Vector2(0f, 0.25f);
            root.AddComponent<Hazard>();
            Reserve(x0 + w / 2f, w / 2f + 0.5f);
        }

        // ================================================================== interactivos
        static Gate BuildGate(float x, float baseY, bool startOpen)
        {
            var go = new GameObject("Reja");
            go.layer = layerGround;
            go.transform.SetParent(gProps);
            go.transform.position = new Vector3(x, baseY + 1.7f, 0f);
            var gsp = S("Props/fence_3.png");
            float segH = gsp.bounds.size.y * 1.1f;
            for (int i = 0; i < 3; i++)
            {
                var sr = SR("Barrotes_" + i, gsp, new Vector3(0f, -1.7f + i * (segH * 0.95f), 0f), 6, go.transform, false, true);
                sr.transform.localScale = Vector3.one * 1.1f;
            }
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(0.9f, 3.4f);
            var g = go.AddComponent<Gate>();
            g.openOffset = new Vector3(0f, 3.8f, 0f);
            g.startOpen = startOpen;
            g.blocker = bc;
            return g;
        }

        static void BuildLever(float x, Gate gate)
        {
            float t;
            if (!TryGroundTop(x, out t)) return;
            var sr = SR("Palanca", S("Props/lever_0.png"), new Vector3(x, t - 0.02f, 0f), -4, gProps, false, false);
            var bc = sr.gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.0f, 1.0f);
            bc.offset = new Vector2(0f, 0.5f);
            var lv = sr.gameObject.AddComponent<GateLever>();
            lv.gate = gate; lv.offSprite = S("Props/lever_0.png"); lv.onSprite = S("Props/lever_1.png");
            AddGlow(sr.gameObject, new Color(0.6f, 1f, 0.6f, 1f));
            Reserve(x, 1.5f);
        }

        static void BuildChest(string id, float x, float y, int coins, bool hidden)
        {
            string a = hidden ? "Pickups/chest2_closed.png" : "Pickups/chest_closed.png";
            string b = hidden ? "Pickups/chest2_open.png" : "Pickups/chest_open.png";
            var sr = SR("Cofre_" + id, S(a), new Vector3(x, y, 0f), 12, gItems, false, false);
            var bc = sr.gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(1.4f, 1.1f);
            bc.offset = new Vector2(0f, 0.55f);
            var ch = sr.gameObject.AddComponent<TreasureChest>();
            ch.chestId = "L" + levelIdx + "_" + id; ch.coins = coins; ch.hidden = hidden;
            ch.closedSprite = S(a); ch.openSprite = S(b);
            AddGlow(sr.gameObject, hidden ? new Color(0.9f, 0.6f, 1f, 1f) : new Color(1f, 0.85f, 0.4f, 1f));
            Reserve(x, 1.3f);
        }

        static void BuildScroll(string id, float x, float y, string fact)
        {
            var sr = SR("Pergamino_" + id, S("UI/icon_pergamino.png"), new Vector3(x, y, 0f), 14, gItems, false, false);
            sr.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            var c = sr.gameObject.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.6f;
            var col = sr.gameObject.AddComponent<Collectible>();
            col.collectibleId = "L" + levelIdx + "_" + id; col.fact = fact; col.coins = 20;
            var g = SR("Brillo", S("Pickups/glow.png"), Vector3.zero, 13, sr.transform, true, true);
            g.color = new Color(1f, 0.95f, 0.6f, 0.45f);
            g.transform.localScale = Vector3.one * 1.6f;
        }

        static void BuildShop(float x)
        {
            float t;
            if (!TryGroundTop(x, out t)) return;
            var cart = SR("CarretaMercader", S("Props/cart.png"), new Vector3(x, t - 0.02f, 0f), -4, gProps, false, false);
            var bc = cart.gameObject.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(2.8f, 1.8f);
            bc.offset = new Vector2(0f, 0.9f);
            cart.gameObject.AddComponent<ShopPoint>();
            AddGlow(cart.gameObject, new Color(1f, 0.9f, 0.5f, 1f));
            SR("Puesto", S("Props/stand.png"), new Vector3(x + 2.7f, t - 0.02f, 0f), -5, gProps, false, false).transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            SR("Jarron", S("Props/jar.png"), new Vector3(x - 1.9f, t - 0.02f, 0f), -4, gProps, false, false);
            Reserve(x, 4f);
        }

        static void Pick(Pickup.Kind kind, float x, float y)
        {
            string sprite;
            switch (kind)
            {
                case Pickup.Kind.Apple: sprite = "Pickups/apple.png"; break;
                case Pickup.Kind.Heart: sprite = "Pickups/heart.png"; break;
                case Pickup.Kind.Diamond: sprite = "Pickups/diamond.png"; break;
                case Pickup.Kind.Star: sprite = "Pickups/star.png"; break;
                default: sprite = "Pickups/coin_0.png"; break;
            }
            var sr = SR(kind.ToString(), S(sprite), new Vector3(x, y, 0f), 15, gItems, false, false);
            var c = sr.gameObject.AddComponent<CircleCollider2D>();
            c.isTrigger = true;
            c.radius = 0.45f;
            var p = sr.gameObject.AddComponent<Pickup>();
            p.kind = kind;
            if (kind == Pickup.Kind.Coin)
            {
                p.coinSprites = new Sprite[6];
                for (int i = 0; i < 6; i++) p.coinSprites[i] = S("Pickups/coin_" + i + ".png");
            }
            p.staticSprite = sr.sprite;
        }

        static void CoinLine(float x0, float x1, float y, int n)
        {
            for (int i = 0; i < n; i++) Pick(Pickup.Kind.Coin, n == 1 ? x0 : Mathf.Lerp(x0, x1, i / (float)(n - 1)), y);
        }

        static void CoinArc(float xc, float y0, float w, float h, int n)
        {
            for (int i = 0; i < n; i++)
            {
                float u = n == 1 ? 0.5f : i / (float)(n - 1);
                Pick(Pickup.Kind.Coin, xc + (u - 0.5f) * w, y0 + Mathf.Sin(u * Mathf.PI) * h);
            }
        }

        static void AutoCoins(float from, float to, float stepMin, float stepMax, float[] avoid)
        {
            int toggle = 0;
            for (float x = from; x < to; x += R(stepMin, stepMax))
            {
                float t;
                if (!OnGround(x, 3f) || !TryGroundTop(x, out t)) continue;
                bool skip = false;
                for (int i = 0; avoid != null && i + 1 < avoid.Length; i += 2) if (x > avoid[i] && x < avoid[i + 1]) skip = true;
                if (skip) continue;
                if (toggle++ % 2 == 0) CoinLine(x - 2f, x + 2f, t + 1.0f, 5); else CoinArc(x, t + 0.8f, 5f, 2.0f, 6);
            }
        }

        static void PitCoins()
        {
            var sorted = new List<Seg>(segs);
            sorted.Sort((a, b) => a.x0.CompareTo(b.x0));
            for (int i = 0; i + 1 < sorted.Count; i++)
            {
                float a = sorted[i].x1, b = sorted[i + 1].x0;
                if (b - a < 0.6f || b - a > 6f) continue;
                CoinArc((a + b) / 2f, 1.2f, b - a + 0.8f, 1.6f, 5);
            }
        }

        static void BuildApples(float[] xs)
        {
            foreach (float x in xs)
            {
                float t;
                if (TryGroundTop(x, out t)) Pick(Pickup.Kind.Apple, x, t + 0.8f);
            }
        }

        static void BuildCheckpoints(float[] xs)
        {
            for (int i = 0; i < xs.Length; i++)
            {
                float t = GroundTop(xs[i]);
                var root = new GameObject("Banderines_" + (i + 1));
                root.transform.SetParent(gCheck);
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
                Reserve(xs[i], 3f);
            }
        }

        static void BuildZones(string[,] z)
        {
            for (int i = 0; i < z.GetLength(0); i++)
            {
                float x = float.Parse(z[i, 0], CI);
                var go = new GameObject("Zona_" + z[i, 1]);
                go.transform.SetParent(gCheck);
                go.transform.position = new Vector3(x, 4f, 0f);
                var bc = go.AddComponent<BoxCollider2D>();
                bc.isTrigger = true;
                bc.size = new Vector2(2f, 16f);
                var zt = go.AddComponent<ZoneTrigger>();
                zt.title = z[i, 1]; zt.subtitle = z[i, 2]; zt.objective = z[i, 3];
            }
        }

        static void BuildExit(float x, string nextScene, string text)
        {
            float t = GroundTop(x);
            var root = new GameObject("SalidaDelNivel");
            root.transform.SetParent(gCheck);
            root.transform.position = new Vector3(x, t, 0f);
            SR("Letrero", S("Props/signpost.png"), new Vector3(-1.2f, -0.02f, 0f), -8, root.transform, false, true);
            var bc = root.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(3f, 12f);
            bc.offset = new Vector2(0f, 4f);
            var ex = root.AddComponent<LevelExit>();
            ex.nextScene = nextScene; ex.text = text;
            BuildTorch(x - 3.2f, true);
            BuildTorch(x + 3.2f, true);
            Reserve(x, 5f);
        }

        // ================================================================== personajes
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
            pl.walk = Clip("walk", Seq("Player", "Walk", 6), 10f, true);
            pl.run = Clip("run", Seq("Player", "Run", 6), 13f, true);
            pl.jump = Clip("jump", Seq("Player", "Jump", 1), 1f, true);
            pl.fall = Clip("fall", Seq("Player", "Fall", 1), 1f, true);
            pl.climb = Clip("climb", Seq("Player", "Climb", 6), 9f, true);
            pl.hit = Clip("hit", Seq("Player", "Hit", 4), 10f, false);
            pl.death = Clip("death", Seq("Player", "Death", 8), 9f, false);
            return pl;
        }

        static GameObject MakeKnightBody(string name, EnemyConfig cfg, float x, float topY, Transform parent, out SpriteRenderer sr, out FrameAnimator an)
        {
            var root = new GameObject(name);
            root.layer = layerEnemy;
            root.transform.SetParent(parent);
            root.transform.position = new Vector3(x, topY + 0.05f, 0f);
            var rb = root.AddComponent<Rigidbody2D>();
            rb.mass = 5f;
            var bc = root.AddComponent<BoxCollider2D>();
            bc.size = new Vector2(cfg.colliderWidth * cfg.scale, cfg.colliderHeight * cfg.scale);
            bc.offset = new Vector2(0f, cfg.colliderHeight * cfg.scale / 2f);
            var vis = new GameObject("Visual");
            vis.transform.SetParent(root.transform, false);
            sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 10;
            sr.sharedMaterial = litMat;
            an = vis.AddComponent<FrameAnimator>();
            an.target = sr;
            var shadow = SR("Sombra", S("Fx/shadow.png"), new Vector3(0f, 0.03f, 0f), 9, root.transform, true, true);
            shadow.transform.localScale = new Vector3(1.5f * cfg.scale, 1f, 1f);
            return root;
        }

        static void BuildKnight(string cfgName, float x, float radius)
        {
            float t;
            if (!TryGroundTop(x, out t)) { Debug.LogWarning("[1810] Enemigo fuera del suelo en x=" + x); return; }
            EnemyConfig cfg = enemyAssets[cfgName];
            SpriteRenderer sr; FrameAnimator an;
            var root = MakeKnightBody("Soldado_" + cfgName + "_" + (int)x, cfg, x, t, gEnemies, out sr, out an);
            var e = root.AddComponent<EnemyKnight>();
            e.config = cfg; e.patrolRadius = radius; e.visual = sr; e.animator = an;
            string f = "Enemies/Knight" + cfg.skin;
            e.idle = Clip("idle", Seq(f, "Idle", 10), 8f, true);
            e.walk = Clip("walk", Seq(f, "Walk", 10), 10f, true);
            e.run = Clip("run", Seq(f, "Run", 10), 14f, true);
            e.attack = Clip("attack", Seq(f, "Attack", 10), 12f, false);
            e.hurt = Clip("hurt", Seq(f, "Hurt", 10), 16f, false);
            e.die = Clip("die", Seq(f, "Die", 10), 12f, false);
            var bar = new GameObject("BarraVida");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(-0.6f, cfg.colliderHeight * cfg.scale + 0.45f, 0f);
            var back = SR("Fondo", S("Fx/pixel.png"), new Vector3(-0.03f, 0f, 0f), 40, bar.transform, true, true);
            back.transform.localScale = new Vector3(1.26f, 0.2f, 1f);
            back.color = new Color(0.08f, 0.03f, 0.03f, 0.9f);
            var fill = SR("Vida", S("Fx/pixel.png"), Vector3.zero, 41, bar.transform, true, true);
            fill.transform.localScale = new Vector3(1.2f, 0.14f, 1f);
            fill.color = new Color(0.35f, 0.9f, 0.2f, 1f);
            e.hpBar = bar.transform; e.hpFill = fill; e.hpBarWidth = 1.2f;
        }

        static void BuildBoss(string cfgName, string bossName, float x, float gateLeftX, float gateRightX, bool finalBoss)
        {
            float t = GroundTop(x);
            EnemyConfig cfg = enemyAssets[cfgName];
            SpriteRenderer sr; FrameAnimator an;
            var root = MakeKnightBody("Jefe_" + cfgName, cfg, x, t, gEnemies, out sr, out an);
            var boss = root.AddComponent<BossKnight>();
            boss.config = cfg; boss.bossName = bossName; boss.visual = sr; boss.animator = an; boss.finalBoss = finalBoss;
            string f = "Enemies/Knight" + cfg.skin;
            boss.idle = Clip("idle", Seq(f, "Idle", 10), 8f, true);
            boss.run = Clip("run", Seq(f, "Run", 10), 14f, true);
            boss.attack = Clip("attack", Seq(f, "Attack", 10), 11f, false);
            boss.hurt = Clip("hurt", Seq(f, "Hurt", 10), 16f, false);
            boss.die = Clip("die", Seq(f, "Die", 10), 10f, false);
            boss.waveSprite = S("Fx/dust.png");
            boss.coinReward = finalBoss ? 80 : 50;
            Gate left = BuildGate(gateLeftX, GroundTop(gateLeftX), true);
            Gate right = BuildGate(gateRightX, GroundTop(gateRightX), false);
            boss.arenaGates = new Gate[] { left };
            boss.rewardGate = right;
            var trig = new GameObject("ArenaDelJefe");
            trig.transform.SetParent(gCheck);
            trig.transform.position = new Vector3(gateLeftX + 5f, 4f, 0f);
            var bc = trig.AddComponent<BoxCollider2D>();
            bc.isTrigger = true;
            bc.size = new Vector2(6f, 14f);
            trig.AddComponent<BossArenaTrigger>().boss = boss;
        }

        // ================================================================== fondo
        static DayNightCycle BuildBackground(LevelMeta meta)
        {
            var dn = gBg.gameObject.AddComponent<DayNightCycle>();
            var sky = new GameObject("Cielo");
            sky.transform.SetParent(gBg);
            var skyNight = SR("Noche", S("Background/sky_night.png"), Vector3.zero, -100, sky.transform, true, true);
            var skyDusk = SR("Atardecer", S("Background/sky_dusk.png"), Vector3.zero, -99, sky.transform, true, true);
            var skyDay = SR("Dia", S("Background/sky_day.png"), Vector3.zero, -98, sky.transform, true, true);
            var sf = sky.AddComponent<SkyFollow>();
            sf.cam = mainCam; sf.reference = skyDay;
            var starsRoot = new GameObject("Estrellas");
            starsRoot.transform.SetParent(gBg);
            var stars = SR("Estrellas", S("Background/stars.png"), Vector3.zero, -97, starsRoot.transform, true, true);
            stars.color = new Color(1f, 1f, 1f, 0f);
            var sf2 = starsRoot.AddComponent<SkyFollow>();
            sf2.cam = mainCam; sf2.reference = stars;
            var sun = SR("Sol", S("Background/sun.png"), new Vector3(8f, 6f, 0f), -96, gBg, true, false);
            sun.transform.localScale = Vector3.one * 1.5f;
            var moon = SR("Luna", S("Background/moon.png"), new Vector3(-6f, 8f, 0f), -95, gBg, true, false);
            moon.transform.localScale = Vector3.one * 1.5f;
            moon.color = new Color(1f, 1f, 1f, 0f);

            var tinted = new List<SpriteRenderer>();
            for (int g = 0; g < 3; g++)
            {
                var root = new GameObject("Nubes_" + g);
                root.transform.SetParent(gBg);
                root.transform.position = new Vector3(3f, 5.2f + g * 1.1f, 0f);
                float tile = 48f;
                for (int j = 0; j < 4; j++)
                {
                    var sp = S("Background/cloud_" + ((j + g) % 4) + ".png");
                    float cx = (j + 0.5f) * (tile / 4f) + R(-2f, 2f) - tile / 2f;
                    float cy = R(-1f, 1.2f);
                    float sc = R(0.8f, 1.4f) * (1f - g * 0.15f);
                    for (int copy = -1; copy <= 1; copy++)
                    {
                        var sr = SR("Nube", sp, new Vector3(copy * tile + cx, cy, 0f), -90 + g, root.transform, true, true);
                        sr.transform.localScale = new Vector3(sc, sc, 1f);
                        sr.color = new Color(1f, 1f, 1f, 0.92f - g * 0.12f);
                        tinted.Add(sr);
                    }
                }
                var pl = root.AddComponent<ParallaxLayer>();
                pl.speedX = 0.06f + g * 0.05f; pl.speedY = 0.5f; pl.tileWidth = tile; pl.drift = 0.15f + g * 0.1f;
            }

            Color white = Color.white;
            tinted.AddRange(LayerSprites(Layer("MontanasLejanas", S("Background/mountains_far.png"), 0.10f, 0.45f, -1.2f, -80, white, gBg, 0f)));
            tinted.AddRange(LayerSprites(Layer("MontanasCercanas", S("Background/mountains_near.png"), 0.20f, 0.5f, -1.2f, -75, white, gBg, 0f)));
            tinted.AddRange(LayerSprites(Layer("Lomas", S("Background/hills_green.png"), 0.34f, 0.55f, -1.2f, -70, white, gBg, 0f)));
            SpriteRenderer[] townLights = new SpriteRenderer[0];
            if (theme == 1)
            {
                tinted.AddRange(LayerSprites(Layer("BosqueLejano", S("Background/forest_far.png"), 0.45f, 0.58f, -1.2f, -66, white, gBg, 0f)));
                tinted.AddRange(LayerSprites(Layer("BosqueCercano", S("Background/forest_near.png"), 0.62f, 0.65f, -1.2f, -62, white, gBg, 0f)));
            }
            else
            {
                tinted.AddRange(LayerSprites(Layer("PuebloLejano", S("Background/town_far.png"), 0.50f, 0.6f, -1.2f, -65, white, gBg, 0f)));
                var lightsLayer = Layer("PuebloLuces", S("Background/town_lights.png"), 0.50f, 0.6f, -1.2f, -64, new Color(1f, 1f, 1f, 0f), gBg, 0f);
                townLights = LayerSprites(lightsLayer).ToArray();
            }

            dn.player = player.transform;
            dn.cam = mainCam;
            dn.startX = 0f;
            dn.endX = Mathf.Max(100f, levelEnd - 40f);
            dn.todStart = meta.todStart;
            dn.todEnd = meta.todEnd;
            dn.globalLight = globalLight;
            dn.skyDay = skyDay; dn.skyDusk = skyDusk; dn.skyNight = skyNight;
            dn.stars = stars; dn.sun = sun; dn.moon = moon;
            dn.townLights = townLights;
            dn.tinted = tinted.ToArray();
            dn.glows = glows.ToArray();
            dn.lamps = lamps.ToArray();
            return dn;
        }
    }
}
#endif
