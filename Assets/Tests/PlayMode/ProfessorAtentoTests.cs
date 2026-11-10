using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using static ProvaImpossivel.Tests.FluxoEProfessorTests;

namespace ProvaImpossivel.Tests
{
    /// <summary>Bloco 7 — "vou ficar de olho em todo mundo" tem consequência: por um tempo ele vê mais e reage mais rápido.</summary>
    public class ProfessorAtentoTests
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
        }

        [UnityTest]
        public IEnumerator AtentoAumentaACampoDeVisaoPorUmTempo()
        {
            yield return EnterGame();
            GameManager.I.Begin(Difficulty.Normal);
            yield return null;
            var prof = ProfessorAI.I;
            Assert.IsFalse(prof.Alerted);
            float fovNormal = prof.CurrentFov;

            prof.BeAlert(0.6f);
            Assert.IsTrue(prof.Alerted);
            Assert.AreEqual(fovNormal + prof.alertedExtraFov, prof.CurrentFov, 0.01f, "atento deveria enxergar mais aberto");

            yield return Wait(0.8f);
            Assert.IsFalse(prof.Alerted, "o efeito deveria passar");
            Assert.AreEqual(fovNormal, prof.CurrentFov, 0.01f);
        }
    }
}
