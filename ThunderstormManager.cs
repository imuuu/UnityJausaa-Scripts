using System.Collections;
using UnityEngine;

public class ThunderstormManager : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioClip[] _thunderClips; // Array of thunder clips
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private float _minInterval = 5f;
    [SerializeField] private float _maxInterval = 15f;

    [Header("Light Settings")]
    [SerializeField] private Light _flashLight;
    [SerializeField] private AnimationCurve _flashCurve;
    [SerializeField] private float _flashDuration = 1f;

    [Header("Particle Settings")]
    [SerializeField] private ParticleSystem _lightningParticle;
    [SerializeField] private float _minXPosition = -10f; // Minimum X position range for the particle
    [SerializeField] private float _maxXPosition = 10f;  // Maximum X position range for the particle

    private void Start()
    {
        // Ensure the AudioSource is initialized
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        StartCoroutine(ThunderstormRoutine());
    }

    private IEnumerator ThunderstormRoutine()
    {
        while (true)
        {
            // Wait for a random interval before triggering the next thunder event
            float waitTime = Random.Range(_minInterval, _maxInterval);
            yield return new WaitForSeconds(waitTime);

            PlayThunder();

            if (_flashLight != null)
            {
                StartCoroutine(FlashLightRoutine());
            }
        }
    }

    private void PlayThunder()
    {
        // Play a random thunder sound effect from the array
        if (_thunderClips.Length > 0 && _audioSource != null)
        {
            int randomIndex = Random.Range(0, _thunderClips.Length);
            _audioSource.PlayOneShot(_thunderClips[randomIndex]);
        }

        // Play the lightning particle effect with random X position
        if (_lightningParticle != null)
        {
            // Randomize X-axis position
            Vector3 randomPosition = _lightningParticle.transform.position;
            randomPosition.x = Random.Range(_minXPosition, _maxXPosition);
            _lightningParticle.transform.position = randomPosition;

            // Randomize rotation of the particle
            var particleTransform = _lightningParticle.transform;
            particleTransform.rotation = Quaternion.Euler(Random.Range(45f, 130f), particleTransform.rotation.eulerAngles.y, particleTransform.rotation.eulerAngles.z);

            _lightningParticle.Play();
        }
    }

    private IEnumerator FlashLightRoutine()
    {
        float elapsedTime = 0f;

        while (elapsedTime < _flashDuration)
        {
            // Calculate normalized time and update light intensity using the curve
            elapsedTime += Time.deltaTime;
            float normalizedTime = elapsedTime / _flashDuration;
            float intensity = _flashCurve.Evaluate(normalizedTime);
            _flashLight.intensity = intensity;
            yield return null;
        }

        // Ensure the light turns off at the end of the flash
        _flashLight.intensity = 0f;
    }
}
