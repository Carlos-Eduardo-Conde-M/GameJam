using UnityEngine;

[ExecuteInEditMode]
[AddComponentMenu("Image Effects/VHS Filter")]
public class VHSFilter : MonoBehaviour
{
    public Shader shader;
    private Material material;

    [Range(0f, 0.1f)] public float distortion = 0.02f;
    [Range(0f, 1f)] public float scanIntensity = 0.3f;
    [Range(0f, 0.01f)] public float chromaticAberration = 0.003f;
    [Range(0f, 1f)] public float noiseIntensity = 0.2f;

    public Texture2D noiseTexture;

    void Start()
    {
        if (shader == null)
        {
            shader = Shader.Find("Custom/VHS_Effect");
        }
        if (!shader || !shader.isSupported)
        {
            enabled = false;
        }
    }

    Material Mat
    {
        get
        {
            if (material == null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (Mat != null)
        {
            Mat.SetFloat("_Distortion", distortion);
            Mat.SetFloat("_ScanIntensity", scanIntensity);
            Mat.SetFloat("_ChromaticAberration", chromaticAberration);
            Mat.SetFloat("_NoiseIntensity", noiseIntensity);

            if (noiseTexture != null)
                Mat.SetTexture("_NoiseTex", noiseTexture);

            Graphics.Blit(src, dest, Mat);
        }
        else
        {
            Graphics.Blit(src, dest);
        }
    }

    void OnDisable()
    {
        if (material) DestroyImmediate(material);
    }
}

