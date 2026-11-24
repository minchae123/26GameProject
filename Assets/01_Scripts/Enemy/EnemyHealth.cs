using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    internal static readonly HashSet<EnemyHealth> ActiveEnemies = new();

    [SerializeField, Min(1)] private int maxHealth = 100;

    private DinosaurHealthBar healthBar;
    private int remainingBurnTicks;
    private double nextBurnTick;
    private double slowedUntil;

    public event System.Action Defeated;
    public float SpeedMultiplier => Time.timeAsDouble < slowedUntil ? 0.7f : 1f;

    public void ApplyBurn()
    {
        if (remainingBurnTicks == 0)
            nextBurnTick = Time.timeAsDouble + 1;
        remainingBurnTicks = 3;
    }

    public void ApplySlow()
    {
        slowedUntil = Time.timeAsDouble + 2;
    }

    private void Update()
    {
        if (remainingBurnTicks == 0 || Time.timeAsDouble < nextBurnTick)
            return;

        remainingBurnTicks--;
        nextBurnTick += 1;
        TakeDamage(4);
    }

    public int CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        healthBar = DinosaurHealthBar.Attach(GetComponentInChildren<SpriteRenderer>(), new Color32(242, 93, 91, 255));
        healthBar?.SetHealth(CurrentHealth, maxHealth);
    }

    private void OnEnable()
    {
        ActiveEnemies.Add(this);
    }

    private void OnDisable()
    {
        ActiveEnemies.Remove(this);
    }

    public void TakeDamage(int damage)
    {
        if (CurrentHealth <= 0 || damage <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        healthBar?.SetHealth(CurrentHealth, maxHealth);
        if (CurrentHealth == 0)
        {
            gameObject.SetActive(false);
            Defeated?.Invoke();
            Destroy(gameObject);
        }
    }
}
