using System.Collections.Generic;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine.UI;
using TMPro;

public class PainterRaycaster2 : MonoBehaviour
{
    public enum DrawMode { Free, Line, ColorPicker, Size, Rewind, Default }

    public enum GameState { Draw, UndoRedo, ChooseColor, Navigation, Menu}
  
    public DrawMode currentMode = DrawMode.Default;
    public GameState currentState = GameState.Menu;

    bool isCircular = false;

    public SoundPlayer sound;

    public GameObject sizeText;

    private bool click = false;

    public Camera playerCamera;
    public float maxDistance = 100f;
    public Color paintColor = Color.red;
    public Color previewColor = Color.gray;
    public int brushSize = 1;
    
    public SingleMeshGrid singleMeshGrid; 

    public GameObject canvaColorChooser;

    public GameObject canvaMenu;

    private float rewindCooldown = 1f;
    private float rewindTimer = 0f;

    private Vector2Int? lastPaintedTile = null;
    private Vector2Int? startTile = null; 
    private LineRenderer previewLine;

    [Header("Mirror Settings")]
    public bool mirrorX = false;
    public bool mirrorY = false;

    private List<Vector3> headPositions = new List<Vector3>();
    private float recordInterval = 0.05f;
    private float recordTimer = 0f;

    [Header("Circular Gesture Settings")]
    public int maxSamples = 60;
    public float closureThreshold = 0.2f; // How close end is to start
    public float minRadius = 0.05f;       // Minimum radius of the circle

    public PlayerController controller;

    Vector3 spawnPosition;
    Quaternion spawnRotation;


    private void Start()
    {
        currentMode = DrawMode.Default;
        float distanceFromCamera = 5f;
        Transform camTransform = Camera.main.transform;

        spawnPosition = camTransform.position + camTransform.forward * distanceFromCamera;


        spawnRotation = Quaternion.LookRotation(-canvaMenu.transform.position + camTransform.position);
        canvaMenu.SetActive(true);
        canvaMenu.transform.position = spawnPosition;
        canvaMenu.transform.rotation = spawnRotation;
    }

    public void ReceiveCommand(string command)
    {

        string[] parts = Regex.Split(command, "G")
                .Select(part => Regex.Replace(part, @"[^a-zA-Z0-9\s]", "").ToLower()) // clean and lowercase
                .Where(part => !string.IsNullOrWhiteSpace(part)) // remove empty
                .ToArray();

        foreach (string word in parts) {

            if (word == "color")
            {
                sound.playConfirm();
                if (currentState != GameState.ChooseColor)
                {
                    currentState = GameState.ChooseColor;


                    float distanceFromCamera = 5f;
                    Transform camTransform = Camera.main.transform;

                    spawnPosition = camTransform.position + camTransform.forward * distanceFromCamera;


                    spawnRotation = Quaternion.LookRotation(spawnPosition - camTransform.position);

                 
                    
                }
            }
            else if (word == "free")
            {
                sound.playConfirm();
                currentMode = DrawMode.Free;
                singleMeshGrid.cleanRewind();

            }

            else if (word == "line")
            {
                sound.playConfirm();
                currentMode = DrawMode.Line;
                
            }

            else if (word == "picker")
            {
                sound.playConfirm();
                currentMode = DrawMode.ColorPicker;
                
            }

            else if (word == "click")
            {
                sound.playClick();
                SimulateClick();
            }
            else if(word == "rewind")
            {
                sound.playConfirm();
                currentMode = DrawMode.Rewind;
            }
            else if(word == "size")
            {
                sound.playConfirm();
                currentMode = DrawMode.Size;
                
            }
            else if (word == "forwards")
            {
                sound.playConfirm();
                controller.HandleZMovement(true);
            }
            else if (word == "backwards")
            {
                sound.playConfirm();
                controller.HandleZMovement(false);
            }
            else if (word == "navigation")
            {
                sound.playConfirm();
                currentState = GameState.Navigation;
            }
            else if (word == "undo")
            {
                sound.playConfirm();
                currentState = GameState.UndoRedo;
            }
            else if (word == "draw")
            {
                sound.playConfirm();
                currentState = GameState.Draw;
            }
            else if (word == "menu")
            {
                sound.playConfirm();
                currentState = GameState.Menu;
                float distanceFromCamera = 5f;
                Transform camTransform = Camera.main.transform;

                spawnPosition = camTransform.position + camTransform.forward * distanceFromCamera;

                spawnRotation = Quaternion.LookRotation(spawnPosition - camTransform.position);
            }
            else if(word == "stop")
            {
                sound.playConfirm();
                currentMode = DrawMode.Default ;
            }

        }

        


    }

