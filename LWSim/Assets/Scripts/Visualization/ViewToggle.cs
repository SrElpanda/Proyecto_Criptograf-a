using UnityEngine;

public class ViewToggle : MonoBehaviour
{
    public GameObject[] views;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            for (int i = 0; i < views.Length; i++)
            {
                if (views[i].activeSelf)
                {
                    views[i].SetActive(false);
                    views[(i + 1) % views.Length].SetActive(true);
                    break;
                }
            }
        }
    }
}
