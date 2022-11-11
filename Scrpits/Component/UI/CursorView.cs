using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

public class CursorView : BaseMonoBehaviour
{
    public Texture2D cursorDef;
    public Texture2D cursorDown;

    protected Vector2 offsetCursor;

    public bool isCustom = false;
    public List<Texture2D> listCursorCustom = new List<Texture2D>();

    private void Awake()
    {
        offsetCursor = new Vector2(40, 0);
        Cursor.SetCursor(cursorDef, Vector2.zero, CursorMode.Auto);
    }

    private void Update()
    {
        if (isCustom)
        {
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Cursor.SetCursor(cursorDown, offsetCursor, CursorMode.Auto);
        }
        if (Input.GetMouseButtonUp(0))
        {
            Cursor.SetCursor(cursorDef, offsetCursor, CursorMode.Auto);
        }
    }

    public void SetCursor(Texture2D cursorIcon)
    {
        StopAllCoroutines();
        isCustom = true;
        Cursor.SetCursor(cursorIcon, offsetCursor, CursorMode.Auto);
    }

    public void SetCursor(List<Texture2D> listCursorCustom)
    {
        StopAllCoroutines();
        isCustom = true;
        this.listCursorCustom.AddRange(listCursorCustom);
        StartCoroutine(CoroutineForCursorAnim());
    }

    public void SetDef()
    {
        StopAllCoroutines();
        isCustom = false;
        this.listCursorCustom.Clear();
        Cursor.SetCursor(cursorDef, offsetCursor, CursorMode.Auto);
    }

    public IEnumerator CoroutineForCursorAnim()
    {
        int cursorAnimPosition = 0;
        while (listCursorCustom.Count > 0)
        {
            Cursor.SetCursor(listCursorCustom[cursorAnimPosition], offsetCursor, CursorMode.Auto);
            yield return new WaitForSecondsRealtime(0.1f);
            cursorAnimPosition++;
            if (cursorAnimPosition >= listCursorCustom.Count)
            {
                cursorAnimPosition = 0;
            }
        }
    }
}