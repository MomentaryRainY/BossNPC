using System.Collections.Generic;
using UnityEngine;

public class PCGStonePlacement : MonoBehaviour
{
    public GameObject[] prefabs;

    [SerializeField] public int count = 10;

    public LayerMask groundMask;

    public Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    private List<Vector3> placedPositions = new List<Vector3>();

    public float minDistance = 10f;

    [SerializeField] private float grassAvoidPadding = 0.5f;

    [ContextMenu("Generate Stones")]
    public void Generate()
    {
        placedPositions.Clear();

        BoxCollider box = GetComponent<BoxCollider>();

        Bounds bounds = box.bounds;

        for (int i = 0; i < count; i++)
        {
            Vector3 randomPos = new Vector3(Random.Range(bounds.min.x, bounds.max.x),
                bounds.max.y + 10f,
                Random.Range(bounds.min.z, bounds.max.z));

            if (Physics.Raycast(randomPos, Vector3.down, out RaycastHit hit, 100f, groundMask))
            {
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
            }
        }
    }

    public List<Bounds> GetGrassBlockBounds()
    {
        List<Bounds> result = new();

        foreach (Transform child in transform)
        {
            Renderer[] renderers = child.GetComponentsInChildren<Renderer>();

            if (renderers.Length == 0)
            {
                continue;
            }

            Bounds bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            bounds.Expand(new Vector3(grassAvoidPadding, 0f, grassAvoidPadding));
            result.Add(bounds);
        }

        return result;
    }

    void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();

        Gizmos.color = Color.green;

        Gizmos.matrix = transform.localToWorldMatrix;

        Gizmos.DrawWireCube(box.center, box.size);
    }

    [ContextMenu("Clear Stones")]
    public void ClearStones()
    {
        List<GameObject> children = new List<GameObject>();

        foreach (Transform child in transform)
        {
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
}