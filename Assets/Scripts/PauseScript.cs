using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public bool paused;
    public CanvasGroup pauseScreen;

    private CursorLockMode originalCursorLockMode;
    private static readonly object pauseLock = new Object();

    // Use this for initialization
    void Start()
    {
        paused = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        originalCursorLockMode = Cursor.lockState;
        paused = !paused;
        if (paused)
        {
            Time.timeScale = 0;
            ShowPauseScreen();
        }
        else if (!paused)
        {
            Time.timeScale = 1;
            HidePauseScreen();
        }
    }

    private void ShowPauseScreen()
    {
        if (originalCursorLockMode == CursorLockMode.Locked)
        {
            UIUtils.UnlockCursor();
        }

        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(pauseScreen, 1, true, 0.05f, 0.025f, pauseLock));
    }

    private void HidePauseScreen()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(pauseScreen, 0, false, 0.05f, 0.025f, pauseLock));
        if (originalCursorLockMode == CursorLockMode.Locked)
        {
            UIUtils.LockCursor();
        }
    }
}