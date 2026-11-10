using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>Gera o executável do Windows com a cena da Fase 1 (menu Prova Impossível > Gerar Build Windows).</summary>
    public static class BuildJogo
    {
        public const string Output = "Builds/Windows/SobrevivendoUnimar.exe";

        [MenuItem("Prova Impossível/Gerar Build Windows")]
        public static void Menu() => Debug.Log(Build());

        public static string Build()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProvaImpossivelSetup.ScenePath },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            return "Build " + s.result + ": " + s.totalErrors + " erro(s), " + s.totalWarnings + " aviso(s), "
                   + (s.totalSize / (1024 * 1024)) + " MB, " + s.totalTime.TotalSeconds.ToString("F0") + " s -> " + s.outputPath;
        }
    }
}
