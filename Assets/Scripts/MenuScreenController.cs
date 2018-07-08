using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreenController : MonoBehaviour
{
    public CanvasGroup helpPanel;
    public CanvasGroup mainPanel;
    private static readonly object menuLock = new object();

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