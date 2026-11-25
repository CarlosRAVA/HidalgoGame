using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Dolores1810
{
    /// <summary>
    /// Cinemática histórica de introducción al iniciar el Nivel 1 (~60 segundos, dividida en 6 actos de 10 s).
    /// Muestra ilustraciones compuestas con los assets del juego, movimiento de cámara Ken Burns, efectos animados
    /// de antorchas y chispas, recorrido de cámara por el pueblo de Dolores, narración con efecto máquina de escribir
    /// y controles para avanzar de acto o saltar la cinemática.
    /// </summary>
    public class IntroStoryCinematic : MonoBehaviour
    {
        public static bool IsPlaying { get; private set; }

        public const float ActDuration = 10.0f;
        public const int ActCount = 6;
        public const float TotalDuration = ActDuration * ActCount; // 60.0 segundos (1 minuto)

        struct StoryAct
        {
            public string badge;
            public string title;
            public string narrative;
            public string quote;
            public string imageFile;
            public Vector2 panStart;
            public Vector2 panEnd;
            public float zoomStart;
            public float zoomEnd;
            public float worldCamX;
            public SfxId sfx;
            public Vector2[] flameNormalizedPositions;
        }

        static readonly StoryAct[] Acts = new StoryAct[]
        {
            new StoryAct
            {
                badge = "ACTO I DE VI  ·  NUEVA ESPAÑA, 1810",
                title = "EL YUGO DEL VIRREINATO",
                narrative = "Durante trescientos años, el pueblo de la Nueva España vivió bajo el dominio de la Corona española. En las villas, minas y haciendas del Bajío, campesinos y artesanos cargaban con pesados tributos e injusticias, mientras las tropas realistas vigilaban cada calle con mano de hierro.",
                quote = "«La tierra que trabajamos con nuestras manos reclama justicia y libertad.»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_1.png",
                panStart = new Vector2(-18f, -4f),
                panEnd = new Vector2(18f, 6f),
                zoomStart = 1.02f,
                zoomEnd = 1.08f,
                worldCamX = 210f,
                sfx = SfxId.Step,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.296f, 0.495f), new Vector2(0.722f, 0.495f) }
            },
            new StoryAct
            {
                badge = "ACTO II DE VI  ·  QUERÉTARO, SEPTIEMBRE DE 1810",
                title = "LA CONSPIRACIÓN DE QUERÉTARO",
                narrative = "Bajo el velo de tertulias literarias en la ciudad de Querétaro, un grupo de patriotas planeaba el despertar de una nación libre: el cura Don Miguel Hidalgo y Costilla, los capitanes Ignacio Allende y Juan Aldama, y la Corregidora Doña Josefa Ortiz de Domínguez.",
                quote = "«El alzamiento estaba pactado para octubre, pero el destino adelantaría la hora.»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_2.png",
                panStart = new Vector2(14f, -6f),
                panEnd = new Vector2(-14f, 6f),
                zoomStart = 1.02f,
                zoomEnd = 1.09f,
                worldCamX = 165f,
                sfx = SfxId.Secret,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.166f, 0.465f), new Vector2(0.834f, 0.465f) }
            },
            new StoryAct
            {
                badge = "ACTO III DE VI  ·  NOCHE DEL 13 AL 15 DE SEPTIEMBRE",
                title = "¡LA CONSPIRACIÓN HA SIDO DESCUBIERTA!",
                narrative = "La traición golpeó antes de tiempo: el plan insurgente fue denunciado ante las autoridades virreinales. Arriesgando su propia libertad en Querétaro, Doña Josefa logró enviar un mensaje urgente a través de la noche hacia San Miguel y el pueblo de Dolores.",
                quote = "«¡Avisa a Allende y al cura Hidalgo! Han descubierto la conjura y vienen por nosotros.»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_3.png",
                panStart = new Vector2(-22f, 4f),
                panEnd = new Vector2(22f, -4f),
                zoomStart = 1.03f,
                zoomEnd = 1.09f,
                worldCamX = 120f,
                sfx = SfxId.Alert,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.231f, 0.425f), new Vector2(0.653f, 0.425f) }
            },
            new StoryAct
            {
                badge = "ACTO IV DE VI  ·  MADRUGADA DEL 16 DE SEPTIEMBRE",
                title = "LA DECISIÓN EN EL CURATO DE DOLORES",
                narrative = "Al recibir la noticia en la madrugada del 16 de septiembre en Dolores, Hidalgo no dudó ni un instante. En lugar de ocultarse o rendirse ante el temor de la Corona, reunió a Allende, Aldama y a sus hombres para iniciar la lucha por la Independencia en ese mismo momento.",
                quote = "«Caballeros, somos perdidos; aquí no hay más recurso que levantarnos por nuestra libertad.» — Miguel Hidalgo",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_4.png",
                panStart = new Vector2(0f, -10f),
                panEnd = new Vector2(0f, 10f),
                zoomStart = 1.01f,
                zoomEnd = 1.08f,
                worldCamX = 82f,
                sfx = SfxId.BossAlert,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.299f, 0.470f), new Vector2(0.701f, 0.470f) }
            },
            new StoryAct
            {
                badge = "ACTO V DE VI  ·  PARROQUIA DE DOLORES, GUANAJUATO",
                title = "EL GRITO DE LA LIBERTAD",
                narrative = "El repicar de la campana de la Parroquia de Dolores rompió el silencio de la noche. Al escuchar su llamado, familias enteras, campesinos y artesanos acudieron al atrio con antorchas, machetes, hondas y lanzas, encendiendo la llama de la libertad que recorrería todo México.",
                quote = "«¡Viva nuestra libertad! ¡Viva la Independencia! ¡Muera el mal gobierno!»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_5.png",
                panStart = new Vector2(0f, 8f),
                panEnd = new Vector2(0f, -8f),
                zoomStart = 1.01f,
                zoomEnd = 1.10f,
                worldCamX = 44f,
                sfx = SfxId.Bell,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.215f, 0.475f), new Vector2(0.785f, 0.475f) }
            },
            new StoryAct
            {
                badge = "ACTO VI DE VI  ·  PLAZA DEL PUEBLO DE DOLORES",
                title = "TU MISIÓN COMIENZA EN DOLORES",
                narrative = "Sin embargo, la guarnición realista del Comandante ha bloqueado las calles y tomado la Plaza de Dolores para sofocar el levantamiento antes del alba. ¡Empuña tu arma, supera los obstáculos, resuelve los acertijos de la bandera y las antorchas, y libera el pueblo de Dolores!",
                quote = "«El destino de la insurgencia está en tus manos. ¡Adelante hacia la Calle Real!»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_act_6.png",
                panStart = new Vector2(-16f, 0f),
                panEnd = new Vector2(16f, 0f),
                zoomStart = 1.02f,
                zoomEnd = 1.08f,
                worldCamX = 12f,
                sfx = SfxId.WeaponSwitch,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.258f, 0.340f), new Vector2(0.901f, 0.250f) }
            }
        };

        // Cinemática final de 30 segundos (3 actos de 10 s) al terminar el Nivel 4 después de tocar la Campana de Dolores
        static readonly StoryAct[] EndingActs = new StoryAct[]
        {
            new StoryAct
            {
                badge = "EPÍLOGO I DE III  ·  ATRIO DE LA PARROQUIA DE DOLORES",
                title = "¡EL PUEBLO RESPONDE AL LLAMADO!",
                narrative = "Al escuchar el repicar de la Campana de Dolores y ver derrotada a la guarnición del General Realista, cientos de campesinos, mineros, artesanos y familias acudieron al atrio de la Parroquia. Armados con machetes, lanzas, hondas y antorchas, juraron luchar unidos por su libertad.",
                quote = "«¡Viva la Independencia! ¡Viva la América! ¡Muera el mal gobierno!» — Grito de Dolores, 16 de septiembre de 1810",
                imageFile = "Dolores_Juego/Art/UI/cinematic_end_1.png",
                panStart = new Vector2(0f, -8f),
                panEnd = new Vector2(0f, 8f),
                zoomStart = 1.01f,
                zoomEnd = 1.09f,
                worldCamX = 246f,
                sfx = SfxId.Bell,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.229f, 0.485f), new Vector2(0.772f, 0.485f) }
            },
            new StoryAct
            {
                badge = "EPÍLOGO II DE III  ·  DE ATOTONILCO A SAN MIGUEL Y GUANAJUATO",
                title = "EL EJÉRCITO INSURGENTE EN MARCHA",
                narrative = "Aquel pequeño grupo que salió de Dolores esa mañana pronto se convirtió en un ejército de miles de almas. En Atotonilco levantaron su estandarte, liberaron San Miguel el Grande y Celaya, y marcharon hacia Guanajuato para encender la causa insurgente en todo el territorio.",
                quote = "«Lo que comenzó con el eco de una campana en Dolores despertó el corazón de toda una nación.»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_end_2.png",
                panStart = new Vector2(-20f, -4f),
                panEnd = new Vector2(20f, 4f),
                zoomStart = 1.02f,
                zoomEnd = 1.08f,
                worldCamX = 165f,
                sfx = SfxId.LevelComplete,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.391f, 0.414f), new Vector2(0.649f, 0.414f) }
            },
            new StoryAct
            {
                badge = "EPÍLOGO III DE III  ·  EL NACIMIENTO DE MÉXICO LIBRE",
                title = "EL LEGADO ETERNO DE LA LIBERTAD",
                narrative = "Tras once años de lucha heroica iniciada aquella madrugada en el pueblo de Dolores, México consumó su Independencia en 1821. Hoy, cada septiembre, el repique de aquella campana vuelve a sonar para honrar a quienes nos dieron patria, dignidad y libertad.",
                quote = "«¡Que resuene por siempre la Campana de Dolores! ¡Viva México libre y soberano!»",
                imageFile = "Dolores_Juego/Art/UI/cinematic_end_3.png",
                panStart = new Vector2(0f, 8f),
                panEnd = new Vector2(0f, -8f),
                zoomStart = 1.02f,
                zoomEnd = 1.10f,
                worldCamX = 248f,
                sfx = SfxId.Victory,
                flameNormalizedPositions = new Vector2[] { new Vector2(0.245f, 0.485f), new Vector2(0.756f, 0.485f) }
            }
        };

        static readonly Dictionary<string, Sprite> s_loadedSprites = new Dictionary<string, Sprite>();

        Canvas canvas;
        CanvasGroup rootGroup;
        Image stageImageA, stageImageB;
        RectTransform stageRectA, stageRectB, stageViewport;
        Text headerTitleText, timerText, actBadgeText, actTitleText, narrativeText, quoteText, nextBtnLabel;
        Image totalProgressBar;
        Image[] actDots;
        Image[] flameOverlays;
        Image[] emberOverlays;
        Vector2[] emberBasePos;
        float[] emberSpeed;
        float[] emberPhase;
        Sprite[] fireFrames;
        Sprite[] actSprites;
        Sprite glowSprite;
        StoryAct[] activeActs;
        bool isEndingCinematic;

        bool skipRequested;
        bool nextActRequested;

        public static IEnumerator Play(GameSession gs)
        {
            if (gs == null) yield break;
            var go = new GameObject("CinematicaHistoriaNivel1");
            var cine = go.AddComponent<IntroStoryCinematic>();
            cine.isEndingCinematic = false;
            cine.activeActs = Acts;
            yield return cine.RunCinematic(gs);
            if (go != null) Destroy(go);
        }

        /// <summary>Reproduce la cinemática final de 30 segundos al concluir el Nivel 4 tras tocar la Campana de Dolores.</summary>
        public static IEnumerator PlayEnding(GameSession gs)
        {
            if (gs == null) yield break;
            var go = new GameObject("CinematicaFinalNivel4");
            var cine = go.AddComponent<IntroStoryCinematic>();
            cine.isEndingCinematic = true;
            cine.activeActs = EndingActs;
            yield return cine.RunCinematic(gs);
            if (go != null) Destroy(go);
        }

        void OnDestroy()
        {
            IsPlaying = false;
        }

        static Sprite LoadFullRectSprite(string relPath)
        {
            Sprite cached;
            if (s_loadedSprites.TryGetValue(relPath, out cached) && cached != null)
                return cached;

            string fullPath = Path.Combine(Application.dataPath, relPath);
            if (File.Exists(fullPath))
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (ImageConversion.LoadImage(tex, bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    tex.wrapMode = TextureWrapMode.Clamp;
                    var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    s_loadedSprites[relPath] = sp;
                    return sp;
                }
            }
            return null;
        }

        static Sprite MakeFallbackActSprite(int actIdx)
        {
            int w = 320, h = 140;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color top = actIdx % 2 == 0 ? new Color(0.10f, 0.08f, 0.22f, 1f) : new Color(0.18f, 0.10f, 0.14f, 1f);
            Color bot = actIdx == 4 ? new Color(0.68f, 0.36f, 0.16f, 1f) : new Color(0.32f, 0.22f, 0.16f, 1f);
            for (int y = 0; y < h; y++)
            {
                Color c = Color.Lerp(bot, top, y / (float)h);
                for (int x = 0; x < w; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        void LoadAssets(GameSession gs)
        {
            if (activeActs == null) activeActs = Acts;
            actSprites = new Sprite[activeActs.Length];
            for (int i = 0; i < activeActs.Length; i++)
            {
                actSprites[i] = LoadFullRectSprite(activeActs[i].imageFile);
                if (actSprites[i] == null) actSprites[i] = MakeFallbackActSprite(i);
            }

            fireFrames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                fireFrames[i] = LoadFullRectSprite("Dolores_Juego/Art/Props/fire_" + i + ".png");
            }
            glowSprite = LoadFullRectSprite("Dolores_Juego/Art/Pickups/glow.png");
            if (glowSprite == null && gs != null) glowSprite = gs.sparkSprite;
        }

        void BuildUI()
        {
            if (activeActs == null) activeActs = Acts;
            Font titleFont = GameUI.I != null && GameUI.I.titleFont != null ? GameUI.I.titleFont : UIFactory.GetFont();

            canvas = UIFactory.CreateCanvas("CinematicaIntroCanvas", 65);
            canvas.transform.SetParent(transform, false);
            Transform root = canvas.transform;

            var holder = UIFactory.Rect("RaizCinematica", root);
            UIFactory.Stretch(holder);
            rootGroup = holder.gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;
            rootGroup.blocksRaycasts = true;
            rootGroup.interactable = true;

            // Fondo semi-transparente oscuro que deja ver suavemente el recorrido de cámara del pueblo de Dolores detrás
            UIFactory.FullScreen(holder, "VeloFondo", new Color(0.04f, 0.02f, 0.01f, 0.76f));

            // Barras de cine superior e inferior con filete dorado colonial
            UIFactory.Box(holder, "BarraCineSup", new Color(0.05f, 0.03f, 0.02f, 0.96f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1920f, 92f));
            UIFactory.Box(holder, "FileteOroSup", new Color(0.92f, 0.74f, 0.28f, 0.9f), new Vector2(0f, 1f), new Vector2(0f, -92f), new Vector2(1920f, 3f));

            UIFactory.Box(holder, "BarraCineInf", new Color(0.05f, 0.03f, 0.02f, 0.96f), new Vector2(0f, 0f), Vector2.zero, new Vector2(1920f, 84f));
            UIFactory.Box(holder, "FileteOroInf", new Color(0.92f, 0.74f, 0.28f, 0.9f), new Vector2(0f, 0f), new Vector2(0f, 84f), new Vector2(1920f, 3f));

            // Encabezado superior: Crónicas / Epílogo + Temporizador + Indicadores de Actos
            string hdrText = isEndingCinematic
                ? "EPÍLOGO HISTÓRICO  ·  1810: EL DESPERTAR DE UNA NACIÓN LIBRE"
                : "CRÓNICAS DE LA INDEPENDENCIA  ·  1810: EL GRITO DE LA LIBERTAD";
            headerTitleText = UIFactory.Label(holder, "EncabezadoHistoria",
                hdrText,
                32, UIFactory.Gold, TextAnchor.MiddleLeft, FontStyle.Bold, true, titleFont);
            UIFactory.Place(headerTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(52f, -16f), new Vector2(1240f, 58f));

            // Puntos indicadores de los actos en la barra superior
            actDots = new Image[activeActs.Length];
            float dotStartRight = -230f - (activeActs.Length - 1) * 40f;
            for (int i = 0; i < activeActs.Length; i++)
            {
                var dotBg = UIFactory.Box(holder, "ActoDotBg_" + i, new Color(0.24f, 0.16f, 0.10f, 0.95f),
                    new Vector2(1f, 1f), new Vector2(dotStartRight + i * 40f, -32f), new Vector2(32f, 26f));
                var dotFill = UIFactory.Box(dotBg.transform, "Fill", UIFactory.Gold,
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 20f));
                UIFactory.PlaceCenter(dotFill.rectTransform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 20f));
                var numLbl = UIFactory.Label(dotBg.transform, "Num", (i + 1).ToString(), 17, new Color(0.12f, 0.06f, 0.02f, 1f), TextAnchor.MiddleCenter, FontStyle.Bold, false);
                numLbl.alignByGeometry = true;
                UIFactory.Stretch(numLbl.rectTransform);
                actDots[i] = dotFill;
            }

            string initTimer = isEndingCinematic ? "00:00 / 00:30" : "00:00 / 01:00";
            timerText = UIFactory.Label(holder, "RelojCinematica", initTimer, 30, UIFactory.Cream, TextAnchor.MiddleRight, FontStyle.Bold);
            UIFactory.Place(timerText.rectTransform, new Vector2(1f, 1f), new Vector2(-52f, -18f), new Vector2(190f, 54f));

            // Marco central del Teatro Ilustrado (1308 x 524)
            var frameOuter = UIFactory.Rect("MarcoEscenarioExterior", holder);
            UIFactory.PlaceCenter(frameOuter, new Vector2(0.5f, 0.5f), new Vector2(0f, 142f), new Vector2(1308f, 524f));
            UIFactory.FullScreen(frameOuter, "BordeOro", new Color(0.88f, 0.68f, 0.24f, 0.95f));

            var frameInner = UIFactory.Rect("MarcoEscenarioInterior", frameOuter);
            UIFactory.PlaceCenter(frameInner, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1296f, 512f));
            UIFactory.FullScreen(frameInner, "FondoNegro", new Color(0.05f, 0.03f, 0.02f, 1f));

            stageViewport = UIFactory.Rect("ViewportEscenario", frameInner);
            UIFactory.PlaceCenter(stageViewport, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1284f, 500f));
            var maskImg = stageViewport.gameObject.AddComponent<Image>();
            maskImg.color = Color.white;
            var mask = stageViewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Dos capas de imagen para fundido cruzado suave + movimiento Ken Burns
            stageRectA = UIFactory.Rect("EscenaImgA", stageViewport);
            UIFactory.PlaceCenter(stageRectA, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 550f));
            stageImageA = stageRectA.gameObject.AddComponent<Image>();
            stageImageA.raycastTarget = false;

            stageRectB = UIFactory.Rect("EscenaImgB", stageViewport);
            UIFactory.PlaceCenter(stageRectB, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 550f));
            stageImageB = stageRectB.gameObject.AddComponent<Image>();
            stageImageB.raycastTarget = false;
            stageImageB.color = new Color(1f, 1f, 1f, 0f);

            // Efectos animados de fuego/antorchas dentro del escenario
            flameOverlays = new Image[2];
            for (int i = 0; i < 2; i++)
            {
                var flRt = UIFactory.Rect("LlamaAnimada_" + i, stageViewport);
                UIFactory.PlaceCenter(flRt, new Vector2(0f, 0f), Vector2.zero, new Vector2(54f, 54f));
                var flImg = flRt.gameObject.AddComponent<Image>();
                flImg.raycastTarget = false;
                if (fireFrames != null && fireFrames[0] != null) flImg.sprite = fireFrames[0];
                flImg.color = new Color(1f, 0.92f, 0.72f, 0.88f);
                flameOverlays[i] = flImg;
            }

            // Partículas de brasas / chispas doradas flotando suavemente por la escena
            int emberCount = 14;
            emberOverlays = new Image[emberCount];
            emberBasePos = new Vector2[emberCount];
            emberSpeed = new float[emberCount];
            emberPhase = new float[emberCount];
            for (int i = 0; i < emberCount; i++)
            {
                var emRt = UIFactory.Rect("Chispa_" + i, stageViewport);
                float sz = 10f + (i % 4) * 5f;
                UIFactory.PlaceCenter(emRt, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(sz, sz));
                var emImg = emRt.gameObject.AddComponent<Image>();
                emImg.raycastTarget = false;
                emImg.sprite = glowSprite;
                emImg.color = new Color(1f, 0.82f, 0.34f, 0.55f);
                emberOverlays[i] = emImg;
                emberBasePos[i] = new Vector2(-580f + (i * 88f), -220f + (i * 37) % 340);
                emberSpeed[i] = 18f + (i % 5) * 7f;
                emberPhase[i] = i * 1.13f;
            }

            // Cinta de capítulo en la parte superior del marco de la ilustración
            var ribbonRt = UIFactory.Rect("CintaActo", frameOuter);
            UIFactory.PlaceCenter(ribbonRt, new Vector2(0.5f, 1f), new Vector2(0f, -25f), new Vector2(1060f, 44f));
            UIFactory.FullScreen(ribbonRt, "FondoCinta", new Color(0.14f, 0.08f, 0.04f, 0.95f));
            UIFactory.Box(ribbonRt, "BordeCintaSup", new Color(0.92f, 0.74f, 0.28f, 0.9f), new Vector2(0f, 1f), Vector2.zero, new Vector2(1060f, 2.5f));
            UIFactory.Box(ribbonRt, "BordeCintaInf", new Color(0.92f, 0.74f, 0.28f, 0.9f), new Vector2(0f, 0f), Vector2.zero, new Vector2(1060f, 2.5f));
            actBadgeText = UIFactory.Label(ribbonRt, "TextoCinta", "", 24, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            actBadgeText.alignByGeometry = true;
            UIFactory.Stretch(actBadgeText.rectTransform);

            // Panel inferior de narración histórica amplio y bien espaciado para evitar cualquier superposición (1620 x 280)
            var storyPanel = UIFactory.Rect("PanelNarracion", holder);
            UIFactory.PlaceCenter(storyPanel, new Vector2(0.5f, 0f), new Vector2(0f, 228f), new Vector2(1620f, 280f));
            UIFactory.FullScreen(storyPanel, "SombraMarco", new Color(0.88f, 0.68f, 0.24f, 0.92f));

            var storyInner = UIFactory.Rect("InteriorNarracion", storyPanel);
            UIFactory.PlaceCenter(storyInner, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1612f, 272f));
            UIFactory.FullScreen(storyInner, "FondoPergaminoOscuro", new Color(0.11f, 0.06f, 0.03f, 0.97f));

            actTitleText = UIFactory.Label(storyInner, "TituloActo", "", 40, UIFactory.Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true, titleFont);
            UIFactory.PlaceCenter(actTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -29f), new Vector2(1520f, 46f));

            UIFactory.Box(storyInner, "SeparadorTitulo", new Color(0.92f, 0.74f, 0.28f, 0.45f), new Vector2(0.5f, 1f), new Vector2(0f, -56f), new Vector2(1020f, 2.5f));

            narrativeText = UIFactory.Label(storyInner, "TextoNarracion", "", 29, UIFactory.Cream, TextAnchor.UpperCenter, FontStyle.Bold, true);
            UIFactory.PlaceCenter(narrativeText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(1540f, 132f));

            quoteText = UIFactory.Label(storyInner, "CitaHistorica", "", 25, new Color(1f, 0.85f, 0.42f, 0.98f), TextAnchor.MiddleCenter, FontStyle.BoldAndItalic, true);
            UIFactory.PlaceCenter(quoteText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 26f), new Vector2(1540f, 40f));

            // Barra inferior de controles y progreso general de 60 segundos
            Image progFill;
            UIFactory.MakeBar(holder, "BarraProgresoTotal", new Color(0.94f, 0.74f, 0.24f, 1f),
                new Vector2(0f, 0f), new Vector2(52f, 28f), new Vector2(880f, 24f), out progFill);
            totalProgressBar = progFill;

            // Botón Siguiente Acto y Botón Saltar Cinemática con fuente grande y centrada
            var nextBtn = UIFactory.MakeButton(holder, "SIGUIENTE ACTO  [ESPACIO]", new Vector2(235f, -498f), new Vector2(440f, 56f), OnNextClicked);
            nextBtnLabel = nextBtn.GetComponentInChildren<Text>();
            if (nextBtnLabel != null)
            {
                nextBtnLabel.fontSize = 26;
                nextBtnLabel.alignByGeometry = true;
            }

            var skipBtn = UIFactory.MakeButton(holder, "SALTAR CINEMÁTICA >>  [ESC]", new Vector2(695f, -498f), new Vector2(440f, 56f), OnSkipClicked);
            var skipLbl = skipBtn.GetComponentInChildren<Text>();
            if (skipLbl != null)
            {
                skipLbl.fontSize = 26;
                skipLbl.alignByGeometry = true;
            }
        }

        void OnNextClicked()
        {
            nextActRequested = true;
        }

        void OnSkipClicked()
        {
            skipRequested = true;
        }

        static bool AdvanceKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && (k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                return true;
            var g = Gamepad.current;
            if (g != null && g.buttonSouth.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
                return true;
#endif
            return false;
        }

        static bool SkipKeyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k != null && k.escapeKey.wasPressedThisFrame)
                return true;
            var g = Gamepad.current;
            if (g != null && g.startButton.wasPressedThisFrame)
                return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
                return true;
