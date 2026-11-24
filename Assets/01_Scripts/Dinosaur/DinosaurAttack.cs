using UnityEngine;

public class DinosaurAttack : MonoBehaviour
{
    public enum Species { Ankylosaurus, Raptor, Pterosaur, Brachiosaurus, Pachycephalosaurus }
    private enum Mutation { None, Fire, Lightning, Ice }

    [SerializeField] private Species species;
    public Species Kind => species;
    private Mutation mutation;

    public void Configure(Species kind, int power, float interval, float range, bool animated)
    {
        species = kind;
        damage = power;
        attackInterval = interval;
        attackRange = range;
        useAttackAnimation = animated;
    }

    public bool TryMerge(DinosaurAttack source)
    {
        if (species != source.species || mutation != Mutation.None || source.mutation != Mutation.None)
            return false;

        int roll = Random.Range(0, 100);
        mutation = roll < 40 ? Mutation.Fire : roll < 70 ? Mutation.Lightning : Mutation.Ice;
        damage = Mathf.RoundToInt(damage * 1.6f);

        if (TryGetComponent(out SpriteRenderer art))
            art.color = mutation switch
            {
                Mutation.Fire => new Color32(255, 162, 107, 255),
                Mutation.Lightning => new Color32(255, 235, 116, 255),
                _ => new Color32(133, 216, 255, 255)
            };

        return true;
    }

    [SerializeField, Min(1)] private int maxHealth = 100;
    [SerializeField, Min(1)] private int damage = 10;
    [SerializeField, Min(0.01f)] private float attackInterval = 0.3f;
    [SerializeField, Min(0f)] private float attackRange = 2f;

    [SerializeField] private bool useAttackAnimation;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");

    private DinosaurHealthBar healthBar;
    public int CurrentHealth { get; private set; }

    public void TakeDamage(int amount)
    {
        if (amount <= 0 || CurrentHealth <= 0)
            return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        healthBar?.SetHealth(CurrentHealth, maxHealth);
        if (CurrentHealth == 0)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    private Animator animator;
    private EnemyHealth pendingTarget;
    private bool waitingForImpact;
    private double nextAttackTime;
    private int enemyLayer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enemyLayer = LayerMask.NameToLayer("Enemy");

        CurrentHealth = maxHealth;
        healthBar = DinosaurHealthBar.Attach(GetComponentInChildren<SpriteRenderer>(), new Color32(121, 221, 105, 255));
        healthBar?.SetHealth(CurrentHealth, maxHealth);
    }

    private void Update()
    {
        double now = Time.timeAsDouble;
        if (now < nextAttackTime)
            return;

        bool playAnimation = useAttackAnimation && animator != null;
        if (playAnimation && (waitingForImpact || animator.GetCurrentAnimatorStateInfo(0).IsTag("Attack")))
            return;

        EnemyHealth target = FindNearestEnemy();
        if (target == null)
            return;

        nextAttackTime = now + attackInterval;
        if (!playAnimation)
        {
            Hit(target);
            return;
        }

        pendingTarget = target;
        waitingForImpact = true;
        animator.SetTrigger(AttackTrigger);
    }

    // 공격 클립의 타격 프레임에서 Unity가 한 번 호출한다.
    public void ApplyDinosaurAttackHit()
    {
        if (!isActiveAndEnabled || !waitingForImpact)
            return;

        EnemyHealth target = pendingTarget;
        pendingTarget = null;
        waitingForImpact = false;

        if (target == null || !target.isActiveAndEnabled)
            return;

        Vector2 distance = (Vector2)target.transform.position - (Vector2)transform.position;
        if (distance.sqrMagnitude <= attackRange * attackRange)
            Hit(target);
    }

    private void Hit(EnemyHealth target)
    {
        Vector2 impactPosition = target.transform.position;
        target.TakeDamage(damage);

        if (mutation == Mutation.Fire && target.isActiveAndEnabled)
            target.ApplyBurn();
        else if (mutation == Mutation.Ice && target.isActiveAndEnabled)
            target.ApplySlow();
        else if (mutation == Mutation.Lightning)
        {
            // TakeDamage can remove enemies from the active set, so select both first.
            EnemyHealth first = null;
            EnemyHealth second = null;
            foreach (EnemyHealth enemy in EnemyHealth.ActiveEnemies)
            {
                if (enemy == target || ((Vector2)enemy.transform.position - impactPosition).sqrMagnitude > 1f)
                    continue;

                if (first == null)
                    first = enemy;
                else
                {
                    second = enemy;
                    break;
                }
            }

            int chainDamage = Mathf.RoundToInt(damage * 0.4f);
            first?.TakeDamage(chainDamage);
            second?.TakeDamage(chainDamage);
        }
    }

    private void OnDisable()
    {
        waitingForImpact = false;
        pendingTarget = null;
        if (useAttackAnimation && animator != null)
            animator.ResetTrigger(AttackTrigger);
    }

    private EnemyHealth FindNearestEnemy()
    {
        EnemyHealth target = null;
        Vector2 position = transform.position;
        float nearestDistanceSquared = attackRange * attackRange;

        foreach (EnemyHealth enemy in EnemyHealth.ActiveEnemies)
        {
            if (enemy.gameObject.layer != enemyLayer || !enemy.CompareTag("Enemy"))
                continue;

            float distanceSquared = ((Vector2)enemy.transform.position - position).sqrMagnitude;
            if (distanceSquared > nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            target = enemy;
        }

        return target;
    }
}
