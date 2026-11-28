#if UNITY_EDITOR
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Dolores1810.EditorTools
{
    /// <summary>
    /// Exportador automatizado para generar las compilaciones listas para subir a Itch.io.
    /// Genera la carpeta y el archivo .ZIP comprimido con un solo clic.
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

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(webglDir, zipPath, CompressionLevel.Optimal, false);

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

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                EditorUtility.ClearProgressBar();

                if (report.summary.result == BuildResult.Succeeded)
                {
                    if (File.Exists(zipPath)) File.Delete(zipPath);
                    ZipFile.CreateFromDirectory(winDir, zipPath, CompressionLevel.Optimal, false);

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
    }
}
#endif
