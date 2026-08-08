using UnityEngine;

public static class ZGSettings
{
    private const string VolumeKey = "ZG_MasterVolume";
    private const string SensitivityKey = "ZG_AimSensitivity";

    public static bool InputLockedByMenu { get; set; }

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 0.85f);
        set
        {
            var clamped = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, clamped);
            AudioListener.volume = clamped;
        }
    }

    public static float AimSensitivity
    {
        get => PlayerPrefs.GetFloat(SensitivityKey, 1f);
        set => PlayerPrefs.SetFloat(SensitivityKey, Mathf.Clamp(value, 0.35f, 2.25f));
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
    }
}
