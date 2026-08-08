using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class ZGZombieAI : MonoBehaviour
{
    private enum State { Idle, Walk, Run, Attack, Dead }

    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int contactDamage = 12;
    [SerializeField] private float walkSpeed = 1.7f;
    [SerializeField] private float runSpeed = 2.9f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.05f;
    [SerializeField] private Transform modelRoot;
    [SerializeField] private Transform[] modelVariants;
    [SerializeField] private AudioClip[] hitClips;
    [SerializeField] private AudioClip[] deathClips;
    [SerializeField] private AudioClip[] attackClips;
    [SerializeField] private GameObject bloodPrefab;
    [SerializeField] private GameObject headshotBloodPrefab;
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField, Range(0f, 1f)] private float healthDropChance = 0.28f;
    [SerializeField] private float headshotDamageMultiplier = 3f;

    private CharacterController controller;
    private Transform target;
    private ZGHealth targetHealth;
    private ZGGameManager gameManager;
    private int health;
    private float nextAttackTime;
    private State state;
    private Vector3 modelStartScale;
    private Renderer[] renderers;
    private Color[] rendererColors;
    private bool dead;
    private AudioSource audioSource;
    private AudioClip fallbackDeathClip;
    private Animator animator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        fallbackDeathClip = AudioClip.Create("ZG_ZombieDeath", 4410, 1, 44100, false);
        var samples = new float[4410];
        var random = new System.Random(name.GetHashCode());
        for (var i = 0; i < samples.Length; i++)
        {
            var t = i / (float)samples.Length;
            samples[i] = ((float)(random.NextDouble() * 2.0 - 1.0) * 0.28f + Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(120f, 48f, t) * i / 44100f) * 0.45f) * Mathf.Exp(-t * 5f);
        }
        fallbackDeathClip.SetData(samples, 0);
        health = maxHealth;
        SelectModelVariant(0);
        animator = GetComponentInChildren<Animator>();
    }

    public void Initialize(Transform player, ZGGameManager manager, int bonusHealth, float speedBonus)
    {
        target = player;
        targetHealth = player.GetComponent<ZGHealth>();
        gameManager = manager;
        maxHealth += bonusHealth;
        health = maxHealth;
        walkSpeed += speedBonus;
        runSpeed += speedBonus;
        SelectModelVariant(bonusHealth > 0 ? 1 : 0);
    }

    private void Update()
    {
        if (dead || target == null || targetHealth == null || targetHealth.IsDead)
        {
            SetState(State.Idle);
            return;
        }

        var flatTarget = new Vector3(target.position.x, transform.position.y, target.position.z);
        var direction = flatTarget - transform.position;
        var distance = direction.magnitude;
        if (distance > 0.05f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction.normalized), Time.deltaTime * 8f);
        }

        if (distance <= attackRange)
        {
            SetState(State.Attack);
            TryAttack();
            return;
        }

        SetState(distance > 8f ? State.Run : State.Walk);
        var speed = state == State.Run ? runSpeed : walkSpeed;
        UpdateAnimator();
        controller.SimpleMove(direction.normalized * speed);
        AnimateLocomotion();
    }

    public void TakeDamage(int amount, Vector3 hitPoint)
    {
        TakeDamage(amount, hitPoint, IsLikelyHeadshot(hitPoint));
    }

    public void TakeDamage(int amount, Vector3 hitPoint, bool headshot)
    {
        if (dead)
        {
            return;
        }

        var finalDamage = headshot ? Mathf.RoundToInt(amount * headshotDamageMultiplier) : amount;
        health = Mathf.Max(health - finalDamage, 0);
        PlayRandom(hitClips, 0.75f);
        SpawnBlood(hitPoint, headshot);
        StartCoroutine(HitFlash());
        if (health == 0)
        {
            Die();
        }
    }

    private void TryAttack()
    {
        AnimateAttack();
        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + attackCooldown;
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }
        PlayRandom(attackClips, 0.7f);
        targetHealth.TakeDamage(contactDamage);
    }

    private void Die()
    {
        dead = true;
        PlayRandom(deathClips, 0.95f, fallbackDeathClip);
        TryDropHealthPickup();
        SetState(State.Dead);
        if (animator != null)
        {
            animator.SetTrigger("Die");
        }
        gameManager?.RegisterZombieKilled();
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        var elapsed = 0f;
        var start = transform.rotation;
        var end = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        while (elapsed < 0.55f)
        {
            elapsed += Time.deltaTime;
            transform.rotation = Quaternion.Slerp(start, end, elapsed / 0.55f);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);
        Destroy(gameObject);
    }

    private IEnumerator HitFlash()
    {
        foreach (var r in renderers)
        {
            if (r != null)
            {
                r.material.color = Color.white;
            }
        }

        yield return new WaitForSeconds(0.06f);
        foreach (var r in renderers)
        {
            if (r != null)
            {
                var index = System.Array.IndexOf(renderers, r);
                r.material.color = index >= 0 && index < rendererColors.Length ? rendererColors[index] : Color.white;
            }
        }
    }

    private void SetState(State next)
    {
        state = next;
    }

    private void SelectModelVariant(int index)
    {
        if (modelVariants != null && modelVariants.Length > 0)
        {
            index = Mathf.Clamp(index, 0, modelVariants.Length - 1);
            for (var i = 0; i < modelVariants.Length; i++)
            {
                if (modelVariants[i] != null)
                {
                    modelVariants[i].gameObject.SetActive(i == index);
                }
            }

            modelRoot = modelVariants[index];
            animator = modelRoot != null ? modelRoot.GetComponentInChildren<Animator>() : null;
        }

        renderers = modelRoot != null ? modelRoot.GetComponentsInChildren<Renderer>() : GetComponentsInChildren<Renderer>();
        rendererColors = new Color[renderers.Length];
        for (var i = 0; i < renderers.Length; i++)
        {
            rendererColors[i] = renderers[i] != null && renderers[i].material.HasProperty("_Color") ? renderers[i].material.color : Color.white;
        }
        modelStartScale = modelRoot != null ? modelRoot.localScale : Vector3.one;
    }

    private void AnimateLocomotion()
    {
        if (modelRoot == null || animator != null)
        {
            return;
        }

        var bob = Mathf.Sin(Time.time * (state == State.Run ? 10f : 6f)) * 0.08f;
        modelRoot.localScale = modelStartScale + new Vector3(0f, bob, 0f);
    }

    private void AnimateAttack()
    {
        if (modelRoot == null || animator != null)
        {
            return;
        }

        var punch = Mathf.Sin(Time.time * 18f) * 0.12f;
        modelRoot.localPosition = new Vector3(0f, 0f, punch);
    }

    private void UpdateAnimator()
    {
        if (animator == null)
        {
            return;
        }

        animator.SetFloat("Speed", state == State.Run ? 2f : 1f, 0.12f, Time.deltaTime);
    }

    private void PlayRandom(AudioClip[] clips, float volume, AudioClip fallback = null)
    {
        var clip = clips != null && clips.Length > 0 ? clips[Random.Range(0, clips.Length)] : fallback;
        if (clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }

    private bool IsLikelyHeadshot(Vector3 hitPoint)
    {
        return hitPoint.y - transform.position.y > 1.35f;
    }

    private void SpawnBlood(Vector3 hitPoint, bool headshot)
    {
        var prefab = headshot && headshotBloodPrefab != null ? headshotBloodPrefab : bloodPrefab;
        if (prefab == null)
        {
            return;
        }

        var fx = Instantiate(prefab, hitPoint, Quaternion.LookRotation(-transform.forward, Vector3.up));
        fx.transform.localScale *= headshot ? 1.35f : 1f;
        Destroy(fx, headshot ? 3.2f : 2.5f);
    }

    private void TryDropHealthPickup()
    {
        if (healthPickupPrefab == null || Random.value > healthDropChance)
        {
            return;
        }

        Instantiate(healthPickupPrefab, transform.position + Vector3.up * 0.35f, Quaternion.identity);
    }
}
