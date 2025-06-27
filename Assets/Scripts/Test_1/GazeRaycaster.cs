using UnityEngine;
using UnityEngine.UI;

public class GazeRaycaster : MonoBehaviour
{
    public float rayDistance = 10f;
    private GazeButton currentButton;
    public Camera mainCamera;
    

    void Update()
    {
        Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            GazeButton gazeButton = hit.collider.GetComponent<GazeButton>();
            Debug.Log(hit.collider);
            if (gazeButton != null)
            {
                if (gazeButton != currentButton)
                {
                    if (currentButton != null)
                        currentButton.StopGaze();

                    currentButton = gazeButton;
                    currentButton.StartGaze();
                }
            }
            else
            {
                if (currentButton != null)
                {
                    currentButton.StopGaze();
                    currentButton = null;
                }
            }
        }
        else
        {
            if (currentButton != null)
            {
                currentButton.StopGaze();
                currentButton = null;
            }
        }
    }
}
