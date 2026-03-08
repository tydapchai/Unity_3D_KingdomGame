using UnityEngine;

public class MissionBeaconVisual : MonoBehaviour
{
    [Header("Beam")]
    [SerializeField] private Color beamColor = new(1f, 0.55f, 0f, 0.9f);
    [SerializeField] private float beamHeight = 30f;
    [SerializeField] private float beamWidth = 0.7f;
    [SerializeField] private float glowRadius = 3.5f;
    [SerializeField] private float glowHeight = 0.08f;
    [SerializeField] private bool suppressExistingEffects = true;

    private Transform _generatedRoot;

    private void Awake()
    {
        if (suppressExistingEffects)
        {
            DisableExistingEffects();
        }

        EnsureVisuals();
    }

    private void OnEnable()
    {
        EnsureVisuals();
        SetGeneratedVisualsActive(true);
    }

    private void OnDisable()
    {
        SetGeneratedVisualsActive(false);
    }

    private void EnsureVisuals()
    {
        if (_generatedRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("GeneratedMissionBeacon");
        if (existing != null)
        {
            _generatedRoot = existing;
            return;
        }

        GameObject root = new("GeneratedMissionBeacon");
        root.transform.SetParent(transform, false);
        _generatedRoot = root.transform;

        CreateBeam();
        CreateGlow();
    }

    private void CreateBeam()
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = "Beam";
        beam.transform.SetParent(_generatedRoot, false);
        beam.transform.localPosition = new Vector3(0f, beamHeight * 0.5f, 0f);
        beam.transform.localScale = new Vector3(beamWidth, beamHeight * 0.5f, beamWidth);

        Collider beamCollider = beam.GetComponent<Collider>();
        if (beamCollider != null)
        {
            Destroy(beamCollider);
        }

        Renderer beamRenderer = beam.GetComponent<Renderer>();
        if (beamRenderer != null)
        {
            beamRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            beamRenderer.receiveShadows = false;
            beamRenderer.sharedMaterial = CreateMaterial("MissionBeamMat", beamColor);
        }
    }

    private void CreateGlow()
    {
        GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        glow.name = "GroundGlow";
        glow.transform.SetParent(_generatedRoot, false);
        glow.transform.localPosition = new Vector3(0f, glowHeight * 0.5f, 0f);
        glow.transform.localScale = new Vector3(glowRadius, glowHeight * 0.5f, glowRadius);

        Collider glowCollider = glow.GetComponent<Collider>();
        if (glowCollider != null)
        {
            Destroy(glowCollider);
        }

        Renderer glowRenderer = glow.GetComponent<Renderer>();
        if (glowRenderer != null)
        {
            glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            glowRenderer.receiveShadows = false;
            Color glowColor = new(beamColor.r, beamColor.g * 0.9f, beamColor.b * 0.8f, 0.45f);
            glowRenderer.sharedMaterial = CreateMaterial("MissionGlowMat", glowColor);
        }
    }

    private void DisableExistingEffects()
    {
        ParticleSystem[] particleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particleSystem in particleSystems)
        {
            if (particleSystem.transform == _generatedRoot)
            {
                continue;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            particleSystem.gameObject.SetActive(false);
        }

        TrailRenderer[] trailRenderers = GetComponentsInChildren<TrailRenderer>(true);
        foreach (TrailRenderer trailRenderer in trailRenderers)
        {
            trailRenderer.enabled = false;
        }

        LineRenderer[] lineRenderers = GetComponentsInChildren<LineRenderer>(true);
        foreach (LineRenderer lineRenderer in lineRenderers)
        {
            lineRenderer.enabled = false;
        }
    }

    private void SetGeneratedVisualsActive(bool isActive)
    {
        if (_generatedRoot != null)
        {
            _generatedRoot.gameObject.SetActive(isActive);
        }
    }

    private Material CreateMaterial(string materialName, Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new(shader)
        {
            name = materialName,
            color = color
        };

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", 1f);
        }

        if (material.HasProperty("_Blend"))
        {
            material.SetFloat("_Blend", 0f);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 2.5f);
        }

        material.enableInstancing = true;
        return material;
    }
}
