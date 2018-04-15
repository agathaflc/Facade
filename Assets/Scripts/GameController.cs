using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class GameController : MonoBehaviour
{
    private const string SEQUENCE_TYPE_DIALOG = "dialog";
    private const string SEQUENCE_TYPE_QUESTION = "question";
    private const string SEQUENCE_TYPE_TIMELINE = "timeline";
    private const string SEQUENCE_TYPE_ENDING = "endingDecision";
    
    private const string DEFAULT_EMOTION = "neutral";
    private const string HAPPY_EMOTION = "happy";
    private const string SAD_EMOTION = "sad";
    private const string SCARED_EMOTION = "scared";
    private const string SURPRISED_EMOTION = "surprised";
    private const string ANGRY_EMOTION = "angry";

    private const string MUSIC_HAPPY = "music_happy";
    private const string MUSIC_NEUTRAL = "music_neutral";
    private const string MUSIC_SAD_SCARED = "music_sad_scared";
    private const string MUSIC_SURPRISED_ANGRY = "music_surprised_angry";

    private const string EFFECT_BLOOM = "bloom";
    private const string EFFECT_MOTION_BLUR = "motionBlur";
    private const string EFFECT_VIGNETTE = "vignette";

    private const float EMOTION_DISTANCE_THRESHOLD = 2.0f;
    private const float FADE_STEP = 0.1f;
    private const float FER_RECORDING_TIME = 4f;
    private const float MAX_SUSPICION_SCORE = 45f;
    private const float ENDING_DECISION_TIME_LIMIT = 25f;

    public float OVERALL_SCORE_THRESHOLD = 25f; // TODO: FINETUNE THIS

    // testing variables
    public bool FER_is_Off;
    public bool gameSceneOnly;
    public bool runTimeline;
    public bool skipAct;
    public bool skipPostActReport;

    public int skipToAct;

    public Text questionDisplayText;
    public Slider scoreDisplayerSlider;
    public Text scoreDisplayText;
    //public Text timeRemainingDisplayText;
    public Slider timeRemainingDisplaySlider;
    public Slider endingTimeRemainingSlider;
    public Text highScoreDisplayText;
    public Text subtitleDisplayText;
    public SimpleObjectPool answerButtonObjectPool;
    public Transform answerButtonParent;

    public GameObject postReport;
    public GameObject questionDisplay;
    public GameObject roundEndDisplay;
    public GameObject questionPictureDisplay;
    public GameObject subtitleDisplay;
    public GameObject endingDecisionDisplay;
    public GameObject detectiveObject;
    public GameObject hans;
    public GameObject kira;
    public GameObject tableGun;
    
    
    public Camera playerCamera;
    public Camera finalCamera;
    private Camera activeCamera;

    public GameObject room;
    public PostProcessingProfile motionBlurEffect;
    public PostProcessingProfile vignetteEffect;
    public PostProcessingProfile bloomEffect;
    private PostProcessingBehaviour postProcessingBehaviour;
    public GameObject player;

    // animation stuff
    private readonly List<List<TimelineAsset>> allTimelines = new List<List<TimelineAsset>>();
    public List<TimelineAsset> act1Timelines;
    public List<TimelineAsset> act2Timelines;
    public List<TimelineAsset> act3Timelines;
    public PlayableDirector playableDirector;
    private int currentTimelineNo;
    public RuntimeAnimatorController hansController;
    public RuntimeAnimatorController kiraController;
    public RuntimeAnimatorController kiraStandUpController;
    private Animator currentDetectiveAnimator;

    private int currentActNo;

    private int animationNoHash = Animator.StringToHash("animationNo");

    private AudioSource detectiveVoice;
    private AudioSource detectiveSoundEffect;
    private AudioSource bgmAudioSource1;
    private AudioSource bgmAudioSource2;

    private AudioSource currentBgmAudioSource;

    private DataController dataController;
    private ActData currentActData;
    private QuestionData[] questionPool;
    private List<QuestionData> allQuestions = new List<QuestionData>();

    private bool isTimerActive;
    private bool isEndingTimerActive;
    private bool isEventDone;
    private bool isClarifying;
    private float timeRemaining;
    private float decisionTimeRemaining;
    private int questionIndex;
    private int sequenceIndex;
    private float displayedScore;
    private readonly List<GameObject> answerButtonGameObjects = new List<GameObject>();
    public AnswerButton selectedBoldAnswer;

    private static QuestionData currentQuestion;
    private static SequenceData currentSequence;
    private static string currentBgm;


    // Use this for initialization
    private void Start()
    {
        allTimelines.Add(act1Timelines);
        allTimelines.Add(act2Timelines);
        allTimelines.Add(act3Timelines);
        
        if (runTimeline)
        {
            StartCoroutine(PlayIntro());
        }

        if (gameSceneOnly) return;

        dataController = FindObjectOfType<DataController>(); // store a ref to data controller

        if (skipAct)
        {
            currentActNo = skipToAct;
            dataController.SetCurrentActNo(currentActNo);
        }
        else
        {
            currentActNo = dataController.GetCurrentActNo();
        }
        
        if (currentActNo != 1)
        {
            detectiveObject = kira;
            detectiveObject.SetActive(true);
            hans.SetActive(false);
        }
        else
        {
            detectiveObject = hans;
            detectiveObject.SetActive(true);
            kira.SetActive(false);
        }

        tableGun.SetActive(currentActNo == 2);

        currentActData = dataController.GetCurrentRoundData();
        
        var detectiveAudioSources = detectiveObject.GetComponents<AudioSource>();
        detectiveVoice = detectiveAudioSources[0];
        detectiveSoundEffect = detectiveAudioSources[1];
//        detectiveVoice = detectiveObject.GetComponent<AudioSource>();

        var audioSource = player.GetComponents<AudioSource>();
        bgmAudioSource1 = audioSource[0];
        bgmAudioSource2 = audioSource[1];
        currentBgmAudioSource = bgmAudioSource1;
        
        postProcessingBehaviour = playerCamera.GetComponent<PostProcessingBehaviour>();

        PlayBgm(currentActData.bgmNeutralClip, MUSIC_NEUTRAL, currentActData.bgmNeutralFile.seek); // always start off with the base clip 
        currentBgm = MUSIC_NEUTRAL;

        displayedScore = 0;
        sequenceIndex = 0;
        currentTimelineNo = 0;

        isTimerActive = false;
        isEndingTimerActive = false;
        isClarifying = false;

        if (!runTimeline)
        {
            StartCoroutine(RunSequence());
        }
    }

    private void SetDetectiveAnimator()
    {
        detectiveObject.GetComponent<Animator>().runtimeAnimatorController =
            detectiveObject == hans ? hansController : kiraController;

        currentDetectiveAnimator = detectiveObject.GetComponent<Animator>();
    }
    
    private void UnsetDetectiveAnimator()
    {
        detectiveObject.GetComponent<Animator>().runtimeAnimatorController = null;
        currentDetectiveAnimator = null;
    }

    private IEnumerator PlayIntro()
    {
        subtitleDisplay.SetActive(true);
        subtitleDisplayText.text = "";

        playableDirector.playableAsset = allTimelines[currentActNo][0];
        UnsetDetectiveAnimator();
        PlayTimeline(playableDirector);

        while (playableDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        subtitleDisplay.SetActive(false);

        SetDetectiveAnimator();
        StartCoroutine(RunSequence());
        
    }
    
    private IEnumerator RunTimeline()
    {
        Debug.Log("run timeline, no: " + currentTimelineNo);
        subtitleDisplay.SetActive(true);
        subtitleDisplayText.text = "";

        playableDirector.playableAsset = allTimelines[currentActNo][currentTimelineNo];
        UnsetDetectiveAnimator();
        PlayTimeline(playableDirector);

        while (playableDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        subtitleDisplay.SetActive(false);

        if (currentTimelineNo == 0) SetDetectiveAnimator();
        currentTimelineNo++;
        StartCoroutine(RunSequence());
    }

    /**
     * Should only be called after the previous sequence is completed
     **/
    private IEnumerator RunSequence()
    {
        if (sequenceIndex >= currentActData.sequence.Length)
        {
            EndRound();
            yield break;
        }

        currentSequence = currentActData.sequence[sequenceIndex];

        if (currentSequence.ending)
        {
            detectiveObject.GetComponent<Animator>().runtimeAnimatorController = kiraStandUpController;
            currentDetectiveAnimator = detectiveObject.GetComponent<Animator>();
        }

        if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_ENDING))
        {
            UnlockCursor();
            detectiveObject.SetActive(false);
            ApplyEndingCamera();
            endingDecisionDisplay.SetActive(true);
//            LightsCameraAction(2);
            ShowSpecialEffect(EFFECT_VIGNETTE);

            decisionTimeRemaining = ENDING_DECISION_TIME_LIMIT;
            isEndingTimerActive = true;

            while (isEndingTimerActive)
            {
                yield return null;
            }
        }
        
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
            LockCursor();
            isEventDone = false;

            if (!string.IsNullOrEmpty(currentSequence.effect)) // show special effects if any
            {
                ShowSpecialEffect(currentSequence.effect);
            }

            ShowAndPlayDialog(DataController.LoadAudioFile(currentSequence.filePath), currentSequence.subtitleText);

            if (string.IsNullOrEmpty(currentSequence.bgm.fileName))
            {
//                Debug.Log("special bgm is not null: " + currentSequence.bgm.fileName);
                PlayBgm(DataController.LoadAudioFile(currentSequence.bgm.fileName), "special_bgm",
                    currentSequence.bgm.seek);
            }
            
            currentDetectiveAnimator.SetInteger(animationNoHash, currentSequence.animationNo);

//            Debug.Log("animation no:" + currentSequence.animationNo);
            var exited = currentSequence.animationNo == 0;
            var exitTime = currentDetectiveAnimator.GetAnimatorTransitionInfo(currentSequence.animatorLayer).duration;

            while (detectiveVoice.isPlaying)
            {
                if (!exited)
                {
                    if (currentDetectiveAnimator.GetAnimatorTransitionInfo(currentSequence.animatorLayer).normalizedTime > exitTime)
                    {
                        currentDetectiveAnimator.SetInteger(animationNoHash, 0);
                        exited = true;
                    }
                }

                yield return null;
            }

            if (currentSequence.readExpression)
            {
                // TODO create a pop-up that instructs player to put on an appropriate expression?
                subtitleDisplay.SetActive(false);

                DataController.StartFER();
                Debug.Log("wait 3 seconds");
                yield return new WaitForSecondsRealtime(FER_RECORDING_TIME);
                DataController.StopFER();

                string closestEmotion;
                SaveAndDisplayScore(CalculateSuspicionScore_EmotionOnly(currentSequence.expectedExpressions,
                    currentSequence.scoreWeight, out closestEmotion));
            }
            
            ConcludeEvent();
        }
        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_TIMELINE))
        {
            StartCoroutine(RunTimeline());
        }

        sequenceIndex++;
    }

    public AudioSource GetDetectiveSoundEffectAudioSource()
    {
        return detectiveSoundEffect;
    }

    public Animator GetCurrentDetectiveAnimator()
    {
        return currentDetectiveAnimator;
    }

    private void PlayBgm(AudioClip clip, string musicType, float seek, bool fadeIn = true)
    {
        if (clip == null)
        {
//            Debug.LogError("Clip is empty!");
            return;
        }

        currentBgm = musicType;

        AudioSource toBeFadedOut;
        AudioSource toBeFadedIn;
        if (currentBgmAudioSource == bgmAudioSource1)
        {
            toBeFadedIn = bgmAudioSource2;
            toBeFadedOut = bgmAudioSource1;
            currentBgmAudioSource = bgmAudioSource2;
        }
        else
        {
            toBeFadedIn = bgmAudioSource1;
            toBeFadedOut = bgmAudioSource2;
            currentBgmAudioSource = bgmAudioSource1;
        }

        toBeFadedIn.clip = clip;
        toBeFadedIn.loop = true;
        toBeFadedIn.time = seek;

        if (fadeIn)
        {
            StartCoroutine(FadeInAudio(toBeFadedIn));
            StartCoroutine(FadeOutAudio(toBeFadedOut));
        }
        else
        {
            toBeFadedIn.Play();
            toBeFadedOut.Stop();
        }
    }

    private IEnumerator FadeInAudio(AudioSource audioSource)
    {
        audioSource.volume = 0f;
        audioSource.Play();

        while (audioSource.volume < 1f)
        {
            audioSource.volume += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator FadeOutAudio(AudioSource audioSource)
    {
        while (audioSource.volume > 0f)
        {
            audioSource.volume -= Time.deltaTime;
            yield return null;
        }

        audioSource.Stop();
    }

    private void ConcludeEvent()
    {
        isEventDone = true;
        if (postProcessingBehaviour != null) postProcessingBehaviour.enabled = false;
    }

    private void SaveQuestion(QuestionData question)
    {
        Debug.Log("save q: " + question.questionDesc);
        allQuestions.Add(question);
    }

    private void ShowSpecialEffect(string currentSequenceEffect)
    {
        if (currentSequenceEffect.Equals(EFFECT_BLOOM))
        {
            Bloom();
            postProcessingBehaviour.enabled = true;
        }
        else if (currentSequenceEffect.Equals(EFFECT_MOTION_BLUR))
        {
            MotionBlur();
            postProcessingBehaviour.enabled = true;
        }
        else if (currentSequenceEffect.Equals(EFFECT_VIGNETTE))
        {
            Vignette();
            postProcessingBehaviour.enabled = true;
        }
    }

    private float CalculateSuspicionScore_EmotionOnly(string[] expectedExpressions, float weight,
        out string closestEmotion)
    {
        var emotionDistance = dataController.ComputeEmotionDistance(expectedExpressions,
            DataController.ReadPlayerEmotion(), out closestEmotion);

        if (!FER_is_Off)
        {
            dataController.DeleteFERDataFile();
        }

        Debug.Log("CalculateSuspicionScore_EmotionOnly: " + emotionDistance);
        return ScoreCalculator.CalculateExpressionScore(emotionDistance, weight, closestEmotion);
    }

    private void ShowAndPlayDialog(AudioClip audioClip, string subtitle)
    {
        if (detectiveVoice == null)
        {
            Debug.LogError("audio source not found!");
        }
        else if (audioClip == null)
        {
            Debug.LogError("clip is empty for " + subtitle);
        }
        else
        {
            detectiveVoice.clip = audioClip;
            detectiveVoice.Play();

            if (string.IsNullOrEmpty(subtitle)) return;
            subtitleDisplay.SetActive(true);
            subtitleDisplayText.text = subtitle;
        }
    }

    private void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private static void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
    }

    private void BeginQuestions()
    {
        UnlockCursor();
        ShowQuestion();
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

        questionDisplay.SetActive(true);
        RemoveAnswerButtons();

        currentQuestion = questionPool[questionIndex];
        questionDisplayText.text = currentQuestion.questionText;

        if (!string.IsNullOrEmpty(currentQuestion.effect))
        {
            ShowSpecialEffect(currentQuestion.effect);
        }

        DisplayAnswers(currentQuestion.answers);

        // show picture if any
        if (currentQuestion.pictureFileName != null)
        {
            questionPictureDisplay.SetActive(true);
            var imageLoader = questionPictureDisplay.GetComponent<ImageLoader>();
            if (imageLoader != null) imageLoader.LoadImage(DataController.LoadImage(currentQuestion.pictureFileName));
        }

        // play the audio if any(?)
        if (!string.IsNullOrEmpty(currentQuestion.filePath))
        {
            ShowAndPlayDialog(DataController.LoadAudioFile(currentQuestion.filePath), null);
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
        questionDisplay.SetActive(false);

        while (detectiveVoice.isPlaying)
        {
            yield return null;
        }

        subtitleDisplay.SetActive(false);
        questionDisplay.SetActive(true);

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
            DataController.ReadPlayerEmotion(), out closestEmotion);

        // Debug.Log ("emotion distance: " + emotionDistance.ToString());
        expressionScore =
            ScoreCalculator.CalculateExpressionScore(emotionDistance, currentQuestion.expressionWeight, closestEmotion);
        suspicionScore += expressionScore;

        Debug.Log("closestEmotion: " + closestEmotion + ", distance: " + emotionDistance + ", score: " +
                  expressionScore);

        if (!FER_is_Off)
        {
            dataController.DeleteFERDataFile();
        }

        return suspicionScore;
    }

    private void SaveAndDisplayScore(float suspicionScore)
    {
        displayedScore += suspicionScore;
        dataController.AddOverallScore(suspicionScore);

        if (displayedScore > MAX_SUSPICION_SCORE)
        {
            // TODO does it just end? does something else happen? does detective say anything?
            EndRound();
        }

        Debug.Log("suspicion score: " + suspicionScore + ", actual score: " + dataController.GetOverallScore());

        // don't let displayedScore go below 0
        if (displayedScore < 0) displayedScore = 0;
        Debug.Log("displayed score: " + displayedScore.ToString("F1"));

        scoreDisplayText.text = "Suspicion: " + displayedScore.ToString("F2");
        scoreDisplayerSlider.value = displayedScore / 60 ;

		if (scoreDisplayerSlider.value >= 0.8) {
			
			scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = new Color32(200,0,0,255);

		} else if (scoreDisplayerSlider.value >= 0.5 && scoreDisplayerSlider.value < 0.8) {
			
			scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = new Color32(250,250,0,255);

		} else {
			
			scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = new Color32(0,200,0,255);

		}
    }

    private IEnumerator HandleAnswer(AnswerButton answerButton)
    {
        if (currentQuestion.considersEmotion && !isClarifying) // no need to record again if it's just clarifying
        {
            DataController.StartFER();
            // TODO (maybe) detective writes some notes animation 
            yield return new WaitForSecondsRealtime(FER_RECORDING_TIME);
            DataController.StopFER();
        }

        float consistencyScore;
        float expressionScore;
        string closestEmotion;
        isTimerActive = false;

        var answerData = answerButton.GetAnswerData();

        var answerStoredBefore = dataController.CheckIfAnswerIsStored(currentQuestion.questionId);

        if (!answerStoredBefore) dataController.StoreNewAnswer(currentQuestion.questionId, answerData.answerId);

        var considerPrevFact = currentQuestion.considersFact && answerStoredBefore;
        var consistentFact = considerPrevFact &&
                             answerData.answerId.Equals(
                                 dataController.GetAnswerIdByQuestionId(currentQuestion.questionId));

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
                StartCoroutine(ClarifyAnswer(answerData.answerId,
                    dataController.GetAnswerIdByQuestionId(currentQuestion.questionId)));
                yield break;
            }
            
            SaveQuestion(currentQuestion);

            if (string.IsNullOrEmpty(answerData.detectiveResponse)) // no specified detective response
            {
                HandleEndOfAQuestion();
            }
            else
            {
                if (currentQuestion.considersEmotion) // if it considers emotion, also pass the expression score
                {
                    GetAndPlayDetectiveResponse(answerData.detectiveResponse, !(expressionScore <= 0f));
                }
                else
                {
                    GetAndPlayDetectiveResponse(answerData.detectiveResponse);
                }

                while (detectiveVoice.isPlaying)
                {
                    yield return null;
                }

                ConcludeEvent();
            }
        }
        else // was clarifying
        {
            GetAndPlayDetectiveResponse(DataController.DETECTIVE_RESPONSE_POST_CLARIFYING);
            while (detectiveVoice.isPlaying)
            {
                yield return null;
            }

            isClarifying = false;
            ConcludeEvent();
        }
    }

    private void GetAndPlayDetectiveResponse(string responseType, bool suspicious = false)
    {
        // Give detective response
        AudioClip clip;
        string subtitle;

        dataController.LoadDetectiveRespClip(out clip, out subtitle, responseType, suspicious);
        ShowAndPlayDialog(clip, subtitle);
        questionDisplay.SetActive(false);
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
            if (!consistent)
            {
                // todo play the foghorn thing??
                if (!currentBgm.Contains(SCARED_EMOTION))
                {
                    PlayBgm(currentActData.bgmSadScaredClip, MUSIC_SAD_SCARED,
                        currentActData.bgmSadScaredFile.seek);
                }

                return;
            }

        if (!considerEmotion) return;

        if (!correctExpression)
        {
            // todo play the foghorn thing??
            if (!currentBgm.Contains(SCARED_EMOTION))
                PlayBgm(currentActData.bgmSadScaredClip, MUSIC_SAD_SCARED, currentActData.bgmSadScaredFile.seek);
            return;
        }

        Debug.Log("AdaptMusicAndLighting: correct expression.");

        // if the current music is the same emotion, don't change
        if (currentBgm.Contains(emotion)) return;

        if (MUSIC_HAPPY.Contains(emotion))
        {
            PlayBgm(currentActData.bgmHappyClip, MUSIC_HAPPY, currentActData.bgmHappyFile.seek);
        }
        else if (MUSIC_NEUTRAL.Contains(emotion))
        {
            PlayBgm(currentActData.bgmNeutralClip, MUSIC_NEUTRAL, currentActData.bgmNeutralFile.seek);
        }
        else if (MUSIC_SAD_SCARED.Contains(emotion))
        {
            PlayBgm(currentActData.bgmSadScaredClip, MUSIC_SAD_SCARED, currentActData.bgmSadScaredFile.seek);
        }
        else if (MUSIC_SURPRISED_ANGRY.Contains(emotion))
        {
            PlayBgm(currentActData.bgmAngrySurprisedClip, MUSIC_SURPRISED_ANGRY,
                currentActData.bgmAngrySurprisedFile.seek);
        }
    }

    private void EndRound()
    {
        UnlockCursor();
        isTimerActive = false;
        dataController.SubmitNewPlayerScore(displayedScore);
        highScoreDisplayText.text = dataController.GetHighestPlayerScore().ToString();

        questionDisplay.SetActive(false);
//        postReport.SetActive(true); // activate (show) the round end display

		if (currentActNo == 0 && !skipPostActReport) {GeneratePostReport();}

        else
        {
            ContinueToNextAct();
        }

        questionPictureDisplay.GetComponent<ImageLoader>().DestroyMaterial();
    }

    public void ReturnToMenu()
    {
        SceneManager.LoadScene("MenuScreen");
    }

    public void ContinueToNextAct()
    {
        dataController.StartNextAct();
    }

    private void UpdateTimeRemainingDisplay()
    {
        if (isTimerActive)
        {
            timeRemainingDisplaySlider.value = timeRemaining / currentQuestion.timeLimitInSeconds;
        }
        
    }

	private void LightsCameraAction(int Num)
    {
        var Lightbulb = room.transform.Find("Lightbulb").gameObject;
        var Lights = Lightbulb.transform.Find("Lamp").gameObject;
        Lights.GetComponent<Light>().enabled = !Lights.GetComponent<Light>().enabled;

        var SpotLight1 = room.transform.Find("Spot light 1").gameObject;
        var SpotLight2 = room.transform.Find("Spot light 2").gameObject;
		if (Num == 1) {
			SpotLight1.GetComponent<Light> ().enabled = !Lights.GetComponent<Light> ().enabled;
		}if (Num == 2) {
			SpotLight2.GetComponent<Light> ().enabled = !Lights.GetComponent<Light> ().enabled;
		}
    }

	private void LightingChanges(int newIntensity,Color32 newColor){
		var Lightbulb = room.transform.Find("Lightbulb").gameObject;
		var Lights = Lightbulb.transform.Find("Lamp").gameObject;

		Lights.GetComponent<Light> ().intensity = newIntensity;
		Lights.GetComponent<Light> ().color = newColor;
		// TODO consider smoothing the changes in update() using e.g. docs.unity3d.com/ScriptReference/Light-color.html
	}

    private void MotionBlur()
    {
        activeCamera.GetComponent<PostProcessingBehaviour>().profile = motionBlurEffect;
    }

    private void Vignette()
    {
        activeCamera.GetComponent<PostProcessingBehaviour>().profile = vignetteEffect;
    }

    private void Bloom()
    {
        activeCamera.GetComponent<PostProcessingBehaviour>().profile = bloomEffect;
    }

	private void GeneratePostReport(int endnum = 0)
    {
        UnlockCursor();
        playerCamera.GetComponent<PlayerLook>().enabled = false;
		//Debug.Log(GetAnswer(1));
        postReport.SetActive(true);
        string report = "";
		report = "Investigation case #160418(HO4)" +
		"\nStatus: On going " +
		"\nDocument classification: Confidential" +
		"\nDetective in Charge: Sgt Suzanna Warren" +
		"\nDate: 15/06/2017 " +
		"\nTime of interrogration: 1900HRS" +
		"\nLocation: Mary Hill police station, interrogation room 5" +
		"\nThe following details the factual statement as recorded..." +
		"\n\nThe suspect stated the following information about themselves..." +
		"\n Name: " + GetAnswer (0) +
		"\n Age: " + GetAnswer (1) +
		"\n Country of Origin: " + GetAnswer (2) +
		"\n Dream Frequency: " + GetAnswer (4) +

		"\n\nThe suspect claimed that they were at " + GetAnswer (5) + " during the time of the incident " +
		"along with " + GetAnswer (6) + " as their alibi." +
		" The suspect reported that they went home by " + GetAnswer (8) +
		" and arrived home at " + GetAnswer (7) +
            
		"\n\n The suspect stated that they, " + GetAnswer(9) +
		", recognise the victim when showed a picture of Lianne. When prompted to recall if anyone was acting suspiciously during time of incident, the suspect accused " +
		GetAnswer(10) + " because of the reason that they " + GetAnswer(11);
     
		postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;

		//endnum 1 = Good reality ending
		if (endnum == 1) {
			report = report + "\n\n--------------------New Entry--------------------" + 
            "\nInvestigation case #160418(HO4)" +
            "\nStatus: On going " +
            "\nDocument classification: Confidential" +
            "\nDetective in Charge: Sgt Suzanna Warren" +
            "\nDate: 17/07/2017 " +
            "\nThe following details the conclusion drawn from the investigation by the detective in charge…" +
            "\n\n The suspect has been cleared of all charges as it has been proven that " + GetAnswer (10) + 
            " is guilty of committing the murder of Lianna Armstrong. The events that transpired during the incident is that during the night of 14/05/2017, " + GetAnswer (10) + 
            ", followed the victim into the bathroom of " + GetAnswer (5) + ", and shot her with a 9mm pistol from behind in cold blood. ";
			DataController.Setfinalreport (report);
			//finalreport = report;
			//postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;
		}
		//endnum 2 = Bad reality ending 
		if (endnum == 2){
			report = report + "\n\n--------------------New Entry--------------------" + 
            "\nInvestigation case #160418(HO4)" +
            "\nStatus: On going " +
            "\nDocument classification: Confidential" +
            "\nDetective in Charge: Sgt Suzanna Warren" +
            "\nDate: 17/07/2017 " +
            "\nThe following details the conclusion drawn from the investigation by the detective in charge…" +
            "\n\n The suspect has been proven of being guilty of committing the murder of Lianna Armstrong. The events that transpired during the incident is that during the night of 14/05/2017,  the suspect followed the victim into the bathroom of " + 
			GetAnswer (5) + ", and shot her with a 9mm pistol from behind in cold blood." +
            "The motivation of which, is because of a heated argument between the suspect and the victim caused jealousy and envy to get the better of the suspect.";
			DataController.Setfinalreport (report);
			//finalreport = report;
			//postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;
		}
	}

    private string GetAnswer(int i)
    {
        string answerIndex = dataController.GetAnswerIdByQuestionId(allQuestions[i].questionId);
        string answer = allQuestions[i].answers.First(a => a.answerId.Equals(answerIndex)).answerText;
		answer = "<b>" + answer + "</b>";
        return answer;
    }

    public void ContinuePostReport()
    {
        LockCursor();
        playerCamera.GetComponent<PlayerLook>().enabled = true;
        postReport.SetActive(false);
        ContinueToNextAct();
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
            questionDisplay.SetActive(false);
            StartCoroutine(RunSequence());
        }
    }

    private void PlayTimeline(PlayableDirector playableDirector)
    {
        playableDirector.Play();
    }

	private void ApplyEndingCamera()
	{
	    playerCamera.GetComponent<PlayerLook>().enabled = false;
	    playerCamera.enabled = false;
	    finalCamera.enabled = true;
	    activeCamera = finalCamera;
	    postProcessingBehaviour = finalCamera.GetComponent<PostProcessingBehaviour>();
	}

    public void EndingScene(bool shoot)
    {
        EndingScene(shoot, dataController.GetOverallScore() <= OVERALL_SCORE_THRESHOLD);
    }

	private void EndingScene(bool shoot, bool consistent){
	    if (shoot)
	    {
	        Initiate.Fade(consistent ? "Ending1" : "Ending3", Color.black, 0.8f);
	    }
	    else
	    {
	        Initiate.Fade(consistent ? "Ending4" : "Ending2", Color.black, 0.8f);
	    }
	}

    // Update is called once per frame
    private void Update()
    {
        if (isEndingTimerActive)
        {
            decisionTimeRemaining -= Time.deltaTime;
            if (decisionTimeRemaining <= 0f) EndingScene(false); // didn't shoot
            endingTimeRemainingSlider.value = decisionTimeRemaining / ENDING_DECISION_TIME_LIMIT;
        }
        
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

        if (Input.GetKeyDown("r"))
        {
			LightsCameraAction(2);
			//LightingChanges(2, new Color(0,0,200,255));
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

        if (Input.GetKeyDown("i"))
        {
            GeneratePostReport();
			GeneratePostReport(1);

        }

		if (Input.GetKeyDown("o"))
		{

			EndingScene (true, false);

		}

//        HandleWalking();
    }
}