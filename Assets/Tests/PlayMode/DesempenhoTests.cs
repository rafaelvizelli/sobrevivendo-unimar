using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>
    /// S12 (Operação Steam): MEDIÇÃO de desempenho (não é meta de aprovação: depende da máquina). Uma prova no Normal,
    /// sala cheia, 10 s depois de 2 s de aquecimento: tempo de quadro médio, p95 e pior, e o tempo de CPU dos scripts.
    /// Grava em Captures/Desempenho/desempenho.md. Só falha se algo absurdo acontecer (quadro médio acima de 100 ms).
    /// </summary>
    public class DesempenhoTests
    {
        [UnityTest, Timeout(120000)]
        public IEnumerator MedeOTempoDeQuadroNaProva()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return Wait(2f);

            var quadros = new List<float>();
            using var scripts = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "BehaviourUpdate", 2000);
            using var tardios = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "PreLateUpdate.ScriptRunBehaviourLateUpdate", 2000);
            float fim = Time.realtimeSinceStartup + 10f;
            while (Time.realtimeSinceStartup < fim)
            {
                yield return null;
                quadros.Add(Time.unscaledDeltaTime * 1000f);
            }
            quadros.Sort();
            float media = 0f;
            foreach (float q in quadros) media += q;
            media /= Mathf.Max(1, quadros.Count);
            float p95 = quadros[Mathf.Min(quadros.Count - 1, (int)(quadros.Count * 0.95f))];
            float pior = quadros[quadros.Count - 1];
            double update = Media(scripts) / 1e6, late = Media(tardios) / 1e6;

            var sb = new StringBuilder();
            sb.AppendLine("# Desempenho (S12) — medição automática");
            sb.AppendLine();
            sb.AppendLine("| Medida | Valor |");
            sb.AppendLine("|---|---|");
            sb.AppendLine("| Quadros medidos | " + quadros.Count + " |");
            sb.AppendLine("| Tempo de quadro médio | " + media.ToString("0.0") + " ms (" + (1000f / media).ToString("0") + " FPS) |");
            sb.AppendLine("| p95 | " + p95.ToString("0.0") + " ms |");
            sb.AppendLine("| Pior quadro | " + pior.ToString("0.0") + " ms |");
            sb.AppendLine("| Scripts: Update (média) | " + update.ToString("0.00") + " ms |");
            sb.AppendLine("| Scripts: LateUpdate (média; poses dos modelos) | " + late.ToString("0.00") + " ms |");
            sb.AppendLine("| Personagens | " + Object.FindObjectsByType<CharacterVisual>(FindObjectsSortMode.None).Length + " |");
            sb.AppendLine("| GPU | " + SystemInfo.graphicsDeviceName + " |");
            sb.AppendLine("| CPU | " + SystemInfo.processorType + " |");
            sb.AppendLine("| Resolução da janela | " + Screen.width + "×" + Screen.height + " |");
            Debug.Log("[Desempenho]\n" + sb);
            try
            {
                string dir = Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "Captures", "Desempenho");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, "desempenho.md"), sb.ToString(), Encoding.UTF8);
            }
            catch (System.Exception e) { Debug.LogWarning("[Desempenho] não gravei: " + e.Message); }

            Assert.Less(media, 100f, "quadro médio acima de 100 ms: algo muito errado");
        }

        static double Media(ProfilerRecorder r)
        {
            if (!r.Valid || r.Count == 0) return 0;
            double s = 0;
            for (int i = 0; i < r.Count; i++) s += r.GetSample(i).Value;
            return s / r.Count;
        }
    }
}
