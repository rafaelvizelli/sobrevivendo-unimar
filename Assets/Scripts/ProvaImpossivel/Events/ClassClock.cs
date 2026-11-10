using UnityEngine;

namespace ProvaImpossivel
{
    /// <summary>Relógio de parede: a prova termina às 10:00, os ponteiros andam junto com o cronômetro.</summary>
    public class ClassClock : MonoBehaviour
    {
        public Transform hourHand, minuteHand, secondHand;
        [Tooltip("Horário (em minutos desde 0h) em que a prova acaba")] public float endMinutes = 600f;

        void Update()
        {
            var gm = GameManager.I;
            float duration = gm != null && gm.Settings != null ? gm.Settings.duration : 360f;
            float left = gm != null ? gm.TimeLeft : duration;
            float totalSeconds = endMinutes * 60f - left;

            float sec = totalSeconds % 60f;
            float min = (totalSeconds / 60f) % 60f;
            float hour = (totalSeconds / 3600f) % 12f;
            // o relógio olha para +Z: rotação negativa em Z = sentido horário para quem está na sala
            if (secondHand) secondHand.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Floor(sec) * 6f);
            if (minuteHand) minuteHand.localRotation = Quaternion.Euler(0f, 0f, -min * 6f);
            if (hourHand) hourHand.localRotation = Quaternion.Euler(0f, 0f, -hour * 30f);
        }
    }
}
