using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace ProvaImpossivel
{
    /// <summary>Tudo o que o jogador pode fazer com um botão (S7, Operação Steam).</summary>
    public enum Acao
    {
        Responder, Celular, Levantar, Interagir, Correr,
        Colinha, Garrafinha, Fone, Bilhete,
        Pausa, CaraDePaisagem,
        Opcao1, Opcao2, Opcao3, Opcao4,
        MenuFacil, MenuNormal, MenuDificil, Tutorial,
        JogarDeNovo, VoltarAoMenu, Voltar,
        /// <summary>Borracha com mira (favor): V / D-pad. Novo no fim (o enum indexa o cache de teclas).</summary>
        Borracha,
    }

    /// <summary>
    /// S7 (Operação Steam): ponto único de entrada. Cada ação vale pelo TECLADO (as mesmas teclas de sempre) ou pelo
    /// CONTROLE (Xbox/PlayStation/Steam Deck), com a mesma semântica de antes (<c>wasPressedThisFrame</c> / <c>isPressed</c>),
    /// então as proteções contra input duplicado no mesmo frame continuam valendo. A ajuda na tela mostra a tecla do
    /// último dispositivo usado (<see cref="Label"/>).
    ///
    /// Controle (layout Xbox; no PlayStation a posição é a mesma): analógico esquerdo anda, direito gira a câmera,
    /// L3 corre. X responde, Y pesquisa no celular (segure), B levanta, A interage / Cara de Paisagem. LB colinha,
    /// RB garrafinha, LT fone, RT bilhete. Start pausa. Na pergunta: X, Y, A, B = alternativas 1 a 4. No menu: X Fácil,
    /// A Normal, Y Difícil, Select tutorial. No fim: Y joga de novo, Select volta ao menu.
    /// </summary>
    public static class Controls
    {
        /// <summary>O último dispositivo usado foi o controle? (troca os nomes das teclas na tela)</summary>
        public static bool UsingGamepad { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay()
        {
            UsingGamepad = false;
            cachedKb = null;
            cachedKeys = null;
        }

        static Keyboard Kb => Keyboard.current;
        static Gamepad Pad => Gamepad.current;

        /// <summary>Apertou neste frame (teclado ou controle).</summary>
        public static bool Pressed(Acao a)
        {
            if (AnyPressed(KeysFor(a))) { UsingGamepad = false; return true; }
            var b = ButtonFor(a);
            if (b != null && b.wasPressedThisFrame) { UsingGamepad = true; return true; }
            return false;
        }

        /// <summary>Está segurando (teclado ou controle).</summary>
        public static bool Held(Acao a)
        {
            if (AnyHeld(KeysFor(a))) return true;
            var b = ButtonFor(a);
            return b != null && b.isPressed;
        }

        /// <summary>Andar: WASD ou analógico esquerdo (x = lado, y = frente), no máximo comprimento 1.</summary>
        public static Vector2 Move()
        {
            Vector2 v = Vector2.zero;
            var kb = Kb;
            if (kb != null)
                v = new Vector2((kb.dKey.isPressed ? 1 : 0) - (kb.aKey.isPressed ? 1 : 0), (kb.wKey.isPressed ? 1 : 0) - (kb.sKey.isPressed ? 1 : 0));
            var pad = Pad;
            if (pad != null)
            {
                Vector2 s = pad.leftStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) { v += s; UsingGamepad = true; }
            }
            return Vector2.ClampMagnitude(v, 1f);
        }

        /// <summary>Girar a câmera neste frame, em "pixels de mouse" (o analógico direito é convertido por segundo).</summary>
        public static Vector2 Look(float stickDegreesPerSecond)
        {
            Vector2 d = Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
            var pad = Pad;
            if (pad != null)
            {
                Vector2 s = pad.rightStick.ReadValue();
                if (s.sqrMagnitude > 0.04f) { d += s * (stickDegreesPerSecond * Time.unscaledDeltaTime); UsingGamepad = true; }
            }
            return d;
        }

        /// <summary>Alguma tecla/botão que escolhe no menu ainda está apertada? (não pode virar ação logo no começo)</summary>
        public static bool AnyMenuKeyHeld() =>
            Held(Acao.MenuFacil) || Held(Acao.MenuNormal) || Held(Acao.MenuDificil) || Held(Acao.Responder) || Held(Acao.Celular);

        // ------------------------------------------------------------------ mapa

        // as teclas de cada ação, montadas uma vez por teclado (os testes trocam o teclado atual por um virtual)
        static Keyboard cachedKb;
        static KeyControl[][] cachedKeys;

        static KeyControl[] KeysFor(Acao a)
        {
            var kb = Kb;
            if (kb == null) return null;
            if (kb != cachedKb || cachedKeys == null)
            {
                var all = (Acao[])System.Enum.GetValues(typeof(Acao));
                cachedKeys = new KeyControl[all.Length][];
                foreach (var x in all) cachedKeys[(int)x] = BuildKeys(kb, x);
                cachedKb = kb;
            }
            return cachedKeys[(int)a];
        }

        static KeyControl[] BuildKeys(Keyboard kb, Acao a)
        {
            return a switch
            {
                Acao.Responder or Acao.Opcao1 or Acao.MenuFacil => new[] { kb.digit1Key, kb.numpad1Key },
                Acao.Celular or Acao.Opcao2 => new[] { kb.digit2Key, kb.numpad2Key },
                Acao.MenuNormal => new[] { kb.digit2Key, kb.numpad2Key, kb.enterKey },
                Acao.Opcao3 or Acao.MenuDificil => new[] { kb.digit3Key, kb.numpad3Key },
                Acao.Opcao4 => new[] { kb.digit4Key, kb.numpad4Key },
                Acao.Levantar or Acao.CaraDePaisagem => new[] { kb.spaceKey },
                Acao.Interagir => new[] { kb.eKey },
                Acao.Correr => new[] { kb.leftShiftKey, kb.rightShiftKey },
                Acao.Colinha => new[] { kb.qKey },
                Acao.Garrafinha => new[] { kb.gKey },
                Acao.Fone => new[] { kb.fKey },
                Acao.Bilhete => new[] { kb.bKey },
                Acao.Borracha => new[] { kb.vKey },
                Acao.Pausa or Acao.Voltar => new[] { kb.escapeKey },
                Acao.Tutorial => new[] { kb.tKey },
                Acao.JogarDeNovo => new[] { kb.rKey },
                Acao.VoltarAoMenu => new[] { kb.mKey },
                _ => null,
            };
        }

        static ButtonControl ButtonFor(Acao a)
        {
            var p = Pad;
            if (p == null) return null;
            return a switch
            {
                Acao.Responder or Acao.Opcao1 or Acao.MenuFacil => p.buttonWest,
                Acao.Celular or Acao.Opcao2 or Acao.MenuDificil or Acao.JogarDeNovo => p.buttonNorth,
                Acao.Opcao3 or Acao.MenuNormal or Acao.Interagir or Acao.CaraDePaisagem => p.buttonSouth,
                Acao.Opcao4 or Acao.Levantar or Acao.Voltar => p.buttonEast,
                Acao.Correr => p.leftStickButton,
                Acao.Colinha => p.leftShoulder,
                Acao.Garrafinha => p.rightShoulder,
                Acao.Fone => p.leftTrigger,
                Acao.Bilhete => p.rightTrigger,
                Acao.Borracha => p.dpad.up,
                Acao.Pausa => p.startButton,
                Acao.Tutorial or Acao.VoltarAoMenu => p.selectButton,
                _ => null,
            };
        }

        static bool AnyPressed(KeyControl[] keys)
        {
            if (keys == null) return false;
            foreach (var k in keys) if (k != null && k.wasPressedThisFrame) return true;
            return false;
        }

        static bool AnyHeld(KeyControl[] keys)
        {
            if (keys == null) return false;
            foreach (var k in keys) if (k != null && k.isPressed) return true;
            return false;
        }

        // ------------------------------------------------------------------ nomes na tela

        /// <summary>Nome da tecla da ação para a ajuda na tela, no dispositivo dado. Puro (testável).</summary>
        public static string Label(Acao a, bool gamepad) => gamepad
            ? a switch
            {
                Acao.Responder or Acao.Opcao1 or Acao.MenuFacil => "X",
                Acao.Celular or Acao.Opcao2 or Acao.MenuDificil or Acao.JogarDeNovo => "Y",
                Acao.Opcao3 or Acao.MenuNormal or Acao.Interagir or Acao.CaraDePaisagem => "A",
                Acao.Opcao4 or Acao.Levantar or Acao.Voltar => "B",
                Acao.Correr => "L3",
                Acao.Colinha => "LB",
                Acao.Garrafinha => "RB",
                Acao.Fone => "LT",
                Acao.Bilhete => "RT",
                Acao.Borracha => "D-pad",
                Acao.Pausa => "Start",
                Acao.Tutorial or Acao.VoltarAoMenu => "Select",
                _ => "?",
            }
            : a switch
            {
                Acao.Responder or Acao.Opcao1 or Acao.MenuFacil => "1",
                Acao.Celular or Acao.Opcao2 or Acao.MenuNormal => "2",
                Acao.Opcao3 or Acao.MenuDificil => "3",
                Acao.Opcao4 => "4",
                Acao.Levantar or Acao.CaraDePaisagem => "Espaço",
                Acao.Interagir => "E",
                Acao.Correr => "Shift",
                Acao.Colinha => "Q",
                Acao.Garrafinha => "G",
                Acao.Fone => "F",
                Acao.Bilhete => "B",
                Acao.Borracha => "V",
                Acao.Pausa or Acao.Voltar => "Esc",
                Acao.Tutorial => "T",
                Acao.JogarDeNovo => "R",
                Acao.VoltarAoMenu => "M",
                _ => "?",
            };

        /// <summary>Nome da tecla da ação no dispositivo que o jogador está usando agora.</summary>
        public static string Label(Acao a) => Label(a, UsingGamepad);

        /// <summary>"[Q]" / "[LB]": o formato da ajuda na tela.</summary>
        public static string Tag(Acao a) => "[" + Label(a) + "]";
    }
}
