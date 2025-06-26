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

    [Header("Collision Avoidance")]
    public float minGroundHeight = 15f;
    public float groundAvoidForce = 20f;
    public float obstacleAvoidDistance = 30f;
    public float obstacleAvoidForce = 15f;
    public float avoidanceViewAngle = 60f;
    public LayerMask groundLayer = 1; // Default layer
    public LayerMask obstacleLayer = -1; // All layers

    [Header("Combat")]
    public Transform gunPoint;
    public GameObject bulletPrefab;
    public float fireRate = 0.3f;
    public float bulletSpeed = 80f;
    public float aimTolerance = 0.8f; // How accurate aim needs to be (0.8 = ~36 degree cone)
    public float leadTargetAccuracy = 1.2f; // Multiplier for prediction accuracy

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
        ApplyCollisionAvoidance();
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
                // Also aim the aircraft towards the predicted position for better shots
                FlyTowards(targetPos);
                return; // Skip the general FlyTowards call

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
            // Calculate time for bullet to reach target
            float distanceToTarget = Vector3.Distance(transform.position, target.position);
            float timeToTarget = distanceToTarget / bulletSpeed;

            // Predict where target will be, with accuracy multiplier
            Vector3 predictedPos = target.position + (targetRb.linearVelocity * timeToTarget * leadTargetAccuracy);

            // Add some vertical lead if target is climbing/diving
            Vector3 verticalVelocity = Vector3.Project(targetRb.linearVelocity, Vector3.up);
            predictedPos += verticalVelocity * timeToTarget * 0.5f;

            return predictedPos;
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

    void ApplyCollisionAvoidance()
    {
        // Ground avoidance
        AvoidGround();

        // Obstacle avoidance
        AvoidObstacles();
    }

    void AvoidGround()
    {
        // Raycast straight down to check ground distance
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, minGroundHeight * 2f, groundLayer))
        {
            float distanceToGround = hit.distance;

            if (distanceToGround < minGroundHeight)
            {
                // Calculate avoidance force - stronger when closer to ground
                float avoidanceStrength = (minGroundHeight - distanceToGround) / minGroundHeight;
                Vector3 upwardForce = Vector3.up * avoidanceStrength * groundAvoidForce;
                rb.AddForce(upwardForce, ForceMode.Acceleration);

                // Also apply pitch up torque for more aggressive climb
                Vector3 pitchUpTorque = transform.right * avoidanceStrength * groundAvoidForce * 0.5f;
                rb.AddTorque(pitchUpTorque);
            }
        }
    }

    void AvoidObstacles()
    {
        Vector3 forwardDir = transform.forward;
        Vector3 rightDir = transform.right;
        Vector3 upDir = transform.up;

        // Create a cone of rays to detect obstacles
        Vector3[] rayDirections = {
            forwardDir,
            Quaternion.AngleAxis(avoidanceViewAngle * 0.5f, upDir) * forwardDir,
            Quaternion.AngleAxis(-avoidanceViewAngle * 0.5f, upDir) * forwardDir,
            Quaternion.AngleAxis(avoidanceViewAngle * 0.5f, rightDir) * forwardDir,
            Quaternion.AngleAxis(-avoidanceViewAngle * 0.5f, rightDir) * forwardDir,
        };

        Vector3 totalAvoidanceForce = Vector3.zero;

        foreach (Vector3 rayDir in rayDirections)
        {
            if (Physics.Raycast(transform.position, rayDir, out RaycastHit hit, obstacleAvoidDistance, obstacleLayer))
            {
                // Skip if it's the ground (handled separately)
                if (hit.collider.CompareTag("Ground")) continue;

                // Calculate avoidance direction (perpendicular to the obstacle)
                Vector3 avoidDirection = Vector3.Cross(hit.normal, Vector3.up);
                if (Vector3.Dot(avoidDirection, rightDir) < 0)
                    avoidDirection = -avoidDirection;

                // Add upward component to avoid flying into obstacles
                avoidDirection += Vector3.up * 0.5f;
                avoidDirection.Normalize();

                // Calculate force strength based on distance
                float distanceRatio = 1f - (hit.distance / obstacleAvoidDistance);
                Vector3 avoidForce = avoidDirection * distanceRatio * obstacleAvoidForce;

                totalAvoidanceForce += avoidForce;
            }
        }

        // Apply the combined avoidance force
        if (totalAvoidanceForce.magnitude > 0.1f)
        {
            rb.AddForce(totalAvoidanceForce, ForceMode.Acceleration);

            // Convert avoidance force to rotational input for more natural steering
            Vector3 localAvoidance = transform.InverseTransformDirection(totalAvoidanceForce.normalized);

            // Apply steering torque based on avoidance direction
            float steerYaw = localAvoidance.x * obstacleAvoidForce * 0.3f;
            float steerPitch = localAvoidance.y * obstacleAvoidForce * 0.2f;

            rb.AddTorque(transform.up * steerYaw);
            rb.AddTorque(transform.right * steerPitch);
        }
    }

    void TryFire()
    {
        if (Time.time - lastFireTime < fireRate || gunPoint == null || bulletPrefab == null)
            return;

        // Get predicted target position
        Vector3 predictedTargetPos = PredictTargetPosition();
        Vector3 toTarget = (predictedTargetPos - gunPoint.position).normalized;

        // Check if predicted target is in firing cone
        float dot = Vector3.Dot(gunPoint.forward, toTarget);

        if (dot > aimTolerance)
        {
            FireBullet(predictedTargetPos);
            lastFireTime = Time.time;
        }
    }

    void FireBullet(Vector3 targetPosition)
    {
        GameObject bullet = Instantiate(bulletPrefab, gunPoint.position, gunPoint.rotation);
        Rigidbody bulletRb = bullet.GetComponent<Rigidbody>();

        if (bulletRb != null)
        {
            // Calculate firing direction towards predicted position
            Vector3 fireDirection = (targetPosition - gunPoint.position).normalized;

            // Add our own velocity to the bullet (realistic physics)
            Vector3 bulletVelocity = fireDirection * bulletSpeed + rb.linearVelocity * 0.3f;
            bulletRb.linearVelocity = bulletVelocity;
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

        // Draw firing direction and predicted target
        if (gunPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(gunPoint.position, gunPoint.forward * 20f);

            // Draw predicted target position when in attack mode
            if (currentState == AIState.Attack && target != null)
            {
                Vector3 predictedPos = PredictTargetPosition();
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(predictedPos, 2f);
                Gizmos.DrawLine(gunPoint.position, predictedPos);
            }
        }

        // Draw collision avoidance rays
        Gizmos.color = Color.magenta;
        Vector3 forwardDir = transform.forward;
        Vector3 rightDir = transform.right;
        Vector3 upDir = transform.up;

        // Ground check ray
        Gizmos.DrawRay(transform.position, Vector3.down * minGroundHeight * 2f);

        // Obstacle avoidance rays
        Vector3[] rayDirections = {
            forwardDir,
            Quaternion.AngleAxis(avoidanceViewAngle * 0.5f, upDir) * forwardDir,
            Quaternion.AngleAxis(-avoidanceViewAngle * 0.5f, upDir) * forwardDir,
            Quaternion.AngleAxis(avoidanceViewAngle * 0.5f, rightDir) * forwardDir,
            Quaternion.AngleAxis(-avoidanceViewAngle * 0.5f, rightDir) * forwardDir,
        };

        foreach (Vector3 rayDir in rayDirections)
        {
            Gizmos.DrawRay(transform.position, rayDir * obstacleAvoidDistance);
        }
    }
}