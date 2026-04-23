using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorNormal;
    [SerializeField] private Texture2D cursorShoot;
    [SerializeField] private Texture2D cursorReload;

    void Start()
    {
        SetCursorTexture(cursorNormal);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            SetCursorTexture(cursorShoot);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            SetCursorTexture(cursorReload);
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.R))
        {
            SetCursorTexture(cursorNormal);
        }
    }

    private void SetCursorTexture(Texture2D cursorTexture)
    {
        if (cursorTexture == null)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            return;
        }

        Vector2 hotSpot = new Vector2(cursorTexture.width * 0.5f, cursorTexture.height * 0.5f);
        Cursor.SetCursor(cursorTexture, hotSpot, CursorMode.Auto);
    }
}
