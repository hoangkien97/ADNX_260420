using UnityEngine;
using TMPro;

public class PlayerNameDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText; 

    private void Start()
    {

        if (ApiManager.EnsureInstance() != null && ApiManager.IsLoggedIn)
        {
            if (nameText != null)
            {
                nameText.text = ApiManager.CurrentUsername;
            }
        }
        else
        {
            if (nameText != null)
            {
                nameText.text = "Guest";
            }
        }
    }
}
