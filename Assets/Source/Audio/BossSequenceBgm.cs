using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public sealed class BossSequenceBgm : MonoBehaviour
{
    private static BossSequenceBgm instance;

    [SerializeField, Range(0f, 1f)] private float VolumeScale = 0.5f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.playOnAwake = false;

        if (instance != null && instance != this)
        {
            source.Stop();
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = VolumeScale;

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (source.clip != null && !source.isPlaying)
        {
            source.Play();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsBossSequenceScene(scene.name))
        {
            Destroy(gameObject);
        }
    }

    private static bool IsBossSequenceScene(string sceneName)
    {
        return sceneName == "Boss" ||
               sceneName == "Boss1" ||
               sceneName == "Boss2" ||
               sceneName == "Boss3";
    }

    private void OnDestroy()
    {
        if (instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        instance = null;
    }
}
