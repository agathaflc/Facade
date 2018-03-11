using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.PostProcessing;
using System.Linq;

public class GameController : MonoBehaviour
{
    private const string SEQUENCE_TYPE_DIALOG = "dialog";
    private const string SEQUENCE_TYPE_QUESTION = "question";
    private const string DEFAULT_EMOTION = "neutral";
    private const string HAPPY_EMOTION = "happy";
    private const string SAD_EMOTION = "sad";
    private const string SCARED_EMOTION = "scared";
    private const string SURPRISED_EMOTION = "surprised";
    private const string ANGRY_EMOTION = "angry";

    private const float EMOTION_DISTANCE_THRESHOLD = 2.0f;

    public Text questionDisplayText;
    public Slider scoreDisplayerSlider;
    public Text scoreDisplayText;
    public Text timeRemainingDisplayText;
    public Slider timeRemainingDisplaySlider;
    public Text highScoreDisplayText;
    public Text subtitleDisplayText;
    public SimpleObjectPool answerButtonObjectPool;
    public Transform answerButtonParent;

    public GameObject questionDisplay;
    public GameObject roundEndDisplay;
    public GameObject questionPictureDisplay;
    public GameObject subtitleDisplay;
    public GameObject detectiveObject;

    public GameObject room;
    public Camera playerCamera;
    public PostProcessingProfile motionBlurEffect;
    public PostProcessingProfile vignetteEffect;
    public PostProcessingProfile bloomEffect;
    public GameObject player;

    private AudioSource detectiveAudioSource;
    private AudioSource bgmAudioSource;

    private DataController dataController;
    private RoundData currentRoundData;
    private QuestionData[] questionPool;
    private Dictionary<string, string> questionIdToAnswerIdMap = new Dictionary<string, string>();

    private bool isTimerActive;
    private bool isDetectiveTalking;
    private float timeRemaining;
    private int questionIndex;
    private int sequenceIndex;
    private float displayedScore;
    private float actualOverallScore;
    private List<GameObject> answerButtonGameObjects = new List<GameObject>();
    public AnswerButton selectedBoldAnswer;

    private static QuestionData currentQuestion;
    private static SequenceData currentSequence;

    // Use this for initialization
    void Start()
    {
        dataController = FindObjectOfType<DataController>(); // store a ref to data controller
        currentRoundData = dataController.GetCurrentRoundData();
        questionPool = currentRoundData.questions;
        detectiveAudioSource = detectiveObject.GetComponent<AudioSource>();
        bgmAudioSource = player.GetComponent<AudioSource>();

        PlayBgm(currentRoundData.bgmNormalClip);

        displayedScore = 0;
        questionIndex = 0;
        sequenceIndex = 0;
        actualOverallScore = 0; // TODO should carry over score from previous round

        isTimerActive = false;

        RunSequence();
    }

    private void PlayBgm(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogError("Clip is empty!");
            return;
        }

