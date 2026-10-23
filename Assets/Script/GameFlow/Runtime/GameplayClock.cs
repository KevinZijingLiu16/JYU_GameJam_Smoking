using TMPro;
using UnityEngine;

namespace Smoking.GameFlow
{
    public sealed class GameplayClock : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        public float Elapsed { get; private set; }
        private void Update()
        {
            Elapsed += Time.deltaTime;
            label.text = "PLAY TIME  " + Elapsed.ToString("000.0") + " s";
        }
    }
}
