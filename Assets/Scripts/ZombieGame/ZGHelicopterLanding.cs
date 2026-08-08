using UnityEngine;

public sealed class ZGHelicopterLanding : MonoBehaviour
{
    [SerializeField] private Transform rotor;
    [SerializeField] private Vector3 landedLocalPosition;
    [SerializeField] private float landingDuration = 5f;
    [SerializeField] private AudioSource audioSource;

    private Vector3 startLocalPosition;
    private float landingTime;
    private bool landing;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = 8f;
        audioSource.maxDistance = 70f;
        audioSource.volume = 0.45f;
        audioSource.clip = CreateHelicopterLoop();
        audioSource.Play();
    }

    private void Update()
    {
        if (rotor != null)
        {
            rotor.Rotate(Vector3.up, 900f * Time.deltaTime, Space.Self);
        }

        if (!landing)
        {
            return;
        }

        landingTime += Time.deltaTime;
        var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(landingTime / landingDuration));
        transform.localPosition = Vector3.Lerp(startLocalPosition, landedLocalPosition, t);
    }

    public void BeginLanding()
    {
        landing = true;
        landingTime = 0f;
    }

    private static AudioClip CreateHelicopterLoop()
    {
        const int sampleRate = 44100;
        const float duration = 1.2f;
        var sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[sampleCount];
        var random = new System.Random(1847);
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            var blade = Mathf.Sin(2f * Mathf.PI * 18f * t) * 0.42f;
            var engine = Mathf.Sin(2f * Mathf.PI * 86f * t) * 0.15f;
            var wobble = Mathf.Sin(2f * Mathf.PI * 5f * t) * 0.08f;
            var noise = (float)(random.NextDouble() * 2.0 - 1.0) * 0.08f;
            samples[i] = Mathf.Clamp((blade + engine + wobble + noise) * 0.45f, -1f, 1f);
        }

        var clip = AudioClip.Create("ZG_HelicopterLoop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
