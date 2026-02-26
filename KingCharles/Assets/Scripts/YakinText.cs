using UnityEngine;

public class YakinText : MonoBehaviour
{
    public GameObject textUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            textUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            textUI.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (textUI != null)
        {
            textUI.SetActive(false);
        }
    }
}