#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Dolores1810.EditorTools
{
    public static partial class JuegoBuilder
    {
        static Mission M(string id, string text, MissionKind kind, int target, int reward)
        {
            var m = new Mission();
            m.id = id; m.text = text; m.kind = kind; m.target = target; m.reward = reward;
            return m;
        }

        static readonly string[] TownHouses =
        {
            "Buildings/casa_00.png", "Buildings/casa_01.png", "Buildings/casa_02.png", "Buildings/casa_03.png", "Buildings/casa_04.png", "Buildings/casa_05.png",
            "Buildings/casa_06.png", "Buildings/casa_07.png", "Buildings/casa_08.png", "Buildings/casa_09.png", "Buildings/pink_arches.png", "Buildings/tile_house.png", "Buildings/stone_house.png"
        };

        static readonly string[] LowProps =
        {
            "Props/fence_1.png", "Props/fence_2.png", "Props/fence_3.png", "Props/rock_1.png", "Props/rock_2.png", "Props/rock_5.png", "Props/sign_1.png", "Props/sack.png", "Props/bag.png", "Props/stone.png", "Props/jar.png"
        };

        static readonly string[] Grasses = { "Props/grass_1.png", "Props/grass_2.png", "Props/bush.png" };

        // ================================================================== NIVEL 1: EL PUEBLO DE DOLORES
        static LevelMeta Level1()
        {
            levelEnd = 790f;
            houseSkips.Clear();
            AddSeg(-6f, 150f, 0f, false); AddSeg(153.5f, 178f, 0f, false); AddSeg(178f, 188f, 1f, true); AddSeg(188f, 300f, 0f, false);
            AddSeg(303.5f, 420f, 0f, false); AddSeg(420f, 430f, 1f, true); AddSeg(430f, 458f, 2f, true); AddSeg(458f, 468f, 1f, true);
            AddSeg(468f, 540f, 0f, false); AddSeg(543.5f, 640f, 0f, false); AddSeg(643.5f, levelEnd, 0f, false);
            BuildTerrain();

            // puntos clave
            BuildShop(118f); BuildShop(490f);
            BuildCheckpoints(new float[] { 285f, 520f, 690f });
            BuildExit(772f, "Level2", "Dejas el pueblo de Dolores en la oscuridad.\nEl camino de la insurgencia apenas comienza...");

            // azoteas y rutas escondidas
            float[] roofA = FeatureHouse("Buildings/casa_07.png", 252f, 0.75f);
            float topA = RoofPlatform(roofA);
            BuildLadder(roofA[0] - 0.2f, 0f, topA, 1f);
            float[] walk = FeatureHouse("Buildings/casa_08.png", 395f, 0.75f);
            float topW = RoofPlatform(walk);
            BuildLadder(walk[0] - 0.2f, 0f, topW, 1f);
            Gate gate = BuildGate(395f, topW, false);
            BuildLever(365f, gate);
            float[] roofC = FeatureHouse("Buildings/casa_01.png", 565f, 0.75f);
            float topC = RoofPlatform(roofC);
            BuildLadder(roofC[0] - 0.2f, 0f, topC, 1f);
            BuildPlatforms(new float[,] { { 55f, 2.0f, 4f }, { 62f, 3.3f, 3f }, { 151.75f, 0.8f, 2f }, { 196f, 2.3f, 6f }, { 301.75f, 0.8f, 2f }, { 440f, 4.5f, 3f },
                { 541.75f, 0.8f, 2f }, { 578f, 2.3f, 6f }, { 641.75f, 0.8f, 2f }, { 702f, 2.3f, 5f } });

            // casas (más separadas) y decorado
            BuildHouseRow(TownHouses, -4f, levelEnd - 4f, 0.7f, 0.8f, 1.8f, 5f, -40, -0.05f, Color.white, 0.75f, "Casa");
            BuildHouseRow(TownHouses, -8f, levelEnd, 0.5f, 0.58f, 3f, 9f, -48, 0.4f, new Color(0.78f, 0.8f, 0.95f, 1f), 0.3f, "CasaFondo");
            Prop("Props/quest_board.png", 9f, -20, false, 1f); Prop("Props/sign_2.png", 14f, -20, false, 1f);
            Prop("Props/target_1.png", 24f, -20, false, 1f); Prop("Props/target_2.png", 28f, -20, false, 1f);
            string[] bar = { "Props/barrier_1.png", "Props/barrier_2.png" };
            float[] bx = { 440f, 456f, 520f, 543f, 590f, 622f };
            for (int i = 0; i < bx.Length; i++) Prop(bar[i % 2], bx[i], -18, false, 1f);
            for (float x = 312f; x < 412f; x += R(16f, 22f))
            {
                if (!OnGround(x, 2f) || !Free(x, 2f)) continue;
                float t = GroundTop(x);
                SR("Puesto", S(rnd.NextDouble() < 0.5 ? "Buildings/stall_open.png" : "Buildings/stall_counter.png"), new Vector3(x, t - 0.05f, 0f), -25, gProps, false, false);
                Prop("Props/table.png", x + 3.4f, -22, false, 1f);
                Prop("Props/chair.png", x + 4.8f, -22, true, 1f);
                Reserve(x + 2f, 3.5f);
            }
            Scatter(LowProps, 8f, 780f, 8f, 16f, -20, 0.95f, 1.05f, 1.2f);
            Scatter(Grasses, 6f, 780f, 6f, 12f, 30, 0.9f, 1.1f, 0.6f);
            BackTrees(new string[] { "Props/tree_oak_0.png", "Props/tree_oak_1.png" }, 10f, 400f, 28f, 55f, -43, 0.8f, 1.0f, new Color(0.82f, 0.86f, 0.95f, 1f));
            BackTrees(new string[] { "Props/tree_1.png", "Props/tree_2.png" }, 650f, 780f, 16f, 26f, -26, 0.9f, 1.1f, Color.white);
            for (float x = 70f; x < 760f; x += 30f) BuildLamp(x, x > 520f);
            BuildBreakables(new float[] { 30f, 54f, 88f, 125f, 140f, 175f, 205f, 240f, 270f, 276f, 320f, 345f, 355f, 372f, 405f, 415f, 440f, 452f, 466f,
                530f, 550f, 570f, 600f, 650f, 665f, 680f, 700f, 730f, 745f });
            BuildSpikes(194f, 5f); BuildSpikes(575f, 6f); BuildSpikes(645f, 4f);

            // enemigos: débiles e intermedios; comandante al final del Nivel 1
            float[,] en = {
                { 0, 46, 3 }, { 0, 78, 3 }, { 0, 100, 4 },
                { 0, 128, 3 }, { 0, 145, 2 }, { 0, 168, 3 }, { 0, 182, 2 }, { 1, 232, 3 }, { 0, 262, 3 }, { 0, 282, 3 },
                { 0, 312, 3 }, { 1, 340, 4 }, { 0, 362, 3 }, { 1, 380, 4 }, { 0, 410, 3 },
                { 0, 445, 2 }, { 1, 462, 3 }, { 0, 495, 3 }, { 0, 520, 3 }, { 1, 555, 3 }, { 0, 575, 3 }, { 0, 600, 3 }, { 1, 625, 3 },
                { 0, 660, 3 }, { 1, 672, 3 }, { 0, 705, 3 }
            };
            for (int i = 0; i < en.GetLength(0); i++) BuildKnight(en[i, 0] < 0.5f ? "soldado" : "granadero", en[i, 1], en[i, 2]);
            BuildBoss("oficial", "Comandante realista", 748f, 720f, 764f, false);

            // tesoros y coleccionables
            AutoCoins(14f, 780f, 24f, 34f, new float[] { 208f, 224f, 570f, 586f, 640f, 652f, 750f, 764f });
            PitCoins();
            CoinLine(53f, 57f, 3.2f, 4); CoinLine(60f, 64f, 4.5f, 4);
            CoinLine(194f, 199f, 3.4f, 5); CoinLine(578f, 583f, 3.4f, 5); CoinLine(563f, 567f, topC + 0.8f, 4);
            BuildApples(new float[] { 120f, 236f, 330f, 548f, 625f, 700f, 760f });
            Pick(Pickup.Kind.Heart, 578f, 3.7f);
            Pick(Pickup.Kind.Diamond, 196f, 3.6f); Pick(Pickup.Kind.Diamond, 565f, topC + 1.3f); Pick(Pickup.Kind.Diamond, 702f, 3.6f);
            Pick(Pickup.Kind.Star, 440f, 6.0f);
            BuildChest("c1", 450f, 2.0f, 25, false);
            BuildChest("c2", 255.5f - 2f, topA, 40, true);
            BuildChest("c3", 397.8f, topW, 60, true);
            BuildScroll("s1", 250f, topA + 1.3f, "Miguel Hidalgo y Costilla nació en 1753 en la hacienda de Corralejo, Guanajuato.");
            BuildScroll("s2", 396.3f, topW + 1.3f, "En 1810 Dolores era una villa del Bajío donde vivían agricultores, artesanos y comerciantes.");
            BuildScroll("s3", 62f, 4.8f, "La Corregidora, Josefa Ortiz de Domínguez, avisó a los conspiradores que habían sido descubiertos.");

            BuildZones(new string[,] {
                { "3", "PLAZA DEL PUEBLO", "Dolores, Guanajuato · Septiembre de 1810", "Cruza la plaza y sigue la calle" },
                { "150", "CALLE REAL", "Los soldados vigilan cada esquina", "Avanza por la Calle Real" },
                { "300", "EL MERCADO", "Entre puestos y rumores cae la tarde", "Cruza el mercado" },
                { "430", "LAS BARRICADAS", "El camino está bloqueado", "Abre paso entre las barricadas" },
                { "640", "SALIDA DEL PUEBLO", "Cae la noche sobre Dolores", "Encuentra la salida del pueblo" } });

            var m = new LevelMeta();
            m.title = "NIVEL 1: EL PUEBLO DE DOLORES"; m.subtitle = "Septiembre de 1810. Algo está por cambiar en el pueblo.";
            m.objective = "Cruza la plaza y sigue la calle";
            m.missions = new Mission[] {
                M("cajas", "Rompe 10 cajas o barriles", MissionKind.Break, 10, 40), M("soldados", "Derrota 15 soldados", MissionKind.Kills, 15, 60),
                M("cofres", "Abre 2 cofres", MissionKind.Chests, 2, 50), M("pergamino", "Encuentra un pergamino histórico", MissionKind.Collect, 1, 40) };
            m.todStart = 0f; m.todEnd = 1f; m.explore = A("music_level.ogg");
            return m;
        }

        // ================================================================== NIVEL 2: EL CAMINO DE LA INSURGENCIA
        static LevelMeta Level2()
        {
            levelEnd = 880f;
            houseSkips.Clear();
            AddSeg(-6f, 110f, 0f, true); AddSeg(124f, 190f, 0f, true); AddSeg(190f, 196f, 1f, true); AddSeg(196f, 202f, 2f, true); AddSeg(202f, 250f, 3f, true);
            AddSeg(250f, 258f, 2f, true); AddSeg(258f, 266f, 1f, true); AddSeg(266f, 360f, 0f, true); AddSeg(372f, 470f, 0f, true); AddSeg(473.5f, 560f, 0f, true);
            AddSeg(560f, 570f, 1f, true); AddSeg(570f, 580f, 2f, true); AddSeg(580f, 620f, 3f, true); AddSeg(620f, 630f, 2f, true); AddSeg(630f, 640f, 1f, true);
            AddSeg(640f, 700f, 0f, true); AddSeg(716f, levelEnd, 0f, true);
            BuildTerrain();
            BuildBridge(110f, 124f, 0f); BuildBridge(360f, 372f, 0f); BuildBridge(700f, 716f, 0f);

            BuildShop(140f); BuildShop(735f);
            BuildCheckpoints(new float[] { 280f, 500f, 660f });
            BuildExit(868f, "Level3", "Amanece sobre el Bajío.\nLa plaza de Dolores espera. La Independencia está por comenzar...");

            // hacienda con azotea, mirador y pasarela con reja
            float[] hac = FeatureHouse("Buildings/hacienda_01.png", 335f, 0.85f);
            float topH = RoofPlatform(hac);
            BuildLadder(hac[0] - 0.2f, 0f, topH, 1f);
            BuildPlatform(232f, 6.2f, 8f, gGround);
            BuildLadder(227.6f, 3f, 6.2f, 1f);
            BuildPlatform(600f, 6.4f, 14f, gGround);
            BuildLadder(592.6f, 3f, 6.4f, 1f);
            Gate gate = BuildGate(600f, 6.4f, false);
            BuildLever(548f, gate);
            BuildPlatforms(new float[,] { { 150f, 2.2f, 5f }, { 158f, 3.5f, 4f }, { 471.75f, 0.8f, 2f }, { 300f, 2.3f, 6f }, { 421f, 2.3f, 5f }, { 429f, 3.6f, 4f }, { 665f, 2.3f, 5f } });

            // haciendas, casitas y mucha naturaleza
            string[] rural = { "Buildings/hacienda_00.png", "Buildings/hacienda_02.png", "Buildings/hacienda_03.png", "Buildings/casita_00.png", "Buildings/casita_01.png", "Buildings/casita_02.png" };
            BuildHouseRow(rural, 20f, levelEnd - 20f, 0.7f, 0.8f, 18f, 40f, -40, -0.05f, Color.white, 0.8f, "Hacienda");
            BackTrees(new string[] { "Props/tree_oak_0.png", "Props/tree_oak_1.png", "Props/tree_oak_2.png", "Props/tree_pine_0.png", "Props/tree_pine_1.png" }, 4f, levelEnd, 9f, 16f, -44, 0.75f, 1.0f, new Color(0.7f, 0.78f, 0.84f, 1f));
            BackTrees(new string[] { "Props/tree_oak_1.png", "Props/tree_pine_0.png", "Props/tree_oak_2.png" }, 8f, levelEnd, 14f, 26f, -30, 0.9f, 1.15f, Color.white);
            Scatter(new string[] { "Props/nopal.png", "Props/maguey.png", "Props/bush.png", "Props/rock_1.png", "Props/rock_2.png", "Props/fence_1.png", "Props/stump.png" }, 6f, 870f, 8f, 15f, -20, 0.9f, 1.1f, 1.5f);
            Scatter(Grasses, 6f, 870f, 5f, 10f, 30, 0.9f, 1.1f, 0.6f);
            Prop("Props/signpost.png", 12f, -20, false, 1f); Prop("Props/signpost.png", 195f, -20, false, 1f);
            // campamentos
            foreach (float cx in new float[] { 18f, 395f, 520f, 748f }) { BuildCampfire(cx); BuildTent(cx + 8f, 0.55f); }
            BuildTent(780f, 0.5f);
            Prop("Props/barrier_1.png", 640f, -18, false, 1f); Prop("Props/barrier_2.png", 668f, -18, false, 1f); Prop("Props/barrier_1.png", 690f, -18, false, 1f);
            for (float x = 6f; x < 120f; x += 22f) BuildTorch(x, true);
            for (float x = 130f; x < 860f; x += 38f) BuildTorch(x, true);
            BuildBreakables(new float[] { 22f, 44f, 70f, 98f, 132f, 160f, 182f, 214f, 240f, 262f, 296f, 322f, 348f, 380f, 404f, 428f, 452f, 486f, 510f, 540f, 590f, 612f, 646f, 676f, 726f, 760f, 790f });
            BuildSpikes(215f, 4f); BuildSpikes(595f, 5f); BuildSpikes(690f, 4f);

            // enemigos: débiles, intermedios y resistentes; capitán al final
            string[] ek = { "soldado", "granadero", "sargento" };
            float[,] en = {
                { 0, 48, 3 }, { 0, 82, 3 }, { 1, 100, 3 }, { 0, 140, 3 }, { 1, 165, 3 }, { 0, 180, 2 }, { 1, 222, 3 }, { 2, 238, 3 }, { 0, 262, 2 },
                { 1, 290, 3 }, { 0, 312, 3 }, { 2, 345, 3 }, { 0, 385, 3 }, { 1, 408, 3 }, { 2, 435, 3 }, { 0, 458, 2 },
                { 0, 495, 3 }, { 1, 518, 3 }, { 2, 545, 3 }, { 1, 598, 3 }, { 0, 650, 3 }, { 1, 672, 3 }, { 0, 730, 3 }, { 2, 750, 3 }, { 1, 775, 3 }
            };
            for (int i = 0; i < en.GetLength(0); i++) BuildKnight(ek[(int)en[i, 0]], en[i, 1], en[i, 2]);
            BuildBoss("capitan", "Capitán realista", 835f, 800f, 862f, false);

            AutoCoins(14f, 780f, 22f, 32f, new float[] { 208f, 224f, 590f, 604f, 684f, 698f });
            PitCoins();
            CoinLine(148f, 152f, 3.4f, 4); CoinLine(156f, 160f, 4.7f, 4); CoinLine(228f, 236f, 7.4f, 6); CoinLine(296f, 304f, 3.5f, 5); CoinLine(594f, 606f, 7.6f, 8);
            BuildApples(new float[] { 96f, 270f, 330f, 478f, 625f, 725f, 790f });
            Pick(Pickup.Kind.Heart, 430f, 4.9f); Pick(Pickup.Kind.Heart, 665f, 3.6f);
            Pick(Pickup.Kind.Diamond, 236f, 7.6f); Pick(Pickup.Kind.Diamond, 338f, topH + 1.4f); Pick(Pickup.Kind.Diamond, 606f, 7.6f);
            Pick(Pickup.Kind.Star, 158f, 5.0f);
            BuildChest("c1", 205f, 3.0f, 30, false);
            BuildChest("c2", 337f, topH, 50, true);
            BuildChest("c3", 606f, 6.4f, 70, true);
            BuildScroll("s1", 332f, topH + 1.3f, "Los insurgentes avanzaron hacia San Miguel y Celaya con el estandarte de la Virgen de Guadalupe.");
            BuildScroll("s2", 604f, 7.8f, "Ignacio Allende era capitán del Regimiento de Dragones de la Reina y fue líder del movimiento.");
            BuildScroll("s3", 234f, 7.6f, "Juan Aldama también era capitán y se unió a la causa junto a Hidalgo y Allende.");

            BuildZones(new string[,] {
                { "3", "CAMINO DE LA INSURGENCIA", "Es de noche. Hay que seguir adelante", "Cruza el puente sobre el río" },
                { "105", "EL RIO", "Las aguas bajan crecidas", "Cruza el puente" },
                { "190", "LA LOMA", "El camino sube entre rocas", "Sube la loma" },
                { "325", "LA HACIENDA", "Una hacienda abandonada al borde del camino", "Atraviesa la hacienda" },
                { "470", "EL BOSQUE", "Los primeros rayos del alba entre los árboles", "Cruza el bosque" },
                { "640", "EL CAMPAMENTO", "Los realistas bloquean el paso", "Atraviesa el campamento" },
                { "790", "LA EXPLANADA", "El capitán realista te espera", "Derrota al capitán realista" } });

            var m = new LevelMeta();
            m.title = "NIVEL 2: EL CAMINO DE LA INSURGENCIA"; m.subtitle = "La noche cubre el camino. Al amanecer, la lucha continúa.";
            m.objective = "Cruza el puente sobre el río";
            m.missions = new Mission[] {
                M("cajas", "Rompe 12 cajas o barriles", MissionKind.Break, 12, 60), M("soldados", "Derrota 18 soldados", MissionKind.Kills, 18, 80),
                M("cofres", "Abre 3 cofres", MissionKind.Chests, 3, 80), M("pergaminos", "Encuentra 2 pergaminos históricos", MissionKind.Collect, 2, 60) };
            m.todStart = 1f; m.todEnd = 0f; m.explore = A("music_night.ogg");
            return m;
        }

        // ================================================================== NIVEL 3: EL GRITO DE INDEPENDENCIA
        static LevelMeta Level3()
        {
            levelEnd = 900f;
            houseSkips.Clear();
            AddSeg(-6f, 160f, 0f, false); AddSeg(163.5f, 330f, 0f, false); AddSeg(330f, 340f, 1f, true); AddSeg(340f, 350f, 2f, true); AddSeg(350f, 380f, 3f, true);
            AddSeg(380f, 390f, 2f, true); AddSeg(390f, 400f, 1f, true); AddSeg(400f, 520f, 0f, false); AddSeg(523.5f, levelEnd, 0f, false);
            BuildTerrain();

            BuildShop(92f); BuildShop(488f);
            BuildCheckpoints(new float[] { 305f, 570f, 790f });
            // iglesia y campana (solo aquí termina el juego)
            var ch = SR("Parroquia", S("Buildings/parroquia.png"), new Vector3(879f, -0.05f, 0f), -30, gHouses, false, false);
            ch.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            AddHouseSkip(870f, 899f);
            for (int i = 0; i < 4; i++)
            {
                var g = SR("LuzParroquia", S("Pickups/glow.png"), new Vector3(875.6f + i * 2.1f, 1.1f, 0f), -29, gHouses, true, false);
                g.color = new Color(1f, 0.8f, 0.4f, 0f);
                g.transform.localScale = Vector3.one * 1.2f;
                glows.Add(g);
            }
            BuildBell(893f);

            float[] roofB = FeatureHouse("Buildings/casa_07.png", 610f, 0.75f);
            float topB = RoofPlatform(roofB);
            BuildLadder(roofB[0] - 0.2f, 0f, topB, 1f);
            float[] walk = FeatureHouse("Buildings/casa_08.png", 700f, 0.75f);
            float topW = RoofPlatform(walk);
            BuildLadder(walk[0] - 0.2f, 0f, topW, 1f);
            Gate gate = BuildGate(700f, topW, false);
            BuildLever(660f, gate);
            BuildPlatform(366f, 6.0f, 8f, gGround);
            BuildLadder(361.6f, 3f, 6.0f, 1f);
            BuildPlatforms(new float[,] { { 60f, 2.2f, 5f }, { 68f, 3.5f, 4f }, { 161.75f, 0.8f, 2f }, { 260f, 2.3f, 6f }, { 268f, 3.6f, 5f }, { 480f, 2.3f, 6f }, { 521.75f, 0.8f, 2f }, { 640f, 2.2f, 5f } });

            string[] town = new string[TownHouses.Length + 4];
            for (int i = 0; i < TownHouses.Length; i++) town[i] = TownHouses[i];
            town[TownHouses.Length] = "Buildings/hacienda_00.png"; town[TownHouses.Length + 1] = "Buildings/hacienda_01.png";
            town[TownHouses.Length + 2] = "Buildings/hacienda_02.png"; town[TownHouses.Length + 3] = "Buildings/hacienda_03.png";
            BuildHouseRow(town, -4f, levelEnd - 40f, 0.72f, 0.82f, 1.6f, 4.8f, -40, -0.05f, Color.white, 0.8f, "Casa");
            BuildHouseRow(town, -8f, levelEnd - 30f, 0.5f, 0.58f, 3f, 8f, -48, 0.4f, new Color(0.78f, 0.8f, 0.95f, 1f), 0.3f, "CasaFondo");
            // mercado y plaza
            for (float x = 30f; x < 520f; x += R(22f, 32f))
            {
                if (!OnGround(x, 2f) || !Free(x, 2f) || (x > 322f && x < 405f)) continue;
                string stall = rnd.NextDouble() < 0.4 ? "Buildings/stall_open.png" : (rnd.NextDouble() < 0.5 ? "Buildings/stall_counter.png" : "Props/cart.png");
                float t = GroundTop(x);
                SR("Puesto", S(stall), new Vector3(x, t - 0.05f, 0f), -25, gProps, false, false);
                Prop("Props/jar.png", x + 3.2f, -22, false, 1f); Prop("Props/stand.png", x + 4.8f, -23, false, 0.9f);
                Reserve(x + 2f, 4f);
            }
            for (float x = 20f; x < 800f; x += 26f)
            {
                var pp = SR("PapelPicado", S("Props/papel_picado.png"), new Vector3(x, 6.0f, 0f), 25, gProps, false, false);
                pp.transform.localScale = Vector3.one * 1.25f;
            }
            Scatter(LowProps, 8f, 800f, 9f, 17f, -20, 0.95f, 1.05f, 1.2f);
            Scatter(Grasses, 6f, 800f, 6f, 12f, 30, 0.9f, 1.1f, 0.6f);
            Prop("Props/banner_2.png", 200f, -18, false, 1.2f); Prop("Props/banner_3.png", 560f, -18, false, 1.2f); Prop("Props/banner_2.png", 820f, -18, false, 1.2f);
            Prop("Props/barrier_1.png", 540f, -18, false, 1f); Prop("Props/barrier_2.png", 600f, -18, false, 1f); Prop("Props/barrier_1.png", 760f, -18, false, 1f);
            for (float x = 40f; x < 870f; x += 24f) BuildLamp(x, x > 400f);
            for (float x = 806f; x < 872f; x += 14f) BuildTorch(x, true);
            BuildBreakables(new float[] { 28f, 52f, 78f, 108f, 134f, 150f, 175f, 198f, 222f, 246f, 280f, 322f, 372f, 420f, 440f, 462f, 505f, 540f, 590f, 624f, 652f, 676f, 720f, 745f, 770f, 800f });
            BuildSpikes(250f, 4f); BuildSpikes(445f, 5f); BuildSpikes(615f, 4f);

            // enemigos: intermedios, resistentes y muy resistentes; coronel al final
            string[] ek = { "granadero", "sargento", "oficial" };
            float[,] en = {
                { 0, 45, 3 }, { 0, 85, 3 }, { 1, 115, 3 }, { 0, 145, 3 }, { 0, 180, 3 }, { 1, 205, 3 }, { 0, 235, 3 }, { 2, 275, 3 }, { 0, 300, 3 }, { 1, 322, 2 },
                { 1, 364, 3 }, { 2, 372, 2 }, { 0, 420, 3 }, { 1, 445, 3 }, { 0, 470, 3 }, { 2, 500, 3 }, { 1, 540, 3 }, { 0, 565, 3 }, { 2, 595, 3 },
                { 1, 625, 3 }, { 0, 650, 3 }, { 2, 680, 3 }, { 1, 725, 3 }, { 2, 745, 3 }, { 0, 768, 3 }
            };
            for (int i = 0; i < en.GetLength(0); i++) BuildKnight(ek[(int)en[i, 0]], en[i, 1], en[i, 2]);
            BuildBoss("coronel", "Coronel realista", 845f, 812f, 866f, true);

            AutoCoins(14f, 800f, 22f, 32f, new float[] { 244f, 258f, 440f, 454f, 610f, 624f });
            PitCoins();
            CoinLine(58f, 62f, 3.4f, 4); CoinLine(66f, 70f, 4.7f, 4); CoinLine(258f, 262f, 3.5f, 4); CoinLine(266f, 270f, 4.8f, 4); CoinLine(362f, 370f, 7.2f, 6);
            BuildApples(new float[] { 100f, 250f, 360f, 540f, 640f, 730f, 810f });
            Pick(Pickup.Kind.Heart, 268f, 5.0f); Pick(Pickup.Kind.Heart, 640f, 3.6f);
            Pick(Pickup.Kind.Diamond, 260f, 3.6f); Pick(Pickup.Kind.Diamond, 366f, 7.6f); Pick(Pickup.Kind.Diamond, 610f, topB + 1.4f); Pick(Pickup.Kind.Diamond, 698f, topW + 1.4f);
            Pick(Pickup.Kind.Star, 68f, 5.0f);
            BuildChest("c1", 345f, 2.0f, 35, false);
            BuildChest("c2", 611.5f, topB, 60, true);
            BuildChest("c3", 703f, topW, 90, true);
            BuildScroll("s1", 362f, 7.4f, "El Grito de Dolores ocurrió en la madrugada del 16 de septiembre de 1810.");
            BuildScroll("s2", 607f, topB + 1.3f, "Hidalgo llamó al pueblo a levantarse en armas contra el mal gobierno.");
            BuildScroll("s3", 701.6f, topW + 1.3f, "Cada 15 de septiembre se conmemora el Grito de Independencia en las plazas de todo México.");

            BuildZones(new string[,] {
                { "3", "PLAZA DE ARMAS", "15 de septiembre de 1810. Dolores despierta", "Cruza la plaza hacia la parroquia" },
                { "165", "CALLE DE LOS FAROLES", "El pueblo se prepara para algo grande", "Recorre la calle de los faroles" },
                { "330", "EL MERCADO GRANDE", "Las voces se unen entre los puestos", "Sube por el mercado" },
                { "520", "CALLEJON DE LA IGLESIA", "La noche llega a la plaza", "Llega a la plaza de la parroquia" },
                { "800", "PLAZA DE LA PARROQUIA", "El coronel realista bloquea la plaza", "Derrota al coronel realista" },
                { "870", "LA CAMPANA", "Es el momento", "Toca la campana de Dolores" } });

            var m = new LevelMeta();
            m.title = "NIVEL 3: LA PLAZA DE ARMAS"; m.subtitle = "La lucha llega al corazón de Dolores. Derrota al Coronel realista.";
            m.objective = "Cruza la Plaza de Armas";
            m.missions = new Mission[] {
                M("cajas", "Rompe 14 cajas o barriles", MissionKind.Break, 14, 80), M("soldados", "Derrota 22 soldados", MissionKind.Kills, 22, 100),
                M("cofres", "Abre 3 cofres", MissionKind.Chests, 3, 100), M("pergaminos", "Encuentra 3 pergaminos históricos", MissionKind.Collect, 3, 100) };
            m.todStart = 0f; m.todEnd = 1f; m.explore = A("music_festive.ogg"); m.isFinal = false;
            return m;
        }

        // ================================================================== NIVEL 4: EL GRITO DE INDEPENDENCIA
        static LevelMeta Level4()
        {
            var m = Level3();
            m.title = "NIVEL 4: EL GRITO DE INDEPENDENCIA";
            m.subtitle = "El asalto final hacia la Parroquia de Dolores. Derrota al General Realista.";
            m.objective = "Avanza hacia la Parroquia de Dolores";
            m.isFinal = true;
            return m;
        }

        static void BuildBell(float x)
        {
            float t = GroundTop(x);
            var root = new GameObject("CampanaDeDolores");
            root.transform.SetParent(gCheck);
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
            AddGlow(bell.gameObject, new Color(1f, 0.9f, 0.4f, 1f));
        }
    }
}
#endif
