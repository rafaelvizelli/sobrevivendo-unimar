using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProvaImpossivel
{
    /// <summary>
    /// Toque de papel nos botões (Bloco 16): com o mouse em cima o texto vira caneta azul, a folha cresce um pouco e
    /// o "( )" ganha um X à mão; apertado, encolhe. Transição curta em tempo REAL (funciona com o jogo pausado).
    /// Só muda aparência: não bloqueia raycast nem consome clique (o Button continua recebendo tudo).
    /// </summary>
    public class PaperButtonFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public Text label;
        public Text mark;          // o X dentro do "( )" (opcional)
        public Selectable target;

        bool over, down;
        float hover, press;        // 0..1, animados

        /// <summary>Quanto o hover está "aceso" agora (testes).</summary>
        public float Hover => hover;

        public void OnPointerEnter(PointerEventData e) => over = true;
        public void OnPointerExit(PointerEventData e) { over = false; down = false; }
        public void OnPointerDown(PointerEventData e) => down = true;
        public void OnPointerUp(PointerEventData e) => down = false;

        void OnDisable() { over = down = false; hover = press = 0f; Apply(); }

        void Update()
        {
            bool on = target == null || target.IsInteractable();
            float step = Time.unscaledDeltaTime / UITheme.Transition;
            hover = Mathf.MoveTowards(hover, on && over ? 1f : 0f, step);
            press = Mathf.MoveTowards(press, on && down ? 1f : 0f, step);
            Apply();
        }

        void Apply()
        {
            bool on = target == null || target.IsInteractable();
            if (label != null) label.color = !on ? UITheme.InkFaint : Color.Lerp(UITheme.Ink, UITheme.Pen, hover);
            if (mark != null) { var c = UITheme.Pen; c.a = on ? hover : 0f; mark.color = c; }
            float s = 1f + 0.03f * hover - 0.05f * press;
            transform.localScale = new Vector3(s, s, 1f);
        }
    }
}
