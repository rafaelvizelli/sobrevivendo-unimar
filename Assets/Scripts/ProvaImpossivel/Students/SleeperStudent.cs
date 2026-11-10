using System.Collections;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>
    /// O Dorminhoco. Dorme a prova inteira, ronca (acontecimento) e acorda gritando se for cutucado,
    /// o que puxa o professor até ele — uma distração útil, mas que exige sair do lugar.
    /// </summary>
    public class SleeperStudent : StudentBase
    {
        public float wakeDistraction = 7f;
        public float pokeCooldown = 25f;

        float awakeUntil, cooldown, zzzTimer;

        bool Asleep => Time.time >= awakeUntil;

        public override string Prompt => "Cutucar o Dorminhoco" + FavorTag;

        protected override void Start()
        {
            base.Start();
            displayName = "DORMINHOCO";
        }

        public override bool CanInteract(PlayerController p) => Playing && Asleep && cooldown <= 0f && HasFavor;

        public override void Interact(PlayerController p)
        {
            cooldown = pokeCooldown;
            WakeUp(8f);
            SoundFX.Play(Sfx.Evento);
            var before = Prof != null ? Prof.State : ProfState.Tranquilo;
            bool ok = Prof != null && Prof.Distract(transform.position, wakeDistraction, Falas.Pick(Falas.ProfBarulho), true);
            if (ok) GameManager.I.SpendFavor();
            if (ok || !ToastDistractionRefused(before)) HUD.I.Toast(ok ? "O Dorminhoco acordou GRITANDO! O professor foi ver. Aproveita!" : "O Dorminhoco acordou gritando... mas o professor nem ligou.",
                             ok ? HUD.Yellow : HUD.Gray, 3f);
        }

        /// <summary>Acontecimento: ronco alto. O professor vai até ele e o acorda.</summary>
        public void Snore()
        {
            Say(Falas.Pick(Falas.DorminhocoRonco), 3f);
            if (Prof != null && Prof.Distract(transform.position, 9f, Falas.Pick(Falas.ProfRonco), true))
                Invoke(nameof(WakeByProfessor), 4f);
        }

        void WakeByProfessor() => WakeUp(6f);

        void WakeUp(float seconds)
        {
            awakeUntil = Time.time + seconds;
            Say(Falas.Pick(Falas.DorminhocoAcorda), 3f);
        }

        void Update()
        {
            if (cooldown > 0f) cooldown -= Time.deltaTime;
            bool asleep = Asleep;
            visual.sleeping = asleep;
            visual.writing = false;
            visual.expression = asleep ? Expression.Dormindo : Expression.Assustado;
            visual.headYaw = asleep ? 0f : Mathf.Sin(Time.time * 3f) * 40f;
            // aviso do ronco: a cabeça começa a cair e voltar (cabeceando)
            visual.headPitch = Nodding && asleep ? Mathf.Sin(Time.time * 5f) * 14f : 0f;
            if (!Playing || !asleep || Nodding) return;

            // sono normal: um "z..." baixinho de vez em quando (o "zZZZ" crescendo fica só para o aviso do ronco)
            zzzTimer -= Time.deltaTime;
            if (zzzTimer <= 0f)
            {
                zzzTimer = Random.Range(7f, 12f);
                HUD.I?.Say(transform, "", "z...", 1.6f, HUD.Gray);
            }
        }

        /// <summary>Está dando o aviso de que vai roncar (cabeceando, "zzz" crescendo)?</summary>
        public bool Nodding { get; private set; }

        /// <summary>Texto dos "z" do aviso, crescendo com o tempo (0 a 1). Público para testes.</summary>
        public static string PreSnoreText(float t01) => t01 < 0.34f ? "z" : t01 < 0.67f ? "zZz" : "zZZZZ...";

        /// <summary>
        /// Aviso antes do ronco (acontecimento): por <paramref name="seconds"/> s ele cabeceia e os "z" crescem.
        /// Quem presta atenção na sala sabe que a janela vem aí, sem texto no HUD.
        /// </summary>
        public IEnumerator SnoreWarning(float seconds)
        {
            if (seconds <= 0f) yield break;
            Nodding = true;
            float start = Time.time;
            int last = -1;
            while (Playing && Asleep && Time.time - start < seconds)
            {
                float t01 = (Time.time - start) / seconds;
                int stage = t01 < 0.34f ? 0 : t01 < 0.67f ? 1 : 2;
                if (stage != last)
                {
                    last = stage;
                    HUD.I?.Say(transform, "", PreSnoreText(t01), seconds / 3f + 0.3f, HUD.Gray);
                }
                yield return null;
            }
            Nodding = false;
        }
    }
}
