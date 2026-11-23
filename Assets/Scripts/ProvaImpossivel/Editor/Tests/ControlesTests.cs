using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace ProvaImpossivel.Tests
{
    /// <summary>S7 (Operação Steam): nomes das teclas na tela, por dispositivo.</summary>
    public class ControlesTests
    {
        [Test]
        public void TodaAcaoTemNomeNoTecladoENoControle()
        {
            foreach (Acao a in Enum.GetValues(typeof(Acao)))
            {
                Assert.AreNotEqual("?", Controls.Label(a, false), a + " sem tecla no teclado");
                Assert.AreNotEqual("?", Controls.Label(a, true), a + " sem botão no controle");
            }
        }

        [Test]
        public void NoTecladoAsTeclasSaoAsDeSempre()
        {
            Assert.AreEqual("1", Controls.Label(Acao.Responder, false));
            Assert.AreEqual("2", Controls.Label(Acao.Celular, false));
            Assert.AreEqual("Q", Controls.Label(Acao.Colinha, false));
            Assert.AreEqual("G", Controls.Label(Acao.Garrafinha, false));
            Assert.AreEqual("B", Controls.Label(Acao.Bilhete, false));
            Assert.AreEqual("F", Controls.Label(Acao.Fone, false));
            Assert.AreEqual("E", Controls.Label(Acao.Interagir, false));
            Assert.AreEqual("Espaço", Controls.Label(Acao.Levantar, false));
            Assert.AreEqual("Esc", Controls.Label(Acao.Pausa, false));
            Assert.AreEqual("[Q]", "[" + Controls.Label(Acao.Colinha, false) + "]");
        }

        /// <summary>Durante a prova, cada ação tem uma tecla/botão só dela (no teclado e no controle).</summary>
        [Test]
        public void AsAcoesDaProvaNaoDividemTeclaNemBotao()
        {
            var naProva = new[] { Acao.Responder, Acao.Celular, Acao.Levantar, Acao.Interagir, Acao.Correr, Acao.Colinha,
                                  Acao.Garrafinha, Acao.Fone, Acao.Bilhete, Acao.Pausa };
            foreach (bool pad in new[] { false, true })
            {
                var usados = new Dictionary<string, Acao>();
                foreach (var a in naProva)
                {
                    string b = Controls.Label(a, pad);
                    Assert.IsFalse(usados.ContainsKey(b), a + " e " + (usados.ContainsKey(b) ? usados[b].ToString() : "") + " usam o mesmo botão " + b);
                    usados[b] = a;
                }
            }
        }

        [Test]
        public void AsQuatroAlternativasTemBotoesDiferentes()
        {
            var opcoes = new[] { Acao.Opcao1, Acao.Opcao2, Acao.Opcao3, Acao.Opcao4 };
            foreach (bool pad in new[] { false, true })
            {
                var vistos = new HashSet<string>();
                foreach (var o in opcoes) Assert.IsTrue(vistos.Add(Controls.Label(o, pad)), o + " repetida (controle: " + pad + ")");
            }
        }
    }
}