    public void disableMenu()
    {
        sound.playConfirm();
        currentState = GameState.Draw;
    }
        

    void Update()
    {
        if(!isCircular)
            HandleHeadMovement();
        if (!canvaColorChooser.activeSelf && currentState == GameState.ChooseColor)
        {
            canvaColorChooser.SetActive(true);
            canvaColorChooser.transform.position = spawnPosition;
            canvaColorChooser.transform.rotation = spawnRotation;
        }
        if (canvaColorChooser.activeSelf && currentState != GameState.ChooseColor)
            canvaColorChooser.SetActive(false);

        if (!canvaMenu.activeSelf && currentState == GameState.Menu)
        {
            canvaMenu.SetActive(true);
            canvaMenu.transform.position = spawnPosition;
            canvaMenu.transform.rotation = spawnRotation;
        }
        if (canvaMenu.activeSelf && currentState != GameState.Menu)
            canvaMenu.SetActive(false);

        singleMeshGrid.drawing = (currentMode == DrawMode.Free);

        if (currentMode != DrawMode.Free)
            lastPaintedTile = null;

        switch (currentState)
        {
            case GameState.Draw:
                switch (currentMode)
                {
                    case DrawMode.Line:
                        HandleLineDrawing();
                        break;
                    case DrawMode.Free:
                        HandleFreeDrawing();
                        break;
                    case DrawMode.ColorPicker:
                        HandleColorPicker();
                        break;
                    case DrawMode.Size:
                        HandleChooseSize();
                        break;
                    case DrawMode.Rewind:
                        HandleRewind();
                        break;
                }
                break;
            case GameState.Navigation:
                controller.LookBasedXYMovementWithDeadzone();
                break;
            case GameState.UndoRedo:
                controller.LookBasedUndoRedoWithDeadzone();
                break;
            


        }

        

    }

    private void HandleRewind()
    {
        recordTimer += Time.deltaTime;
        rewindTimer = Time.time;

        if (recordTimer >= recordInterval)
        {
            recordTimer = 0f;
            RecordHeadPosition();

            if (headPositions.Count >= maxSamples)
            {
                if (DetectCircularMovement() && rewindCooldown < rewindTimer)
                {
                    Debug.Log("Círculo detetado com a cabeça!");
                    headPositions.Clear(); // reset
                    rewindTimer = Time.time + rewindCooldown;
                }
            }
        }
    }

    void RecordHeadPosition()
    {
        Vector3 headPos = playerCamera.transform.position;
        headPositions.Add(headPos);

        if (headPositions.Count > maxSamples)
            headPositions.RemoveAt(0); // manter o tamanho
    }

    int GetRotationDirection2D(List<Vector3> positions)
    {
        int clockwiseCount = 0;
        int counterClockwiseCount = 0;

        for (int i = 1; i < positions.Count - 1; i++)
        {
            Vector2 a = new Vector2(positions[i - 1].x, positions[i - 1].z);
            Vector2 b = new Vector2(positions[i].x, positions[i].z);
            Vector2 c = new Vector2(positions[i + 1].x, positions[i + 1].z);

            Vector2 ab = (b - a).normalized;
            Vector2 bc = (c - b).normalized;

            float cross = ab.x * bc.y - ab.y * bc.x;

            if (cross > 0.01f) counterClockwiseCount++;
            else if (cross < -0.01f) clockwiseCount++;
        }

        int total = clockwiseCount + counterClockwiseCount;

        // Verifica se há direção dominante clara (ex: >70%)
        if (total == 0) return 0;

        float ratio = Mathf.Abs(clockwiseCount - counterClockwiseCount) / (float)total;

        if (ratio < 0.1f) return 0; // muito equilibrado → não se decide

        return (clockwiseCount > counterClockwiseCount) ? -1 : 1;
    }

    private int previousZone = -1;

