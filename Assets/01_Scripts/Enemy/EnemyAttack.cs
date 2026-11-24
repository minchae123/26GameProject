using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float attackInterval = 1f;
    [SerializeField, Min(0f)] private float attackWindup;

    [SerializeField, Min(1)] private int dinosaurDamage = 15;

    private static readonly int AttackTrigger = Animator.StringToHash("Attack");
    private static readonly int AtNestParameter = Animator.StringToHash("AtNest");

    private NestHealth nest;
    private DinosaurAttack dinosaurTarget;
    private bool targetingDinosaur;
    private Animator animator;
    private double nextAttackTime;
    private bool attackPending;
    private double impactDeadline;

    private bool UsesAttackAnimation => animator != null && attackWindup > 0f;

    private float AttackCycleInterval => UsesAttackAnimation
        ? Mathf.Max(attackInterval, attackWindup * 2f)
        : Mathf.Max(0.01f, attackInterval);

    private void Awake()
    {
        animator = GetComponent<Animator>();
        enabled = false;
    }

    public void Prepare(NestHealth target)
    {
        nest = target;
        StopAttacking();
    }

    public void BeginAttacking(DinosaurAttack target = null)
    {
        if (enabled && dinosaurTarget == target)
            return;

        dinosaurTarget = target;
        targetingDinosaur = target != null;
        attackPending = false;
        nextAttackTime = Time.timeAsDouble + AttackCycleInterval - (UsesAttackAnimation ? attackWindup : 0f);

        if (UsesAttackAnimation)
        {
            animator.speed = 1f;
            animator.ResetTrigger(AttackTrigger);
            animator.SetBool(AtNestParameter, true);
        }
        else if (animator != null)
            animator.speed = 0f;

        enabled = targetingDinosaur || (nest != null && nest.HasEggs);
    }

    public void StopAttacking()
    {
        enabled = false;
        attackPending = false;
        targetingDinosaur = false;
        dinosaurTarget = null;

        if (animator == null)
            return;

        animator.speed = 1f;
        if (UsesAttackAnimation)
        {
            animator.ResetTrigger(AttackTrigger);
            animator.SetBool(AtNestParameter, false);
        }
    }

    private void Update()
    {
        if (targetingDinosaur
            ? dinosaurTarget == null || !dinosaurTarget.isActiveAndEnabled
            : nest == null || !nest.HasEggs)
        {
            StopAttacking();
            return;
        }

        double now = Time.timeAsDouble;
        if (attackPending)
        {
            // 애니메이션 이벤트가 누락되면 예정된 타격을 한 번만 처리한다.
            if (now >= impactDeadline)
                ApplyAttackHit();
            return;
        }

        if (now < nextAttackTime)
            return;

        nextAttackTime = now + AttackCycleInterval;
        if (UsesAttackAnimation)
        {
            attackPending = true;
            impactDeadline = now + attackWindup + 0.1f;
            animator.SetTrigger(AttackTrigger);
        }
        else
        {
            DamageTarget();
        }
    }

    // Called by the attack clip on its impact frame.
    public void ApplyAttackHit()
    {
        if (!isActiveAndEnabled || !attackPending)
            return;

        attackPending = false;
        DamageTarget();
    }

    private void DamageTarget()
    {
        if (targetingDinosaur)
        {
            if (dinosaurTarget != null && dinosaurTarget.isActiveAndEnabled)
                dinosaurTarget.TakeDamage(dinosaurDamage);
        }
        else if (nest != null)
        {
            nest.TakeHit();
        }
    }
}
