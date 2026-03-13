using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private bool destroyOnDeath;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0f;

    private void Awake()
    {
        if (maxHealth < 1f)
        {
            maxHealth = 1f;
        }

        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
    }

    public void Configure(float newMaxHealth, bool fillCurrentHealth, bool destroyOnDeathOnZero)
    {
        maxHealth = Mathf.Max(1f, newMaxHealth);
        destroyOnDeath = destroyOnDeathOnZero;

        if (fillCurrentHealth)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            if (currentHealth <= 0f)
            {
                currentHealth = maxHealth;
            }
        }
    }

    public void ApplyDamage(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        if (!IsAlive && destroyOnDeath)
        {
            Destroy(gameObject);
        }
    }

    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
