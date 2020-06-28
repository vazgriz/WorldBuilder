using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType {
    Water,
    Land
}

public struct Tile {
    public TileType type;
    public float mountainFactor;
}

public class Generator : MonoBehaviour {
    [SerializeField]
    int width;
    [SerializeField]
    int height;
    [SerializeField]
    float tileSize;
    [SerializeField]
    float marginSize;

    [SerializeField]
    Color waterColor;
    [SerializeField]
    Gradient landColor;

    [SerializeField]
    int seed;
    [SerializeField]
    float noiseRadius;
    [SerializeField]
    float noiseHeight;
    [SerializeField]
    float seaLevel;
    [SerializeField]
    float mountainLevel;

    HexGrid<Tile> tiles;

    FastNoise noise;

    Mesh mesh;
    MeshFilter filter;
    new MeshRenderer renderer;

    void Start() {
        tiles = new HexGrid<Tile>(width, height);
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        filter = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();

        filter.mesh = mesh;

        noise = new FastNoise(seed);
        noise.SetFractalOctaves(4);
        noise.SetFrequency(1f);

        GenerateMap();
        CreateMesh();
    }

    void GenerateMap() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                //sample in cylinder shape to get wrapping noise
                Vector3 tileCenter = tiles.GetCenter(x, y);
                float t = tileCenter.x / (width * HexGrid<Tile>.widthMult);
                float r = t * 2 * Mathf.PI;

                Vector3 samplePos = new Vector3(Mathf.Sin(r) * noiseRadius, tileCenter.y * noiseHeight * noiseRadius, Mathf.Cos(r) * noiseRadius);
                float sample = noise.GetSimplexFractal(samplePos.x, samplePos.y, samplePos.z);

                if (sample > seaLevel) {
                    tiles[x, y].type = TileType.Land;
                    tiles[x, y].mountainFactor = Mathf.Clamp01(Mathf.InverseLerp(seaLevel, mountainLevel, sample));
                } else {
                    tiles[x, y].type = TileType.Water;
                }
            }
        }
    }

    void CreateMesh() {
        List<Vector3> vertices = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> triangles = new List<int>();

        Vector3[] offsets = new Vector3[6];

        for (int i = 0; i < 6; i++) {
            offsets[i] = new Vector3(Mathf.Sin(i * 60 * Mathf.Deg2Rad), Mathf.Cos(i * 60 * Mathf.Deg2Rad), 0) * marginSize;
        }

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3 center = tiles.GetGridCenter() + tiles.GetCenter(x, y);
                Tile tile = tiles[x, y];
                Color color;

                if (tile.type == TileType.Water) {
                    color = waterColor;
                } else {
                    color = landColor.Evaluate(tile.mountainFactor);
                }

                for (int i = 0; i < 6; i++) {
                    triangles.Add(vertices.Count);
                    triangles.Add(vertices.Count + 1);
                    triangles.Add(vertices.Count + 2);
                    vertices.Add(center * tileSize);
                    vertices.Add((center + offsets[i]) * tileSize);
                    vertices.Add((center + offsets[(i + 1) % 6]) * tileSize);
                    colors.Add(color);
                    colors.Add(color);
                    colors.Add(color);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
    }
}
