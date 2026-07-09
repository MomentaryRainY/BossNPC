using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Board : MonoBehaviour
{
    [SerializeField] private Renderer BoardRenderer;

    [SerializeField] private int Width;
    [SerializeField] private int Height;
    [SerializeField] private float LineWidth;

    public int BoardWidth => Width;
    public int BoardHeight => Height;

    private Texture2D HighlightTexture;
    private Color32[] HighlightPixels;
    private Material BoardMaterial;

    public TileData[,] Tiles { get; private set; }

    private float CellWidth;
    private float CellHeight;

    private Bounds BoardBounds;

    private void Awake()
    {
        if (!BoardRenderer) BoardRenderer = GetComponent<Renderer>();

        BoardMaterial = BoardRenderer.material;

        HighlightTexture = new Texture2D(Width, Height, TextureFormat.RGBA32, false);
        HighlightTexture.filterMode = FilterMode.Point;
        HighlightTexture.wrapMode = TextureWrapMode.Clamp;

        HighlightPixels = new Color32[Width * Height];

        ClearHighlights();
    }


    public void Init()
    {
        BoardMaterial.SetTexture("_HighlightTex", HighlightTexture);
        BoardMaterial.SetFloat("_GridX", Width);
        BoardMaterial.SetFloat("_GridY", Height);
        BoardMaterial.SetFloat("_LineWidth", LineWidth);
        
        BoardBounds = BoardRenderer.bounds;
        CellWidth = BoardRenderer.bounds.size.x / Width;
        CellHeight = BoardRenderer.bounds.size.z / Height;

        BoardMaterial.SetVector("_BoardOriginSize", 
            new Vector4(BoardBounds.min.x,
                        BoardBounds.min.z,
                        BoardBounds.size.x,
                        BoardBounds.size.z));

        Tiles = new TileData[Width, Height];
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tiles[x, y] = new TileData { Coord = new Vector2Int(x, y) };
            }
        }
    }

    public Vector3 GridToWorld(Vector2Int cell)
    {
        float x = BoardBounds.min.x + (cell.x + 0.5f) * CellWidth;

        float z = BoardBounds.min.z + (cell.y + 0.5f) * CellHeight;

        return new Vector3(x, transform.position.y, z);
    }

    public Vector2Int WorldToGrid(Vector3 pos)
    {
        float localX = pos.x - BoardBounds.min.x;
        float localZ = pos.z - BoardBounds.min.z;

        int x = Mathf.FloorToInt(localX / CellWidth);
        int y = Mathf.FloorToInt(localZ / CellHeight);

        x = Mathf.Clamp(x, 0, Width - 1);
        y = Mathf.Clamp(y, 0, Height - 1);

        return new Vector2Int(x, y);
    }

    public void ClearHighlights()
    {
        Color32 off = new Color32(0, 0, 0, 255);

        for (int i = 0; i < HighlightPixels.Length; i++)
        {
            HighlightPixels[i] = off;
        }

        HighlightTexture.SetPixels32(HighlightPixels);
        HighlightTexture.Apply(false);
    }
    public void SetHighlightCells(List<Vector2Int> cells, BoardHighlightMode mode)
    {
        Color32 off = new Color32(0, 0, 0, 255); 
        Color highlightColor = mode == BoardHighlightMode.Move ? new Color(0.9f, 0.8f, 0.2f, .7f) : new Color(0.9f, 0.2f, 0.2f, .7f);
        BoardMaterial.SetColor("_HighlightColor", highlightColor);

        for (int i = 0; i < HighlightPixels.Length; i++)
        {
            HighlightPixels[i] = off;
        }

        foreach (Vector2Int cell in cells)
        {
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                continue;
            }

            int index = cell.x + cell.y * Width;
            HighlightPixels[index] = highlightColor;
        }

        HighlightTexture.SetPixels32(HighlightPixels);
        HighlightTexture.Apply(false);
    }
    public bool IsInside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < Width
            && cell.y >= 0 && cell.y < Height;
    }

    public bool IsOccupied(Vector2Int cell)
    {
        return IsInside(cell) && Tiles[cell.x, cell.y].Occupant != null;
    }

    public Unit GetOccupant(Vector2Int cell)
    {
        if (!IsInside(cell))
        {
            return null;
        }

        return Tiles[cell.x, cell.y].Occupant;
    }

    public bool TryPlaceUnit(Unit unit, Vector2Int cell)
    {
        if (!IsInside(cell) || IsOccupied(cell))
        {
            return false;
        }

        Tiles[cell.x, cell.y].Occupant = unit;
        return true;
    }

    public void MoveUnit(Unit unit, Vector2Int from, Vector2Int to)
    {
        if (IsInside(from) && Tiles[from.x, from.y].Occupant == unit)
        {
            Tiles[from.x, from.y].Occupant = null;
        }

        if (IsInside(to))
        {
            Tiles[to.x, to.y].Occupant = unit;
        }
    }

    public void RemoveUnit(Unit unit)
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Tiles[x, y].Occupant == unit)
                {
                    Tiles[x, y].Occupant = null;
                    return;
                }
            }
        }
    }
}
public enum BoardHighlightMode
{
    Move,
    Card
}
public class TileData
{
    public Vector2Int Coord;

    public Unit Occupant;
}

