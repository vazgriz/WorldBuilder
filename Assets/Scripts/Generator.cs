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

public enum CurrentType {
    Major,
    Generated
}

public struct Current {
    public HexDirection direction;
    public CurrentType type;
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

    [SerializeField]
    GameObject currentPrefab;

    HexGrid<Tile> tiles;
    HexGrid<Current> currents;

    FastNoise noise;

    new Transform transform;
    Mesh mesh;
    MeshFilter filter;
    new MeshRenderer renderer;

    void Start() {
        tiles = new HexGrid<Tile>(width, height);
        currents = new HexGrid<Current>(width, height);

        transform = GetComponent<Transform>();

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        filter = GetComponent<MeshFilter>();
        renderer = GetComponent<MeshRenderer>();

        filter.mesh = mesh;

        noise = new FastNoise(seed);
        noise.SetFractalOctaves(4);
        noise.SetFrequency(1f);

        GenerateMap();
        CalculateOceanCurrents();
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

    void AddCurrent(HexDirection dir, int y) {
        for (int x = 0; x < width; x++) {
            Tile tile = tiles[x, y];

            if (tile.type == TileType.Land) continue;

            var offset = HexGrid<Current>.GetOffset(dir);
            var start = new Vector2Int(x, y);
            var pos = start;

            for (int i = 0; i < width; i++) {
                var next = pos + offset;
                ref Current nextCurrent = ref currents[next.x, next.y];
                var nextTile = tiles[next.x, next.y];

                if (nextCurrent.type == CurrentType.Generated) break;
                if (nextTile.type == TileType.Land) break;

                pos = next;
                nextCurrent.direction = dir;
                nextCurrent.type = CurrentType.Generated;
            }
        }
    }

    void DrawCurrents(HexDirection direction) {
        int xStart = 0;
        int xEnd = width;
        int xDelta = 1;
        int yStart = 0;
        int yEnd = height;
        int yDelta = 1;

        if (direction == HexDirection.NorthWest || direction == HexDirection.West || direction == HexDirection.SouthWest) {
            xStart = width - 1;
            xEnd = -1;
            xDelta = -1;
        }

        if (direction == HexDirection.SouthEast || direction == HexDirection.SouthWest) {
            yStart = height - 1;
            yEnd = -1;
            yDelta = -1;
        }

        for (int x = xStart; x != xEnd; x += xDelta) {
            for (int y = yStart; y != yEnd; y += yDelta) {
                var current = currents[x, y];
                if (current.direction != direction) continue;

                var startPos = new Vector2Int(x, y);
                var pos = startPos;
                var offset = HexGrid<Current>.GetOffset(direction);

                while (current.type == CurrentType.Generated) {
                    var nextPos = pos + offset;
                    if (nextPos.x < 0 || nextPos.x >= width) break;

                    Current next = currents[nextPos.x, nextPos.y];

                    if (next.type == CurrentType.Major) break;
                    if (next.direction != direction) break;

                    pos = nextPos;
                }

                if (pos != startPos) {
                    var currentGO = Instantiate(currentPrefab);

                    var currentTransform = currentGO.GetComponent<Transform>();
                    currentTransform.parent = transform;
                    currentTransform.localPosition = currents.GetGridCenter();
                    currentTransform.localRotation = Quaternion.identity;

                    var lineRenderer = currentGO.GetComponent<LineRenderer>();

                    var s = currents.GetCenter(startPos.x, startPos.y);
                    var p = currents.GetCenter(pos.x, pos.y);
                    lineRenderer.SetPosition(0, new Vector3(s.x, s.y, -1));
                    lineRenderer.SetPosition(1, new Vector3(p.x, p.y, -1));
                }
            }
        }
    }

    void CalculateOceanCurrents() {
        int equator = (height / 2) + 1;
        int northEquatorial = equator + 1;
        int southEquatorial = equator - 1;

        AddCurrent(HexDirection.East, equator);
        AddCurrent(HexDirection.West, northEquatorial);
        AddCurrent(HexDirection.West, southEquatorial);

        DrawCurrents(HexDirection.East);
        DrawCurrents(HexDirection.NorthEast);
        DrawCurrents(HexDirection.NorthWest);
        DrawCurrents(HexDirection.West);
        DrawCurrents(HexDirection.SouthWest);
        DrawCurrents(HexDirection.SouthEast);
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
