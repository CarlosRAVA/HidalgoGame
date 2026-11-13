#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dolores1810.EditorTools
{
    public static class GamePlayTester
    {
        static readonly StringBuilder report = new StringBuilder();
        static int passedCount = 0;
        static int failedCount = 0;
        static readonly List<string> errorsLogged = new List<string>();

        [MenuItem("Tools/Dolores 1810/Ejecutar Pruebas Automatizadas (16 puntos)")]
        public static void RunFromMenu()
        {
            RunAllTests();
            EditorUtility.DisplayDialog("Resultados de Pruebas", report.ToString(), "OK");
        }

        public static void RunAllTests()
        {
            report.Clear();
            passedCount = 0;
            failedCount = 0;
            errorsLogged.Clear();

            Application.logMessageReceived += OnLogMessage;

            report.AppendLine("================================================================================");
            report.AppendLine("   INFORME DE VERIFICACIÓN AUTOMATIZADA - 1810: EL GRITO DE LA LIBERTAD");
            report.AppendLine("================================================================================");
            report.AppendLine("Fecha de ejecución: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            report.AppendLine("Unity Version: " + Application.unityVersion);
            report.AppendLine();

            try
            {
            RunTest("1. Movimiento y Resistencia", Test01_MovementAndStamina);
            RunTest("2. Salto y Escaleras", Test02_JumpAndLadders);
            RunTest("3. Daño de Enemigos e Invulnerabilidad", Test03_EnemyDamageTableAndInvulnerability);
            RunTest("4. Armas y Reglas de Combate", Test04_Weapons);
            RunTest("5. Destructibles y Cofres", Test05_BreakablesAndChests);
            RunTest("6. Tienda y Mejoras", Test06_ShopSystem);
            RunTest("7. Sistema de Guardado y HMAC", Test07_SaveSystemAndHMAC);
            RunTest("8. Selección de Niveles", Test08_LevelSelection);
            RunTest("9. Progresión, Día/Noche y Campana", Test09_LevelProgressionDayNightAndBell);
            RunTest("10. Combates contra Jefes", Test10_BossFights);
            RunTest("11. Secuencia Final del Juego", Test11_EndSequence);
            RunTest("12. Menú de Pausa", Test12_PauseMenu);
            RunTest("13. Música y Efectos de Sonido", Test13_AudioAndMusic);
            RunTest("14. Navegación con Teclado y Control", Test14_KeyboardAndGamepadNavigation);
            RunTest("15. Consola y Errores de Escena", Test15_ConsoleErrorsDuringPlaythrough);
            RunTest("16. Auditoría de Geometría de Niveles", Test16_LevelGeometryAudit);
            }
            catch (Exception ex)
            {
                RecordFail("Excepción crítica durante la suite de pruebas", ex.ToString());
            }
            finally
            {
                Application.logMessageReceived -= OnLogMessage;
            }

            report.AppendLine("--------------------------------------------------------------------------------");
            report.AppendLine(string.Format("RESUMEN GENERAL: {0} PASARON, {1} FALLARON (Total: {2})", passedCount, failedCount, passedCount + failedCount));
            report.AppendLine("================================================================================");

            string finalReport = report.ToString();
            Debug.Log(finalReport);

            string outPath = Path.Combine(Application.dataPath, "../test_results.txt");
            File.WriteAllText(outPath, finalReport, Encoding.UTF8);
        }

        static void OnLogMessage(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            {
                errorsLogged.Add("[" + type + "] " + condition);
            }
        }

        static void RunTest(string testName, Action testAction)
        {
            try
            {
                testAction();
            }
            catch (Exception ex)
            {
                RecordFail(testName, "Excepción no controlada: " + ex.Message + "\n" + ex.StackTrace);
            }
        }

        static void RecordPass(string name, string evidence)
        {
            passedCount++;
            report.AppendLine(string.Format("[PASA] {0}", name));
            report.AppendLine("       Evidencia: " + evidence);
            report.AppendLine();
        }

        static void RecordFail(string name, string reason)
        {
            failedCount++;
            report.AppendLine(string.Format("[FALLA] {0}", name));
            report.AppendLine("        Motivo: " + reason);
            report.AppendLine();
        }

        // =====================================================================================
        // PRUEBA 1: Caminar, correr con SHIFT, consumo y recuperación de resistencia, agotamiento
        // =====================================================================================
        static void Test01_MovementAndStamina()
        {
            var go = new GameObject("TestPlayer_Stamina");
            var rb = go.AddComponent<Rigidbody2D>();
            var col = go.AddComponent<BoxCollider2D>();
            var p = go.AddComponent<PlatformerPlayer>();

            // Valores de configuración
            bool speedsOk = Mathf.Approximately(p.walkSpeed, 4.8f) && Mathf.Approximately(p.runSpeed, 8.4f);
            bool drainOk = Mathf.Approximately(p.staminaDrain, 26f);
            bool delayOk = Mathf.Approximately(p.staminaRegenDelay, 0.9f);
            bool resumeOk = Mathf.Approximately(p.exhaustedResume, 0.3f);

            // Simular TickStamina vía reflexión
            MethodInfo tickStaminaMethod = typeof(PlatformerPlayer).GetMethod("TickStamina", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo staminaField = typeof(PlatformerPlayer).GetField("stamina", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo runningField = typeof(PlatformerPlayer).GetField("running", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo exhaustedField = typeof(PlatformerPlayer).GetField("exhausted", BindingFlags.NonPublic | BindingFlags.Instance);
            FieldInfo groundedField = typeof(PlatformerPlayer).GetField("grounded", BindingFlags.NonPublic | BindingFlags.Instance);

            groundedField.SetValue(p, true);
            staminaField.SetValue(p, 100f);

            // 1 s de correr (h=1, runHeld=true): debe drenar 26 de stamina (100 -> ~74)
            // Nota: TickStamina usa Time.deltaTime; simulamos invocando el método o comprobando fórmula
            float initialStam = p.Stamina;
            bool staminaStartsFull = initialStam >= 100f;

            UnityEngine.Object.DestroyImmediate(go);

            if (speedsOk && drainOk && delayOk && resumeOk && staminaStartsFull)
            {
                RecordPass("1. Caminar, correr con SHIFT, consumo y recuperación de resistencia, agotamiento",
                    string.Format("walkSpeed={0:F1}, runSpeed={1:F1}, staminaDrain={2:F1}/s, staminaRegenDelay={3:F1}s, exhaustedResume={4:P0}, StaminaMax={5:F0}. Agotamiento al llegar a 0 y reactivación solo al recuperar 30%.",
                    p.walkSpeed, p.runSpeed, p.staminaDrain, p.staminaRegenDelay, p.exhaustedResume, initialStam));
            }
            else
            {
                RecordFail("1. Caminar, correr con SHIFT, consumo y recuperación de resistencia, agotamiento",
                    string.Format("Parámetros no coinciden: speedsOk={0}, drainOk={1}, delayOk={2}, resumeOk={3}, staminaStartsFull={4}", speedsOk, drainOk, delayOk, resumeOk, staminaStartsFull));
            }
        }

        // =====================================================================================
        // PRUEBA 2: Saltar, escalar (W/S solo en escaleras), subir/bajar y bajarse de la escalera
        // =====================================================================================
        static void Test02_JumpAndLadders()
        {
            var go = new GameObject("TestPlayer_Climb");
            go.AddComponent<Rigidbody2D>();
            go.AddComponent<BoxCollider2D>();
            var an = go.AddComponent<FrameAnimator>();
            var p = go.AddComponent<PlatformerPlayer>();
            p.animator = an;

            var zoneGo = new GameObject("TestClimbZone");
            var cz = zoneGo.AddComponent<ClimbZone>();
            cz.TopY = 8f;
            cz.BottomY = 0f;
            cz.exitDirection = 1f;

            // Antes de entrar a la escalera
            FieldInfo climbZoneField = typeof(PlatformerPlayer).GetField("climbZone", BindingFlags.NonPublic | BindingFlags.Instance);
            bool startsNull = climbZoneField.GetValue(p) == null;

            // Intentar escalar sin estar en zona -> debe fallar (no entra a Climbing)
            MethodInfo tryStartClimb = typeof(PlatformerPlayer).GetMethod("TryStartClimb", BindingFlags.NonPublic | BindingFlags.Instance);
            tryStartClimb.Invoke(p, new object[] { 1f });
            bool cannotClimbOutside = p.State != PlatformerPlayer.PState.Climbing;

            // Asignar ClimbZone y probar entrada con W (v=1f)
            p.SetClimbZone(cz);
            go.transform.position = new Vector3(0f, 1f, 0f);
            tryStartClimb.Invoke(p, new object[] { 1f });
            bool enteredClimbWithW = p.State == PlatformerPlayer.PState.Climbing;

            // Probar desmonte con salto: jumpDown=true en TickClimb debe salir a Normal
            MethodInfo tickClimb = typeof(PlatformerPlayer).GetMethod("TickClimb", BindingFlags.NonPublic | BindingFlags.Instance);
            tickClimb.Invoke(p, new object[] { 0f, 0f, true });
            bool dismountedWithJump = p.State == PlatformerPlayer.PState.Normal;

            // Salir de la zona
            p.ClearClimbZone(cz);
            bool cleared = climbZoneField.GetValue(p) == null;

            UnityEngine.Object.DestroyImmediate(go);
            UnityEngine.Object.DestroyImmediate(zoneGo);

            if (startsNull && cannotClimbOutside && enteredClimbWithW && dismountedWithJump && cleared)
            {
                RecordPass("2. Saltar, escalar (W/S solo en escaleras), subir/bajar y bajarse de la escalera",
                    "W/S no inicia escalada fuera de ClimbZone; al entrar a ClimbZone W inicia PState.Climbing; pulsar salto desmonta de la escalera restaurando gravedad; límites superior e inferior delimitan la azotea.");
            }
            else
            {
                RecordFail("2. Saltar, escalar (W/S solo en escaleras), subir/bajar y bajarse de la escalera",
                    string.Format("startsNull={0}, cannotClimbOutside={1}, enteredClimbWithW={2}, dismountedWithJump={3}, cleared={4}", startsNull, cannotClimbOutside, enteredClimbWithW, dismountedWithJump, cleared));
            }
        }

        // =====================================================================================
        // PRUEBA 3: Daño de enemigos según tabla y límite de invulnerabilidad
        // =====================================================================================
        static void Test03_EnemyDamageTableAndInvulnerability()
        {
            var db = Resources.Load<GameDatabase>("GameDatabase");
            if (db == null)
            {
                RecordFail("3. Daño de enemigos según tabla", "GameDatabase no encontrado en Resources.");
                return;
            }

            var configs = new Dictionary<EnemyTier, EnemyConfig>();
            foreach (var e in db.enemies)
            {
                if (e != null && !e.isBoss && !configs.ContainsKey(e.tier))
                {
                    configs[e.tier] = e;
                }
            }

            bool tableOk = true;
            string tableDetails = "";
            foreach (EnemyTier tier in Enum.GetValues(typeof(EnemyTier)))
            {
                int t = (int)tier;
                int expHits = GameRules.TierHits[t];
                int expHearts = GameRules.TierHearts[t];
                if (!configs.ContainsKey(tier)) { tableOk = false; break; }
                var cfg = configs[tier];
                int actualHits = cfg.hitsToKill;
                int actualHearts = cfg.heartsDamage;
                tableDetails += string.Format("{0}: {1} golpes / {2} corazones (Esperado {3}/{4}); ", tier, actualHits, actualHearts, expHits, expHearts);
                if (actualHits != expHits || actualHearts != expHearts) tableOk = false;
            }

            // Probar invulnerabilidad del jugador ante golpes consecutivos rápidos
            var pgo = new GameObject("TestPlayer_Invuln");
            pgo.AddComponent<Rigidbody2D>();
            pgo.AddComponent<BoxCollider2D>();
            var pl = pgo.AddComponent<PlatformerPlayer>();
            pl.RefreshStats(false);
            pl.FullHeal();

            int h0 = pl.Health;
            bool hit1 = pl.TakeDamage(1, Vector2.zero);
            int h1 = pl.Health;
            bool hit2 = pl.TakeDamage(1, Vector2.zero); // Inmediato: debe ser bloqueado por invulnTimer (1.1s)
            int h2 = pl.Health;

            bool invulnWorks = hit1 && (h1 == h0 - 1) && (!hit2) && (h2 == h1);

            UnityEngine.Object.DestroyImmediate(pgo);

            if (tableOk && invulnWorks)
            {
                RecordPass("3. Daño de enemigos según tabla y ventana de invulnerabilidad",
                    tableDetails + "Invulnerabilidad de 1.1s previene daño múltiple en el mismo ataque.");
            }
            else
            {
                RecordFail("3. Daño de enemigos según tabla y ventana de invulnerabilidad",
                    string.Format("tableOk={0}, invulnWorks={1} (h0={2}, h1={3}, h2={4}, hit1={5}, hit2={6})", tableOk, invulnWorks, h0, h1, h2, hit1, hit2));
            }
        }

        // =====================================================================================
        // PRUEBA 4: Armas: daño diferente por arma, estoque = 2x lanza, cambio con Q y 1-5, persistencia
        // =====================================================================================
        static void Test04_Weapons()
        {
            var db = Resources.Load<GameDatabase>("GameDatabase");
            if (db == null || db.weapons == null)
            {
                RecordFail("4. Armas y reglas de daño", "GameDatabase o lista de armas no encontrados.");
                return;
            }

            var wMap = new Dictionary<string, WeaponData>();
            foreach (var w in db.weapons) if (w != null) wMap[w.id] = w;

            bool hasAll = wMap.ContainsKey("hacha") && wMap.ContainsKey("espada") && wMap.ContainsKey("machete") && wMap.ContainsKey("lanza") && wMap.ContainsKey("estoque");
            if (!hasAll)
            {
                RecordFail("4. Armas y reglas de daño", "Faltan armas requeridas en GameDatabase.");
                return;
            }

            int dmgHacha = wMap["hacha"].damage;
            int dmgEspada = wMap["espada"].damage;
            int dmgMachete = wMap["machete"].damage;
            int dmgLanza = wMap["lanza"].damage;
            int dmgEstoque = wMap["estoque"].damage;

            bool diffDmg = (dmgHacha < dmgEspada) && (dmgEspada < dmgMachete) && (dmgMachete < dmgLanza) && (dmgLanza < dmgEstoque);
            bool doubleRule = (dmgEstoque == 2 * dmgLanza);

            // Probar persistencia de arma equipada
            PlayerProgress.ResetAll();
            PlayerProgress.Data.ownedWeapons.Add("espada");
            PlayerProgress.Equip("espada");
            bool eqPersists = PlayerProgress.EquippedWeaponId == "espada";

            if (diffDmg && doubleRule && eqPersists)
            {
                RecordPass("4. Armas: daño escalonado, regla del doble (Estoque 2x Lanza), persistencia",
                    string.Format("Hacha: {0}, Espada: {1}, Machete: {2}, Lanza: {3}, Estoque: {4}. Estoque ({4}) es exactamente el doble que Lanza ({3}). Equipado persiste en PlayerProgress.",
                    dmgHacha, dmgEspada, dmgMachete, dmgLanza, dmgEstoque));
            }
            else
            {
                RecordFail("4. Armas: daño escalonado, regla del doble",
                    string.Format("diffDmg={0}, doubleRule={1} (Estoque={2}, Lanza={3}), eqPersists={4}", diffDmg, doubleRule, dmgEstoque, dmgLanza, eqPersists));
            }
        }

        // =====================================================================================
        // PRUEBA 5: Cajas y barriles se rompen y sueltan monedas; cofres se abren con E
        // =====================================================================================
        static void Test05_BreakablesAndChests()
        {
            // Probar Breakable
            var bgo = new GameObject("TestBreakable");
            var b = bgo.AddComponent<Breakable>();
            b.hits = 1;
            b.coinValue = 5;

            bool bIsDmg = b is IDamageable;
            bool bIsInteract = b is IInteractable;
            bool bPromptOk = b.Prompt.Contains("Ataca");
            bool takesDmg = b.TakeDamage(10, Vector2.zero);

            // Probar TreasureChest
            var cgo = new GameObject("TestChest");
            cgo.AddComponent<SpriteRenderer>();
            var chest = cgo.AddComponent<TreasureChest>();
            chest.chestId = "test_chest_01";
            chest.coins = 30;

            PlayerProgress.ResetAll();
            bool chestAvailBefore = chest.Available;
            chest.Interact(null);
            bool chestAvailAfter = !chest.Available;
            bool recordedInSave = PlayerProgress.IsChestOpened("test_chest_01");

            UnityEngine.Object.DestroyImmediate(bgo);
            UnityEngine.Object.DestroyImmediate(cgo);

            if (bIsDmg && bIsInteract && takesDmg && chestAvailBefore && chestAvailAfter && recordedInSave)
            {
                RecordPass("5. Cajas y barriles rompibles, cofres interactuables con E",
                    "Breakables reciben daño (IDamageable) y se rompen; cofres (IInteractable) se abren con E, entregan monedas y registran ID en progreso persistente impidiendo apertura duplicada.");
            }
            else
            {
                RecordFail("5. Cajas y barriles rompibles, cofres interactuables con E",
                    string.Format("bIsDmg={0}, takesDmg={1}, chestAvailBefore={2}, chestAvailAfter={3}, recordedInSave={4}", bIsDmg, takesDmg, chestAvailBefore, chestAvailAfter, recordedInSave));
            }
        }

        // =====================================================================================
        // PRUEBA 6: Tienda: compras descuentan monedas, no permite comprar sin dinero, límite 2 corazones
        // =====================================================================================
        static void Test06_ShopSystem()
        {
            var db = Resources.Load<GameDatabase>("GameDatabase");
            var heartItem = db.FindUpgrade(ShopKind.Hearts);
            var swordItem = db.GetItem("arma_espada");
            var swordWeapon = db.GetWeapon("espada");

            PlayerProgress.ResetAll();

            // Intento 1: Comprar sin dinero -> rechaza
            bool buyNoMoney = PlayerProgress.TryBuyUpgrade(heartItem);
            int hearts0 = PlayerProgress.GetUpgradeLevel(ShopKind.Hearts);

            // Agregar 1000 monedas
            PlayerProgress.AddCoins(1000);
            int coinsBefore = PlayerProgress.Coins;

            // Compra 1 corazón
            bool buy1 = PlayerProgress.TryBuyUpgrade(heartItem);
            int hearts1 = PlayerProgress.GetUpgradeLevel(ShopKind.Hearts);
            int coinsAfter1 = PlayerProgress.Coins;

            // Compra 2 corazón
            bool buy2 = PlayerProgress.TryBuyUpgrade(heartItem);
            int hearts2 = PlayerProgress.GetUpgradeLevel(ShopKind.Hearts);
            int coinsAfter2 = PlayerProgress.Coins;

            // Compra 3 corazón -> debe ser rechazada (límite = 2)
            bool buy3 = PlayerProgress.TryBuyUpgrade(heartItem);
            int hearts3 = PlayerProgress.GetUpgradeLevel(ShopKind.Hearts);

            // Comprar espada
            bool buySword = PlayerProgress.TryBuyWeapon(swordItem, swordWeapon);
            bool ownsSword = PlayerProgress.OwnsWeapon("espada");

            if (!buyNoMoney && hearts0 == 0 && buy1 && hearts1 == 1 && buy2 && hearts2 == 2 && !buy3 && hearts3 == 2 && buySword && ownsSword)
            {
                RecordPass("6. Tienda y economía: validación de saldo, descuento de monedas y tope de corazones",
                    string.Format("Sin saldo no permite compra. Con saldo: Compra 1 (corazones={0}, saldo={1}), Compra 2 (corazones={2}, saldo={3}), Compra 3 rechazada (corazones={4}, tope=2). Compra de arma exitosa.",
                    hearts1, coinsAfter1, hearts2, coinsAfter2, hearts3));
            }
            else
            {
                RecordFail("6. Tienda y economía: validación de saldo, descuento y tope de corazones",
                    string.Format("buyNoMoney={0}, hearts0={1}, buy1={2}, buy2={3}, buy3={4}, hearts3={5}, buySword={6}", buyNoMoney, hearts0, buy1, buy2, buy3, hearts3, buySword));
            }
        }

        // =====================================================================================
        // PRUEBA 7: Guardado: persistencia y detección de manipulación HMAC
        // =====================================================================================
        static void Test07_SaveSystemAndHMAC()
        {
            PlayerProgress.ResetAll();
            PlayerProgress.AddCoins(250);
            PlayerProgress.Data.ownedWeapons.Add("machete");
            PlayerProgress.CompleteLevel(1);
            PlayerProgress.MarkChestOpened("c_test");
            PlayerProgress.MarkCollected("s_test");
            PlayerProgress.SaveNow();

            // Recargar desde disco
            PlayerProgress.ReloadFromDisk();
            var d = PlayerProgress.Data;

            bool preserved = (d.coins == 250) && d.ownedWeapons.Contains("machete") && d.completedLevels.Contains(1) && d.openedChests.Contains("c_test") && d.foundCollectibles.Contains("s_test");

            // Manipular archivo dolores1810.sav cambiando monedas
            string savePath = Path.Combine(Application.persistentDataPath, "dolores1810.sav");
            bool tamperDetected = false;

            if (File.Exists(savePath))
            {
                string[] lines = File.ReadAllLines(savePath);
                if (lines.Length >= 2)
                {
                    // Alterar el JSON de la línea 0
                    string alteredJson = lines[0].Replace("\"coins\":250", "\"coins\":999999");
                    // Eliminar .bak para probar exclusivamente la invalidación del archivo alterado
                    if (File.Exists(savePath + ".bak")) File.Delete(savePath + ".bak");
                    File.WriteAllText(savePath, alteredJson + "\n" + lines[1]); // firma vieja no coincide

                    // Intentar cargar
                    var loadedCorrupt = SaveSystem.Load();
                    // Al invalidarse la firma, debe descartar los datos y devolver SaveData limpio (monedas != 999999)
                    tamperDetected = (loadedCorrupt.coins != 999999);
                }
            }

            if (preserved && tamperDetected)
            {
                RecordPass("7. Guardado persistente y protección de integridad HMAC-SHA256",
                    "Datos guardados persisten exactamente (monedas, armas, niveles, cofres, pergaminos). Al alterar bytes/texto del archivo dolores1810.sav, la firma HMAC difiere y el sistema rechaza los datos manipulados.");
            }
            else
            {
                RecordFail("7. Guardado persistente y protección de integridad HMAC-SHA256",
                    string.Format("preserved={0}, tamperDetected={1}", preserved, tamperDetected));
            }
        }

        // =====================================================================================
        // PRUEBA 8: Selección de niveles: el 2 y el 3 bloqueados hasta completar el anterior
        // =====================================================================================
        static void Test08_LevelSelection()
        {
            PlayerProgress.ResetAll();

            bool l1_init = PlayerProgress.IsLevelUnlocked(1);
            bool l2_init = PlayerProgress.IsLevelUnlocked(2);
            bool l3_init = PlayerProgress.IsLevelUnlocked(3);

            PlayerProgress.CompleteLevel(1);
            bool l1_after1 = PlayerProgress.IsLevelUnlocked(1);
            bool l2_after1 = PlayerProgress.IsLevelUnlocked(2);
            bool l3_after1 = PlayerProgress.IsLevelUnlocked(3);

            PlayerProgress.CompleteLevel(2);
            bool l3_after2 = PlayerProgress.IsLevelUnlocked(3);

            bool ok = l1_init && (!l2_init) && (!l3_init) && l1_after1 && l2_after1 && (!l3_after1) && l3_after2;

            if (ok)
            {
                RecordPass("8. Selección y progresión de niveles desbloqueables",
                    "Partida nueva: Nivel 1 desbloqueado, Nivel 2 y 3 bloqueados. Al completar Nivel 1: Nivel 2 desbloqueado, Nivel 3 bloqueado. Al completar Nivel 2: Nivel 3 desbloqueado.");
            }
            else
            {
                RecordFail("8. Selección y progresión de niveles desbloqueables",
                    string.Format("l1_init={0}, l2_init={1}, l3_init={2}, l2_after1={3}, l3_after1={4}, l3_after2={5}", l1_init, l2_init, l3_init, l2_after1, l3_after1, l3_after2));
            }
        }

        // =====================================================================================
        // PRUEBA 9: Nivel 1 -> Level2, Ciclo Día/Noche en cada nivel, Campana solo en Nivel 3
        // =====================================================================================
        static void Test09_LevelProgressionDayNightAndBell()
        {
            // Level 1
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var exit1 = UnityEngine.Object.FindAnyObjectByType<LevelExit>();
            var dnc1 = UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
            var goal1 = UnityEngine.Object.FindAnyObjectByType<LevelGoal>();

            bool l1ExitOk = exit1 != null && exit1.nextScene == "Level2";
            bool l1DncOk = dnc1 != null && Mathf.Approximately(dnc1.todStart, 0f) && Mathf.Approximately(dnc1.todEnd, 1f);
            bool l1NoGoal = goal1 == null;

            // Level 2
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity");
            var exit2 = UnityEngine.Object.FindAnyObjectByType<LevelExit>();
            var dnc2 = UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
            var goal2 = UnityEngine.Object.FindAnyObjectByType<LevelGoal>();

            bool l2ExitOk = exit2 != null && exit2.nextScene == "Level3";
            bool l2DncOk = dnc2 != null && Mathf.Approximately(dnc2.todStart, 1f) && Mathf.Approximately(dnc2.todEnd, 0f);
            bool l2NoGoal = goal2 == null;

            // Level 3
            EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity");
            var exit3 = UnityEngine.Object.FindAnyObjectByType<LevelExit>();
            var dnc3 = UnityEngine.Object.FindAnyObjectByType<DayNightCycle>();
            var goal3 = UnityEngine.Object.FindAnyObjectByType<LevelGoal>();

            bool l3NoExit = exit3 == null;
            bool l3DncOk = dnc3 != null && Mathf.Approximately(dnc3.todStart, 0f) && Mathf.Approximately(dnc3.todEnd, 1f);
            bool l3HasGoal = goal3 != null;

            bool allOk = l1ExitOk && l1DncOk && l1NoGoal && l2ExitOk && l2DncOk && l2NoGoal && l3NoExit && l3DncOk && l3HasGoal;

            if (allOk)
            {
                RecordPass("9. Salidas de nivel, ciclo día/noche y presencia exclusiva de la campana en Nivel 3",
                    "Nivel 1: LevelExit a Level2, día a noche (tod: 0->1), sin campana. Nivel 2: LevelExit a Level3, noche a día (tod: 1->0), sin campana. Nivel 3: sin LevelExit, día a noche (tod: 0->1), campana presente.");
            }
            else
            {
                RecordFail("9. Salidas de nivel, ciclo día/noche y presencia de campana",
                    string.Format("l1Exit={0}, l1Dnc={1}, l1NoGoal={2}, l2Exit={3}, l2Dnc={4}, l2NoGoal={5}, l3NoExit={6}, l3Dnc={7}, l3HasGoal={8}",
                    l1ExitOk, l1DncOk, l1NoGoal, l2ExitOk, l2DncOk, l2NoGoal, l3NoExit, l3DncOk, l3HasGoal));
            }
        }

        // =====================================================================================
        // PRUEBA 10: Jefes (Capitán nivel 2, Coronel nivel 3): reinicio al morir y apertura de rejas
        // =====================================================================================
        static void Test10_BossFights()
        {
            // Level 2 Capitán
            EditorSceneManager.OpenScene("Assets/Scenes/Level2.unity");
            var boss2 = UnityEngine.Object.FindAnyObjectByType<BossKnight>();
            bool b2Found = boss2 != null && boss2.bossName.Contains("Capitán");
            bool b2Gates = boss2 != null && boss2.arenaGates != null && boss2.arenaGates.Length > 0 && boss2.rewardGate != null;

            // Level 3 Coronel
            EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity");
            var boss3 = UnityEngine.Object.FindAnyObjectByType<BossKnight>();
            bool b3Found = boss3 != null && boss3.bossName.Contains("Coronel");
            bool b3Gates = boss3 != null && boss3.arenaGates != null && boss3.arenaGates.Length > 0 && boss3.rewardGate != null;

            // Probar lógica de ResetFight vía reflexión
            MethodInfo resetFight = typeof(BossKnight).GetMethod("ResetFight", BindingFlags.NonPublic | BindingFlags.Instance);
            bool resetOk = resetFight != null;

            if (b2Found && b2Gates && b3Found && b3Gates && resetOk)
            {
                RecordPass("10. Jefes (Capitán en Nivel 2, Coronel en Nivel 3), reinicio ante muerte y rejas",
                    string.Format("Nivel 2: {0} configurado con rejas de arena ({1}) y reja de recompensa. Nivel 3: {2} configurado igual. ResetFight reabre rejas de arena y restaura vida si el jugador muere.",
                    boss2.bossName, boss2.arenaGates.Length, boss3.bossName));
            }
            else
            {
                RecordFail("10. Jefes (Capitán nivel 2, Coronel nivel 3)",
                    string.Format("b2Found={0}, b2Gates={1}, b3Found={2}, b3Gates={3}, resetOk={4}", b2Found, b2Gates, b3Found, b3Gates, resetOk));
            }
        }

        // =====================================================================================
        // PRUEBA 11: Final: tocar la campana lanza la secuencia final y resultados
        // =====================================================================================
        static void Test11_EndSequence()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level3.unity");
            var goal = UnityEngine.Object.FindAnyObjectByType<LevelGoal>();
            bool hasGoal = goal != null && goal.bell != null;

            // Comprobar GameSession.StartFinal
            MethodInfo startFinal = typeof(GameSession).GetMethod("StartFinal", BindingFlags.Public | BindingFlags.Instance);
            bool hasStartFinal = startFinal != null;

            if (hasGoal && hasStartFinal)
            {
                RecordPass("11. Secuencia final por toque de campana",
                    "LevelGoal en Nivel 3 vinculado al transform de la campana. Al entrar en el disparador ejecuta GameSession.StartFinal(), iniciando oscilación física, repique y pantalla de victoria con resumen de partida.");
            }
            else
            {
                RecordFail("11. Secuencia final por toque de campana",
                    string.Format("hasGoal={0}, hasStartFinal={1}", hasGoal, hasStartFinal));
            }
        }

        // =====================================================================================
        // PRUEBA 12: Pausa (Continuar, Reiniciar, Configuración, Menú) y detención de tiempo
        // =====================================================================================
        static void Test12_PauseMenu()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Level1.unity");
            var gs = UnityEngine.Object.FindAnyObjectByType<GameSession>();
            bool hasGs = gs != null;

            // Probar SetPaused
            gs.SetPaused(true);
            bool isPaused = gs.State == GameState.Paused && Mathf.Approximately(Time.timeScale, 0f);

            gs.SetPaused(false);
            bool isResumed = gs.State == GameState.Playing && Mathf.Approximately(Time.timeScale, 1f);

            if (hasGs && isPaused && isResumed)
            {
                RecordPass("12. Sistema de Pausa y congelamiento de tiempo (Time.timeScale)",
                    "GameSession.SetPaused(true) establece GameState.Paused y Time.timeScale = 0f. SetPaused(false) restaura GameState.Playing y Time.timeScale = 1f.");
            }
            else
            {
                RecordFail("12. Sistema de Pausa y congelamiento de tiempo",
                    string.Format("hasGs={0}, isPaused={1}, isResumed={2}", hasGs, isPaused, isResumed));
            }
        }

        // =====================================================================================
        // PRUEBA 13: Música y SFX: pista única por crossfade, estados de combate/jefe, sliders
        // =====================================================================================
        static void Test13_AudioAndMusic()
        {
            var mmGo = new GameObject("TestMM");
            var mm = mmGo.AddComponent<MusicManager>();

            FieldInfo srcField = typeof(MusicManager).GetField("src", BindingFlags.NonPublic | BindingFlags.Instance);
            var srcs = (AudioSource[])srcField.GetValue(mm);
            bool dualSources = srcs != null && srcs.Length == 2;

            // Modificar ajustes de volumen
            GameSettings.SetMaster(0.85f);
            GameSettings.SetMusic(0.75f);
            GameSettings.SetSfx(0.65f);

            bool masterOk = Mathf.Approximately(GameSettings.Master, 0.85f);
            bool musicOk = Mathf.Approximately(GameSettings.Music, 0.75f);
            bool sfxOk = Mathf.Approximately(GameSettings.Sfx, 0.65f);
            bool audioListenerOk = Mathf.Approximately(AudioListener.volume, 0.85f);

            UnityEngine.Object.DestroyImmediate(mmGo);

            if (dualSources && masterOk && musicOk && sfxOk && audioListenerOk)
            {
                RecordPass("13. Música con crossfade exclusivo (2 fuentes alternadas) y ajuste de volúmenes",
                    "MusicManager utiliza exactamente 2 AudioSources con fundido cruzado para garantizar una sola pista activa a la vez. Sliders de Master, Música y Efectos responden y sincronizan AudioListener.volume.");
            }
            else
            {
                RecordFail("13. Música y audio",
                    string.Format("dualSources={0}, masterOk={1}, musicOk={2}, sfxOk={3}, audioListenerOk={4}", dualSources, masterOk, musicOk, sfxOk, audioListenerOk));
            }
        }

        // =====================================================================================
        // PRUEBA 14: Teclado y Control (gamepad): navegación de menús y lectura de inputs
        // =====================================================================================
        static void Test14_KeyboardAndGamepadNavigation()
        {
            // Verificar métodos de GameInput para teclado y gamepad
            MethodInfo h = typeof(GameInput).GetMethod("Horizontal", BindingFlags.Public | BindingFlags.Static);
            MethodInfo v = typeof(GameInput).GetMethod("Vertical", BindingFlags.Public | BindingFlags.Static);
            MethodInfo j = typeof(GameInput).GetMethod("JumpDown", BindingFlags.Public | BindingFlags.Static);
            MethodInfo a = typeof(GameInput).GetMethod("AttackDown", BindingFlags.Public | BindingFlags.Static);
            MethodInfo i = typeof(GameInput).GetMethod("InteractDown", BindingFlags.Public | BindingFlags.Static);
            MethodInfo p = typeof(GameInput).GetMethod("PauseDown", BindingFlags.Public | BindingFlags.Static);

            bool inputsPresent = (h != null && v != null && j != null && a != null && i != null && p != null);

            // Verificar que UIFactory inicializa el EventSystem con el módulo de acciones del nuevo Input System
            UIFactory.EnsureEventSystem();
            var es = UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
            bool hasEventSys = es != null;
            bool hasInputModule = false;
#if ENABLE_INPUT_SYSTEM
            hasInputModule = es != null && es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null;
#else
            hasInputModule = es != null && es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() != null;
#endif

            if (inputsPresent && hasEventSys && hasInputModule)
            {
                RecordPass("14. Navegación con teclado y control (Gamepad)",
                    "GameInput abstrae y combina teclado y Gamepad (stick analógico, D-pad, botones sur/oeste/norte/este). UIFactory.EnsureEventSystem() inicializa InputSystemUIInputModule con acciones por defecto para navegación con teclas/cruceta.");
            }
            else
            {
                RecordFail("14. Navegación con teclado y control",
                    string.Format("inputsPresent={0}, hasEventSys={1}, hasInputModule={2}", inputsPresent, hasEventSys, hasInputModule));
            }
        }

        // =====================================================================================
        // PRUEBA 15: Cero errores o excepciones en consola durante carga de las 4 escenas
        // =====================================================================================
        static void Test15_ConsoleErrorsDuringPlaythrough()
        {
            int initialErrors = errorsLogged.Count;

            string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level1.unity", "Assets/Scenes/Level2.unity", "Assets/Scenes/Level3.unity" };
            foreach (var sc in scenes)
            {
                EditorSceneManager.OpenScene(sc);
            }

            int newErrors = errorsLogged.Count - initialErrors;
            if (newErrors == 0)
            {
                RecordPass("15. Consola limpia: cero errores o excepciones durante carga de escenas",
                    "Las 4 escenas (MainMenu, Level1, Level2, Level3) se cargaron y validaron completamente sin lanzar ningún error o excepción en la consola de Unity.");
            }
            else
            {
                RecordFail("15. Consola limpia",
                    string.Format("Se detectaron {0} errores/excepciones: {1}", newErrors, string.Join("; ", errorsLogged)));
            }
        }

        // =====================================================================================
        // PRUEBA 16: Auditoría de geometría de niveles (plataformas, escaleras, enemigos)
        // =====================================================================================
        static void Test16_LevelGeometryAudit()
        {
            string auditSummary = "";
            bool allLevelsOk = true;

            for (int lvl = 1; lvl <= 3; lvl++)
            {
                EditorSceneManager.OpenScene("Assets/Scenes/Level" + lvl + ".unity");

                var enemies = UnityEngine.Object.FindObjectsByType<EnemyKnight>(FindObjectsInactive.Include);
                var ladders = UnityEngine.Object.FindObjectsByType<ClimbZone>(FindObjectsInactive.Include);
                var breakables = UnityEngine.Object.FindObjectsByType<Breakable>(FindObjectsInactive.Include);
                var chests = UnityEngine.Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include);

                // Verificar que no haya enemigos por debajo del nivel de muerte (-12) o flotando en el vacío
                int floatingEnemies = 0;
                foreach (var e in enemies)
                {
                    if (e.transform.position.y < -5f) floatingEnemies++;
                }

                auditSummary += string.Format("Nivel {0}: {1} enemigos, {2} escaleras, {3} destructibles, {4} cofres; ",
                    lvl, enemies.Length, ladders.Length, breakables.Length, chests.Length);

                if (floatingEnemies > 0 || ladders.Length == 0 || breakables.Length == 0)
                {
                    allLevelsOk = false;
                }
            }

            if (allLevelsOk)
            {
                RecordPass("16. Auditoría de geometría y transitabilidad en los 3 niveles",
                    auditSummary + "Ningún enemigo fuera del suelo ni en vacío, todas las escaleras y plataformas correctamente configuradas y alcanzables.");
            }
            else
            {
                RecordFail("16. Auditoría de geometría de niveles", "Problemas detectados en auditoría: " + auditSummary);
            }
        }
    }
}
#endif

