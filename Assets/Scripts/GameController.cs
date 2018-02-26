using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.PostProcessing;

public class GameController : MonoBehaviour {

	public Text questionDisplayText;
	public Text scoreDisplayText;
	public Text timeRemainingDisplayText;
	public Text highScoreDisplayText;
	public SimpleObjectPool answerButtonObjectPool;
	public Transform answerButtonParent;

	public GameObject questionDisplay;
	public GameObject roundEndDisplay;
	public GameObject questionPictureDisplay;
	public GameObject subtitleDisplay;
	public GameObject detectiveObject;

    public GameObject Room;
    public Camera PlayerCamera;
    public GameObject Player;

	private AudioSource detectiveAudioSource;

	private DataController dataController;
	private RoundData currentRoundData;
	private QuestionData[] questionPool;
	private Dictionary<string, AnswerData> playerAnswers = new Dictionary<string, AnswerData> ();

	private bool isTimerActive;
	private float timeRemaining;
	private int questionIndex;
	private int sequenceIndex;
	private float playerScore;
	private List<GameObject> answerButtonGameObjects = new List<GameObject> ();
    public AnswerButton SelectedBoldAnswer;

	private static QuestionData currentQuestion;
	private static SequenceData currentSequence;

	// Use this for initialization
	void Start () {
		dataController = FindObjectOfType<DataController> (); // store a ref to data controller
		currentRoundData = dataController.GetCurrentRoundData();
		questionPool = currentRoundData.questions;
		detectiveAudioSource = detectiveObject.GetComponent<AudioSource> ();
		timeRemaining = 10;
		UpdateTimeRemainingDisplay ();

		playerScore = 0;
		questionIndex = 0;
		sequenceIndex = 0;

		isTimerActive = false;

		RunSequence ();
	}

	/**
	 * Should only be called after the previous sequence is completed
	 **/
	private void RunSequence() {
		if (sequenceIndex >= currentRoundData.sequence.Length) {
			EndRound ();
			return;
		}

		currentSequence = currentRoundData.sequence [sequenceIndex];
			
		if (currentSequence.sequenceType.Equals ("question")) {
			BeginQuestions ();
		} else if (currentSequence.sequenceType.Equals ("dialog")) {
			// TODO play audio, show subtitle
			Debug.Log("current sequence is dialog");
			AudioClip clip = dataController.LoadAudioFile (currentSequence.filePath);

			if (detectiveAudioSource == null) {
				Debug.LogError ("audio source not found!");
			} else if (clip == null) {
				Debug.LogError ("clip is empty!");
			} else {
				detectiveAudioSource.clip = clip;
				detectiveAudioSource.Play ();
			}
		}

		sequenceIndex++;
	}

