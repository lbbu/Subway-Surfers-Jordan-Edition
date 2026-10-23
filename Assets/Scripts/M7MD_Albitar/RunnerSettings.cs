using UnityEngine;

/// <summary>إعدادات محلية تخص هذا المشهد وتطبق على مصادر صوته وردود فعله فقط.</summary>
public sealed class RunnerSettings : MonoBehaviour
{
    [SerializeField] private PlayerFeedback feedback;
    [SerializeField] private AudioSource[] audioSources;
    [SerializeField] private string keyPrefix = "JordanRunner.M7MD.";
    public bool Sound { get; private set; } = true;
    public bool Haptics { get; private set; }
    public bool Shake { get; private set; } = true;
    private void OnEnable() => Load();
    public void Load()
    {
        Sound = PlayerPrefs.GetInt(keyPrefix + "Sound", 1) != 0;
        Haptics = PlayerPrefs.GetInt(keyPrefix + "Haptics", 0) != 0;
        Shake = PlayerPrefs.GetInt(keyPrefix + "Shake", 1) != 0; Apply();
    }
    public void ToggleSound() { Sound = !Sound; Save(); }
    public void ToggleHaptics() { Haptics = !Haptics; Save(); }
    public void ToggleShake() { Shake = !Shake; Save(); }
    private void Save()
    {
        PlayerPrefs.SetInt(keyPrefix + "Sound", Sound ? 1 : 0);
        PlayerPrefs.SetInt(keyPrefix + "Haptics", Haptics ? 1 : 0);
        PlayerPrefs.SetInt(keyPrefix + "Shake", Shake ? 1 : 0);
        PlayerPrefs.Save(); Apply();
    }
    private void Apply()
    {
        if (feedback) feedback.SetAccessibility(Shake ? 1 : 0, Haptics);
        if (audioSources != null) foreach (var source in audioSources) if (source) source.mute = !Sound;
    }
}
