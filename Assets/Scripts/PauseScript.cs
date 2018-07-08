using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    public bool paused;
    public CanvasGroup pauseMenu;
    private static object pauseLock;

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

    private void TogglePause()
    {
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
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(pauseMenu, 1, false, 0.05f, 0.05f, pauseLock));
    }

    private void HidePauseScreen()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(pauseMenu, 0, false, 0.05f, 0.05f, pauseLock));
    }
}