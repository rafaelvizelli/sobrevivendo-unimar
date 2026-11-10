using System.Collections;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Figurante: faz a prova, olha em volta e, às vezes, derruba a mochila (acontecimento).</summary>
    public class ExtraStudent : StudentBase
    {
        [Tooltip("Mochila no chão ao lado da carteira (opcional)")]
        public Transform bag;

        bool dropped;

        protected override void Start()
        {
            base.Start();
            if (string.IsNullOrEmpty(displayName) || displayName == "Aluno") displayName = "ALUNO";
        }

        public bool CanDropBag => bag != null && !dropped;

        /// <summary>Acontecimento: a mochila cai e espalha tudo. O professor olha pra cá.</summary>
        public void DropBag()
        {
            if (!CanDropBag) return;
            dropped = true;
            Say(Falas.Pick(Falas.FiguranteMochila), 2.5f);
            StartCoroutine(Tip());
            Prof?.Distract(transform.position, 5f, Falas.Pick(Falas.ProfBarulho), false);
        }

        IEnumerator Tip()
        {
            Quaternion from = bag.rotation, to = from * Quaternion.Euler(85f, 0f, 0f);
            for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
            {
                bag.rotation = Quaternion.Slerp(from, to, t);
                yield return null;
            }
            bag.rotation = to;
            Color[] colors = { new(0.95f, 0.85f, 0.2f), Color.white, new(0.3f, 0.6f, 0.95f), new(0.9f, 0.3f, 0.3f) };
            for (int i = 0; i < 4; i++)
            {
                var go = GameObject.CreatePrimitive(i % 2 == 0 ? PrimitiveType.Cube : PrimitiveType.Cylinder);
                Destroy(go.GetComponent<Collider>());
                go.name = "Bagunça";
                go.transform.position = bag.position + new Vector3(Random.Range(-0.5f, 0.5f), 0.02f, Random.Range(-0.5f, 0.5f));
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), i % 2 == 0 ? 0f : 90f);
                go.transform.localScale = i % 2 == 0 ? new Vector3(0.2f, 0.02f, 0.28f) : new Vector3(0.02f, 0.08f, 0.02f);
                go.GetComponent<MeshRenderer>().sharedMaterial = MaterialLibrary.Get(colors[i]);
            }
        }

        void Update()
        {
            visual.writing = true;
            visual.expression = ProfessorAI.I != null && ProfessorAI.I.State == ProfState.Flagrante ? Expression.Assustado : Expression.Neutro;
            if (Playing) IdleLook(Time.deltaTime, 30f);
        }
    }
}
