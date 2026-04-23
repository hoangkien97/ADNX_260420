using UnityEngine;

public class CursorManager : MonoBehaviour
{
    [SerializeField] private Texture2D cursorNormal;
    [SerializeField] private Texture2D cursorShoot;
    [SerializeField] private Texture2D cursorReload;
    private Vector2 hotSpot = new Vector2(0.5f, 0.5f);

    void Start()
    {
        Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(cursorShoot, hotSpot, CursorMode.Auto);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            Cursor.SetCursor(cursorReload, hotSpot, CursorMode.Auto);
        }
        else if (Input.GetMouseButtonUp(0) || Input.GetKeyUp(KeyCode.R))
        {
            Cursor.SetCursor(cursorNormal, hotSpot, CursorMode.Auto);
        }
    }
}
