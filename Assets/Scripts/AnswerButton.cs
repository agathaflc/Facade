using UnityEngine;
using UnityEngine.UI;

public class AnswerButton : MonoBehaviour
{
    private AnswerData answerData;
    public Text answerText;
    private GameController gameController;

    // Use this for initialization
    private void Start()
    {
        gameController = FindObjectOfType<GameController>();
    }

    public void Setup(AnswerData data)
    {
        answerData = data;
        answerText.text = answerData.answerText;
    }

    public AnswerData GetAnswerData()
    {
        return answerData;
    }

    public void HandleClick()
    {
        gameController.AnswerButtonClicked(this);
    }

    public void Bold()
    {
        answerText.text = "<b>" + answerData.answerText + "</b>";
    }

    public void UnBold()
    {
        answerText.text = answerData.answerText;
    }

    public void HandleOnMouseEnter()
    {
        gameController.selectedBoldAnswer.UnBold();
        gameController.selectedBoldAnswer = this;
        Bold();
    }
}