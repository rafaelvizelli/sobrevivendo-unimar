using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 13 — balões e mensagens não se sobrepõem ao HUD nem ao ícone do professor.</summary>
    public class InterfacePlayTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        static IEnumerator StartNormal()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
        }

        [UnityTest]
        public IEnumerator BalaoNuncaFicaEmCimaDoHud()
        {
            yield return StartNormal();
            var cam = Camera.main;
            // alvos que caem bem em cima dos painéis: alto no centro (tempo/mensagens), alto à direita (NOTA), embaixo (ajuda)
            var alto = new GameObject("alvo alto").transform;
            var direita = new GameObject("alvo direita").transform;
            var baixo = new GameObject("alvo baixo").transform;
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    alto.position = cam.transform.position + cam.transform.forward * 4f + cam.transform.up * 2.2f;
                    direita.position = cam.transform.position + cam.transform.forward * 4f + cam.transform.up * 2.2f + cam.transform.right * 3f;
                    baixo.position = cam.transform.position + cam.transform.forward * 4f - cam.transform.up * 4.2f;
                    HUD.I.Say(alto, "TESTE", "Balão que nasceria em cima do tempo e das mensagens", 3f, Color.white);
                    HUD.I.Say(direita, "TESTE", "zZZZZ...", 3f, Color.white);
                    HUD.I.Say(baixo, "TESTE", "Balão que nasceria em cima da ajuda", 3f, Color.white);
                    yield return null;
                }
                Assert.IsFalse(HUD.I.AnyBubbleOverlapsHud(), "um balão ficou em cima de um painel do HUD");
            }
            finally
            {
                Object.Destroy(alto.gameObject);
                Object.Destroy(direita.gameObject);
                Object.Destroy(baixo.gameObject);
            }
        }

        [UnityTest]
        public IEnumerator MensagensDesviamDoIconeDoProfessor()
        {
            yield return StartNormal(); // o começo da prova já mostra duas mensagens no topo
            Assert.Less(HUD.I.ToastStackOffset.magnitude, 1f, "pré-condição: sem ícone, as mensagens ficam no lugar");
            // um ícone (como o "?" do professor) bem no meio da faixa das mensagens
            var icone = new Rect(Screen.width * 0.5f - 30f, Screen.height - Screen.height * 0.16f, 60f, 60f);
            for (int i = 0; i < 40; i++)
            {
                HUD.I.ReportWorldIcon(icone);
                yield return null;
            }
            Assert.Greater(HUD.I.ToastStackOffset.magnitude, 20f, "as mensagens deveriam sair de cima do ícone");
        }
    }
}
