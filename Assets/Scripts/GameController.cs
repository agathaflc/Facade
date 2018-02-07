using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {

	public Text questionDisplayText;
	public Text scoreDisplayText;
	public Text timeRemainingDisplayText;
	public Text highScoreDisplayText;
	public SimpleObjectPool answerButtonObjectPool;
	public Transform answerButtonParent;

	public GameObject questionDisplay;
	public GameObject roundEndDisplay;

    public Camera PlayerCamera;
    public GameObject Player;

	private DataController dataController;
	private RoundData currentRoundData;
	private QuestionData[] questionPool;

	private bool isRoundActive;
	private float timeRemaining;
	private int questionIndex;
	private int playerScore;
	private List<GameObject> answerButtonGameObjects = new List<GameObject> ();
    public AnswerButton SelectedBoldAnswer;

	// Use this for initialization
	void Start () {
		dataController = FindObjectOfType<DataController> (); // store a ref to data controller
		currentRoundData = dataController.GetCurrentRoundData();
		questionPool = currentRoundData.questions;
		timeRemaining = currentRoundData.timeLimitInSeconds;
		UpdateTimeRemainingDisplay ();

		playerScore = 0;
		questionIndex = 0;

		ShowQuestion ();
		isRoundActive = true;
	}

    private void BeginQuestions() {
        questionDisplay.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        PlayerCamera.GetComponent<PlayerLook>().enabled = false;
        Player.transform.rotation = Quaternion.Euler(new Vector3(0,90,0));
        PlayerCamera.transform.rotation = Quaternion.Euler(new Vector3(0, 90, 0));
    }

    private void ShowQuestion() {
		RemoveAnswerButtons ();
		QuestionData questionData = questionPool [questionIndex];
		questionDisplayText.text = questionData.questionText;

		for (int i = 0; i < questionData.answers.Length; i++) {
			GameObject answerButtonGameObject = answerButtonObjectPool.GetObject ();
			answerButtonGameObjects.Add (answerButtonGameObject); // add the current answer button to the list of ACTIVE answer buttonsso we can keep track of it
			answerButtonGameObject.transform.SetParent (answerButtonParent);

			AnswerButton answerButton = answerButtonGameObject.GetComponent<AnswerButton> ();
			answerButton.Setup (questionData.answers [i]);
            if (i == 0)
            {
                SelectedBoldAnswer = answerButton;
                SelectedBoldAnswer.Bold();
            }
		}
	}

	// remove the existing answer buttons
	private void RemoveAnswerButtons() {
		while (answerButtonGameObjects.Count > 0) {
			// return it to object pool i.e ready to be recycled and reused
			answerButtonObjectPool.ReturnObject (answerButtonGameObjects [0]); 
			answerButtonGameObjects.RemoveAt (0); // remove it from list of active answerButtongameObjects
		}
	}

	public void AnswerButtonClicked(bool isCorrect) {
		if (isCorrect) { // add score if answer is correct
			playerScore += currentRoundData.pointsAddedForCorrectAnswer;
			scoreDisplayText.text = "Score: " + playerScore.ToString ();
		}

		// show another question if there are still questions to ask
		if (questionPool.Length > questionIndex + 1) {
			questionIndex++;
			ShowQuestion ();
		} else {
			EndRound ();
			print ("end this round");
		}
	}

	public void EndRound() {
		isRoundActive = false;
		dataController.SubmitNewPlayerScore (playerScore);
		highScoreDisplayText.text = dataController.GetHighestPlayerScore ().ToString ();

		questionDisplay.SetActive (false); // deactivate the question display
		roundEndDisplay.SetActive (true); // activate (show) the round end display
	}

	public void ReturnToMenu(){
		SceneManager.LoadScene ("MenuScreen");
	}

	private void UpdateTimeRemainingDisplay() {
		timeRemainingDisplayText.text = "Time: " + Mathf.Round (timeRemaining).ToString ();
	}

	// Update is called once per frame
	void Update () {
		if (isRoundActive && questionDisplay.activeSelf == true) {
			timeRemaining -= Time.deltaTime;
			UpdateTimeRemainingDisplay ();

			if (timeRemaining <= 0f) {
				EndRound ();
			}
		}

        if (Input.GetKeyDown("e")) {
            BeginQuestions();
        }
	}
}
