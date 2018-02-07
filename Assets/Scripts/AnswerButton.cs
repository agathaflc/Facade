using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AnswerButton : MonoBehaviour {

	public Text answerText;
	private AnswerData answerData;
	private GameController gameController;

	// Use this for initialization
	void Start () {
		gameController = FindObjectOfType<GameController> ();
	}

	public void Setup(AnswerData data) {
		answerData = data;
		answerText.text = answerData.answerText;
	}

	public void HandleClick() {
		gameController.AnswerButtonClicked ();
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
        gameController.SelectedBoldAnswer.UnBold();
        gameController.SelectedBoldAnswer = this;
        this.Bold();
    }
}
