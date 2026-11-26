using NUnit.Framework;
using ProvaImpossivel.EditorTools;
using UnityEditor;

namespace ProvaImpossivel.Tests
{
    /// <summary>Suporte a Linux — cada plataforma gera o executável no lugar certo.</summary>
    public class BuildJogoTests
    {
        [Test]
        public void WindowsContinuaNoMesmoLugar()
        {
            Assert.AreEqual("Builds/Windows/SobrevivendoUnimar.exe", BuildJogo.OutputFor(BuildTarget.StandaloneWindows64));
            Assert.AreEqual(BuildJogo.Output, BuildJogo.OutputFor(BuildTarget.StandaloneWindows64), "notas antigas usam Output");
        }

        [Test]
        public void LinuxGeraX86_64NaPastaLinux()
        {
            string s = BuildJogo.OutputFor(BuildTarget.StandaloneLinux64);
            Assert.AreEqual("Builds/Linux/SobrevivendoUnimar.x86_64", s);
            Assert.AreNotEqual(BuildJogo.Output, s, "as duas builds não podem se sobrescrever");
        }

        [Test]
        public void PlataformaNaoSuportadaNaoGera()
        {
            Assert.IsNull(BuildJogo.OutputFor(BuildTarget.Android));
            StringAssert.Contains("não suportada", BuildJogo.Build(BuildTarget.Android), "recusa sem chamar o BuildPipeline");
        }
    }
}
