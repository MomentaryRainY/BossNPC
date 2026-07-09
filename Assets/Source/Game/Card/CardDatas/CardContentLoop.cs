using UnityEngine;

public class CardContentLoop : MonoBehaviour
{
    [SerializeField] private Transform Target;

    [Header("Base Pose")]
    [SerializeField] private Vector3 BaseEuler = new Vector3(0f, 0f, -35f);

    [Header("Spin")]
    [SerializeField] private Vector3 LocalSpinAxis = Vector3.up;
    [SerializeField] private float SpinSpeed = 45f;

    private void Awake()
    {
        if (Target == null)
        {
            Target = transform;
        }
    }

    private void Update()
    {
        Quaternion baseRotation = Quaternion.Euler(BaseEuler);
        Quaternion spinRotation = Quaternion.AngleAxis(
            Time.time * SpinSpeed,
            LocalSpinAxis.normalized
        );

        Target.localRotation = baseRotation * spinRotation;
    }
}