#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Dolores1810.EditorTools
{
    /// <summary>
    /// Exportador automatizado para generar las compilaciones listas para subir a Itch.io y paquetes Android APK.
    /// Genera la carpeta y el archivo comprimido o APK con un solo clic.
    /// </summary>
    public static class ItchExporter
    {
        static readonly string[] GameScenes = new string[]
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Level1.unity",
            "Assets/Scenes/Level2.unity",
            "Assets/Scenes/Level3.unity",
            "Assets/Scenes/Level4.unity"
        };

        /// <summary>
        /// Compila abriendo temporalmente una escena limpia para evitar que el analizador de shaders
        /// de URP 2D (Light2DPrefilteringAnalysis) reporte falsos errores de "More than one global light"
        /// al abrir aditivamente las escenas de la compilación.
        /// </summary>
        static BuildReport BuildWithCleanScene(BuildPlayerOptions opts)
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            string currentPath = currentScene.path;
            bool hasSavedScene = !string.IsNullOrEmpty(currentPath);

            if (currentScene.isDirty && hasSavedScene)
            {
                EditorSceneManager.SaveScene(currentScene);
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                return BuildPipeline.BuildPlayer(opts);
            }
            finally
            {
                if (hasSavedScene && File.Exists(currentPath))
                {
                    EditorSceneManager.OpenScene(currentPath, OpenSceneMode.Single);
                }
            }
        }

        [MenuItem("Tools/Dolores 1810/Exportar para Itch.io (Navegador WebGL)", priority = 20)]
        public static void ExportWebGL()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildsDir = Path.Combine(projectRoot, "Builds");
            string webglDir = Path.Combine(buildsDir, "WebGL");
            string zipPath = Path.Combine(buildsDir, "1810_Dolores_WebGL.zip");

            if (!Directory.Exists(buildsDir)) Directory.CreateDirectory(buildsDir);
            if (Directory.Exists(webglDir)) Directory.Delete(webglDir, true);
            Directory.CreateDirectory(webglDir);

            Debug.Log("[ItchExporter] Iniciando compilación WebGL para Itch.io...");
            EditorUtility.DisplayProgressBar("Itch.io Exporter", "Compilando juego para WebGL...", 0.3f);

            try
            {
                BuildPlayerOptions opts = new BuildPlayerOptions();
                opts.scenes = GameScenes;
                opts.locationPathName = webglDir;
                opts.target = BuildTarget.WebGL;
                opts.options = BuildOptions.None;

                BuildReport report = BuildWithCleanScene(opts);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(webglDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

                    Debug.Log("<color=green>[ItchExporter] ¡Exportación WebGL exitosa!</color> Archivo ZIP generado en: " + zipPath);
                    EditorUtility.RevealInFinder(zipPath);
                    EditorUtility.DisplayDialog("Exportación WebGL Completa",
                        "El archivo ZIP listo para subir a Itch.io se ha generado en:\n\n" + zipPath +
                        "\n\nEn Itch.io selecciona 'HTML' y marca 'This file will be played in the browser'.", "Entendido");
                }
                else
                {
                    Debug.LogError("[ItchExporter] La compilación WebGL falló con resultado: " + report.summary.result);
                    EditorUtility.DisplayDialog("Error de compilación", "La compilación WebGL no pudo completarse. Revisa la consola de Unity.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[ItchExporter] Excepción durante la exportación: " + ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        [MenuItem("Tools/Dolores 1810/Exportar para PC (Windows .exe)", priority = 21)]
        public static void ExportWindows()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildsDir = Path.Combine(projectRoot, "Builds");
            string winDir = Path.Combine(buildsDir, "Windows");
            string exePath = Path.Combine(winDir, "1810_ElGritoDeLaLibertad.exe");
            string zipPath = Path.Combine(buildsDir, "1810_Dolores_Windows_PC.zip");

            if (!Directory.Exists(buildsDir)) Directory.CreateDirectory(buildsDir);
            if (Directory.Exists(winDir)) Directory.Delete(winDir, true);
            Directory.CreateDirectory(winDir);

            Debug.Log("[ItchExporter] Iniciando compilación de Windows...");
            EditorUtility.DisplayProgressBar("Itch.io Exporter", "Compilando juego para Windows PC...", 0.3f);

            try
            {
                BuildPlayerOptions opts = new BuildPlayerOptions();
                opts.scenes = GameScenes;
                opts.locationPathName = exePath;
                opts.target = BuildTarget.StandaloneWindows64;
                opts.options = BuildOptions.None;

                BuildReport report = BuildWithCleanScene(opts);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(winDir, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);

                    Debug.Log("<color=green>[ItchExporter] ¡Exportación Windows exitosa!</color> Archivo ZIP en: " + zipPath);
                    EditorUtility.RevealInFinder(zipPath);
                    EditorUtility.DisplayDialog("Exportación Windows Completa",
                        "El archivo ejecutable (.zip) para PC se ha generado en:\n\n" + zipPath, "Entendido");
                }
                else
                {
                    Debug.LogError("[ItchExporter] La compilación de Windows falló.");
                    EditorUtility.DisplayDialog("Error de compilación", "La compilación no pudo completarse. Revisa la consola de Unity.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[ItchExporter] Excepción: " + ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }

        [MenuItem("Tools/Dolores 1810/Exportar APK para Android (.apk)", priority = 22)]
        public static void ExportAndroidAPK()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string buildsDir = Path.Combine(projectRoot, "Builds");
            string androidDir = Path.Combine(buildsDir, "Android");
            string apkPath = Path.Combine(androidDir, "1810_Dolores_Android.apk");

            // 1. Configurar PlayerSettings y compatibilidad universal para Android
            PlayerSettings.companyName = "Dolores 1810";
            PlayerSettings.productName = "1810: Dolores - El Grito de la Libertad";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.Dolores1810.ElGritoDeLaLibertad");
            PlayerSettings.bundleVersion = "1.0";
            PlayerSettings.Android.bundleVersionCode = 1;

            // Orientación: Exclusivamente horizontal (Landscape Left y Landscape Right permitidos)
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;

            // Versión mínima de Android: API 26 (Android 8.0 Oreo - estándar moderno de Unity 6)
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            // Modo inmersivo en pantalla completa
            PlayerSettings.Android.fullscreenMode = FullScreenMode.ExclusiveFullScreen;
            PlayerSettings.Android.renderOutsideSafeArea = true;

            // Generar archivo .APK instalable directamente (no .AAB)
            EditorUserBuildSettings.buildAppBundle = false;
            EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;

            // 2. Verificar soporte de compilación de Android en Unity
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            {
                Debug.LogWarning("[ItchExporter] El módulo 'Android Build Support' no está instalado en este editor de Unity.");
                EditorUtility.DisplayDialog(
                    "Se requiere módulo de Android en Unity Hub",
                    "Para compilar el archivo instalable .apk, necesitas activar el módulo 'Android Build Support' en Unity Hub (toma 1 minuto):\n\n" +
                    "1. Abre la ventana de Unity Hub.\n" +
                    "2. En la barra lateral izquierda, entra a 'Installs' (Instalaciones).\n" +
                    "3. En tu versión de Unity (6000.6.0f1), haz clic en el engrane ⚙️ y elige 'Add modules' (Añadir módulos).\n" +
                    "4. Marca la casilla 'Android Build Support' (junto con 'Android SDK & NDK Tools' y 'OpenJDK').\n" +
                    "5. Presiona 'Install'. Una vez termine, reinicia Unity.\n\n" +
                    "Al reiniciar, vuelve a hacer clic aquí y generará tu archivo .apk de inmediato.",
                    "Entendido");
                return;
            }

            if (!Directory.Exists(buildsDir)) Directory.CreateDirectory(buildsDir);
            if (!Directory.Exists(androidDir)) Directory.CreateDirectory(androidDir);
            if (File.Exists(apkPath)) File.Delete(apkPath);

            Debug.Log("[ItchExporter] Iniciando compilación de Android APK...");
            EditorUtility.DisplayProgressBar("Android APK Exporter", "Compilando paquete Android (.apk)...", 0.3f);

            try
            {
                BuildPlayerOptions opts = new BuildPlayerOptions();
                opts.scenes = GameScenes;
                opts.locationPathName = apkPath;
                opts.target = BuildTarget.Android;
                opts.options = BuildOptions.None;

                BuildReport report = BuildWithCleanScene(opts);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    Debug.Log("<color=green>[ItchExporter] ¡Exportación Android exitosa!</color> APK generado en: " + apkPath);
                    EditorUtility.RevealInFinder(apkPath);
                    EditorUtility.DisplayDialog("Exportación Android Exitosa",
                        "¡El paquete de Android (.apk) funcional se ha generado con éxito!\n\n" +
                        "Archivo: " + apkPath + "\n\n" +
                        "Ya puedes transferir este archivo a tu celular Android (por cable USB, WhatsApp Web, Google Drive) o instalarlo en un emulador.\n" +
                        "En el teléfono solo debes abrir el archivo y seleccionar 'Instalar'.", "Entendido");
                }
                else
                {
                    Debug.LogError("[ItchExporter] La compilación Android falló con resultado: " + report.summary.result);
                    EditorUtility.DisplayDialog("Error de compilación", "La compilación Android no pudo completarse. Revisa la consola de Unity.", "OK");
                }
            }
            catch (System.Exception ex)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError("[ItchExporter] Excepción durante la exportación de Android: " + ex);
                EditorUtility.DisplayDialog("Error", ex.Message, "OK");
            }
        }
    }
}
#endif
