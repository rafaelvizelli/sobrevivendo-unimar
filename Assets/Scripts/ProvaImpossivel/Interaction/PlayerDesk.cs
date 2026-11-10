namespace ProvaImpossivel
{
    /// <summary>A carteira do jogador: perto dela, E para sentar.</summary>
    public class PlayerDesk : Interactable
    {
        public override string Prompt => "Sentar no seu lugar";
        public override bool CanInteract(PlayerController p) => !p.IsSeated;
        public override void Interact(PlayerController p) => p.SitDown();
    }
}
