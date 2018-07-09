using System.Collections;
using UnityEngine;

public static class UIUtils
{
    public static IEnumerator GraduallyChangeCanvasGroupAlpha(CanvasGroup canvas, float targetAlpha, bool increase,
        float step, float waitTimeInSeconds, object canvasLock)
    {
        lock (canvasLock)
        {
            if (increase)
            {
                canvas.blocksRaycasts = true;

                while (canvas.alpha + step < targetAlpha)
                {
                    canvas.alpha += step;
                    yield return new WaitForSecondsRealtime(waitTimeInSeconds);
                }
            }
            else
            {
                while (canvas.alpha - step > targetAlpha)
                {
                    canvas.alpha -= step;
                    yield return new WaitForSecondsRealtime(waitTimeInSeconds);
                }

                canvas.blocksRaycasts = false;
            }
        }
    }
    
    public static void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}