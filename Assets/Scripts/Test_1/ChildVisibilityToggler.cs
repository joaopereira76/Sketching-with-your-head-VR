using UnityEngine;

public class ChildVisibilityToggler : MonoBehaviour
{
    bool showing = false;
    public void HideShowChildren()
    {
        showing = !showing;
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(showing);
        }
    }

}
