using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreenController : MonoBehaviour
{
    public CanvasGroup helpPanel;
    public CanvasGroup mainPanel;
    public CanvasGroup areYouSurePanel;
    private static readonly object menuLock = new object();
    private static readonly object areYouSureLock = new object();

    public void StartGame()
    {
        Initiate.Fade("GameScene", Color.black, 0.8f);
        //SceneManager.LoadScene("Game");
    }

    public void ShowHelp()
    {
        StartCoroutine(HideMainShowHelp());
    }

    public void BackFromHelp()
    {
        StartCoroutine(HideHelpShowMain());
    }

    public void QuitGame()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(areYouSurePanel, 1, true, 0.05f, 0.025f, areYouSureLock));
    }

    public void CancelQuit()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(areYouSurePanel, 0, false, 0.05f, 0.025f, areYouSureLock));
    }

    public void ConfirmQuit()
    {
        Application.Quit();
    }
    private IEnumerator HideMainShowHelp()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(mainPanel, 0, false, 0.05f, 0.05f, menuLock));
        yield return new WaitForSecondsRealtime(1f);
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(helpPanel, 1, true, 0.05f, 0.05f, menuLock));
    }
    
    private IEnumerator HideHelpShowMain()
    {
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(helpPanel, 0, false, 0.05f, 0.05f, menuLock));
        yield return new WaitForSecondsRealtime(1f);
        StartCoroutine(UIUtils.GraduallyChangeCanvasGroupAlpha(mainPanel, 1, true, 0.05f, 0.05f, menuLock));
    }
}