#endif
            return false;
        }

        void UpdateAnimatedEffects(StoryAct act, float globalElapsed)
        {
            // Animar cuadros de fuego en las antorchas/braseros del acto actual
            if (flameOverlays != null)
            {
                int frameIdx = ((int)(globalElapsed * 10f)) % 4;
                Sprite fSp = (fireFrames != null && fireFrames[frameIdx] != null) ? fireFrames[frameIdx] : glowSprite;
                float pulse = 0.92f + 0.14f * Mathf.Sin(globalElapsed * 9.5f);

                for (int i = 0; i < flameOverlays.Length; i++)
                {
                    if (flameOverlays[i] == null) continue;
                    if (act.flameNormalizedPositions != null && i < act.flameNormalizedPositions.Length)
                    {
                        flameOverlays[i].gameObject.SetActive(true);
                        flameOverlays[i].sprite = fSp;
                        Vector2 norm = act.flameNormalizedPositions[i];
                        Vector2 vpSize = stageViewport.sizeDelta;
                        flameOverlays[i].rectTransform.anchoredPosition = new Vector2((norm.x - 0.5f) * vpSize.x, (norm.y - 0.5f) * vpSize.y);
                        flameOverlays[i].rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                    }
                    else
                    {
                        flameOverlays[i].gameObject.SetActive(false);
                    }
                }
            }

            // Animar brasas doradas flotantes
            if (emberOverlays != null)
            {
                for (int i = 0; i < emberOverlays.Length; i++)
                {
                    if (emberOverlays[i] == null) continue;
                    float yOff = (globalElapsed * emberSpeed[i] + i * 43f) % 520f;
                    float xSway = Mathf.Sin(globalElapsed * 1.8f + emberPhase[i]) * 26f;
                    float y = -260f + yOff;
                    float alpha = Mathf.Sin(Mathf.Clamp01(yOff / 520f) * Mathf.PI) * 0.62f;
                    emberOverlays[i].rectTransform.anchoredPosition = new Vector2(emberBasePos[i].x + xSway, y);
                    emberOverlays[i].color = new Color(1f, 0.84f, 0.36f, alpha);
                }
            }
        }

        IEnumerator RunCinematic(GameSession gs)
        {
            IsPlaying = true;
            if (activeActs == null) activeActs = Acts;
            float activeTotalDuration = activeActs.Length * ActDuration;
            int maxSec = Mathf.RoundToInt(activeTotalDuration);
            string totalTimeLabel = (maxSec / 60).ToString("D2") + ":" + (maxSec % 60).ToString("D2");

            LoadAssets(gs);
            BuildUI();

            // Crear objetivo móvil para que la cámara del nivel recorra suavemente el pueblo de Dolores de fondo
            var camTargetGo = new GameObject("CamaraTourCinematica");
            camTargetGo.transform.SetParent(transform, false);
            float startCamX = activeActs[0].worldCamX;
            camTargetGo.transform.position = new Vector3(startCamX, 1.8f, 0f);
            if (gs.cam != null)
            {
                gs.cam.FocusOn(camTargetGo.transform, 5.2f);
                gs.cam.Snap();
            }

            // Fundido de entrada de la cinemática
            float fadeIn = 0f;
            while (fadeIn < 1f)
            {
                fadeIn += Time.unscaledDeltaTime * 1.8f;
                rootGroup.alpha = Mathf.Clamp01(fadeIn);
                yield return null;
            }
            rootGroup.alpha = 1f;

            float totalElapsed = 0f;

            for (int actIdx = 0; actIdx < activeActs.Length; actIdx++)
            {
                if (skipRequested) break;

                StoryAct act = activeActs[actIdx];
                actBadgeText.text = act.badge;
                actTitleText.text = act.title;
                quoteText.text = "";
                narrativeText.text = "";

                if (nextBtnLabel != null)
                {
                    if (actIdx == activeActs.Length - 1)
                    {
                        nextBtnLabel.text = isEndingCinematic
                            ? "VER RESULTADOS  [ESPACIO]"
                            : "¡COMENZAR AVENTURA!  [ESPACIO]";
                    }
                    else
                    {
                        nextBtnLabel.text = "SIGUIENTE ACTO  [ESPACIO]";
                    }
                }

                // Actualizar indicadores de actos
                for (int d = 0; d < actDots.Length; d++)
                {
                    if (actDots[d] == null) continue;
                    actDots[d].color = (d < actIdx)
                        ? new Color(0.35f, 0.85f, 0.45f, 1f)
                        : ((d == actIdx) ? UIFactory.Gold : new Color(0.25f, 0.18f, 0.12f, 0.65f));
                }

                // Preparar transición entre imágenes de escenario
                Sprite nextSp = actSprites[actIdx];
                if (actIdx == 0)
                {
                    stageImageA.sprite = nextSp;
                    stageImageA.color = Color.white;
                    stageImageB.color = new Color(1f, 1f, 1f, 0f);
                }
                else
                {
                    stageImageB.sprite = stageImageA.sprite;
                    stageImageB.color = Color.white;
                    stageRectB.anchoredPosition = stageRectA.anchoredPosition;
                    stageRectB.localScale = stageRectA.localScale;

                    stageImageA.sprite = nextSp;
                    stageImageA.color = new Color(1f, 1f, 1f, 0f);
                }

                AudioHub.Play(act.sfx, 0.03f, 0.85f);
                if ((!isEndingCinematic && actIdx == 4) || (isEndingCinematic && (actIdx == 0 || actIdx == 2)))
                {
                    // Repique solemne de la Campana de Dolores
                    StartCoroutine(DelayedBellChime(1.6f));
                }

                float actElapsed = 0f;
                float typeDuration = 6.0f; // Primeros 6 segundos escriben la historia, 4 segundos para contemplar la escena
                int lastCharCount = 0;
                bool textCompleted = false;
                nextActRequested = false;

                float prevWorldX = (actIdx == 0) ? activeActs[0].worldCamX : activeActs[actIdx - 1].worldCamX;
                float targetWorldX = act.worldCamX;

                while (actElapsed < ActDuration)
                {
                    if (skipRequested || SkipKeyPressed())
                    {
                        skipRequested = true;
                        break;
                    }

                    float dt = Time.unscaledDeltaTime;
                    actElapsed += dt;
                    totalElapsed = actIdx * ActDuration + Mathf.Min(actElapsed, ActDuration);

                    // Comprobar si el usuario presionó Espacio/Enter/Clic en Siguiente
                    if (nextActRequested || AdvanceKeyPressed())
                    {
                        nextActRequested = false;
                        if (!textCompleted)
                        {
                            // Si aún se está escribiendo el texto, mostrar todo el texto del acto de inmediato
                            textCompleted = true;
                            narrativeText.text = act.narrative;
                            quoteText.text = act.quote;
                            AudioHub.Play(SfxId.Click, 0.02f, 0.6f);
                        }
                        else
                        {
                            // Si el texto ya estaba completo, avanzar al siguiente acto
                            AudioHub.Play(SfxId.Click, 0.02f, 0.8f);
                            break;
                        }
                    }

                    // Fundido cruzado de 0.8s entre actos
                    float fadeU = (actIdx == 0) ? 1f : Mathf.Clamp01(actElapsed / 0.8f);
                    stageImageA.color = new Color(1f, 1f, 1f, fadeU);
                    stageImageB.color = new Color(1f, 1f, 1f, 1f - fadeU);

                    // Movimiento Ken Burns (paneo + zoom suave durante los 10 segundos del acto)
                    float u = Mathf.Clamp01(actElapsed / ActDuration);
                    float smoothU = u * u * (3f - 2f * u);
                    stageRectA.anchoredPosition = Vector2.Lerp(act.panStart, act.panEnd, smoothU);
                    float sc = Mathf.Lerp(act.zoomStart, act.zoomEnd, smoothU);
                    stageRectA.localScale = new Vector3(sc, sc, 1f);

                    // Mover suavemente la cámara del nivel en el fondo
                    if (camTargetGo != null)
                    {
                        float wx = Mathf.Lerp(prevWorldX, targetWorldX, smoothU);
                        camTargetGo.transform.position = new Vector3(wx, 1.8f, 0f);
                    }

                    // Efecto máquina de escribir para la narración histórica
                    if (!textCompleted)
                    {
                        float typeU = Mathf.Clamp01(actElapsed / typeDuration);
                        int charCount = Mathf.RoundToInt(typeU * act.narrative.Length);
                        if (charCount != lastCharCount)
                        {
                            narrativeText.text = act.narrative.Substring(0, Mathf.Clamp(charCount, 0, act.narrative.Length));
                            if (charCount > lastCharCount && charCount % 5 == 0)
                            {
                                AudioHub.PlayPitched(SfxId.Hover, 0.95f + (charCount % 3) * 0.04f, 0.22f);
                            }
                            lastCharCount = charCount;
                        }
                        if (typeU >= 1f)
                        {
                            textCompleted = true;
                            narrativeText.text = act.narrative;
                            quoteText.text = act.quote;
                        }
                    }

                    // Actualizar reloj (00:00 / 01:00 o 00:00 / 00:30) y barra de progreso
                    int curSec = Mathf.Clamp(Mathf.FloorToInt(totalElapsed), 0, maxSec);
                    int curM = curSec / 60;
                    int curS = curSec % 60;
                    if (timerText != null)
                        timerText.text = curM.ToString("D2") + ":" + curS.ToString("D2") + " / " + totalTimeLabel;
                    if (totalProgressBar != null)
                        UIFactory.SetBar(totalProgressBar, Mathf.Clamp01(totalElapsed / activeTotalDuration));

                    UpdateAnimatedEffects(act, totalElapsed);
                    yield return null;
                }
            }

            // Devolver la cámara suavemente al jugador al terminar la cinemática de introducción
            if (!isEndingCinematic && gs.cam != null)
            {
                gs.cam.ClearFocus();
                gs.cam.Snap();
            }

            // Fundido de salida de la cinemática
            float fadeOut = 1f;
            while (fadeOut > 0f)
            {
                fadeOut -= Time.unscaledDeltaTime * 2.2f;
                rootGroup.alpha = Mathf.Clamp01(fadeOut);
                yield return null;
            }
            rootGroup.alpha = 0f;
            IsPlaying = false;
        }

        IEnumerator DelayedBellChime(float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                if (skipRequested) yield break;
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            AudioHub.Play(SfxId.Bell, 0.02f, 0.9f);
        }
    }
}
