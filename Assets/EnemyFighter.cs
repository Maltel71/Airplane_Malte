using UnityEngine;

public class EnemyFighter : MonoBehaviour
{
    [Header("Flight Settings")]
    public float throttle = 8f;
    public float maxSpeed = 20f;
    public float liftPower = 40f;
    public float dragFactor = 0.98f;

    [Header("Maneuverability")]
    public float turnSpeed = 15f;
    public float rollSpeed = 20f;
    public float pitchSpeed = 10f;

    [Header("Stabilization")]
    public float rollDamping = 5f;
    public float pitchDamping = 2f;
    public float rollStabilization = 5f;
    public float pitchStabilization = 1f;

    [Header("AI Behavior")]
    public Transform target;
    public float engageDistance = 100f;
    public float attackDistance = 50f;
    public float avoidDistance = 15f;

    [Header("Combat")]
    public Transform gunPoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.3f;
    public float bulletSpeed = 80f;

    private Rigidbody rb;
    private float lastFireTime;
    private AIState currentState = AIState.Patrol;
    private Vector3 patrolTarget;
    private float stateTimer;

    enum AIState { Patrol, Pursue, Attack, Evade }

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Find player if no target assigned
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }

        // Set random patrol point
        SetRandomPatrolPoint();
    }

    void Update()
    {
        UpdateAI();
    }

    void FixedUpdate()
    {
        ApplyFlight();
    }

    void UpdateAI()
    {
        if (target == null) return;

        float distToTarget = Vector3.Distance(transform.position, target.position);
        stateTimer += Time.deltaTime;

        // State transitions
        switch (currentState)
        {
            case AIState.Patrol:
                if (distToTarget < engageDistance)
                    ChangeState(AIState.Pursue);
                break;

            case AIState.Pursue:
                if (distToTarget < attackDistance)
                    ChangeState(AIState.Attack);
                else if (distToTarget > engageDistance * 1.5f)
                    ChangeState(AIState.Patrol);
                break;

            case AIState.Attack:
                if (distToTarget > attackDistance * 1.5f)
                    ChangeState(AIState.Pursue);
                else if (distToTarget < avoidDistance)
                    ChangeState(AIState.Evade);
                break;

            case AIState.Evade:
                if (distToTarget > avoidDistance * 2f)
                    ChangeState(AIState.Attack);
                break;
        }

        // Execute current state
        ExecuteState();
    }

    void ChangeState(AIState newState)
    {
        currentState = newState;
        stateTimer = 0f;

        if (newState == AIState.Patrol)
            SetRandomPatrolPoint();
    }

    void ExecuteState()
    {
        Vector3 targetPos = Vector3.zero;

        switch (currentState)
        {
            case AIState.Patrol:
                targetPos = patrolTarget;
                // Set new patrol point if close to current one
                if (Vector3.Distance(transform.position, patrolTarget) < 20f)
                    SetRandomPatrolPoint();
                break;

            case AIState.Pursue:
                targetPos = target.position;
                break;

            case AIState.Attack:
                targetPos = PredictTargetPosition();
                TryFire();
                break;

            case AIState.Evade:
                targetPos = transform.position + (transform.position - target.position).normalized * 50f;
                break;
        }

        FlyTowards(targetPos);
    }

    Vector3 PredictTargetPosition()
    {
        if (target == null) return Vector3.zero;

        Rigidbody targetRb = target.GetComponent<Rigidbody>();
        if (targetRb != null)
        {
            float timeToTarget = Vector3.Distance(transform.position, target.position) / bulletSpeed;
            return target.position + targetRb.linearVelocity * timeToTarget;
        }

        return target.position;
    }

    void FlyTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        Vector3 localDir = transform.InverseTransformDirection(direction);

        // Calculate desired rotation inputs
        float desiredPitch = Mathf.Clamp(-localDir.y, -1f, 1f);
        float desiredYaw = Mathf.Clamp(localDir.x, -1f, 1f);
        float desiredRoll = -desiredYaw * 0.7f; // Bank into turns

        // Apply rotational forces
        rb.AddTorque(transform.right * desiredPitch * pitchSpeed);
        rb.AddTorque(transform.up * desiredYaw * turnSpeed);
        rb.AddTorque(transform.forward * desiredRoll * rollSpeed);

        // Auto-level when not turning aggressively
        if (Mathf.Abs(desiredRoll) < 0.3f)
        {
            Vector3 levelTorque = Vector3.Cross(transform.up, Vector3.up) * rollStabilization;
            rb.AddTorque(levelTorque);
        }

        // Pitch stabilization - prevent extreme nose diving
        if (Mathf.Abs(desiredPitch) < 0.2f && transform.forward.y < -0.5f)
        {
            Vector3 pitchUpTorque = transform.right * pitchStabilization;
            rb.AddTorque(pitchUpTorque);
        }

        // Apply damping to reduce oscillations
        ApplyStabilizationDamping();
    }

    void ApplyFlight()
    {
        // Thrust
        Vector3 thrust = transform.forward * throttle;
        rb.AddForce(thrust);

        // Lift
        Vector3 lift = Vector3.up * rb.linearVelocity.magnitude * liftPower / 1000f;
        rb.AddForce(lift);

        // Drag
        rb.linearVelocity *= dragFactor;

        // Speed limit
        if (rb.linearVelocity.magnitude > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
    }

    void ApplyStabilizationDamping()
    {
        Vector3 angularVel = rb.angularVelocity;

        // Apply roll damping
        float rollVel = Vector3.Dot(angularVel, transform.forward);
        Vector3 rollDampingTorque = -transform.forward * rollVel * rollDamping;
        rb.AddTorque(rollDampingTorque);

        // Apply pitch damping
        float pitchVel = Vector3.Dot(angularVel, transform.right);
        Vector3 pitchDampingTorque = -transform.right * pitchVel * pitchDamping;
        rb.AddTorque(pitchDampingTorque);
    }

    void TryFire()
    {
        if (Time.time - lastFireTime < fireRate || gunPoint == null || bulletPrefab == null)
            return;

        // Check if target is roughly in front
        Vector3 toTarget = (target.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, toTarget);

        if (dot > 0.7f) // ~45 degree cone
        {
            FireBullet();
            lastFireTime = Time.time;
        }
    }

    void FireBullet()
    {
        GameObject bullet = Instantiate(bulletPrefab, gunPoint.position, gunPoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            bulletRb.linearVelocity = gunPoint.forward * bulletSpeed + rb.linearVelocity;
        }
    }

    void SetRandomPatrolPoint()
    {
        Vector3 randomDir = Random.insideUnitSphere.normalized;
        randomDir.y = Mathf.Abs(randomDir.y) * 0.3f; // Prefer staying above ground
        patrolTarget = transform.position + randomDir * Random.Range(50f, 100f);
    }

    void OnDrawGizmosSelected()
    {
        // Draw state info
        Gizmos.color = currentState == AIState.Attack ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(transform.position,
            currentState == AIState.Attack ? attackDistance : engageDistance);

        // Draw current target
        if (currentState == AIState.Patrol)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(patrolTarget, 5f);
            Gizmos.DrawLine(transform.position, patrolTarget);
        }
        else if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, target.position);
        }

        // Draw firing direction
        if (gunPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(gunPoint.position, gunPoint.forward * 20f);
        }
    }
}