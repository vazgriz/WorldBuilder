using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TileType {
    Water,
    Land
}

public struct Tile {
    public TileType type;
}

public class Generator : MonoBehaviour {
    const float widthMult = 1.73205080757f;
    const float heightMult = 2;

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
    Color landColor;

    Tile[,] tiles;
    Mesh mesh;

    MeshFilter filter;
    new MeshRenderer renderer;

    void Start() {
        tiles = new Tile[width, height];
        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        filter = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();

        filter.mesh = mesh;

        CreateMesh();
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
                bool offset = y % 2 == 0;
                Vector3 center = new Vector3((width * widthMult) / -2, (height * heightMult * 0.75f) / -2, 0) + new Vector3(x * widthMult + (widthMult * (offset ? 0.5f : 0)), y * 0.75f * heightMult, 0);

                for (int i = 0; i < 6; i++) {
                    triangles.Add(vertices.Count);
                    triangles.Add(vertices.Count + 1);
                    triangles.Add(vertices.Count + 2);
                    vertices.Add(center * tileSize);
                    vertices.Add((center + offsets[i]) * tileSize);
                    vertices.Add((center + offsets[(i + 1) % 6]) * tileSize);
                    colors.Add(waterColor);
                    colors.Add(waterColor);
                    colors.Add(waterColor);
                }
            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.colors = colors.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateBounds();
    }
}
