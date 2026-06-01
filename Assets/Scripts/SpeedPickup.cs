using UnityEngine;

public class SpeedPickup : MonoBehaviour
{
    public enum PickupType { Boost, Slow }

    [Header("Pickup Settings")]
    [SerializeField] private PickupType pickupType = PickupType.Boost;
    [SerializeField] private float multiplierFactor = 2f;  // Boost: x2 brzina, Slow: x0.5 brzina
    [SerializeField] private float duration = 6f;

    [Header("Particle Effect")]
    [SerializeField] private ParticleSystem collectEffect;

    [Header("Sound")]
    [SerializeField] private AudioClip[] collectSounds;
    [SerializeField] private float soundVolume = 1f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null) return;

        // Boost mnozi brzinu, Slow deli - oba koriste isti applyModifier interface
        float actualMultiplier = pickupType == PickupType.Boost
            ? multiplierFactor
            : 1f / multiplierFactor;

        player.ApplyModifier(actualMultiplier, duration);

        if (collectEffect != null)
        {
            // Instanciramo efekat na poziciji pickup-a i automatski ga uništavamo
            ParticleSystem fx = Instantiate(collectEffect, transform.position, Quaternion.identity);
            float lifetime    = fx.main.duration + fx.main.startLifetime.constantMax;
            Destroy(fx.gameObject, lifetime);
        }

        if (collectSounds != null && collectSounds.Length > 0 && SoundManager.instance != null)
            SoundManager.instance.PlaySoundFX(collectSounds, soundVolume);

        gameObject.SetActive(false);
    }
}
