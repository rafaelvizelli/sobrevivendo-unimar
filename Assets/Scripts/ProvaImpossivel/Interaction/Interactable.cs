using System.Collections.Generic;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// Qualquer coisa que o jogador pode usar com a tecla E.
    /// HoldTime &gt; 0 = precisa segurar E (e a ação pode ser suspeita enquanto segura).
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new();

        [Tooltip("Distância máxima (no plano) para interagir")]
        public float range = 1.8f;

        public virtual string Prompt => "Interagir";
        public virtual float HoldTime => 0f;
        public virtual string HoldLabel => Prompt + "...";
        /// <summary>Suspeita por segundo gerada enquanto o jogador segura a interação.</summary>
        public virtual float SuspicionPerSecond => 0f;
        public virtual string SuspicionReason => "fazendo algo estranho";
        /// <summary>Tipo da infração enquanto segura (é o que as regras usam; o texto acima é só exibição).</summary>
        public virtual Infracao SuspicionKind => Infracao.Outra;
        public virtual Vector3 Position => transform.position;

        public virtual bool CanInteract(PlayerController p) => true;
        public abstract void Interact(PlayerController p);

        public virtual void OnHoldStart(PlayerController p) { }
        /// <summary>Chamado a cada frame enquanto segura. Retorne false para cancelar.</summary>
        public virtual bool OnHoldTick(PlayerController p, float progress01) => true;
        public virtual void OnHoldCancel(PlayerController p) { }

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
    }
}
