using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;

public class PCGTreePlacement : MonoBehaviour
{
    public GameObject[] prefabs;

    [SerializeField] public int count = 50;

    public LayerMask groundMask;

    [SerializeField] private BoxCollider inclusion;

    [SerializeField] private BoxCollider[] exclusions;

    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    private List<Vector3> placedPositions = new List<Vector3>();

    public float minDistance = 3f;

    private static readonly int LeafTintId = Shader.PropertyToID("_Color");
    private static readonly int ColorDepthId = Shader.PropertyToID("_ColorDepth");
    private static readonly int ShadowStrengthId = Shader.PropertyToID("_ShadowStrength");

    [SerializeField] private Vector2 LeafDepthRange = new Vector2(0.75f, 1.15f);
    [SerializeField] private Vector2 LeafShadowRange = new Vector2(0.6f, 0.9f);

    [SerializeField, Range(0f, 1f)] private float DryTreeChance = 0.15f;

    [SerializeField] private Color LeafGreenA = new Color(0.45f, 0.75f, 0.32f, 1f);
    [SerializeField] private Color LeafGreenB = new Color(0.25f, 0.55f, 0.24f, 1f);

    [SerializeField] private Color LeafDryA = new Color(0.90f, 0.66f, 0.25f, 1f);
    [SerializeField] private Color LeafDryB = new Color(0.58f, 0.38f, 0.16f, 1f);

    [ContextMenu("Generate Trees")]
    public void Generate()
    {
        placedPositions.Clear();

        if (inclusion == null) { Debug.Log("Placement inclusion is not set!"); }

        Bounds bounds = inclusion.bounds;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 10f,
                Random.Range(bounds.min.z, bounds.max.z));

            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 100f, groundMask))
            {
                if (IsInsideExclusionZone(hit.point))
                {
                    i--;
                    continue;
                }

                bool valid = true;

                foreach (Vector3 pos in placedPositions)
                {
                    Vector2 a = new Vector2(hit.point.x, hit.point.z);

                    Vector2 b = new Vector2(pos.x, pos.z);

                    if (Vector2.Distance(a, b) < minDistance)
                    {
                        valid = false;
                        break;
                    }
                }

                if (!valid)
                {
                    i--;
                    continue;
                }

                placedPositions.Add(hit.point);

                if (prefabs == null || prefabs.Length == 0)
                {
                    Debug.LogError("No prefabs assigned!");
                    return;
                }

                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];

                Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                GameObject tree = Instantiate(prefab, hit.point, rotation);

                float scale = Random.Range(scaleRange.x, scaleRange.y);
                tree.transform.localScale = Vector3.one * scale;

                tree.transform.SetParent(transform);

                ApplyRandomLeafColor(tree);
            }
        }
    }

    private void Start()
    {
        foreach (Transform child in transform)
        {
            ApplyRandomLeafColor(child.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        if (inclusion == null) { return; }

        Gizmos.color = Color.green;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(inclusion.center, inclusion.size);
    }

    [ContextMenu("Clear Trees")]
    public void ClearTrees()
    {
        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
        {
            if (child.GetComponent<BoxCollider>() != null)
            {
                continue;
            }

            children.Add(child.gameObject);
        }

        foreach (GameObject obj in children)
        {
#if UNITY_EDITOR
            DestroyImmediate(obj);
#else
        Destroy(obj);
#endif
        }
    }

    private bool IsInsideExclusionZone(Vector3 position)
    {
        foreach (var zone in exclusions)
        {
            if (zone.bounds.Contains(position))
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyRandomLeafColor(GameObject tree)
    {
        Renderer[] renderers = tree.GetComponentsInChildren<Renderer>();

        bool dry = Random.value < DryTreeChance;

        Color tint = dry
            ? Color.Lerp(LeafDryA, LeafDryB, Random.value)
            : Color.Lerp(LeafGreenA, LeafGreenB, Random.value);
        float depth = Random.Range(LeafDepthRange.x, LeafDepthRange.y);
        float shadow = Random.Range(LeafShadowRange.x, LeafShadowRange.y);

        foreach (Renderer renderer in renderers)
        {
            Material[] materials = renderer.sharedMaterials;

            for (int i = 0; i < materials.Length; i++)
            {
                Material mat = materials[i];
                //Debug.Log($"{tree.name} / {renderer.name} / {mat.name} / {mat.shader.name} / hasDepth:{mat.HasProperty(ColorDepthId)}");
                if (mat == null || !mat.HasProperty(ColorDepthId))
                {
                    continue;
                }
                
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block, i);

                block.SetColor(LeafTintId, tint);
                block.SetFloat(ColorDepthId, depth);
                block.SetFloat(ShadowStrengthId, shadow);

                renderer.SetPropertyBlock(block, i);
            }
        }
    }
}
