using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Só no editor: se existir o arquivo Temp/play_fase1.sinal, apaga o sinal, abre a cena da Fase 1 e entra em Play.
    /// Serve para dar Play de fora do Unity (ex.: pelo Claude) quando a ponte MCP não está conectada.
    /// O editor verifica o sinal a cada atualização; ele só avança com a janela do Unity em foco.
    /// </summary>
    [InitializeOnLoad]
    static class PlayPorSinal
    {
        const string Sinal = "Temp/play_fase1.sinal";
        const string ScenePath = "Assets/Scenes/Fase1_ProvaImpossivel.unity";

        static PlayPorSinal()
        {
            EditorApplication.update += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || !File.Exists(Sinal)) return;
                File.Delete(Sinal);
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
                EditorApplication.isPlaying = true;
            };
        }
    }
}