    private void HandleHeadMovement()
    {
        float roll = playerCamera.transform.eulerAngles.z;
        if (roll > 180f) roll -= 360f; // Normalize to [-180, 180]

        // Limita o roll entre -45 e +45
        float clampedRoll = Mathf.Clamp(roll, -45f, 45f);

        // Mapear o valor para uma zona [1,5]
        int zone = Mathf.FloorToInt(((clampedRoll + 45f) / 90f) * 5f) + 1;
        zone = Mathf.Clamp(zone, 1, 5);

        // Só atua quando a zona muda
        if (zone != previousZone)
        {
            previousZone = zone;

            switch (zone)
            {
                case 1:
                    Debug.Log("Cabeça inclinada para a ESQUERDA → abrir menu");
                    
                    currentState = GameState.Menu;
                    float distanceFromCamera = 5f;
                    Transform camTransform = Camera.main.transform;

                    spawnPosition = camTransform.position + camTransform.forward * distanceFromCamera;



                    spawnRotation = Quaternion.LookRotation(spawnPosition - camTransform.position);
                    sound.playConfirm();
                    break;

                case 5:
                    Debug.Log("Cabeça inclinada para a DIREITA → trocar modo de desenho");
                    if(currentState == GameState.Draw)
                    {
                        DrawMode current = currentMode;
                        currentMode = (DrawMode)(((int)current + 1) % System.Enum.GetNames(typeof(DrawMode)).Length);

                        sound.playConfirm();
                    }
                    
                    break;
            }
        }
    }



    bool DetectCircularMovement()
    {
        if (headPositions.Count < 10) return false;

        Vector3 center = headPositions.Aggregate(Vector3.zero, (acc, p) => acc + p) / headPositions.Count;

        float avgRadius = 0f;
        float variance = 0f;

        foreach (var pos in headPositions)
        {
            float dist = Vector3.Distance(pos, center);
            avgRadius += dist;
        }

        avgRadius /= headPositions.Count;

        foreach (var pos in headPositions)
        {
            float dist = Vector3.Distance(pos, center);
            variance += Mathf.Pow(dist - avgRadius, 2);
        }

        variance /= headPositions.Count;

        float closure = Vector3.Distance(headPositions[0], headPositions[^1]);
        bool isClosed = closure <= closureThreshold;
        isCircular = variance <= 0.01f && avgRadius > minRadius;

        if (isClosed && isCircular)
        {
            int direction = GetRotationDirection2D(headPositions);
            if (direction == 1)
            {
                Debug.Log("Círculo no sentido HORÁRIO");
                singleMeshGrid.rewindStepForward();
                sound.playConfirm();
                
            }
            else if (direction == -1)
            {
                Debug.Log("Círculo no sentido ANTI-HORÁRIO");
                singleMeshGrid.rewindStepBackward();
                sound.playConfirm();
                
            }

            return true;
        }
        isCircular = false;
        return false;
    }



    private void HandleChooseSize()
    {
        float pitch = playerCamera.transform.eulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // Normalize to [-180, 180]

        // Limita o pitch entre -45 (olhar para cima) e +45 (olhar para baixo)
        float clampedPitch = Mathf.Clamp(pitch, -45f, 45f);

        // Mapear -45 (cima) → 1 e +45 (baixo) → 5
        int zone = Mathf.FloorToInt(((clampedPitch + 45f) / 90f) * 5f) + 1;
        zone = Mathf.Clamp(zone, 1, 5);

        if (brushSize != zone)
        {
            brushSize = zone;

            sizeText.GetComponent<TMPro.TMP_Text>().text = "Size: " + brushSize;
            sound.playSelect();
        }
    }


    public void IncreaseBrushSize()
    {
        brushSize = Mathf.Min(brushSize + 1, 4);
        
    }

    public void DecreaseBrushSize()
    {
        brushSize = Mathf.Max(1, brushSize - 1);
        
    }

    public void ToggleMirrorX()
    {
        mirrorX = !mirrorX;
        Debug.Log("Mirror X toggled (via voice)");
    }