        bgmAudioSource.clip = clip;
        bgmAudioSource.Play();
    }

    /**
     * Should only be called after the previous sequence is completed
     **/
    private void RunSequence()
    {
        if (sequenceIndex >= currentRoundData.sequence.Length)
        {
            EndRound();
            return;
        }

        currentSequence = currentRoundData.sequence[sequenceIndex];

        if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
        {
            // Debug.Log ("RunSequence: current sequence is question");
            subtitleDisplay.SetActive(false);
            BeginQuestions();
        }
        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_DIALOG))
        {
            // Debug.Log ("RunSequence: current sequence is dialog");
            isDetectiveTalking = true;
            ShowAndPlayDialog(dataController.LoadAudioFile(currentSequence.filePath), currentSequence.subtitleText);
        }

        sequenceIndex++;
    }

    private void ShowAndPlayDialog(AudioClip audioClip, string subtitle)
    {
        if (detectiveAudioSource == null)
        {
            Debug.LogError("audio source not found!");
        }
        else if (audioClip == null)
        {
            Debug.LogError("clip is empty for " + subtitle);
        }
        else
        {
            detectiveAudioSource.clip = audioClip;
            detectiveAudioSource.Play();

            subtitleDisplay.SetActive(true);
            subtitleDisplayText.text = subtitle;
        }
    }

    private void BeginQuestions()
    {
        //questionDisplay.SetActive(true);
        ShowQuestion();
        Cursor.lockState = CursorLockMode.None;
    }

    private void ShowQuestion()
    {
        questionDisplay.SetActive(true);
        RemoveAnswerButtons();
        currentQuestion = questionPool[questionIndex];
        questionDisplayText.text = currentQuestion.questionText;

        for (int i = 0; i < currentQuestion.answers.Length; i++)
        {
            GameObject answerButtonGameObject = answerButtonObjectPool.GetObject();
            answerButtonGameObjects.Add(
                answerButtonGameObject); // add the current answer button to the list of ACTIVE answer buttonsso we can keep track of it
            answerButtonGameObject.transform.SetParent(answerButtonParent);

            AnswerButton answerButton = answerButtonGameObject.GetComponent<AnswerButton>();
            answerButton.Setup(currentQuestion.answers[i]);
            if (i == 0)
            {
                selectedBoldAnswer = answerButton;
                selectedBoldAnswer.Bold(); // TODO (UI) also shade it maybe?
            }
        }

        // show picture if any
        if (currentQuestion.pictureFileName != null)
        {
            questionPictureDisplay.SetActive(true);
            ImageLoader imageLoader = questionPictureDisplay.GetComponent<ImageLoader>();
            if (imageLoader != null)
            {
                // Debug.Log ("ImageLoader is found");
                imageLoader.LoadImage(dataController.LoadImage(currentQuestion.pictureFileName));
            }
        }

        isTimerActive = true;
        timeRemaining = currentQuestion.timeLimitInSeconds;
        UpdateTimeRemainingDisplay();
    }

    // remove the existing answer buttons
    private void RemoveAnswerButtons()
    {
        while (answerButtonGameObjects.Count > 0)
        {
            // return it to object pool i.e ready to be recycled and reused
            answerButtonObjectPool.ReturnObject(answerButtonGameObjects[0]);
            answerButtonGameObjects.RemoveAt(0); // remove it from list of active answerButtongameObjects
        }
    }

    private void ClarifyAnswer(string questionId, string answerId1, string answerId2)
    {
        // Give the 
        QuestionData q = dataController.GetQuestionDataById(questionId);
        AnswerData a1 = q.answers.FirstOrDefault(e => e.answerId.Equals(answerId1));
        AnswerData a2 = q.answers.FirstOrDefault(e => e.answerId.Equals(answerId2));

        // TODO IDK CRYYYYY
        // basically like ShowQuestion() but???
        RemoveAnswerButtons();
        questionDisplayText.text = currentQuestion.questionText;
    }

    private float CalculateSuspicionScore(
        bool considersFact,
        bool considersEmotion,
        string[] expectedExpression,
        out float consistencyScore,
        out float expressionScore,
        out string closestEmotion,
        bool consistent = true)
    {
        float suspicionScore = 0;
        consistencyScore = 0;
        expressionScore = 0;
        closestEmotion = DEFAULT_EMOTION;

        // check consistency if question considers fact, check for prev answer and so on
        if (considersFact)
        {
            if (consistent)
            {
                // answer is consistent
                consistencyScore = ScoreCalculator.CalculateConsistencyScore(true, currentQuestion.consistencyWeight);
                suspicionScore += consistencyScore;
            }
            else
            {
                // wrong answer
                consistencyScore = ScoreCalculator.CalculateConsistencyScore(false, currentQuestion.consistencyWeight);
                suspicionScore += consistencyScore;
            }
        }

        if (!considersEmotion) return suspicionScore;
        // Debug.Log ("considers emotion");
        var emotionDistance = dataController.ComputeEmotionDistance(expectedExpression,
            dataController.ReadPlayerEmotion(), out closestEmotion);

        // Debug.Log ("emotion distance: " + emotionDistance.ToString());
        expressionScore = ScoreCalculator.CalculateExpressionScore(emotionDistance, currentQuestion.expressionWeight);
        suspicionScore += expressionScore;

        Debug.Log("closestEmotion: " + closestEmotion + ", distance: " + emotionDistance.ToString() + ", score: " +
                  expressionScore);

        // TODO: UNCOMMENT THIS AFTER INTEGRATION WITH FER
        // dataController.DeleteFERDataFile ();

        return suspicionScore;
    }

    private IEnumerator HandleAnswer(AnswerButton answerButton)
    {
        if (currentQuestion.considersEmotion)
        {
            dataController.StartFER();
            // TODO (maybe) detective writes some notes animation 
            yield return new WaitForSecondsRealtime(2f);
            dataController.StopFER();
        }

        float consistencyScore;
        float expressionScore;
        string closestEmotion;
        isTimerActive = false;

        var answerData = answerButton.GetAnswerData();

        var answerStoredBefore = questionIdToAnswerIdMap.ContainsKey(currentQuestion.questionId);

        if (!answerStoredBefore)
        {
            questionIdToAnswerIdMap.Add(currentQuestion.questionId, answerData.answerId);
        }

        var reallyConsidersFact = currentQuestion.considersFact && answerStoredBefore;
        var consistentFact = reallyConsidersFact &&
                             answerData.answerId.Equals(questionIdToAnswerIdMap[currentQuestion.questionId]);

        var suspicionScore = CalculateSuspicionScore(
            reallyConsidersFact,
            currentQuestion.considersEmotion,
            answerData.expectedExpression,
            out consistencyScore,
            out expressionScore,
            out closestEmotion,
            consistentFact);

        displayedScore += suspicionScore;
        actualOverallScore += suspicionScore;

        Debug.Log("actual score: " + actualOverallScore);

        // don't let displayedScore go below 0
        if (displayedScore < 0)
        {
            displayedScore = 0;
        }

        scoreDisplayText.text = "Suspicion: " + displayedScore.ToString("F2");
        scoreDisplayerSlider.value = displayedScore / 10; //TODO suspicion scoring system

        if (questionPictureDisplay.activeSelf)
        {
            questionPictureDisplay.GetComponent<ImageLoader>().DestroyMaterial();
            questionPictureDisplay.SetActive(false);
        }

        //Debug.Log ("consistency score: " + consistencyScore);

        // if consistencyScore <= 0, it means answer is consistent (consistent = true)
        // if expressionScore <= 0, it means expression is correct (correctExpression = true)
        AdaptMusicAndLighting(
            currentQuestion.considersFact,
            currentQuestion.considersEmotion,
            closestEmotion,
            consistencyScore <= 0f,
            expressionScore <= 0f
        );

        if (string.IsNullOrEmpty(answerData.detectiveResponse))
        {
            HandleEndOfAQuestion();
        }
        else
        {
            // Give detective response
            AudioClip clip;
            string subtitle;

            dataController.LoadDetectiveRespClip((suspicionScore > 0), out clip, out subtitle,
                answerData.detectiveResponse);
            // Debug.Log ("response subtitle: " + subtitle);
            isDetectiveTalking = true;
            ShowAndPlayDialog(clip, subtitle);
            questionDisplay.SetActive(false);
        }
    }

    public void AnswerButtonClicked(AnswerButton answerButton)
    {
        questionDisplay.SetActive(false);
        StartCoroutine(HandleAnswer(answerButton));
    }

    /**
     * follows the algorithm here: https://trello.com/c/TDz6Ixgb/31-dream-building-algorithm
     * */
    private void AdaptMusicAndLighting(
        bool considerConsistency,
        bool considerEmotion,
        string emotion = DEFAULT_EMOTION,
        bool consistent = true,
        bool correctExpression = true
    )
    {
        if (considerConsistency)
        {
            if (!consistent)
            {
                PlayBgm(currentRoundData.bgmNegativeClip);
                return;
            }
        }

        if (considerEmotion)
        {
            if (!correctExpression)
            {
                PlayBgm(currentRoundData.bgmNegativeClip);
                return;
            }

            if (emotion.Equals(HAPPY_EMOTION))
            {
                Debug.Log("happy emotion");
                PlayBgm(currentRoundData.bgmPositiveClip);
            }
            else if (emotion.Equals(DEFAULT_EMOTION))
            {
                PlayBgm(currentRoundData.bgmNormalClip);
            }
            else
            {
                PlayBgm(currentRoundData.bgmNegativeClip);
            }

            return;
        }

        // don't change anything if it gets to this point i.e doesn't consider emotion
        return;
    }

    public void EndRound()
    {
        isTimerActive = false;
        dataController.SubmitNewPlayerScore(displayedScore);
        highScoreDisplayText.text = dataController.GetHighestPlayerScore().ToString();

        questionDisplay.SetActive(false); // deactivate the question display
        roundEndDisplay.SetActive(true); // activate (show) the round end display

        questionPictureDisplay.GetComponent<ImageLoader>().DestroyMaterial();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MenuScreen");
    }

    private void UpdateTimeRemainingDisplay()
    {
        if (isTimerActive)
        {
            timeRemainingDisplayText.text = "Time: " + Mathf.Round(timeRemaining).ToString();
            timeRemainingDisplaySlider.value = (timeRemaining / currentQuestion.timeLimitInSeconds);
        }
        else
        {
            timeRemainingDisplayText.text = "Time: -";
        }
    }

    public void LightsCameraAction()
    {
        GameObject Lightsbulb = room.transform.Find("Lightbulb").gameObject;
        GameObject Lights = Lightsbulb.transform.Find("Lamp").gameObject;
        Lights.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;

        GameObject SpotLight1 = room.transform.Find("Spot light 1").gameObject;
        GameObject SpotLight2 = room.transform.Find("Spot light 2").gameObject;

        SpotLight1.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
        SpotLight2.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
    }

    public void MotionBlur()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = motionBlurEffect;
    }

    public void Vignette()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = vignetteEffect;
    }

    public void Bloom()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = bloomEffect;
    }

    void HandleEndOfAQuestion()
    {
        // show another question if there are still questions to ask
        if (questionPool.Length > questionIndex + 1)
        {
            // Debug.Log ("show another question");
            questionIndex++;
            ShowQuestion();
        }
        else
        {
            // Debug.Log ("end of questions");
            questionDisplay.SetActive(false);
            RunSequence();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isTimerActive && questionDisplay.activeSelf == true)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                // pick whichever answer is selected right now
                AnswerButtonClicked(selectedBoldAnswer);
            }
        }

        UpdateTimeRemainingDisplay();

        // handle the sequence thing
        if (isDetectiveTalking && !detectiveAudioSource.isPlaying)
        {
            isDetectiveTalking = false;
            subtitleDisplay.SetActive(false);


            if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
            {
                // Debug.Log ("Update: current sequence is question");
                HandleEndOfAQuestion();
            }
            else
            {
                RunSequence();
            }
        }

        if (Input.GetKeyDown("r"))
        {
            LightsCameraAction();
        }

        if (Input.GetKeyDown("t"))
        {
            MotionBlur();
            playerCamera.GetComponent<PostProcessingBehaviour>().enabled =
                !playerCamera.GetComponent<PostProcessingBehaviour>().enabled;
        }

        if (Input.GetKeyDown("y"))
        {
            Vignette();
            playerCamera.GetComponent<PostProcessingBehaviour>().enabled =
                !playerCamera.GetComponent<PostProcessingBehaviour>().enabled;
        }

        if (Input.GetKeyDown("u"))
        {
            Bloom();
            playerCamera.GetComponent<PostProcessingBehaviour>().enabled =
                !playerCamera.GetComponent<PostProcessingBehaviour>().enabled;
        }
    }
}