    private void BeginQuestions() {
        questionDisplay.SetActive(true);
		ShowQuestion ();
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowQuestion() {
		RemoveAnswerButtons ();
		currentQuestion = questionPool [questionIndex];
		QVariationData variation = currentQuestion.variations [0]; // TODO determine which variation
		questionDisplayText.text = variation.questionText;

		for (int i = 0; i < variation.answers.Length; i++) {
			GameObject answerButtonGameObject = answerButtonObjectPool.GetObject ();
			answerButtonGameObjects.Add (answerButtonGameObject); // add the current answer button to the list of ACTIVE answer buttonsso we can keep track of it
			answerButtonGameObject.transform.SetParent (answerButtonParent);

			AnswerButton answerButton = answerButtonGameObject.GetComponent<AnswerButton> ();
			answerButton.Setup (variation.answers [i]);
            if (i == 0)
            {
                SelectedBoldAnswer = answerButton;
                SelectedBoldAnswer.Bold();
            }
		}

		// show picture if any
		if (currentQuestion.pictureFileName != null) {
			questionPictureDisplay.SetActive (true);
			ImageLoader imageLoader = questionPictureDisplay.GetComponent<ImageLoader> ();
			if (imageLoader != null) {
				print ("ImageLoader is found");
				imageLoader.LoadImage (dataController.LoadImage (currentQuestion.pictureFileName));
			}
		}

		isTimerActive = true;
		timeRemaining = currentQuestion.timeLimitInSeconds;
		UpdateTimeRemainingDisplay ();
	}

	// remove the existing answer buttons
	private void RemoveAnswerButtons() {
		while (answerButtonGameObjects.Count > 0) {
			// return it to object pool i.e ready to be recycled and reused
			answerButtonObjectPool.ReturnObject (answerButtonGameObjects [0]); 
			answerButtonGameObjects.RemoveAt (0); // remove it from list of active answerButtongameObjects
		}
	}

	public void AnswerButtonClicked(AnswerData answerData) {
		Debug.Log ("answer button clicked");
		// TODO tell model to read expression (?)
		// TODO save the answer??

		float suspicionScore = 0;
		isTimerActive = false;

		// check consistency if question considers fact
		if (currentQuestion.considersFact) {
			if (playerAnswers.ContainsKey (currentQuestion.questionId)) { // if prior answer was stored
				if (answerData.answerId.Equals (playerAnswers [currentQuestion.questionId])) { // answer is consistent
					suspicionScore += ScoreCalculator.CalculateConsistencyScore(true, currentQuestion.consistencyWeight);
				} else { // wrong answer
					// TODO ask question again
					suspicionScore += ScoreCalculator.CalculateConsistencyScore(false, currentQuestion.consistencyWeight);
				}
			} else { // this is the first time that particular question was asked
				playerAnswers.Add(currentQuestion.questionId, answerData); // store the answer
			}
		}

		if (currentQuestion.considersEmotion) {
			Debug.Log ("considers emotion");
			float emotionDistance = dataController.ComputeEmotionDistance (answerData.expectedExpression, 
				dataController.ReadPlayerEmotion (questionIndex));

			// Debug.Log ("emotion distance: " + emotionDistance.ToString());
			suspicionScore += ScoreCalculator.CalculateExpressionScore (emotionDistance, currentQuestion.expressionWeight);
		}

		playerScore += suspicionScore;
		scoreDisplayText.text = "Suspicion: " + playerScore.ToString ("F2");

		if (questionPictureDisplay.activeSelf) {
			questionPictureDisplay.GetComponent<ImageLoader> ().DestroyMaterial ();
			questionPictureDisplay.SetActive (false);
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
		isTimerActive = false;
		dataController.SubmitNewPlayerScore (playerScore);
		highScoreDisplayText.text = dataController.GetHighestPlayerScore ().ToString ();

		questionDisplay.SetActive (false); // deactivate the question display
		roundEndDisplay.SetActive (true); // activate (show) the round end display
	}

	public void ReturnToMenu(){
		SceneManager.LoadScene ("MenuScreen");
	}

	private void UpdateTimeRemainingDisplay() {
		if (isTimerActive) {
			timeRemainingDisplayText.text = "Time: " + Mathf.Round (timeRemaining).ToString ();
		} else {
			timeRemainingDisplayText.text = "Time: -";
		}
	}

    public void LightsCameraAction()
    {
        GameObject Lightsbulb = Room.transform.Find("Lightbulb").gameObject;
        GameObject Lights = Lightsbulb.transform.Find("Lamp").gameObject;
        Lights.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;

        GameObject SpotLight1 = Room.transform.Find("Spot light 1").gameObject;
        GameObject SpotLight2 = Room.transform.Find("Spot light 2").gameObject;

        SpotLight1.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
        SpotLight2.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
    }

    public void MotionBlur()
    {
        PlayerCamera.GetComponent<PostProcessingBehaviour>().enabled = !PlayerCamera.GetComponent<PostProcessingBehaviour>().enabled;
    }
	// Update is called once per frame
	void Update () {
		if (isTimerActive && questionDisplay.activeSelf == true) {
			timeRemaining -= Time.deltaTime;
			UpdateTimeRemainingDisplay ();

			if (timeRemaining <= 0f) {
				EndRound (); // TODO what do we again here?
			}
		}

        if (Input.GetKeyDown("e")) {
            BeginQuestions();
        }

        if (Input.GetKeyDown("r")) {
            LightsCameraAction();
        }

        if (Input.GetKeyDown("t"))
        {
            MotionBlur();
        }
    }
}
