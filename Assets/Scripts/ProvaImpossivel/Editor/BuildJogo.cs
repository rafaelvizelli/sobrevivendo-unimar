using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Gera o executável com a cena da Fase 1 (menu Prova Impossível > Gerar Build Windows / Linux).
    /// Cada plataforma precisa do módulo de build correspondente instalado no Unity Hub.
    /// </summary>
    public static class BuildJogo
    {
        /// <summary>Saída da build Windows (mantida com este nome: as notas e scripts antigos usam).</summary>
        public const string Output = "Builds/Windows/SobrevivendoUnimar.exe";
        public const string OutputLinux = "Builds/Linux/SobrevivendoUnimar.x86_64";

        [MenuItem("Prova Impossível/Gerar Build Windows")]
        public static void Menu() => Debug.Log(Build(BuildTarget.StandaloneWindows64));

        [MenuItem("Prova Impossível/Gerar Build Linux")]
        public static void MenuLinux() => Debug.Log(Build(BuildTarget.StandaloneLinux64));

        /// <summary>Caminho do executável para cada plataforma suportada; null se não for suportada.</summary>
        public static string OutputFor(BuildTarget target) => target switch
        {
            BuildTarget.StandaloneWindows64 => Output,
            BuildTarget.StandaloneLinux64 => OutputLinux,
            _ => null,
        };

        /// <summary>Mantido para quem chamava sem argumento (gera a build Windows).</summary>
        public static string Build() => Build(BuildTarget.StandaloneWindows64);

        public static string Build(BuildTarget target)
        {
            string output = OutputFor(target);
            if (output == null)
                return "Build não suportada para " + target + " (só Windows 64 e Linux 64).";

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ProvaImpossivelSetup.ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            return "Build " + s.result + ": " + s.totalErrors + " erro(s), " + s.totalWarnings + " aviso(s), "
                   + (s.totalSize / (1024 * 1024)) + " MB, " + s.totalTime.TotalSeconds.ToString("F0") + " s -> " + s.outputPath;
        }
    }
}
