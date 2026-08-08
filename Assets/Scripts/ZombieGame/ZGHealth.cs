using System;
using UnityEngine;

public sealed class ZGHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private bool dead;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action<int> Healed;
    public event Action Died;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsDead => dead;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void Start()
    {
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (dead || amount <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(currentHealth - amount, 0);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Damaged?.Invoke(amount);
        if (currentHealth == 0)
        {
            dead = true;
            Died?.Invoke();
        }
    }

    public void Heal(int amount)
    {
        if (dead || amount <= 0)
        {
            return;
        }

        var before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
        Healed?.Invoke(currentHealth - before);
    }
}
