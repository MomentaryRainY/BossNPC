using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnowDeformPainter : MonoBehaviour
{
    [SerializeField] private Material SnowMaterial;
    [SerializeField] private Material StampMaterial;

    [SerializeField] private int TextureSize = 1024;
    private Vector2 LocalSize = new Vector2(20f, 20f);
    private Vector2 LocalOrigin = new Vector2(-10f, -10f);

    private RenderTexture rtA;
    private RenderTexture rtB;
    private bool useA = true;

    private void ClearRT()
    {
        RenderTexture.active = rtA;
        GL.Clear(false, true, Color.white);
        RenderTexture.active = rtB;
        GL.Clear(false, true, Color.white);
        RenderTexture.active = null;
    }

    void Start()
    {
        Mesh mesh = transform.GetComponent<MeshFilter>().sharedMesh;
        Bounds b = mesh.bounds;

        LocalOrigin = new Vector2(b.min.x, b.min.z);
        LocalSize = new Vector2(b.size.x, b.size.z);
        rtA = CreateRT();
        rtB = CreateRT();

        ClearRT();

        ApplyCurrentRT();
    }

    private RenderTexture CreateRT()
    {
        RenderTexture rt = new RenderTexture(TextureSize, TextureSize, 0, RenderTextureFormat.R8);
        rt.wrapMode = TextureWrapMode.Clamp;
        rt.filterMode = FilterMode.Bilinear;
        rt.Create();
        return rt;
    }

    public void Stamp(Vector3 worldPos, float worldRadius, float strength)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);

        float u = (local.x - LocalOrigin.x) / LocalSize.x;
        float v = (local.z - LocalOrigin.y) / LocalSize.y;

        if (u < 0f || u > 1f || v < 0f || v > 1f)
        {
            return;
        }

        float radiusU = worldRadius / LocalSize.x;
        float radiusV = worldRadius / LocalSize.y;

        RenderTexture current = useA ? rtA : rtB;
        RenderTexture next = useA ? rtB : rtA;

        StampMaterial.SetTexture("_PrevTex", current);
        StampMaterial.SetVector("_StampUVRadius", new Vector4(u, v, radiusU, radiusV));
        StampMaterial.SetFloat("_StampStrength", strength);

        Graphics.Blit(current, next, StampMaterial);

        useA = !useA;

        ApplyCurrentRT();
    }

    private void ApplyCurrentRT()
    {
        RenderTexture current = useA ? rtA : rtB;

        SnowMaterial.SetTexture("_SnowDeformTex", current);
        SnowMaterial.SetMatrix("_SnowWorldToLocal", transform.worldToLocalMatrix);
        SnowMaterial.SetVector(
            "_SnowMapOriginSize",
            new Vector4(LocalOrigin.x, LocalOrigin.y, LocalSize.x, LocalSize.y)
        );
    }

    void Update()
    {
        
    }

    private void OnDestroy()
    {
        if (rtA != null) rtA.Release();
        if (rtB != null) rtB.Release();
    }
}
