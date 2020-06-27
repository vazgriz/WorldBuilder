using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HexDirection {
    East,
    NorthEast,
    NorthWest,
    West,
    SouthWest,
    SouthEast
}

public class HexGrid<T> {
    const float widthMult = 1.73205080757f;
    const float heightMult = 2;

    public int Width { get; private set; }
    public int Height { get; private set; }

    T[,] tiles;

    public ref T this[int x, int y] {
        get {
            return ref tiles[Mod(x, Width), y];
        }
    }

    public HexGrid(int width, int height) {
        Width = width;
        Height = height;

        tiles = new T[width, height];
    }
    static int Mod(int x, int m) {
        return (x % m + m) % m;
    }

    static readonly Vector2Int[] offsets = {
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(0, 1),
        new Vector2Int(-1, 0),
        new Vector2Int(0, -1),
        new Vector2Int(1, -1),
    };

    public Vector2Int GetNeighbor(int x, int y, HexDirection direction) {
        var offset = offsets[(int)direction];
        return new Vector2Int(x, y) + offset;
    }

    public Vector2 GetCenter(int x, int y) {
        bool even = y % 2 == 0;
        return new Vector2(x * widthMult + (widthMult * (even ? 0 : 0.5f)), y * 0.75f * heightMult);
    }

    public Vector2 GetGridCenter() {
        return new Vector3((Width * widthMult) / -2, (Height * heightMult * 0.75f) / -2);
    }
}
