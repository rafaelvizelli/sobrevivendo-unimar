using System.Collections;
using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Projetor que pode falhar (pisca) e obriga o professor a ir até a frente.</summary>
    public class ProjectorDevice : MonoBehaviour
    {
        public Light beam;
        [Tooltip("Onde o professor fica mexendo nos cabos")] public Transform fixSpot;

        float baseIntensity;
        public bool Failing { get; private set; }

        void Awake()
        {
            if (beam != null) baseIntensity = beam.intensity;
        }

        public void Fail(float duration)
        {
            if (Failing) return;
            StartCoroutine(FailRoutine(duration));
            var spot = fixSpot != null ? fixSpot.position : transform.position;
            ProfessorAI.I?.Distract(spot, duration, Falas.Pick(Falas.ProfProjetor), true);
        }

        IEnumerator FailRoutine(float duration)
        {
            Failing = true;
            float end = Time.time + duration;
            while (Time.time < end)
            {
                if (beam != null) beam.intensity = Random.value < 0.5f ? 0f : baseIntensity * Random.Range(0.3f, 1.4f);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.25f));
            }
            if (beam != null) beam.intensity = baseIntensity;
            Failing = false;
        }
    }
}
