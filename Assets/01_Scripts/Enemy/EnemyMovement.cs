using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-10)]
public class EnemyMovement : MonoBehaviour
{
    [SerializeField] private GameObject eggTarget;
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private float stopDistance = 0.6f;

    [SerializeField, Min(0.01f)] private float blockerProbeRadius = 0.23f;

    private const float ColliderStopGap = 0.02f;
    private readonly List<RaycastHit2D> blockerHits = new();
    private ContactFilter2D blockerFilter;
    private Animator animator;
    private EnemyHealth health;
    private Transform stopPoint;
    private DinoPlacementBoard placementBoard;
    private EnemyAttack enemyAttack;
    private bool hasArrived;

    private void Awake()
    {
        enemyAttack = GetComponent<EnemyAttack>();
        animator = GetComponent<Animator>();
        health = GetComponent<EnemyHealth>();
        blockerFilter = new ContactFilter2D { useTriggers = true };
        blockerFilter.SetLayerMask(LayerMask.GetMask("Dino"));
    }

    private void Start()
    {
        if (stopPoint != null)
            return;

        if (eggTarget == null)
            eggTarget = GameObject.FindGameObjectWithTag("Egg");

        if (enemyAttack != null && eggTarget != null)
            enemyAttack.Prepare(eggTarget.GetComponent<NestHealth>());
    }

    public void SetTarget(Transform target, NestHealth targetNest = null, DinoPlacementBoard board = null)
    {
        stopPoint = target;
        placementBoard = board;
        hasArrived = false;
        enabled = true;

        if (enemyAttack != null)
            enemyAttack.Prepare(targetNest);
    }

    private void Update()
    {
        Move();
    }

    public void Move()
    {
        if (hasArrived || (stopPoint == null && eggTarget == null))
            return;

        Vector3 current = transform.position;
        Vector3 target = stopPoint != null ? stopPoint.position : eggTarget.transform.position;
        target.z = current.z;

        Vector2 toTarget = target - current;
        // 스포너가 넘긴 정지점은 이미 둥지 앞에 있다.
        float distanceFromTarget = stopPoint == null ? stopDistance : 0f;
        float distanceToStop = toTarget.magnitude - distanceFromTarget;
        if (distanceToStop <= 0f)
        {
            Arrive();
            return;
        }

        float speedMultiplier = health == null ? 1f : health.SpeedMultiplier;
        float maxStep = Mathf.Min(moveSpeed * speedMultiplier * Time.deltaTime, distanceToStop);
        if (maxStep <= 0f)
            return;

        Vector2 directionToNest = toTarget.normalized;
        DinosaurAttack blocker;
        float allowedStep = placementBoard != null
            ? placementBoard.LimitStep(current, directionToNest, maxStep, out blocker)
            : LimitStepAtDinoCollider(current, directionToNest, maxStep, out blocker);
        transform.position = current + (Vector3)(directionToNest * allowedStep);

        if (blocker != null && enemyAttack != null)
        {
            enemyAttack.BeginAttacking(blocker);
            return;
        }

        if (enemyAttack != null && enemyAttack.enabled)
            enemyAttack.StopAttacking();

        if (animator != null)
            animator.speed = allowedStep < maxStep ? 0f : 1f;

        if (allowedStep >= distanceToStop)
            Arrive();
    }

    private float LimitStepAtDinoCollider(Vector2 position, Vector2 direction, float maxStep, out DinosaurAttack blocker)
    {
        blocker = null;
        Physics2D.CircleCast(position, blockerProbeRadius, direction, blockerFilter,
            blockerHits, maxStep + ColliderStopGap);

        float allowedStep = maxStep;
        foreach (RaycastHit2D hit in blockerHits)
        {
            var dino = hit.collider.GetComponentInParent<DinosaurAttack>();
            float stop = Mathf.Max(0f, hit.distance - ColliderStopGap);
            if (dino == null || !dino.isActiveAndEnabled || dino.CurrentHealth <= 0 || stop > allowedStep)
                continue;

            blocker = dino;
            allowedStep = stop;
        }

        return allowedStep;
    }

    private void Arrive()
    {
        hasArrived = true;
        enabled = false;

        if (enemyAttack != null)
            enemyAttack.BeginAttacking();
    }
}
