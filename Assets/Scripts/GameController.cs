﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameController : MonoBehaviour {

	public Text questionDisplayText;
	public Text scoreDisplayText;
	public Text timeRemainingDisplayText;
	public SimpleObjectPool answerButtonObjectPool;
	public Transform answerButtonParent;
	public GameObject questionDisplay;
	public GameObject roundEndDisplay;

	private DataController dataController;
	private RoundData currentRoundData;
	private QuestionData[] questionPool;

	private bool isRoundActive;
	private float timeRemaining;
	private int questionIndex;
	private int playerScore;
	private List<GameObject> answerButtonGameObjects = new List<GameObject> ();

	// Use this for initialization
	void Start () {
		dataController = FindObjectOfType<DataController> ();
		currentRoundData = dataController.GetCurrentRoundData ();
		questionPool = currentRoundData.questions;
		timeRemaining = currentRoundData.timeLimitInSeconds;
		UpdateTimeRemainingDisplay ();

		playerScore = 0;
		questionIndex = 0;

		ShowQuestion ();
		isRoundActive = true;
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
		}
	}

	public void EndRound() {
		isRoundActive = false;

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
		if (isRoundActive) {
			timeRemaining -= Time.deltaTime;
			UpdateTimeRemainingDisplay ();

			if (timeRemaining <= 0f) {
				EndRound ();
			}
		}
	}
}
