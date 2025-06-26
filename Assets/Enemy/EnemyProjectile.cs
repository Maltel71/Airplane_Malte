using UnityEngine;

public class EnemyProjectile : MonoBehaviour
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

    [Header("Damage Settings")]
    public float damage = 25f;
    public string[] targetTags = { "Player" }; // What this bullet can hit

    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // Auto-destroy bullet after some time to prevent infinite bullets
        Destroy(gameObject, 10f);
    }

    void OnCollisionEnter(Collision collision)
    {
        // Check if we hit a valid target
        bool validTarget = false;

        foreach (string tag in targetTags)
        {
            if (collision.gameObject.CompareTag(tag))
            {
                validTarget = true;
                break;
            }
        }

        // Also explode on terrain/obstacles
        if (!validTarget && (collision.gameObject.CompareTag("Ground") ||
                           collision.gameObject.CompareTag("Obstacle") ||
                           collision.gameObject.CompareTag("Untagged")))
        {
            validTarget = true;
        }

        if (validTarget)
        {
            // Deal damage if target has health component
            var healthComponent = collision.gameObject.GetComponent<MonoBehaviour>();
            if (healthComponent != null)
            {
                // Try to call TakeDamage if it exists
                healthComponent.SendMessage("TakeDamage", damage, SendMessageOptions.DontRequireReceiver);
            }

            Explode(collision.contacts[0].point);
        }
    }

    void Explode(Vector3 explosionPoint)
    {
        // Apply explosion force to nearby rigidbodies
        ApplyExplosionForce(explosionPoint);

        // Spawn explosion particles
        if (explosionParticlesPrefab != null)
        {
            GameObject particles = Instantiate(explosionParticlesPrefab, explosionPoint, Quaternion.identity);

            // Auto-destroy particles after their duration
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
        }
    }

    // Visualize explosion radius in Scene view
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}