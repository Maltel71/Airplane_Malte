using UnityEngine;

public class AirplaneExplosion : MonoBehaviour
{
    [Header("Meshes")]
    public GameObject planeMesh;
    public GameObject wreckageMesh;

    [Header("Effects")]
    public ParticleSystem explosionParticles;
    public AudioSource explosionSound;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ground"))
        {
            Explode();
        }
    }

    private void Explode()
    {
        // Hide plane and show wreckage
        planeMesh.SetActive(false);
       // wreckageMesh.SetActive(true);

        // Play explosion effects
        explosionParticles.Play();
        explosionSound.Play();
    }
}