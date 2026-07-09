using UnityEngine;


[RequireComponent(typeof(AudioSource))]
public class SceneBgm : MonoBehaviour
{
    [SerializeField] private float VolumeScale = 1f;

    public AudioSource Source { get; private set; }
    public float VolumeScaleValue => VolumeScale;

    private void Awake()
    {
        Source = GetComponent<AudioSource>();
    }
}
