namespace ProvaImpossivel
{
    /// <summary>Mesa do professor: segurar E entrega a prova e encerra a fase.</summary>
    public class TeacherDesk : Interactable
    {
        bool Enough => ExamSystem.I.Grade >= GameManager.I.Settings.minGrade;

        public override string Prompt => Enough ? "Entregar a prova" : "Entregar a prova (nota insuficiente: vai reprovar!)";
        public override string HoldLabel => "Entregando a prova...";
        public override float HoldTime => 1.2f;
        public override bool CanInteract(PlayerController p) => GameManager.I.IsPlaying;
        public override void Interact(PlayerController p) => GameManager.I.DeliverExam();
    }
}
