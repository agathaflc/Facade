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
        StartCoroutine(GraduallyChangeCanvasGroupAlpha(mainPanel, 0, false, 0.05f, 0.05f));
        yield return new WaitForSecondsRealtime(1f);
        StartCoroutine(GraduallyChangeCanvasGroupAlpha(helpPanel, 1, true, 0.05f, 0.05f));
    }
    
    private IEnumerator HideHelpShowMain()
    {
        StartCoroutine(GraduallyChangeCanvasGroupAlpha(helpPanel, 0, false, 0.05f, 0.05f));
        yield return new WaitForSecondsRealtime(1f);
        StartCoroutine(GraduallyChangeCanvasGroupAlpha(mainPanel, 1, true, 0.05f, 0.05f));
    }

    private static IEnumerator GraduallyChangeCanvasGroupAlpha(CanvasGroup canvas, float targetAlpha, bool increase,
        float step, float waitTimeInSeconds)
    {
        lock (menuLock)
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
}