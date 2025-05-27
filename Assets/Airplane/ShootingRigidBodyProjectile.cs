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
        Explode();
    }

    void Explode()
    {
        // Spawn explosion particles
        if (explosionParticlesPrefab != null)
        {
            Instantiate(explosionParticlesPrefab, transform.position, Quaternion.identity);
        }

        // Play explosion sound
        if (explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(explosionSound, transform.position);
        }

        // Destroy projectile
        Destroy(gameObject);
    }
}