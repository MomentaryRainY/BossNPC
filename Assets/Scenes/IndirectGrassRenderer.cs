using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;

public class IndirectGrassRenderer : MonoBehaviour
{
    [SerializeField] private GameObject GrassPrefab;
    private Transform Player;

    [SerializeField] private int InstanceCount = 50000;
    [SerializeField] private Vector2 AreaSize = new Vector2(80f, 80f);

    [SerializeField] private float MinScale = 0.8f;
    [SerializeField] private float MaxScale = 1.4f;

    [SerializeField] private float PlayerBendRadius = 1.5f;
    [SerializeField] private float BendStrength = 1.2f;

    [SerializeField] private Texture2D GroundTintTexture;
    [SerializeField, Range(0f, 1f)] private float TintStrength = 0.5f;

    [SerializeField] private PCGStonePlacement StonePlacement;

    [SerializeField] private bool UseDensityNoise = true;

    [SerializeField] private float DensityNoiseScale = 0.06f;

    [SerializeField, Range(0f, 1f)]
    private float DensityThreshold = 0.45f;

    [SerializeField, Range(0.001f, 0.5f)]
    private float DensitySoftness = 0.12f;

    [SerializeField] private Vector2 DensityNoiseOffset;

    [SerializeField] private Light lightsource;

    private Mesh mesh;
    private Material material;

    private ComputeBuffer matrixBuffer;
    private ComputeBuffer argsBuffer;

    private Bounds drawBounds;

    private bool hasTriedFindPlayer;

    private static readonly int MatricesId = Shader.PropertyToID("_Matrices");
    private static readonly int PlayerPosRadiusId = Shader.PropertyToID("_PlayerPosRadius");
    private static readonly int BendStrengthId = Shader.PropertyToID("_BendStrength");
    private static readonly int GroundTintTexId = Shader.PropertyToID("_GroundTintTex");
    private static readonly int TintStrengthId = Shader.PropertyToID("_TintStrength");
    private static readonly int TintMapOriginSizeId = Shader.PropertyToID("_TintMapOriginSize");
    private static readonly int LightSourceId = Shader.PropertyToID("_GrassShadowDirection");

    private void Start()
    {
        MeshFilter meshFilter = GrassPrefab.GetComponentInChildren<MeshFilter>();
        MeshRenderer meshRenderer = GrassPrefab.GetComponentInChildren<MeshRenderer>();

        mesh = meshFilter.sharedMesh;
        material = new Material(meshRenderer.sharedMaterial);

        List<Matrix4x4> matrices = new();
        List<Bounds> stoneBounds = StonePlacement != null
            ? StonePlacement.GetGrassBlockBounds()
            : new List<Bounds>();

        int attempts = 0;
        int maxAttempts = InstanceCount * 5;

        while (matrices.Count < InstanceCount && attempts < maxAttempts)
        {
            attempts++;

            float x = Random.Range(-AreaSize.x * 0.5f, AreaSize.x * 0.5f);
            float z = Random.Range(-AreaSize.y * 0.5f, AreaSize.y * 0.5f);

            Vector3 position = transform.position + new Vector3(x, 0f, z);

            if (IsInsideAnyStoneBounds(position, stoneBounds))
            {
                continue;
            }

            float density = 1f;

            if (UseDensityNoise && !PassDensityNoise(position, out density))
            {
                continue;
            }

            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            float widthScale = Random.Range(MinScale, MaxScale);
            float heightScale = Random.Range(MinScale, MaxScale);

            heightScale *= Mathf.Lerp(0.55f, 1.25f, density);

            if (Random.value < 0.05f * density)
            {
                heightScale *= Random.Range(1.5f, 2.2f);
            }

            Vector3 scale = new Vector3(widthScale, heightScale, widthScale);
            matrices.Add(Matrix4x4.TRS(position, rotation, scale));
        }

        matrixBuffer = new ComputeBuffer(matrices.Count, sizeof(float) * 16);
        matrixBuffer.SetData(matrices);

        uint[] args = new uint[5];
        args[0] = mesh.GetIndexCount(0);
        args[1] = (uint)matrices.Count;
        args[2] = mesh.GetIndexStart(0);
        args[3] = mesh.GetBaseVertex(0);
        args[4] = 0;

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        material.SetBuffer(MatricesId, matrixBuffer);

        drawBounds = new Bounds(
            transform.position,
            new Vector3(AreaSize.x, 30f, AreaSize.y)
        );

        // grass texture
        if (GroundTintTexture != null)
        {
            material.SetTexture(GroundTintTexId, GroundTintTexture);
        }

        material.SetFloat(TintStrengthId, TintStrength);

        Vector4 originSize = new Vector4(
            transform.position.x - AreaSize.x * 0.5f,
            transform.position.z - AreaSize.y * 0.5f,
            AreaSize.x,
            AreaSize.y
        );

        material.SetVector(TintMapOriginSizeId, originSize);

        Vector3 lightDir = lightsource.transform.forward;
        Vector2 shadowDir = new Vector2(lightDir.x, lightDir.z);

        if (shadowDir.sqrMagnitude > 0.0001f)
        {
            shadowDir.Normalize();
            material.SetVector(LightSourceId, new Vector4(shadowDir.x, 0f, shadowDir.y, 0f));
        }
    }

    private void Update()
    {
        if (Player == null && !hasTriedFindPlayer)
        {
            hasTriedFindPlayer = true;

            Player player = FindFirstObjectByType<Player>();
            if (player != null)
            {
                Player = player.transform;
            }
        }

        if (Player != null)
        {
            Vector3 p = Player.position;
            material.SetVector(PlayerPosRadiusId, new Vector4(p.x, p.y, p.z, PlayerBendRadius));
            material.SetFloat(BendStrengthId, BendStrength);
        }

        Graphics.DrawMeshInstancedIndirect(
            mesh,
            0,
            material,
            drawBounds,
            argsBuffer,
            0,
            null,
            ShadowCastingMode.On,
            true
        );
    }

    private bool IsInsideAnyStoneBounds(Vector3 position, List<Bounds> boundsList)
    {
        Vector3 testPos = position;

        foreach (Bounds bounds in boundsList)
        {
            testPos.y = bounds.center.y;

            if (bounds.Contains(testPos))
            {
                return true;
            }
        }

        return false;
    }

    private bool PassDensityNoise(Vector3 worldPosition, out float density)
    {
        float u = worldPosition.x * DensityNoiseScale + DensityNoiseOffset.x;
        float v = worldPosition.z * DensityNoiseScale + DensityNoiseOffset.y;

        float noise = Mathf.PerlinNoise(u, v);

        float edge0 = DensityThreshold;
        float edge1 = DensityThreshold + DensitySoftness;

        density = Mathf.InverseLerp(edge0, edge1, noise);
        density = Mathf.Clamp01(density);
        density = density * density * (3f - 2f * density);

        return Random.value < density;
    }

    private void OnDisable()
    {
        matrixBuffer?.Release();
        matrixBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }
}