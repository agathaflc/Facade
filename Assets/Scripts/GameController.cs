using System;
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
    private const string EFFECT_STATUS_ON = "on";
    private const string EFFECT_STATUS_OFF = "off";
    private const string EFFECT_STATUS_INTENSIFY = "intensify";

    private const float LIGHT_INTENSITY_STEP = 0.25f;
    private const float BLOOM_INTENSITY_STEP = 0.05f;
    private const float VIGNETTE_INTENSITY_STEP = 0.005f;
    private const float MOTION_BLUR_SHUTTER_ANGLE_STEP = 10f;
    private const float MOTION_BLUR_DEFAULT_SHUTTER_ANGLE = 210f;

    private const float BLOOM_WAIT_TIME = 0.01f;

    private const float FER_RECORDING_TIME = 4f;
    private const float ENDING_DECISION_TIME_LIMIT = 25f;

    public float MAX_SUSPICION_SCORE;
    public float OVERALL_SCORE_THRESHOLD; // TODO: FINETUNE THIS
    public float musicVolume = 1.0f;

    // testing variables
    public bool FER_is_Off;
    public bool gameSceneOnly;
    public bool runTimeline;
    public bool skipAct;
    public bool skipPostActReport;
    public bool bgmOff;

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
    public Image BlackScreenDisplay;
    public GameObject detectiveObject;
    public GameObject badCop;
    public GameObject goodCop;
    public GameObject tableGun;

    public Light mainLight;
    public Light spotLight1;
    public Light spotLight2;
    public Color32 oldLightingColor;
    public Color32 newLightingColor;

    public Image FERIndicator;
    public Image FERCorrectness;
    public Sprite tick;
    public Sprite cross;
    public Text expectedExpression;

    public Camera playerCamera;
    public Camera finalCamera;
    private Camera activeCamera;

    public GameObject room;
    public PostProcessingProfile motionBlurEffect;
    public PostProcessingProfile vignetteEffect;
    public PostProcessingProfile bloomEffect;
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

    public AudioClip silenceClip;
    public AudioClip gunShot;
    public AudioSource soundEffectAudioSource;

    private int currentActNo;

    private int animationNoHash = Animator.StringToHash("animationNo");

    private AudioSource detectiveVoice;
    private AudioSource detectiveSoundEffect;

    private AudioSource bgmAudioSourceA;
    private AudioSource bgmAudioSourceB;

    private AudioSource currentBgmAudioSource;

    private static DataController dataController;
    private ActData currentActData;
    private QuestionData[] questionPool;

    private float lightingColorTimer;
    private const float MAX_COLOR_TIMER = 1f;

    private bool isMusicAdaptive;
    private bool isLightingAdaptive;
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
    private static int currentBgmLevel;
    private static readonly object bgmLock = new object();

    // Use this for initialization
    private void Start()
    {
        activeCamera = playerCamera;
        SetDefaultProcessingBehaviourProfiles();

        BlackScreenDisplay.color = new Color32(0, 0, 0, 255);

        lightingColorTimer = MAX_COLOR_TIMER;

        allTimelines.Add(act1Timelines);
        allTimelines.Add(act2Timelines);
        allTimelines.Add(act3Timelines);

        if (runTimeline)
        {
            StartCoroutine(PlayCurrentTimelineAsset());
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
            detectiveObject = goodCop;
            detectiveObject.SetActive(true);
            badCop.SetActive(false);
        }
        else
        {
            detectiveObject = badCop;
            detectiveObject.SetActive(true);
            goodCop.SetActive(false);
        }

        currentActData = dataController.GetCurrentRoundData();

        tableGun.SetActive(currentActData.showGun);
        if (currentActData.showGun)
        {
            // deactivate the collider until ending decision is shown
            tableGun.GetComponent<Collider>().enabled = false;
        }

        var defaultLighting = currentActData.neutralLighting;
        mainLight.intensity = defaultLighting.intensity;
        mainLight.color = new Color32(defaultLighting.colorR, defaultLighting.colorG, defaultLighting.colorB,
            defaultLighting.colorA);

        var detectiveAudioSources = detectiveObject.GetComponents<AudioSource>();
        detectiveVoice = detectiveAudioSources[0];
        detectiveSoundEffect = detectiveAudioSources[1];

        bgmAudioSourceA = player.AddComponent<AudioSource>();
        bgmAudioSourceB = player.AddComponent<AudioSource>();
        bgmAudioSourceA.volume = musicVolume;
        bgmAudioSourceB.volume = 0.0f;
        currentBgmAudioSource = bgmAudioSourceA;

        PlayBgm(currentActData.bgmNeutralClip, MUSIC_NEUTRAL,
            currentActData.bgmNeutralFile.seek); // always start off with the base clip 
        currentBgm = MUSIC_NEUTRAL;

        displayedScore = 0;
        sequenceIndex = 0;
        currentTimelineNo = 0;

        isMusicAdaptive = true;
        isLightingAdaptive = true;
        isTimerActive = false;
        isEndingTimerActive = false;
        isClarifying = false;
        currentBgmLevel = 0;

        if (!runTimeline)
        {
            StartCoroutine(RunSequence());
        }
    }

    private void SetDetectiveAnimator()
    {
        Debug.Log("SetDetectiveAnimator");
        detectiveObject.GetComponent<Animator>().runtimeAnimatorController =
            detectiveObject == badCop ? hansController : kiraController;

        currentDetectiveAnimator = detectiveObject.GetComponent<Animator>();
    }

    private void UnsetDetectiveAnimator()
    {
        detectiveObject.GetComponent<Animator>().runtimeAnimatorController = null;
        currentDetectiveAnimator = null;
    }

    private IEnumerator PlayCurrentTimelineAsset()
    {
        subtitleDisplay.SetActive(true);
        subtitleDisplayText.text = "";

        UnsetDetectiveAnimator();
        PlayTimeline(playableDirector);

        while (playableDirector.state == PlayState.Playing)
        {
            yield return null;
        }

        subtitleDisplay.SetActive(false);

        currentTimelineNo++;
//        StartCoroutine(RunSequence());
    }

    private IEnumerator RunTimeline()
    {
        subtitleDisplay.SetActive(true);
        subtitleDisplayText.text = "";

        playableDirector.playableAsset = allTimelines[currentActNo][currentTimelineNo];
        UnsetDetectiveAnimator();
        PlayTimeline(playableDirector);

        var crossFaded = false;
        while (playableDirector.state == PlayState.Playing)
        {
            if (currentSequence.earlyFade && !crossFaded &&
                playableDirector.time >= playableDirector.playableAsset.duration - 7)
            {
                BlackScreenDisplay.CrossFadeAlpha(1, 2.5f, true);
                crossFaded = true;
                yield return new WaitForSecondsRealtime(5f);
                EndRound();
            }

            yield return null;
        }

        subtitleDisplay.SetActive(false);

        currentTimelineNo++;

        // re-set the detective animator only when it's not the last timeline
        if (currentTimelineNo <= allTimelines[currentActNo].Count) SetDetectiveAnimator();
        isEventDone = true;
    }

    /**
     * Should only be called after the previous sequence is completed
     **/
    private IEnumerator RunSequence()
    {
        if (Time.timeScale <= 0)
        {
            yield break;
        }
        if (sequenceIndex >= currentActData.sequence.Length)
        {
            isEventDone = true;
            EndRound();
            yield break;
        }

        currentSequence = currentActData.sequence[sequenceIndex];

        if (currentSequence.fadeScreenToBlack)
        {
            BlackScreenDisplay.CrossFadeAlpha(1, 1f, true);
        }

        if (currentSequence.fadeScreenToTransparent)
        {
            BlackScreenDisplay.CrossFadeAlpha(0, 1f, true);
        }

        if (currentSequence.ending) // only for the animation when standing up
        {
            detectiveObject.GetComponent<Animator>().runtimeAnimatorController = kiraStandUpController;
            currentDetectiveAnimator = detectiveObject.GetComponent<Animator>();
        }

        if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_ENDING))
        {
            UIUtils.UnlockCursor();
            detectiveObject.SetActive(false);
            ApplyEndingSettings();
            endingDecisionDisplay.SetActive(true);

            var vignette = new SpecialEffect
            {
                type = EFFECT_VIGNETTE,
                intensity = 0.6f,
                colorR = 0,
                colorG = 0,
                colorB = 0,
                colorA = 1,
                roundness = 1,
                smoothness = 1
            };

            ShowSpecialEffect(vignette);

            decisionTimeRemaining = ENDING_DECISION_TIME_LIMIT;
            isEndingTimerActive = true;
            isEventDone = false;

            while (isEndingTimerActive)
            {
                yield return null;
            }
        }

        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
        {
            questionPool = currentSequence.questions;
            questionIndex = 0;

            subtitleDisplay.SetActive(false);
            BeginQuestions();
        }
        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_DIALOG))
        {
            UIUtils.LockCursor();
            isEventDone = false;

            if (currentSequence.turnOnSpotlight)
            {
                LightsCameraAction(1);
                LightsCameraAction(2);
            }

            var currentEffects = currentSequence.effects;
            if (currentEffects != null && currentEffects.Length > 0) // show special effects if any
            {
                foreach (var effect in currentEffects)
                {
                    ShowSpecialEffect(effect);
                }
            }

            if (currentSequence.hasLightingEffect)
            {
                var currentLighting = currentSequence.lighting;

                StartCoroutine(LightingChanges(new Color32(currentLighting.colorR, currentLighting.colorG,
                    currentLighting.colorB,
                    currentLighting.colorA), currentLighting.intensity));
            }

            ShowAndPlayDialog(DataController.LoadAudioFile(currentSequence.filePath), currentSequence.subtitleText);

            if (currentSequence.playCustomBgm)
            {
                Debug.Log("playCustomBgm in dialog");
                PlayBgm(DataController.LoadAudioFile(currentSequence.bgm.fileName), "special_bgm",
                    currentSequence.bgm.seek);
            }

            if (currentSequence.usemaxBgm)
            {
                Debug.Log("usemaxBgm");
                isMusicAdaptive = false;
                currentBgmLevel = currentActData.bgmLevelClips.Count - 1;
                PlayBgm(currentActData.bgmLevelClips[currentBgmLevel], "level",
                    currentActData.bgmLevels[currentBgmLevel].seek);
            }

            if (currentSequence.turnOffAdaptiveLighting)
            {
                isLightingAdaptive = false;
            }

            if (currentDetectiveAnimator != null)
                currentDetectiveAnimator.SetInteger(animationNoHash, currentSequence.animationNo);

            var exited = currentSequence.animationNo == 0;
            var exitTime = (currentDetectiveAnimator == null)
                ? 0
                : currentDetectiveAnimator.GetAnimatorTransitionInfo(currentSequence.animatorLayer).duration;

            // show indicator if needed
            if (currentSequence.readExpression) FERIndicator.enabled = true;

            while (detectiveVoice.isPlaying)
            {
                if (currentDetectiveAnimator != null)
                {
                    if (!exited)
                    {
                        if (currentDetectiveAnimator.GetAnimatorTransitionInfo(currentSequence.animatorLayer)
                                .normalizedTime > exitTime)
                        {
                            currentDetectiveAnimator.SetInteger(animationNoHash, 0);
                            exited = true;
                        }
                    }
                }

                yield return null;
            }

            if (currentSequence.readExpression)
            {
                subtitleDisplay.SetActive(false);

                DataController.StartFER();
                Debug.Log("wait " + FER_RECORDING_TIME + " seconds");
                yield return new WaitForSecondsRealtime(FER_RECORDING_TIME);
                DataController.StopFER();

                string closestEmotion;
                SaveAndDisplayScore(CalculateSuspicionScore_EmotionOnly(currentSequence.expectedExpressions,
                    currentSequence.scoreWeight, out closestEmotion));
            }

            FERIndicator.enabled = false;
            ConcludeEvent();
        }
        else if (currentSequence.sequenceType.Equals(SEQUENCE_TYPE_TIMELINE))
        {
            isEventDone = false;

            UIUtils.LockCursor();
            if (BlackScreenDisplay.color.a > 0 && !currentSequence.earlyFade)
            {
                BlackScreenDisplay.CrossFadeAlpha(0, 1f, true);
            }

            StartCoroutine(RunTimeline());

            while (!isEventDone)
            {
                yield return null;
            }
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

    private IEnumerator CrossFade(AudioSource a, AudioSource b, float seconds)
    {
        // calculate the duration for each step
        var stepInterval = seconds / 20.0f;
        var volumeInterval = musicVolume / 20.0f;

        b.Play();

        // fade between the two, taking A to 0 volume and B to musicVolume
        for (var i = 0; i < 20; i++)
        {
            a.volume -= volumeInterval;
            b.volume += volumeInterval;

            // wait for one interval then continue the loop
            yield return new WaitForSecondsRealtime(stepInterval);
        }

        if (a.isPlaying) a.Stop();
    }

    private IEnumerator SwitchTracks(AudioClip clip, float seek, float seconds = 4.0f)
    {
        if (clip != null)
        {
            lock (bgmLock)
            {
                Debug.Log("switch tracks");
                var playA = !(Math.Abs(bgmAudioSourceB.volume) < 0.01);

                if (playA)
                {
                    bgmAudioSourceA.clip = clip;
                    bgmAudioSourceA.loop = true;
                    bgmAudioSourceA.time = seek;
                    yield return StartCoroutine(CrossFade(bgmAudioSourceB, bgmAudioSourceA, seconds));
                }
                else
                {
                    bgmAudioSourceB.clip = clip;
                    bgmAudioSourceB.loop = true;
                    bgmAudioSourceB.time = seek;
                    yield return StartCoroutine(CrossFade(bgmAudioSourceA, bgmAudioSourceB, seconds));
                }
            }
        }
        else
        {
            Debug.LogError("SwitchTracks: audio clip is null");
        }
    }

    private void PlayBgm(AudioClip clip, string musicType, float seek)
    {
        if (clip == null)
        {
            Debug.LogError("clip is empty");
            return;
        }

        currentBgm = musicType;

        if (bgmOff) return;
        StartCoroutine(SwitchTracks(clip, seek));
    }

    private void ConcludeEvent()
    {
        isEventDone = true;
    }

    private static void SaveQuestion(QuestionData question)
    {
        var allQuestions = DataController.GetAllQuestions();
        allQuestions.Add(question);
    }

    private void ShowSpecialEffect(string currentSequenceEffect)
    {
        if (currentSequenceEffect.Equals(EFFECT_VIGNETTE))
        {
            Vignette();
            activeCamera.GetComponent<PostProcessingBehaviour>().enabled = true;
        }
    }

    private void SetDefaultProcessingBehaviourProfiles()
    {
        var postProcessingBehaviour = activeCamera.GetComponent<PostProcessingBehaviour>();

        if (postProcessingBehaviour.enabled == false)
        {
            postProcessingBehaviour.enabled = true;
        }

        if (postProcessingBehaviour.profile == null)
        {
            Debug.Log("postProcessingBehaviour profile is null, creating profile");
            postProcessingBehaviour.profile = ScriptableObject.CreateInstance<PostProcessingProfile>();
        }

        postProcessingBehaviour.profile.motionBlur.settings = motionBlurEffect.motionBlur.settings;
        postProcessingBehaviour.profile.motionBlur.enabled = false;

        postProcessingBehaviour.profile.vignette.settings = vignetteEffect.vignette.settings;
        postProcessingBehaviour.profile.vignette.enabled = true;

        postProcessingBehaviour.profile.bloom.settings = bloomEffect.bloom.settings;
        postProcessingBehaviour.profile.bloom.enabled = true;
    }

    private void ShowSpecialEffect(SpecialEffect effect)
    {
        var postProcessingBehaviour = activeCamera.GetComponent<PostProcessingBehaviour>();

        switch (effect.type)
        {
            case EFFECT_BLOOM:
                StartCoroutine(GraduallyChangeBloomIntensity(postProcessingBehaviour,
                    postProcessingBehaviour.profile.bloom.settings.bloom.intensity < effect.intensity,
                    effect.intensity));
                break;
            case EFFECT_MOTION_BLUR:
                switch (effect.status)
                {
                    case EFFECT_STATUS_ON:
                        StartCoroutine(GraduallyChangeMotionBlur(postProcessingBehaviour, true,
                            MOTION_BLUR_DEFAULT_SHUTTER_ANGLE));
                        break;
                    case EFFECT_STATUS_OFF:
                        StartCoroutine(GraduallyChangeMotionBlur(postProcessingBehaviour, false, 0f));
                        break;
                    case EFFECT_STATUS_INTENSIFY:
                        StartCoroutine(GraduallyChangeMotionBlur(postProcessingBehaviour, true, 360f));
                        break;
                    default:
                        break;
                }

                break;
            case EFFECT_VIGNETTE:
                var vignetteSettings = postProcessingBehaviour.profile.vignette.settings;

                // set color, smoothness and roundness
                vignetteSettings.color = new Color32(effect.colorR, effect.colorG, effect.colorB, effect.colorA);
                vignetteSettings.smoothness = effect.smoothness;
                vignetteSettings.roundness = effect.roundness;

                postProcessingBehaviour.profile.vignette.settings = vignetteSettings;

                StartCoroutine(GraduallyChangeVignetteIntensity(postProcessingBehaviour,
                    vignetteSettings.intensity < effect.intensity, effect.intensity));
                break;
            default:
                break;
        }
    }

    private static IEnumerator GraduallyChangeVignetteIntensity(PostProcessingBehaviour postProcessingBehaviour,
        bool increase, float targetIntensity)
    {
        var settings = postProcessingBehaviour.profile.vignette.settings;

        if (increase)
        {
            while (settings.intensity + VIGNETTE_INTENSITY_STEP <= targetIntensity)
            {
                settings.intensity += VIGNETTE_INTENSITY_STEP;
                postProcessingBehaviour.profile.vignette.settings = settings;
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }
        else
        {
            while (settings.intensity - VIGNETTE_INTENSITY_STEP >= targetIntensity)
            {
                settings.intensity -= VIGNETTE_INTENSITY_STEP;
                postProcessingBehaviour.profile.vignette.settings = settings;
                yield return new WaitForSecondsRealtime(0.03f);
            }
        }
    }

    private static IEnumerator GraduallyChangeBloomIntensity(PostProcessingBehaviour postProcessingBehaviour,
        bool increase, float targetIntensity)
    {
        var settings = postProcessingBehaviour.profile.bloom.settings;

        if (increase)
        {
            while (settings.bloom.intensity + BLOOM_INTENSITY_STEP <= targetIntensity)
            {
                settings.bloom.intensity += BLOOM_INTENSITY_STEP;
                postProcessingBehaviour.profile.bloom.settings = settings;
                yield return new WaitForSecondsRealtime(BLOOM_WAIT_TIME);
            }
        }
        else
        {
            while (settings.bloom.intensity - BLOOM_INTENSITY_STEP >= targetIntensity)
            {
                settings.bloom.intensity -= BLOOM_INTENSITY_STEP;
                postProcessingBehaviour.profile.bloom.settings = settings;
                yield return new WaitForSecondsRealtime(BLOOM_WAIT_TIME);
            }
        }
    }

    private static IEnumerator GraduallyChangeMotionBlur(PostProcessingBehaviour postProcessingBehaviour, bool increase,
        float targetShutterAngle)
    {
        postProcessingBehaviour.profile.motionBlur.enabled = true;
        var settings = postProcessingBehaviour.profile.motionBlur.settings;

        if (increase)
        {
            while (settings.shutterAngle + MOTION_BLUR_SHUTTER_ANGLE_STEP <= targetShutterAngle)
            {
                settings.shutterAngle += MOTION_BLUR_SHUTTER_ANGLE_STEP;
                postProcessingBehaviour.profile.motionBlur.settings = settings;
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
        else
        {
            while (settings.shutterAngle - MOTION_BLUR_SHUTTER_ANGLE_STEP >= targetShutterAngle)
            {
                settings.shutterAngle -= MOTION_BLUR_SHUTTER_ANGLE_STEP;
                postProcessingBehaviour.profile.motionBlur.settings = settings;
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }

        if (settings.shutterAngle <= MOTION_BLUR_SHUTTER_ANGLE_STEP)
        {
            postProcessingBehaviour.profile.motionBlur.enabled = false;
        }
    }

    private void Vignette()
    {
        activeCamera.GetComponent<PostProcessingBehaviour>().profile = vignetteEffect;
    }

    private void AdaptLightingByEmotion(LightingEffect lightingEffect)
    {
        if (isLightingAdaptive)
            StartCoroutine(LightingChanges(
                new Color32(lightingEffect.colorR, lightingEffect.colorG, lightingEffect.colorB, lightingEffect.colorA),
                lightingEffect.intensity));
    }

    private IEnumerator LightingChanges(Color32 newColor, float newIntensity = 1)
    {
        Debug.Log("LightingChanges");

        oldLightingColor = mainLight.color;
        newLightingColor = newColor;

        StartCoroutine(GraduallyChangeLightIntensity(newIntensity));

        var lightLerp = 0f;
        while (lightLerp < 1)
        {
            mainLight.GetComponent<Light>().color = Color32.Lerp(oldLightingColor, newColor, lightLerp);
            yield return new WaitForSecondsRealtime(0.15f);
            lightLerp += 0.05f; // waiting time: 0.15 / 0.05 = 3 secs
        }
    }

    private IEnumerator GraduallyChangeLightIntensity(float newIntensity)
    {
        if (mainLight.intensity < newIntensity)
        {
            while (mainLight.intensity + LIGHT_INTENSITY_STEP < newIntensity)
            {
                mainLight.intensity += LIGHT_INTENSITY_STEP;
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
        else
        {
            while (mainLight.intensity - LIGHT_INTENSITY_STEP > newIntensity)
            {
                mainLight.intensity -= LIGHT_INTENSITY_STEP;
                yield return new WaitForSecondsRealtime(0.25f);
            }
        }
    }

    private void LightsCameraAction(int Num)
    {
        switch (Num)
        {
            case 1:
                spotLight1.enabled = true;
                break;
            case 2:
                spotLight2.enabled = true;
                break;
            default:
                break;
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

    private void BeginQuestions()
    {
        UIUtils.UnlockCursor();
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

        if (currentQuestion.playCustomBgm)
        {
            PlayBgm(DataController.LoadAudioFile(currentQuestion.bgm.fileName), "special_bgm",
                currentQuestion.bgm.seek);
        }

        if (currentQuestion.hasLightingEffect)
        {
            var currentLighting = currentQuestion.lighting;

            StartCoroutine(LightingChanges(new Color32(currentLighting.colorR, currentLighting.colorG,
                currentLighting.colorB,
                currentLighting.colorA), currentLighting.intensity));
        }

        if (currentQuestion.considersEmotion)
        {
            FERIndicator.enabled = true;
        }

        var currentEffects = currentQuestion.effects;
        if (currentEffects != null && currentEffects.Length > 0) // show special effects if any
        {
            foreach (var effect in currentEffects)
            {
                ShowSpecialEffect(effect);
            }
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
        var emotionDistance = dataController.ComputeEmotionDistance(expectedExpression,
            DataController.ReadPlayerEmotion(), out closestEmotion);

        expressionScore =
            ScoreCalculator.CalculateExpressionScore(emotionDistance, currentQuestion.expressionWeight, closestEmotion);
        suspicionScore += expressionScore;

        Debug.Log("closestEmotion: " + closestEmotion + ", distance: " + emotionDistance + ", score: " +
                  expressionScore);

        StartCoroutine(ShowFERCorrectness(expressionScore <= 0, closestEmotion));

        if (!FER_is_Off)
        {
            dataController.DeleteFERDataFile();
        }

        return suspicionScore;
    }

    private IEnumerator ShowFERCorrectness(bool correct, string expected)
    {
        FERCorrectness.sprite = correct ? tick : cross;
        FERCorrectness.enabled = true;

        expectedExpression.text = expected;
        expectedExpression.enabled = true;

        yield return new WaitForSecondsRealtime(1f);

        FERCorrectness.enabled = false;
        expectedExpression.enabled = false;
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

        scoreDisplayText.text = "Suspicion: " + displayedScore.ToString("F2");
        scoreDisplayerSlider.value = displayedScore / MAX_SUSPICION_SCORE;

        if (scoreDisplayerSlider.value >= 0.8)
        {
            scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color =
                new Color32(200, 0, 0, 255);
        }
        else if (scoreDisplayerSlider.value >= 0.5 && scoreDisplayerSlider.value < 0.8)
        {
            scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color =
                new Color32(250, 250, 0, 255);
        }
        else
        {
            scoreDisplayerSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color =
                new Color32(0, 200, 0, 255);
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

        if (currentQuestion.considersEmotion)
        {
            FERIndicator.enabled = false;
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
            if (isMusicAdaptive)
            {
                AdaptMusicAndLighting(
                    currentQuestion.considersFact,
                    currentQuestion.considersEmotion,
                    closestEmotion,
                    consistencyScore <= 0f,
                    expressionScore <= 0f,
                    !currentQuestion.dontAdaptLighting
                );
            }

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
        bool correctExpression = true,
        bool adaptLighting = true
    )
    {
        if (considerConsistency)
            if (!consistent)
            {
                if (!currentActData.useBgmLevels)
                {
                    // todo play the foghorn thing??
                    if (!currentBgm.Contains(SURPRISED_EMOTION))
                    {
                        PlayBgm(currentActData.bgmAngrySurprisedClip, MUSIC_SURPRISED_ANGRY,
                            currentActData.bgmAngrySurprisedFile.seek);

                        if (adaptLighting) AdaptLightingByEmotion(currentActData.angrySurprisedLighting);
                    }

                    return;
                }
                else
                {
                    currentBgmLevel++;
                    if (currentBgmLevel < currentActData.bgmLevels.Length)
                    {
                        Debug.Log("current bgm level: " + currentBgmLevel);
                        PlayBgm(currentActData.bgmLevelClips[currentBgmLevel], "level",
                            currentActData.bgmLevels[currentBgmLevel].seek);
                    }
                }
            }

        if (!considerEmotion) return;

        if (!correctExpression)
        {
            if (!currentActData.useBgmLevels)
            {
                // todo play the foghorn thing??
                if (!currentBgm.Contains(SCARED_EMOTION))
                {
                    PlayBgm(currentActData.bgmSadScaredClip, MUSIC_SAD_SCARED,
                        currentActData.bgmSadScaredFile.seek);

                    if (adaptLighting) AdaptLightingByEmotion(currentActData.sadScaredLighting);
                }

                return;
            }
            else
            {
                currentBgmLevel++;
                if (currentBgmLevel < currentActData.bgmLevels.Length)
                {
                    Debug.Log("current bgm level: " + currentBgmLevel);
                    PlayBgm(currentActData.bgmLevelClips[currentBgmLevel], "level",
                        currentActData.bgmLevels[currentBgmLevel].seek);
                }

                return;
            }
        }

        Debug.Log("AdaptLightingByEmotion: correct expression.");

        // if the current music is the same emotion, don't change
        if (currentBgm.Contains(emotion)) return;

        if (MUSIC_HAPPY.Contains(emotion))
        {
            PlayBgm(currentActData.bgmHappyClip, MUSIC_HAPPY, currentActData.bgmHappyFile.seek);
            if (adaptLighting) AdaptLightingByEmotion(currentActData.happyLighting);
        }
        else if (MUSIC_NEUTRAL.Contains(emotion))
        {
            PlayBgm(currentActData.bgmNeutralClip, MUSIC_NEUTRAL, currentActData.bgmNeutralFile.seek);
            if (adaptLighting) AdaptLightingByEmotion(currentActData.neutralLighting);
        }
        else if (MUSIC_SAD_SCARED.Contains(emotion))
        {
            PlayBgm(currentActData.bgmSadScaredClip, MUSIC_SAD_SCARED, currentActData.bgmSadScaredFile.seek);
            if (adaptLighting) AdaptLightingByEmotion(currentActData.sadScaredLighting);
        }
        else if (MUSIC_SURPRISED_ANGRY.Contains(emotion))
        {
            PlayBgm(currentActData.bgmAngrySurprisedClip, MUSIC_SURPRISED_ANGRY,
                currentActData.bgmAngrySurprisedFile.seek);
            if (adaptLighting) AdaptLightingByEmotion(currentActData.angrySurprisedLighting);
        }
    }

    private void EndRound()
    {
        isTimerActive = false;
        isEventDone = false;
        dataController.SubmitNewPlayerScore(displayedScore);
        highScoreDisplayText.text = dataController.GetHighestPlayerScore().ToString();

        questionDisplay.SetActive(false);

        if (currentActNo == 0 && !skipPostActReport)
        {
            GeneratePostReport();
        }
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
        Destroy(this);
    }

    private void UpdateTimeRemainingDisplay()
    {
        if (isTimerActive)
        {
            timeRemainingDisplaySlider.value = timeRemaining / currentQuestion.timeLimitInSeconds;
        }
    }

    private void GeneratePostReport(int endnum = 0)
    {
        string report = "";

        if (endnum == 0)
        {
            if (Cursor.lockState.Equals(CursorLockMode.Locked)) UIUtils.UnlockCursor();
            playerCamera.GetComponent<PlayerLook>().enabled = false;
            postReport.SetActive(true);
            report = "Investigation case #160418(HO4)" +
                     "\nStatus: On going " +
                     "\nDocument classification: Confidential" +
                     "\nDetective in Charge: Sgt Suzanna Warren" +
                     "\nDate: 15/06/2017 " +
                     "\nTime of interrogration: 1900HRS" +
                     "\nLocation: Mary Hill police station, interrogation room 5" +
                     "\nThe following details the factual statement as recorded..." +
                     "\n\nThe suspect stated the following information about themselves..." +
                     "\n Name: " + GetAnswer(0) +
                     "\n Age: " + GetAnswer(1) +
                     "\n Country of Origin: " + GetAnswer(2) +
                     "\n Dream Frequency: " + GetAnswer(4) +
                     "\n\nThe suspect claimed that they were at " + GetAnswer(5) + " during the time of the incident " +
                     "along with " + GetAnswer(6) + " as their alibi." +
                     " The suspect reported that they went home by " + GetAnswer(8) +
                     " and arrived home at " + GetAnswer(7) +
                     "\n\n The suspect stated that they, " + GetAnswer(9) +
                     ", recognise the victim when showed a picture of Lianne. When prompted to recall if anyone was acting suspiciously during time of incident, the suspect accused " +
                     GetAnswer(10) + " because of the reason that they " + GetAnswer(11);

            postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text =
                report;
        }

        // call GeneratePostReport(1/2) at corresponding times. 
        //endnum 1 = Good reality ending
        else if (endnum == 1)
        {
            report = report + "\n\n-----------------------------New Entry-----------------------------" +
                     "\nInvestigation case #160418(HO4)" +
                     "\nStatus: Closed " +
                     "\nDocument classification: Confidential" +
                     "\nDetective in Charge: Sgt Suzanna Warren" +
                     "\nDate: 17/07/2017 " +
                     "\nThe following details the conclusion drawn from the investigation by the detective in charge…" +
                     "\n\nThe suspect has been cleared of all charges as it has been proven that " + GetAnswer(10) +
                     " is guilty of committing the murder of Lianna Armstrong. The events that transpired during the incident is that during the night of 14/05/2017, " +
                     GetAnswer(10) +
                     ", followed the victim into the bathroom of " + GetAnswer(5) +
                     ", and shot her with a 9mm pistol from behind in cold blood. ";
            DataController.Setfinalreport(report);
            //finalreport = report;
            //postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;
        }

        //endnum 2 = Bad reality ending 
        else if (endnum == 2)
        {
            report = report + "\n\n-----------------------------New Entry-----------------------------" +
                     "\nInvestigation case #160418(HO4)" +
                     "\nStatus: Closed " +
                     "\nDocument classification: Confidential" +
                     "\nDetective in Charge: Sgt Suzanna Warren" +
                     "\nDate: 17/07/2017 " +
                     "\nThe following details the conclusion drawn from the investigation by the detective in charge…" +
                     "\n\nThe suspect has been proven of being guilty of committing the murder of Lianna Armstrong. The events that transpired during the incident is that during the night of 14/05/2017,  the suspect followed the victim into the bathroom of " +
                     GetAnswer(5) + ", and shot her with a 9mm pistol from behind in cold blood." +
                     "The motivation of which, is because of a heated argument between the suspect and the victim caused jealousy and envy to get the better of the suspect.";
            DataController.Setfinalreport(report);
            //finalreport = report;
            //postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;
        }

        //postReport.transform.Find("ScrollView/Viewport/Content/Report").gameObject.GetComponent<Text>().text = report;
    }

    private static string GetAnswer(int i)
    {
        string answer = "NOT_FOUND";
        try
        {
            var allQuestions = DataController.GetAllQuestions();
            string answerIndex = dataController.GetAnswerIdByQuestionId(allQuestions[i].questionId);
            answer = allQuestions[i].answers.First(a => a.answerId.Equals(answerIndex)).answerText;
        }
        catch (ArgumentOutOfRangeException)
        {
            Debug.LogError("answer no. " + i + " not found");
        }

        answer = "<b>" + answer + "</b>";
        return answer;
    }

    public void ContinuePostReport()
    {
        playerCamera.GetComponent<PlayerLook>().enabled = true;
        ContinueToNextAct();
    }

    private void HandleEndOfAQuestion()
    {
        // show another question if there are still questions to ask
        if (questionPool.Length > questionIndex + 1)
        {
            questionIndex++;
            ShowQuestion();
        }
        else
        {
            questionDisplay.SetActive(false);
            StartCoroutine(RunSequence());
        }
    }

    private void PlayTimeline(PlayableDirector playableDirector)
    {
        playableDirector.Play();
    }

    private void ApplyEndingSettings()
    {
        playerCamera.GetComponent<PlayerLook>().enabled = false;
        playerCamera.enabled = false;
        finalCamera.enabled = true;
        activeCamera = finalCamera;
        SetDefaultProcessingBehaviourProfiles();

        StartCoroutine(SwitchTracks(silenceClip, 0f, 0.5f));

        tableGun.GetComponent<Collider>().enabled = true;
    }

    public void EndingScene(bool shoot)
    {
        StartCoroutine(EndingScene(shoot, dataController.GetOverallScore() <= OVERALL_SCORE_THRESHOLD));
    }

    private IEnumerator EndingScene(bool shoot, bool consistent)
    {
        if (shoot)
        {
            UIUtils.LockCursor();
            isEndingTimerActive = false;
            if (!skipPostActReport) GeneratePostReport(2);
            BlackScreenDisplay.CrossFadeAlpha(1, 2f, true);

            yield return new WaitForSecondsRealtime(4f);

            soundEffectAudioSource.clip = gunShot;
            soundEffectAudioSource.Play();

            yield return new WaitForSecondsRealtime(4f);

            Initiate.Fade(consistent ? "Ending1" : "Ending3", Color.black, 0.8f);
        }
        else
        {
            if (!skipPostActReport) GeneratePostReport(1);
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

            if (currentSequence != null && currentSequence.sequenceType.Equals(SEQUENCE_TYPE_QUESTION))
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
            StartCoroutine(GraduallyChangeMotionBlur(activeCamera.GetComponent<PostProcessingBehaviour>(), true,
                MOTION_BLUR_DEFAULT_SHUTTER_ANGLE));
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
            StartCoroutine(
                GraduallyChangeBloomIntensity(activeCamera.GetComponent<PostProcessingBehaviour>(), true, 8f));
            playerCamera.GetComponent<PostProcessingBehaviour>().enabled =
                !playerCamera.GetComponent<PostProcessingBehaviour>().enabled;
        }

        if (Input.GetKeyDown("i"))
        {
            GeneratePostReport();
            //GeneratePostReport(1);
        }

        if (Input.GetKeyDown("o"))
        {
            EndingScene(true, false);
        }
    }
}