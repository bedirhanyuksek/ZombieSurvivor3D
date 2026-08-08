using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class ZGHealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 30;
    [SerializeField] private float lifeTime = 18f;
    [SerializeField] private AudioClip pickupClip;

    private void Awake()
    {
        var trigger = GetComponent<Collider>();
        trigger.isTrigger = true;
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, 110f * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        var health = other.GetComponentInParent<ZGHealth>();
        if (health == null || health.IsDead || health.CurrentHealth >= health.MaxHealth)
        {
            return;
        }

        health.Heal(healAmount);
        if (pickupClip != null)
        {
            AudioSource.PlayClipAtPoint(pickupClip, transform.position, 0.8f);
        }
        Destroy(gameObject);
    }
}
