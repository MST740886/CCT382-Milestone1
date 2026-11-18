using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Header("Shared data")]
    public FloatVariableSO currentHealth;
    public FloatVariableSO maxHealth;
    public GameEventSO onHealthChanged;

    [Header("UI")]
    public Slider slider;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip changeSound;
    [Min(0.01f)] public float damagePitchMin = 0.85f;
    [Min(0.01f)] public float damagePitchMax = 1.15f;

    private float lastKnownHealth;
    private bool hasKnownHealth;

    private void OnEnable()
    {
        if (onHealthChanged != null)
            onHealthChanged.RegisterListener(OnHealthChangedEvent);
        RefreshFromCurrentHealth();
    }

    private void OnDisable()
    {
        if (onHealthChanged != null)
            onHealthChanged.UnregisterListener(OnHealthChangedEvent);
    }

    private void OnHealthChangedEvent()
    {
        bool healthDecreased = false;
        if (currentHealth != null)
        {
            healthDecreased = hasKnownHealth && currentHealth.value < lastKnownHealth;
            lastKnownHealth = currentHealth.value;
            hasKnownHealth = true;
        }

        SyncSlider();
        if (healthDecreased)
            PlayDamageSound();
    }

    private void SyncSlider()
    {
        if (slider == null || currentHealth == null || maxHealth == null || maxHealth.value <= 0f)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = Mathf.Clamp01(currentHealth.value / maxHealth.value);
    }

    /// <summary>Refreshes the bar without playing the health-change sound.</summary>
    public void RefreshFromCurrentHealth()
    {
        if (currentHealth != null)
        {
            lastKnownHealth = currentHealth.value;
            hasKnownHealth = true;
        }

        SyncSlider();
    }

    private void PlayDamageSound()
    {
        if (audioSource == null || changeSound == null)
            return;

        float minPitch = Mathf.Max(0.01f, Mathf.Min(damagePitchMin, damagePitchMax));
        float maxPitch = Mathf.Max(minPitch, Mathf.Max(damagePitchMin, damagePitchMax));
        float pitch = Random.Range(minPitch, maxPitch);

        // Use a temporary source so pitch variation affects only this damage
        // instance, not the reusable AudioSource or any other sound.
        GameObject oneShotObject = new GameObject("Damage Audio One Shot");
        oneShotObject.transform.position = audioSource.transform.position;
        AudioSource oneShotSource = oneShotObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.clip = changeSound;
        oneShotSource.volume = audioSource.volume;
        oneShotSource.pitch = pitch;
        oneShotSource.spatialBlend = audioSource.spatialBlend;
        oneShotSource.panStereo = audioSource.panStereo;
        oneShotSource.priority = audioSource.priority;
        oneShotSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        oneShotSource.mute = audioSource.mute;
        oneShotSource.ignoreListenerPause = audioSource.ignoreListenerPause;
        oneShotSource.ignoreListenerVolume = audioSource.ignoreListenerVolume;
        oneShotSource.bypassEffects = audioSource.bypassEffects;
        oneShotSource.bypassListenerEffects = audioSource.bypassListenerEffects;
        oneShotSource.bypassReverbZones = audioSource.bypassReverbZones;
        oneShotSource.Play();

        float playbackDuration = changeSound.length / Mathf.Max(0.01f, pitch);
        Destroy(oneShotObject, playbackDuration + 0.1f);
    }
}
