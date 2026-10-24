using UnityEngine;

namespace Smoking.Environment
{
    // Unlit glow effects (light shafts, dust) ignore scene lighting, so they would keep shining
    // when the level goes dark. This dims every renderer below it along with a reference light.
    public sealed class LightLinkedFade : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [Tooltip("The glow follows this light's intensity relative to its value at start.")]
        [SerializeField] private Light source;

        private Renderer[] renderers;
        private Color[] baseColors;
        private MaterialPropertyBlock block;
        private float baseIntensity;
        private float applied = -1f;

        private void Start()
        {
            renderers = GetComponentsInChildren<Renderer>();
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                baseColors[i] = renderers[i].sharedMaterial.GetColor(BaseColorId);
            block = new MaterialPropertyBlock();
            baseIntensity = source != null ? source.intensity : 1f;
        }

        private void LateUpdate()
        {
            if (source == null || baseIntensity <= 0f) return;
            float k = Mathf.Clamp01(source.intensity / baseIntensity);
            if (Mathf.Approximately(k, applied)) return;
            applied = k;
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(block);
                block.SetColor(BaseColorId, baseColors[i] * new Color(1f, 1f, 1f, k));
                renderers[i].SetPropertyBlock(block);
            }
        }
    }
}
