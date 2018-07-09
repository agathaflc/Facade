using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseScript : MonoBehaviour
{
    private string BACK_TO_MAIN_MENU = "MAIN_MENU";
    private string QUIT_GAME = "QUIT_GAME";
    
    public bool paused;
    public CanvasGroup pauseScreen;
    public CanvasGroup areYouSureScreen;

    private CursorLockMode originalCursorLockMode;
    private string menuOrQuit;
    private static readonly object pauseLock = new Object();
    private static readonly object areYouSureLock = new Object();

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

    public void BackToMainMenu()
    {
        menuOrQuit = BACK_TO_MAIN_MENU;
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(areYouSureScreen, 1, true, 0.05f, 0.025f, areYouSureLock));
    }

    public void QuitGame()
    {
        menuOrQuit = QUIT_GAME;
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(areYouSureScreen, 1, true, 0.05f, 0.025f, areYouSureLock));
    }

    public void CancelQuit()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(areYouSureScreen, 0, false, 0.05f, 0.025f, areYouSureLock));
    }

    public void ConfirmQuit()
    {
        Time.timeScale = 1;
        if (menuOrQuit.Equals(BACK_TO_MAIN_MENU)) Initiate.Fade("MenuScreen", Color.black, 0.8f);
        else if (menuOrQuit.Equals(QUIT_GAME)) Application.Quit();
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