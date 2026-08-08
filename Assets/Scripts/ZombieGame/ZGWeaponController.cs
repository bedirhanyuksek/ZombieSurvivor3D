using System.Collections;
using UnityEngine;

public sealed class ZGWeaponController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform muzzlePoint;
    [SerializeField] private LayerMask hitMask = ~0;
    [SerializeField] private int damage = 34;
    [SerializeField] private float range = 90f;
    [SerializeField] private float fireCooldown = 0.16f;
    [SerializeField] private int magazineSize = 24;
    [SerializeField] private float reloadDuration = 1.1f;
    [SerializeField] private LineRenderer tracerPrefab;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] fireClips;
    [SerializeField] private AudioClip reloadClipAsset;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Transform recoilRoot;
    [SerializeField] private Transform thirdPersonWeaponRoot;
    [SerializeField] private Transform firstPersonWeaponRoot;

    private int ammo;
    private AudioClip fallbackFireClip;
    private AudioClip fallbackReloadClip;
    private float nextFireTime;
    private bool reloading;
    private float recoilAmount;
    private float reloadAmount;

    public System.Action<int, int, bool> AmmoChanged;
    public System.Action<bool> HitConfirmed;
    public System.Action Fired;
    public System.Action ReloadStarted;

    private void Awake()
    {
        ammo = magazineSize;
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        fallbackFireClip = CreateGunClip("ZG_RifleShot", 0.11f, 0.82f);
        fallbackReloadClip = CreateReloadClip("ZG_Reload");
    }

    private void Start()
    {
        AmmoChanged?.Invoke(ammo, magazineSize, false);
    }

    private void Update()
    {
        recoilAmount = Mathf.MoveTowards(recoilAmount, 0f, Time.deltaTime * 9f);

        if (ZGInput.ReloadPressed)
        {
            StartCoroutine(Reload());
        }

        if (ZGInput.FireHeld)
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (reloading || Time.time < nextFireTime)
        {
            return;
        }

        if (ammo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        nextFireTime = Time.time + fireCooldown;
        ammo--;
        AmmoChanged?.Invoke(ammo, magazineSize, false);

        var ray = BuildAimRay();
        var hitPoint = ray.origin + ray.direction * range;
        var hitEnemy = false;
        var hits = Physics.RaycastAll(ray, range, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            hitPoint = hit.point;
            var enemy = hit.collider.GetComponentInParent<ZGZombieAI>();
            if (enemy != null)
            {
                var hitZone = hit.collider.GetComponent<ZGHitZone>();
                var headshot = hitZone != null && hitZone.IsHeadshot;
                enemy.TakeDamage(damage, hit.point, headshot);
                HitConfirmed?.Invoke(headshot);
                hitEnemy = true;
            }
            break;
        }

        if (!hitEnemy && TryCloseRangeBodyHit(out var closeHitPoint))
        {
            hitPoint = closeHitPoint;
            hitEnemy = true;
        }

        recoilAmount = 1f;

        var clip = fireClips != null && fireClips.Length > 0 ? fireClips[Random.Range(0, fireClips.Length)] : fallbackFireClip;
        audioSource.PlayOneShot(clip, 0.95f);
        Fired?.Invoke();
        SpawnMuzzleFlash();
        StartCoroutine(ShowTracer(GetTracerStart(), hitPoint));
    }

    private Ray BuildAimRay()
    {
        if (playerCamera == null)
        {
            return new Ray(transform.position + Vector3.up * 1.45f, transform.forward);
        }

        var viewportPoint = new Vector3(0.5f, 0.5f, 0f);
        return playerCamera.ViewportPointToRay(viewportPoint);
    }

    private Vector3 GetTracerStart()
    {
        if (muzzlePoint != null)
        {
            return muzzlePoint.position;
        }

        var activeWeapon = GetActiveWeaponRoot();
        return activeWeapon != null ? activeWeapon.position : transform.position + Vector3.up * 1.25f + transform.forward * 0.65f;
    }

    private Transform GetActiveWeaponRoot()
    {
        if (firstPersonWeaponRoot != null && firstPersonWeaponRoot.gameObject.activeInHierarchy)
        {
            return firstPersonWeaponRoot;
        }

        if (thirdPersonWeaponRoot != null && thirdPersonWeaponRoot.gameObject.activeInHierarchy)
        {
            return thirdPersonWeaponRoot;
        }

        return recoilRoot;
    }

    private bool TryCloseRangeBodyHit(out Vector3 hitPoint)
    {
        var origin = transform.position + Vector3.up * 1.15f;
        var direction = transform.forward;
        var hits = Physics.SphereCastAll(origin, 0.55f, direction, 2.35f, hitMask, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var hit in hits)
        {
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            var enemy = hit.collider.GetComponentInParent<ZGZombieAI>();
            if (enemy != null)
            {
                hitPoint = hit.point;
                enemy.TakeDamage(damage, hit.point, false);
                HitConfirmed?.Invoke(false);
                return true;
            }
        }

        hitPoint = origin + direction * 2.35f;
        return false;
    }

    private IEnumerator Reload()
    {
        if (reloading || ammo == magazineSize)
        {
            yield break;
        }

        reloading = true;
        ReloadStarted?.Invoke();
        audioSource.PlayOneShot(reloadClipAsset != null ? reloadClipAsset : fallbackReloadClip, 0.85f);
        AmmoChanged?.Invoke(ammo, magazineSize, true);
        yield return new WaitForSeconds(reloadDuration);
        ammo = magazineSize;
        reloading = false;
        AmmoChanged?.Invoke(ammo, magazineSize, false);
    }

    private IEnumerator RecoilRoutine()
    {
        if (recoilRoot == null)
        {
            yield break;
        }

        var start = recoilRoot.localRotation;
        var kick = start * Quaternion.Euler(-7f, 0f, 0f);
        var elapsed = 0f;
        while (elapsed < 0.055f)
        {
            elapsed += Time.deltaTime;
            recoilRoot.localRotation = Quaternion.Slerp(start, kick, elapsed / 0.055f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.12f)
        {
            elapsed += Time.deltaTime;
            recoilRoot.localRotation = Quaternion.Slerp(kick, start, elapsed / 0.12f);
            yield return null;
        }
    }

    private void SpawnMuzzleFlash()
    {
        if (muzzleFlashPrefab == null || muzzlePoint == null)
        {
            return;
        }

        var flash = Instantiate(muzzleFlashPrefab, muzzlePoint.position, muzzlePoint.rotation);
        Destroy(flash, 0.35f);
    }

    private IEnumerator ShowTracer(Vector3 start, Vector3 end)
    {
        var tracer = tracerPrefab != null ? Instantiate(tracerPrefab) : CreateTracer();
        tracer.SetPosition(0, start);
        tracer.SetPosition(1, end);
        yield return new WaitForSeconds(0.035f);
        Destroy(tracer.gameObject);
    }

    private LineRenderer CreateTracer()
    {
        var go = new GameObject("ZG_Tracer");
        var line = go.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.06f;
        line.endWidth = 0.018f;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = new Color(1f, 0.86f, 0.28f, 1f);
        line.endColor = new Color(1f, 0.25f, 0.08f, 0.25f);
        return line;
    }

    private static AudioClip CreateGunClip(string name, float duration, float tone)
    {
        const int sampleRate = 44100;
        var sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[sampleCount];
        var random = new System.Random(name.GetHashCode());
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleCount;
            var envelope = Mathf.Exp(-t * 24f);
            var noise = (float)(random.NextDouble() * 2.0 - 1.0);
            var body = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(210f * tone, 72f * tone, t) * i / sampleRate);
            samples[i] = Mathf.Clamp((noise * 0.75f + body * 0.32f) * envelope, -1f, 1f);
        }

        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip CreateReloadClip(string name)
    {
        const int sampleRate = 44100;
        const float duration = 0.82f;
        var sampleCount = Mathf.CeilToInt(duration * sampleRate);
        var samples = new float[sampleCount];
        var random = new System.Random(name.GetHashCode());
        for (var i = 0; i < sampleCount; i++)
        {
            var t = i / (float)sampleRate;
            samples[i] += MetalClick(t, 0.05f, 0.09f, 1200f, random);
            samples[i] += MetalClick(t, 0.24f, 0.12f, 860f, random);
            samples[i] += MetalClick(t, 0.55f, 0.16f, 1450f, random);
            samples[i] = Mathf.Clamp(samples[i], -1f, 1f);
        }

        var clip = AudioClip.Create(name, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static float MetalClick(float time, float start, float length, float frequency, System.Random random)
    {
        var x = time - start;
        if (x < 0f || x > length)
        {
            return 0f;
        }

        var envelope = Mathf.Exp(-(x / length) * 8f);
        var noise = (float)(random.NextDouble() * 2.0 - 1.0);
        var ring = Mathf.Sin(2f * Mathf.PI * frequency * x) * 0.55f;
        return (noise * 0.35f + ring) * envelope;
    }
}
