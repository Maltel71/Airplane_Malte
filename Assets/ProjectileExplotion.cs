using UnityEngine;

public class ProjectileExplosion : MonoBehaviour
{
    [Header("Explosion Effects")]
    public ParticleSystem explosionParticles;
    public AudioClip explosionSound;

    [Header("Force Settings")]
    public float explosionForce = 500f;
    public float explosionRadius = 5f;

    void OnCollisionEnter(Collision collision)
    {
        Explode(collision.contacts[0].point);
    }

    void Explode(Vector3 point)
    {
        // Play particles
        if (explosionParticles != null)
        {
            Instantiate(explosionParticles, point, Quaternion.identity);
        }

        // Play sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, point);
        }

        // Apply force to nearby rigidbodies
        Collider[] hits = Physics.OverlapSphere(point, explosionRadius);
        foreach (Collider hit in hits)
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            if (rb != null && hit.gameObject != gameObject)
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}