    public void ToggleMirrorY()
    {
        mirrorY = !mirrorY;
        Debug.Log("Mirror Y toggled (via voice)");
    }
    /*
    void HandleMirrorToggle()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            mirrorX = !mirrorX;
            Debug.Log("Mirror X: " + mirrorX);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            mirrorY = !mirrorY;
            Debug.Log("Mirror Y: " + mirrorY);
        }
    }

    void HandleModeSwitch()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            currentMode = DrawMode.Line;
            startTile = null;
            if (previewLine != null) Destroy(previewLine.gameObject);
            Debug.Log("Mode: LINE");
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            currentMode = DrawMode.Free;
            lastPaintedTile = null;
            startTile = null;
            if (previewLine != null) Destroy(previewLine.gameObject);
            Debug.Log("Mode: FREE DRAW");
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            currentMode = DrawMode.ColorPicker;
            if (previewLine != null) Destroy(previewLine.gameObject);
            Debug.Log("Mode: COLOR PICKER");
        }
    }

    void HandleBrushSizeInput()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            brushSize = Mathf.Min(brushSize + 1, 4);
            Debug.Log($"Brush size increased to {brushSize}");
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            brushSize = Mathf.Max(1, brushSize - 1);
            Debug.Log($"Brush size decreased to {brushSize}");
        }
    }

    void HandleFreeDrawing()
    {
        if (!Input.GetMouseButton(0))
        {
            lastPaintedTile = null; 
            return;
        }

        paintTimer = 0f;

        if (TryGetTileUnderCursor(out Vector2Int tile))
        {
            if (lastPaintedTile.HasValue && tile != lastPaintedTile.Value)
            {
                DrawLineBetween(lastPaintedTile.Value, tile, paintColor);
            }
            else
            {
                ApplyBrush(tile.x, tile.y, paintColor);
            }

            lastPaintedTile = tile;
            singleMeshGrid.ApplyPaint();
        }
    }

    void HandleLineDrawing()
    {
        if (TryGetTileUnderCursor(out Vector2Int tile))
        {
            if (startTile.HasValue && tile != startTile.Value && !Input.GetMouseButton(0))
            {
                ShowPreviewLine(startTile.Value, tile);
            }

            if (Input.GetMouseButtonDown(0))
            {
                if (!startTile.HasValue)
                {
                    startTile = tile;
                    Debug.Log($"Start tile set at: {tile}");
                }
                else
                {
                    DrawLineBetween(startTile.Value, tile, paintColor);
                    singleMeshGrid.ApplyPaint();
                    if (previewLine != null) Destroy(previewLine.gameObject);
                    startTile = null;
                }
            }
        }
    }

    void HandleColorPicker()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        if (TryGetTileUnderCursor(out Vector2Int tile))
        {
            paintColor = singleMeshGrid.GetColorAt(tile.x, tile.y);
            Debug.Log($"Picked color: {paintColor}");
        }
    }

    void DrawLineBetween(Vector2Int from, Vector2Int to, Color color)
    {
        List<Vector2Int> points = GetLinePoints(from, to);
        foreach (var p in points)
        {
            ApplyBrush(p.x, p.y, color);
        }
    }
    */

    void HandleFreeDrawing()
    {
        sound.playDraw();

        if (TryGetTileUnderCursor(out Vector2Int tile))
        {
            if (lastPaintedTile.HasValue && tile != lastPaintedTile.Value)
            {
                DrawLineBetween(lastPaintedTile.Value, tile, paintColor);
            }
            else
            {
                ApplyBrush(tile.x, tile.y, paintColor);
            }

            lastPaintedTile = tile;
            singleMeshGrid.ApplyPaint();
        }
    }

    void HandleLineDrawing()
    {
        if (TryGetTileUnderCursor(out Vector2Int tile))
        {
            if (startTile.HasValue && tile != startTile.Value && !click)
            {
                ShowPreviewLine(startTile.Value, tile);
            }

            if (IsClickTriggered())
            {
                if (!startTile.HasValue)
                {
                    startTile = tile;
                    Debug.Log($"Start tile set at: {tile}");
                }
                else
                {
                    DrawLineBetween(startTile.Value, tile, paintColor);
                    singleMeshGrid.ApplyPaint();
                    if (previewLine != null) Destroy(previewLine.gameObject);
                    startTile = null;
                }
            }
        }
    }

    void HandleColorPicker()
    {
        if (TryGetTileUnderCursor(out Vector2Int tile) && IsClickTriggered())
        {
            paintColor = singleMeshGrid.GetColorAt(tile.x, tile.y);
            Debug.Log($"Picked color: {paintColor}");
        }
    }

    void DrawLineBetween(Vector2Int from, Vector2Int to, Color color)
    {
        List<Vector2Int> points = GetLinePoints(from, to);
        foreach (var p in points)
        {
            ApplyBrush(p.x, p.y, color);
        }
    }

