using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SingleMeshGrid : MonoBehaviour
{
    public int rows = 100;
    public int cols = 100;
    public float tileSize = 0.1f;
    public Material gridMaterial;

    public bool drawing = false;

    private Texture2D paintTexture;
    private Color[,] colorCache;

    private Stack<Texture2D> undoStack = new Stack<Texture2D>();
    private Stack<Texture2D> redoStack = new Stack<Texture2D>();
    Stack<(int x, int y, Color pastColor, Color newColor)> rewind = new Stack<(int, int, Color, Color)>();
    Stack<(int x, int y, Color pastColor, Color newColor)> rewindAux = new Stack<(int, int, Color, Color)>();
    private int maxHistory = 6;



    private bool isPaintingStroke = false; 

    void Start()
    {
        gameObject.AddComponent<MeshCollider>();
        GenerateMesh();
        InitializeTexture();
    }

    void Update()
    {
        /*
        if (Input.GetMouseButtonDown(0))
        {
            StartStroke();
        }

        if (Input.GetMouseButtonUp(0))
        {
            EndStroke();
        }*/

        if (drawing)
        {
            StartStroke();
        }else
        {
            EndStroke();
        }

    }

    void StartStroke()
    {
        isPaintingStroke = true;
    }

    void EndStroke()
    {
        if (isPaintingStroke)
        {
            SaveTextureSnapshot();
            isPaintingStroke = false;
        }
    }

    public Texture2D GetPaintTexture()
    {
        return paintTexture;
    }

    void SaveTextureSnapshot()
    {
        Texture2D snapshot = new Texture2D(paintTexture.width, paintTexture.height, TextureFormat.RGBA32, false);
        snapshot.SetPixels(paintTexture.GetPixels());
        snapshot.Apply();

        undoStack.Push(snapshot);

        if (undoStack.Count > maxHistory)
        {
            undoStack.TrimExcess();
            undoStack.Pop();
        }

        redoStack.Clear(); 
    }



    void GenerateMesh()
    {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[(rows + 1) * (cols + 1)];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[rows * cols * 6];

        for (int y = 0; y <= rows; y++)
        {
            for (int x = 0; x <= cols; x++)
            {
                int i = y * (cols + 1) + x;
                vertices[i] = new Vector3(x * tileSize, 0, y * tileSize);
                uv[i] = new Vector2((float)x / cols, (float)y / rows);
            }
        }

        int ti = 0;
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int i = y * (cols + 1) + x;
                triangles[ti++] = i;
                triangles[ti++] = i + cols + 1;
                triangles[ti++] = i + 1;
                triangles[ti++] = i + 1;
                triangles[ti++] = i + cols + 1;
                triangles[ti++] = i + cols + 2;
            }
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshRenderer>().material = gridMaterial;

        MeshCollider mc = GetComponent<MeshCollider>();
        if (mc != null)
        {
            mc.sharedMesh = mesh;
        }
    }

    void InitializeTexture()
    {
        paintTexture = new Texture2D(cols, rows, TextureFormat.RGBA32, false);
        paintTexture.filterMode = FilterMode.Point;
        paintTexture.wrapMode = TextureWrapMode.Clamp;

        colorCache = new Color[cols, rows];

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                paintTexture.SetPixel(x, y, Color.white);
                colorCache[x, y] = Color.white;
            }
        }

        paintTexture.Apply();
        gridMaterial.mainTexture = paintTexture;
    }

    public void cleanRewind()
    {
        rewind = new Stack<(int, int, Color, Color)>();
        rewindAux = new Stack<(int, int, Color, Color)>();
    }

    public void rewindStepForward()
    {
        var (x, y, lastColor, newColor) = rewindAux.Pop();
        rewind.Push((x,y, lastColor, newColor));

        PaintCell(x, y, newColor,false);
        ApplyPaint();
    }

    public void rewindStepBackward()
    {
        Debug.Log(rewind.Count);
        var (x, y, lastColor, newColor) = rewind.Pop();
        rewindAux.Push((x, y, lastColor, newColor));
        Debug.Log(rewind.Count);
        PaintCell(x, y, lastColor,false);
        ApplyPaint();
    }

    public void PaintCell(int x, int y, Color color,bool saveRewind)
    {
        if (x >= 0 && y >= 0 && x < cols && y < rows)
        {
            if (colorCache[x, y] != color)
            {
                if(saveRewind)rewind.Push((x, y, GetColorAt(x, y), color));
                colorCache[x, y] = color;
                paintTexture.SetPixel(x, y, color);
                
                
            }
        }
    }

    public void ApplyPaint()
    {
        paintTexture.Apply();
    }

    public Color GetColorAt(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < cols && y < rows)
        {
            return colorCache[x, y];
        }

        return Color.white;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;

        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 localPos = new Vector3(x * tileSize + tileSize / 2f, 0.01f, y * tileSize + tileSize / 2f);
                Vector3 worldPos = transform.TransformPoint(localPos);
                Gizmos.DrawWireCube(worldPos, new Vector3(tileSize, 0.001f, tileSize));
            }
        }
    }

    public void Undo()
    {
        if (undoStack.Count > 0)
        {
            Texture2D lastTexture = undoStack.Pop();
            redoStack.Push(CopyCurrentTexture());
            LoadTexture(lastTexture);
        }
    }

    public void Redo()
    {
        if (redoStack.Count > 0)
        {
            Texture2D nextTexture = redoStack.Pop();
            undoStack.Push(CopyCurrentTexture());
            LoadTexture(nextTexture);
        }
    }

    Texture2D CopyCurrentTexture()
    {
        Texture2D copy = new Texture2D(paintTexture.width, paintTexture.height, TextureFormat.RGBA32, false);
        copy.SetPixels(paintTexture.GetPixels());
        copy.Apply();
        return copy;
    }

    public void LoadTexture(Texture2D texture)
    {
        paintTexture.SetPixels(texture.GetPixels());
        paintTexture.Apply();
        gridMaterial.mainTexture = paintTexture;

        // Also update colorCache to match the loaded texture
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                colorCache[x, y] = paintTexture.GetPixel(x, y);
            }
        }
    }

    void RestoreState(Color[,] state)
    {
        for (int x = 0; x < cols; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                colorCache[x, y] = state[x, y];
                paintTexture.SetPixel(x, y, state[x, y]);
            }
        }
        paintTexture.Apply();
    }
    public void SaveTextureToPictures(string fileName = "PaintedTexture")
    {
        // Ensure the texture is applied before encoding
        paintTexture.Apply();

        // Encode texture into PNG format
        byte[] bytes = paintTexture.EncodeToPNG();

        string fullPath = Path.Combine(Application.persistentDataPath, fileName + ".png");

        // Write the file
        File.WriteAllBytes(fullPath, bytes);

        Debug.Log($"Texture saved to: {fullPath}");
    }
}
