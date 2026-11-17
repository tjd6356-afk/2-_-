using System;

public interface IHealth
{
    float MaxHealth { get; }

    float CurrentHealth { get; }

    bool IsDead { get; }


    event Action<float, float> OnHealthChanged;

    event Action OnDied;


    void TakeDamage(float damage);

    void Heal(float amount);
}