    void ApplyBrush(int centerX, int centerY, Color color)
    {
        ApplySingleBrush(centerX, centerY, color);

        if (mirrorX)
        {
            int mirroredX = (singleMeshGrid.cols - 1) - centerX;
            ApplySingleBrush(mirroredX, centerY, color);
        }

        if (mirrorY)
        {
            int mirroredY = (singleMeshGrid.rows - 1) - centerY;
            ApplySingleBrush(centerX, mirroredY, color);
        }

        if (mirrorX && mirrorY)
        {
            int mirroredX = (singleMeshGrid.cols - 1) - centerX;
            int mirroredY = (singleMeshGrid.rows - 1) - centerY;
            ApplySingleBrush(mirroredX, mirroredY, color);
        }
    }

    void ApplySingleBrush(int centerX, int centerY, Color color)
    {
        if (brushSize == 1)
        {
            singleMeshGrid.PaintCell(centerX, centerY, color,true);
            return;
        }

        for (int dx = -brushSize; dx <= brushSize; dx++)
        {
            for (int dy = -brushSize; dy <= brushSize; dy++)
            {
                singleMeshGrid.PaintCell(centerX + dx, centerY + dy, color,true);
            }
        }
    }


    bool TryGetTileUnderCursor(out Vector2Int tile)
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Vector3 localPos = singleMeshGrid.transform.InverseTransformPoint(hit.point);
            int tileX = Mathf.FloorToInt(localPos.x / singleMeshGrid.tileSize);
            int tileY = Mathf.FloorToInt(localPos.z / singleMeshGrid.tileSize);
            tile = new Vector2Int(tileX, tileY);
            return true;
        }

        tile = Vector2Int.zero;
        return false;
    }

    List<Vector2Int> GetLinePoints(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> points = new List<Vector2Int>();

        int x0 = start.x;
        int y0 = start.y;
        int x1 = end.x;
        int y1 = end.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            points.Add(new Vector2Int(x0, y0));

            if (x0 == x1 && y0 == y1) break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return points;
    }

    void ShowPreviewLine(Vector2Int start, Vector2Int end)
    {
        if (previewLine == null)
        {
            GameObject lineObj = new GameObject("PreviewLine");
            previewLine = lineObj.AddComponent<LineRenderer>();
            previewLine.material = new Material(Shader.Find("Sprites/Default"));
            previewLine.startColor = previewColor;
            previewLine.endColor = previewColor;
            previewLine.startWidth = 0.02f;
            previewLine.endWidth = 0.02f;
            previewLine.positionCount = 0;
        }

        List<Vector2Int> points = GetLinePoints(start, end);
        previewLine.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 pos = new Vector3((points[i].x + 0.5f) * singleMeshGrid.tileSize, 0.01f, (points[i].y + 0.5f) * singleMeshGrid.tileSize);
            previewLine.SetPosition(i, singleMeshGrid.transform.TransformPoint(pos));
        }
    }

    private void SimulateClick()
    {
        click = true;
    }

    private bool IsClickTriggered()
    {
        if (click)
        {
            click = false;
            return true;
        }
        return false;
    }

    public void SaveTextureToPictures()
    {
        singleMeshGrid.SaveTextureToPictures();
        sound.playConfirm();
    }

    

    public void SetStateToDraw()
    {
        currentState = GameState.Draw;
        Debug.Log("GameState: Draw");
        
    }

    public void SetStateToUndoRedo()
    {
        currentState = GameState.UndoRedo;
        sound.playConfirm();
        Debug.Log("GameState: UndoRedo");
    }

    public void SetStateToChooseColor()
    {
        currentState = GameState.ChooseColor;
        sound.playConfirm();
        Debug.Log("GameState: ChooseColor");
        float distanceFromCamera = 5f;
        Transform camTransform = Camera.main.transform;

        spawnPosition = camTransform.position + camTransform.forward * distanceFromCamera;

        spawnRotation = Quaternion.LookRotation(spawnPosition - camTransform.position);

    }

    public void SetStateToNavigation()
    {
        currentState = GameState.Navigation;
        sound.playConfirm();
        Debug.Log("GameState: Navigation");
    }

    public void SetStateToMenu()
    {
        currentState = GameState.Menu;
        Debug.Log("GameState: Menu");
    }

    public void ExitApplication()
    {
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            activity.Call("finish");
        }

        Debug.Log("Aplicação encerrada.");
    }




}
