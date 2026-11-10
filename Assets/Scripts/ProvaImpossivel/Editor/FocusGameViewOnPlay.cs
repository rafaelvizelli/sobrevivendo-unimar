using UnityEditor;

namespace ProvaImpossivel.EditorTools
{
    /// <summary>
    /// Só no editor: ao entrar em Play, traz a aba Game para frente e dá foco a ela.
    /// O Input System (configuração padrão) só entrega teclado/mouse ao jogo quando a aba Game está focada;
    /// sem isso, se a aba Scene estiver na frente, nada responde até o jogador clicar na aba Game.
    /// </summary>
    [InitializeOnLoad]
    static class FocusGameViewOnPlay
    {
        static FocusGameViewOnPlay()
        {
            EditorApplication.playModeStateChanged += state =>
            {
                if (state != PlayModeStateChange.EnteredPlayMode) return;
                var gameView = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                if (gameView != null) EditorWindow.FocusWindowIfItsOpen(gameView);
            };
        }
    }
}
