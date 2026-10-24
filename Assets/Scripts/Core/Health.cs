using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    [Header("Runtime (debug view)")]
    [SerializeField] private float currentHealth;

    public float Max => maxHealth;
    public float Current => currentHealth;
    public float Normalized => maxHealth > 0f ? currentHealth / maxHealth : 0f; // 0 to 1, handy for health bars
    public bool IsDead { get; private set; }

    public event Action<float, float> Changed; // (current, max)
    public event Action Died;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(currentHealth - amount, 0f);
        Changed?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            IsDead = true;
            Died?.Invoke();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        Changed?.Invoke(currentHealth, maxHealth);
    }

    // Later the wave system can use this to make enemies tougher each wave
    public void SetMaxHealth(float value, bool refill = true)
    {
        maxHealth = value;
        if (refill) currentHealth = maxHealth;
        Changed?.Invoke(currentHealth, maxHealth);
    }

    // Lets you test damage from the Inspector (see testing steps below)
    [ContextMenu("Test: Take 10 Damage")]
    private void TestDamage()
    {
        TakeDamage(10f);
    }
}