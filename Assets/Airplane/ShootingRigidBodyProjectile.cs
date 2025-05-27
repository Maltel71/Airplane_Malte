using UnityEngine;

public class ShootingRigidbodyProjectile : MonoBehaviour
{
    [Header("Shooting Settings")]
    public KeyCode shootKey = KeyCode.Mouse0;
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float shootForce = 100f;

    void Update()
    {
        if (Input.GetKeyDown(shootKey))
        {
            Shoot();
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || shootPoint == null) return;

        GameObject projectile = Instantiate(projectilePrefab, shootPoint.position, shootPoint.rotation);
        Rigidbody rb = projectile.GetComponent<Rigidbody>();

        if (rb != null)
        {
            // Set low mass for bullets
            rb.mass = 0.1f;
            // Reduce gravity effect
            rb.linearDamping = 0f;
            rb.angularDamping = 0f;
            // Use velocity instead of AddForce for instant speed
            rb.linearVelocity = shootPoint.forward * shootForce;
        }
    }
}

public class Projectile : MonoBehaviour
{
    [Header("Explosion Settings")]
    public GameObject explosionParticlesPrefab;
    public AudioClip explosionSound;
    public float explosionDelay = 0f;

    [Header("Explosion Force Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 1f;
    public LayerMask affectedLayers = -1; // All layers by default

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Explode(collision.contacts[0].point);
    }

    void Explode(Vector3 explosionPoint)
    {
        // Apply explosion force to nearby rigidbodies
        ApplyExplosionForce(explosionPoint);

        // Spawn explosion particles
        if (explosionParticlesPrefab != null)
        {
            GameObject particles = Instantiate(explosionParticlesPrefab, explosionPoint, Quaternion.identity);

            // Auto-destroy particles after their duration (optional)
            ParticleSystem ps = particles.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(particles, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(particles, 5f); // Fallback destroy time
            }
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, explosionPoint);
        }

        // Destroy projectile
        Destroy(gameObject);
    }

    void ApplyExplosionForce(Vector3 explosionPoint)
    {
        // Find all colliders within explosion radius
        Collider[] colliders = Physics.OverlapSphere(explosionPoint, explosionRadius, affectedLayers);

        foreach (Collider col in colliders)
        {
            // Skip the projectile itself
            if (col.gameObject == gameObject) continue;

            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply explosion force
                rb.AddExplosionForce(explosionForce, explosionPoint, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }

            // Optional: Apply force to other objects (like particle systems, etc.)
            // You can add custom behavior here for non-rigidbody objects
        }
    }

    // Optional: Visualize explosion radius in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}