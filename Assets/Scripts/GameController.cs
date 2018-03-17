using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    private const float FADE_STEP = 0.1f;

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
    private readonly Dictionary<string, string> questionIdToAnswerIdMap = new Dictionary<string, string>();

    private bool isTimerActive;
    private bool isEventDone;
    private bool isClarifying;
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
    private void Start()
    {
        dataController = FindObjectOfType<DataController>(); // store a ref to data controller
        currentRoundData = dataController.GetCurrentRoundData();
        detectiveAudioSource = detectiveObject.GetComponent<AudioSource>();
        bgmAudioSource = player.GetComponent<AudioSource>();

        PlayBgm(currentRoundData.bgmNormalClip);

        displayedScore = 0;
        sequenceIndex = 0;
        actualOverallScore = 0; // TODO should carry over score from previous round

        isTimerActive = false;
        isClarifying = false;

        StartCoroutine(RunSequence());
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
    private IEnumerator RunSequence()
    {
        if (sequenceIndex >= currentRoundData.sequence.Length)
        {
            EndRound();
            yield break;
        }

        currentSequence = currentRoundData.sequence[sequenceIndex];

        if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
        {
            // Debug.Log ("RunSequence: current sequence is question");
            questionPool = currentSequence.questions;
            questionIndex = 0;

            subtitleDisplay.SetActive(false);
            BeginQuestions();
        }
        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_DIALOG))
        {
            // Debug.Log ("RunSequence: current sequence is dialog");
            isEventDone = false;
            ShowAndPlayDialog(dataController.LoadAudioFile(currentSequence.filePath), currentSequence.subtitleText);

            if (!string.IsNullOrEmpty(currentSequence.bgm))
            {
                PlayBgm(dataController.LoadAudioFile(currentSequence.bgm));
            }

            if (currentSequence.readExpression)
            {
                // TODO create a pop-up that instructs player to put on an appropriate expression?

                while (detectiveAudioSource.isPlaying)
                {
                    yield return null;
                }
                
                subtitleDisplay.SetActive(false);

                dataController.StartFER();
                Debug.Log("wait 2 seconds");
                yield return new WaitForSecondsRealtime(2f);
                dataController.StopFER();

                string closestEmotion;
                SaveAndDisplayScore(CalculateSuspicionScore_EmotionOnly(currentSequence.expectedExpressions,
                    currentSequence.scoreWeight, out closestEmotion));

                isEventDone = true;
            }
            else
            {
                while (detectiveAudioSource.isPlaying)
                {
                    yield return null;
                }

                isEventDone = true;
            }
        }

        sequenceIndex++;
    }

    private float CalculateSuspicionScore_EmotionOnly(string[] expectedExpressions, float weight,
        out string closestEmotion)
    {
        var emotionDistance = dataController.ComputeEmotionDistance(expectedExpressions,
            dataController.ReadPlayerEmotion(), out closestEmotion);

        // TODO: UNCOMMENT THIS AFTER INTEGRATION WITH FER
        // dataController.DeleteFERDataFile ();

        Debug.Log("CalculateSuspicionScore_EmotionOnly: " + emotionDistance);
        return ScoreCalculator.CalculateExpressionScore(emotionDistance, weight);
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

            if (string.IsNullOrEmpty(subtitle)) return;
            subtitleDisplay.SetActive(true);
            subtitleDisplayText.text = subtitle;
        }
    }

    private void BeginQuestions()
    {
        ShowQuestion();
        Cursor.lockState = CursorLockMode.None;
    }

    private void DisplayAnswers(IList<AnswerData> answers)
    {
        for (var i = 0; i < answers.Count; i++)
        {
            var answerButtonGameObject = answerButtonObjectPool.GetObject();
            answerButtonGameObjects.Add(
                answerButtonGameObject); // add the current answer button to the list of ACTIVE answer buttons so we can keep track of it
            answerButtonGameObject.transform.SetParent(answerButtonParent);

            var answerButton = answerButtonGameObject.GetComponent<AnswerButton>();
            answerButton.Setup(answers[i]);

            if (i != 0) continue; // only make bold the first item
            selectedBoldAnswer = answerButton;
            selectedBoldAnswer.Bold(); // TODO (UI) also shade it maybe?
        }
    }

    private void ShowQuestion()
    {
        isEventDone = false;
        StartCoroutine(FadeInAndActivateDisplay(questionDisplay));
        RemoveAnswerButtons();
        currentQuestion = questionPool[questionIndex];
        questionDisplayText.text = currentQuestion.questionText;

        DisplayAnswers(currentQuestion.answers);

        // show picture if any
        if (currentQuestion.pictureFileName != null)
        {
            questionPictureDisplay.SetActive(true);
            var imageLoader = questionPictureDisplay.GetComponent<ImageLoader>();
            if (imageLoader != null) imageLoader.LoadImage(dataController.LoadImage(currentQuestion.pictureFileName));
        }

        // play the audio if any(?)
        if (!string.IsNullOrEmpty(currentQuestion.filePath))
        {
            ShowAndPlayDialog(dataController.LoadAudioFile(currentQuestion.filePath), null);
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

    private IEnumerator FadeOutAndDeactivateDisplay(GameObject display)
    {
//        var displayRenderer = display.GetComponent<CanvasRenderer>();
//
//        Debug.Log(displayRenderer);
//        if (displayRenderer != null && displayRenderer.GetAlpha() > 0)
//        {
//            Debug.Log("Fade out: for loop");
//            for (var f = 1f; f >= 0; f -= FADE_STEP)
//            {
//                displayRenderer.SetAlpha(f);
//                yield return null;
//            }
//        }
//                
        display.SetActive(false);
        yield break;
    }
    
    private IEnumerator FadeInAndActivateDisplay(GameObject display)
    {
        display.SetActive(true);

//        var displayRenderer = display.GetComponent<CanvasRenderer>();
//
//        Debug.Log(displayRenderer);
//        if (displayRenderer != null && displayRenderer.GetAlpha().Equals(0))
//        {
//            Debug.Log("Fade in: for loop");
//            for (var f = 0f; f < 1f; f += FADE_STEP)
//            {
//                displayRenderer.SetAlpha(f);
//                yield return null;
//            }
//        }
        yield break;
    }

    /**
     * Handles the event when player is inconsistent with the facts in their answer
     */
    private IEnumerator ClarifyAnswer(string answerId1, string answerId2)
    {
        // 1. Make music scarier and respond with "Your answer doesn't match with our record" or sth
        AudioClip clip;
        string subtitle;

        dataController.LoadDetectiveRespClip(out clip, out subtitle, DataController.DETECTIVE_RESPONSE_CLARIFYING);
        ShowAndPlayDialog(clip, subtitle);
        StartCoroutine(FadeOutAndDeactivateDisplay(questionDisplay));

        while (detectiveAudioSource.isPlaying)
        {
            yield return null;
        }

        subtitleDisplay.SetActive(false);
        StartCoroutine(FadeInAndActivateDisplay(questionDisplay));

        // 2. Ask again (Show the same q with only the 2 options)
        // basically like ShowQuestion() but???
        RemoveAnswerButtons();
        questionDisplayText.text = currentQuestion.questionText;

        AnswerData[] answerDatas =
        {
            currentQuestion.answers.FirstOrDefault(e => e.answerId.Equals(answerId1)),
            currentQuestion.answers.FirstOrDefault(e => e.answerId.Equals(answerId2))
        };

        DisplayAnswers(answerDatas);

        isClarifying = true;
        isTimerActive = true;
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
            consistencyScore = ScoreCalculator.CalculateConsistencyScore(consistent, currentQuestion.consistencyWeight);
            suspicionScore += consistencyScore;

            Debug.Log("suspicion score after calculating consistency: " + suspicionScore);
        }

        if (!considersEmotion) return suspicionScore;
        // Debug.Log ("considers emotion");
        var emotionDistance = dataController.ComputeEmotionDistance(expectedExpression,
            dataController.ReadPlayerEmotion(), out closestEmotion);

        // Debug.Log ("emotion distance: " + emotionDistance.ToString());
        expressionScore = ScoreCalculator.CalculateExpressionScore(emotionDistance, currentQuestion.expressionWeight);
        suspicionScore += expressionScore;

        Debug.Log("closestEmotion: " + closestEmotion + ", distance: " + emotionDistance + ", score: " +
                  expressionScore);

        // TODO: UNCOMMENT THIS AFTER INTEGRATION WITH FER
        // dataController.DeleteFERDataFile ();

        return suspicionScore;
    }

    private void SaveAndDisplayScore(float suspicionScore)
    {
        displayedScore += suspicionScore;
        actualOverallScore += suspicionScore;

        Debug.Log("suspicion score: " + suspicionScore + ", actual score: " + actualOverallScore);

        // don't let displayedScore go below 0
        if (displayedScore < 0) displayedScore = 0;

        scoreDisplayText.text = "Suspicion: " + displayedScore.ToString("F2");
        scoreDisplayerSlider.value = displayedScore / 10;
    }

    private IEnumerator HandleAnswer(AnswerButton answerButton)
    {
        if (currentQuestion.considersEmotion && !isClarifying) // no need to record again if it's just clarifying
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

        if (!answerStoredBefore) questionIdToAnswerIdMap.Add(currentQuestion.questionId, answerData.answerId);

        var considerPrevFact = currentQuestion.considersFact && answerStoredBefore;
        var consistentFact = considerPrevFact &&
                             answerData.answerId.Equals(questionIdToAnswerIdMap[currentQuestion.questionId]);

        if (isClarifying)
        {
            consistentFact = false;
        }

        var suspicionScore = CalculateSuspicionScore(
            considerPrevFact,
            currentQuestion.considersEmotion,
            answerData.expectedExpressions,
            out consistencyScore,
            out expressionScore,
            out closestEmotion,
            consistentFact);

        SaveAndDisplayScore(suspicionScore);

        if (questionPictureDisplay.activeSelf)
        {
            questionPictureDisplay.GetComponent<ImageLoader>().DestroyMaterial();
            questionPictureDisplay.SetActive(false);
        }


        if (!isClarifying)
        {
            // if consistencyScore <= 0, it means answer is consistent (consistent = true)
            // if expressionScore <= 0, it means expression is correct (correctExpression = true)
            AdaptMusicAndLighting(
                currentQuestion.considersFact,
                currentQuestion.considersEmotion,
                closestEmotion,
                consistencyScore <= 0f,
                expressionScore <= 0f
            );

            if (considerPrevFact && !consistentFact) // player answers inconsistent fact
            {
                StartCoroutine(ClarifyAnswer(answerData.answerId, questionIdToAnswerIdMap[currentQuestion.questionId]));
                yield break;
            }

            if (string.IsNullOrEmpty(answerData.detectiveResponse))
            {
                HandleEndOfAQuestion();
            }
            else
            {
                GetAndPlayDetectiveResponse(answerData.detectiveResponse);
                while (detectiveAudioSource.isPlaying)
                {
                    yield return null;
                }

                isEventDone = true;
            }
        }
        else
        {
            GetAndPlayDetectiveResponse(DataController.DETECTIVE_RESPONSE_POST_CLARIFYING);
            while (detectiveAudioSource.isPlaying)
            {
                yield return null;
            }

            isEventDone = true;
        }
    }

    private void GetAndPlayDetectiveResponse(string responseType)
    {
        // Give detective response
        AudioClip clip;
        string subtitle;

        dataController.LoadDetectiveRespClip(out clip, out subtitle, responseType);
        ShowAndPlayDialog(clip, subtitle);
        StartCoroutine(FadeOutAndDeactivateDisplay(questionDisplay));
    }

    public void AnswerButtonClicked(AnswerButton answerButton)
    {
        StartCoroutine(FadeOutAndDeactivateDisplay(questionDisplay));
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
            if (!consistent)
            {
                PlayBgm(currentRoundData.bgmNegativeClip);
                return;
            }

        if (!considerEmotion) return;

        if (!correctExpression)
        {
            PlayBgm(currentRoundData.bgmNegativeClip);
            return;
        }

        if (emotion.Equals(HAPPY_EMOTION))
        {
            bgmAudioSource.pitch = 1.3f;
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
    }

    private void EndRound()
    {
        isTimerActive = false;
        dataController.SubmitNewPlayerScore(displayedScore);
        highScoreDisplayText.text = dataController.GetHighestPlayerScore().ToString();

        StartCoroutine(FadeOutAndDeactivateDisplay(questionDisplay));
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
            timeRemainingDisplayText.text = "Time: " + Mathf.Round(timeRemaining);
            timeRemainingDisplaySlider.value = timeRemaining / currentQuestion.timeLimitInSeconds;
        }
        else
        {
            timeRemainingDisplayText.text = "Time: -";
        }
    }

    private void LightsCameraAction()
    {
        var Lightsbulb = room.transform.Find("Lightbulb").gameObject;
        var Lights = Lightsbulb.transform.Find("Lamp").gameObject;
        Lights.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;

        var SpotLight1 = room.transform.Find("Spot light 1").gameObject;
        var SpotLight2 = room.transform.Find("Spot light 2").gameObject;

        SpotLight1.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
        SpotLight2.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;
    }

    private void MotionBlur()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = motionBlurEffect;
    }

    private void Vignette()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = vignetteEffect;
    }

    private void Bloom()
    {
        playerCamera.GetComponent<PostProcessingBehaviour>().profile = bloomEffect;
    }

    private void HandleEndOfAQuestion()
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
            StartCoroutine(FadeOutAndDeactivateDisplay(questionDisplay));
            StartCoroutine(RunSequence());
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (isTimerActive && questionDisplay.activeSelf)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f) AnswerButtonClicked(selectedBoldAnswer);
        }

        UpdateTimeRemainingDisplay();

        // handle the sequence thing
        if (isEventDone)
        {
            subtitleDisplay.SetActive(false);

            if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
                HandleEndOfAQuestion();
            else
                StartCoroutine(RunSequence());
        }

        if (Input.GetKeyDown("r")) LightsCameraAction();

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