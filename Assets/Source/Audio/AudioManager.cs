using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioMixer Mixer;
    [SerializeField] private AudioMixerGroup BgmGroup;

    private const string MasterVolume = "MasterVolume";
    private AudioSource currentBgm;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        SetMasterVolume(PlayerPrefs.GetFloat(MasterVolume, 1f));
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        BindSceneBgm();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindSceneBgm();
    }

    private void BindSceneBgm()
    {
        SceneBgm sceneBgm = FindFirstObjectByType<SceneBgm>();

        if (sceneBgm == null || sceneBgm.Source == null)
        {
            currentBgm = null;
            return;
        }

        currentBgm = sceneBgm.Source;
        currentBgm.outputAudioMixerGroup = BgmGroup;
        currentBgm.loop = true;
        currentBgm.spatialBlend = 0f;
        currentBgm.volume = sceneBgm.VolumeScaleValue;

        if (!currentBgm.isPlaying && currentBgm.clip != null)
        {
            currentBgm.Play();
        }
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        float db = Mathf.Log10(value) * 20f;
        Mixer.SetFloat(MasterVolume, db);

        PlayerPrefs.SetFloat(MasterVolume, value);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
