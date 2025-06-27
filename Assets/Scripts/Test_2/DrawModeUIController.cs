using UnityEngine;
using UnityEngine.UI;

public class DrawModeUIController : MonoBehaviour
{
    [Header("Mode Icons (RawImages)")]
    [SerializeField] private GameObject freeDrawIcon;
    [SerializeField] private GameObject lineDrawIcon;
    [SerializeField] private GameObject colorPickerIcon;
    [SerializeField] private GameObject sizeText;
    [SerializeField] private GameObject RepeatIcon;

    [Header("Painter Raycaster")]
    [SerializeField] private PainterRaycaster2 painter;

    void Update()
    {
        if (painter == null) return;

        
        freeDrawIcon.SetActive((painter.currentMode == PainterRaycaster2.DrawMode.Free || painter.currentMode == PainterRaycaster2.DrawMode.Default) && painter.currentState == PainterRaycaster2.GameState.Draw);
        lineDrawIcon.SetActive(painter.currentMode == PainterRaycaster2.DrawMode.Line && painter.currentState == PainterRaycaster2.GameState.Draw);
        colorPickerIcon.SetActive(painter.currentMode == PainterRaycaster2.DrawMode.ColorPicker && painter.currentState == PainterRaycaster2.GameState.Draw);
        sizeText.SetActive(painter.currentMode == PainterRaycaster2.DrawMode.Size && painter.currentState == PainterRaycaster2.GameState.Draw);
        RepeatIcon.SetActive(painter.currentMode == PainterRaycaster2.DrawMode.Rewind && painter.currentState == PainterRaycaster2.GameState.Draw);


    }
}
