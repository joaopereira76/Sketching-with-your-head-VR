using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GazeButton : MonoBehaviour
{
    public float gazeTime = 2f; // Time to trigger interaction
    public UnityEvent onGazeComplete;

    public Color color = Color.white;
    public Color highlightColor = Color.gray;


    private float gazeTimer = 0f;
    private bool isGazing = false;

    void Update()
    {
        if (isGazing)
        {
            gazeTimer += Time.deltaTime;
            GetComponent<Image>().color = highlightColor;
            if (gazeTimer >= gazeTime)
            {

                onGazeComplete.Invoke();
                gazeTimer = 0f; // Reset if you want single-use
                isGazing = false;
                
            }
        }
        else
        {
            gazeTimer = 0f;
            GetComponent<Image>().color = color;
        }
    }

    public void StartGaze()
    {
        isGazing = true;
        //GetComponent<Outline>().enabled = true;
    }

    public void StopGaze()
    {
        isGazing = false;
        //GetComponent<Outline>().enabled = false;
    }
}
