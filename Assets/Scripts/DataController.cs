using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataController : MonoBehaviour
{
    private const string MENU_SCREEN = "MenuScreen";
    private const string HIGHEST_SCORE_KEY = "highestScore";

    private const string DETECTIVE_RESPONSE_NEUTRAL = "neutral";
    private const string DETECTIVE_RESPONSE_POSITIVE = "positive";
    private const string DETECTIVE_RESPONSE_NEGATIVE = "negative";
    public const string DETECTIVE_RESPONSE_CLARIFYING = "clarifying";
    public const string DETECTIVE_RESPONSE_POST_CLARIFYING = "postClarifying";

    private const string ACT_ONE_QUESTIONS_FILE_NAME = "questionsACT1.json";
    private const string ACT_ONE_DATA_FILE_NAME = "act1.json";
    private const string EXPRESSION_DATA_FILE_NAME = "expression_data.json";
    private const string DISTANCEMAP_DATA_FILE_NAME = "distances.json";
    private const string DISTANCE_MAPPING_FILE_NAME = "distances_2d_mapping.json";
    private const string FER_FLAG_FILE_NAME = "flag.txt";

    private const string RECORD_EXPRESSION = "record";
    private RoundData currentRound;
    private DistanceData distanceMap;

    private PlayerProgress playerProgress;

    // Use this for initialization
    private void Start()
    {
        DontDestroyOnLoad(gameObject); // prevent destroy objects in previous scene that has been unloaded
        //LoadGameData(ACT_ONE_QUESTIONS_FILE_NAME);
        LoadRoundData(ACT_ONE_DATA_FILE_NAME);
        LoadPlayerProgress();
        ReadDistanceMap();

        SceneManager.LoadScene(MENU_SCREEN);
    }

    public RoundData GetCurrentRoundData()
    {
        return currentRound;
    }

    public void SubmitNewPlayerScore(float newScore)
    {
        if (newScore > playerProgress.highestScore)
        {
            playerProgress.highestScore = newScore;
            SavePlayerProgress();
        }
    }

    public float GetHighestPlayerScore()
    {
        return playerProgress.highestScore;
    }

    private void LoadPlayerProgress()
    {
        playerProgress = new PlayerProgress();

        if (PlayerPrefs.HasKey(HIGHEST_SCORE_KEY))
            playerProgress.highestScore = PlayerPrefs.GetFloat(HIGHEST_SCORE_KEY);
    }

    private void SavePlayerProgress()
    {
        PlayerPrefs.SetFloat(HIGHEST_SCORE_KEY, playerProgress.highestScore);
    }

    public QuestionData GetQuestionDataById(string questionId)
    {
        var q = currentRound.questions.FirstOrDefault(e => e.questionId.Equals(questionId));

        if (q == null) Debug.LogError("GetQuestionTextById: question not found!");

        return q;
    }

    public void StartFER()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, FER_FLAG_FILE_NAME);

        if (File.Exists(filePath))
            File.WriteAllText(filePath, RECORD_EXPRESSION);
        else
            Debug.LogError("FER flag does not exist!");
    }

    public void StopFER()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, FER_FLAG_FILE_NAME);

        if (File.Exists(filePath))
            File.WriteAllText(filePath, "");
        else
            Debug.LogError("FER flag does not exist!");
    }

    public void DeleteFERDataFile()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, EXPRESSION_DATA_FILE_NAME);

        if (File.Exists(filePath))
            File.Delete(filePath);
        else
            Debug.LogError("FER data file doesn't exist");
    }

    private void LoadRoundData(string fileName)
    {
        var filePath =
            Path.Combine(Application.streamingAssetsPath,
                fileName); // streamingAssetsPath is the folder that stores the json

        if (File.Exists(filePath))
        {
            var dataAsJson = File.ReadAllText(filePath);
            currentRound = JsonUtility.FromJson<RoundData>(dataAsJson);

            LoadAllDetectiveResponses(currentRound.responsesPath);
            LoadBgms();
        }
        else
        {
            Debug.LogError("Cannot load game data!");
        }
    }

    public void LoadBgms()
    {
        if (currentRound == null)
        {
            Debug.LogError("current round is null!");
            return;
        }

        currentRound.bgmPositiveClip = LoadAudioFile(currentRound.bgmPositiveFile);
        currentRound.bgmNegativeClip = LoadAudioFile(currentRound.bgmNegativeFile);
        currentRound.bgmNormalClip = LoadAudioFile(currentRound.bgmNormalFile);
    }

    public AudioClip LoadAudioFile(string relativeResourcePath)
    {
        return Resources.Load<AudioClip>(relativeResourcePath);
    }

    public void LoadDetectiveRespClip(out AudioClip clip, out string subtitle,
        string responseType = DETECTIVE_RESPONSE_NEUTRAL, bool suspicious = false)
    {
        ResponseData[] responseData;

        switch (responseType)
        {
            case null:
                responseData = currentRound.detectiveResponses.notSuspiciousNeutral;
                break;
            case DETECTIVE_RESPONSE_POSITIVE:
                responseData = suspicious
                    ? currentRound.detectiveResponses.suspiciousPositive
                    : currentRound.detectiveResponses.notSuspiciousPositive;
                break;
            case DETECTIVE_RESPONSE_NEGATIVE:
                responseData = suspicious
                    ? currentRound.detectiveResponses.suspiciousNegative
                    : currentRound.detectiveResponses.notSuspiciousNegative;
                break;
            case DETECTIVE_RESPONSE_NEUTRAL:
                responseData = suspicious
                    ? currentRound.detectiveResponses.suspiciousNeutral
                    : currentRound.detectiveResponses.notSuspiciousNeutral;
                break;
            case DETECTIVE_RESPONSE_CLARIFYING:
                responseData = currentRound.detectiveResponses.clarifying;
                break;
            case DETECTIVE_RESPONSE_POST_CLARIFYING:
                responseData = currentRound.detectiveResponses.postClarifying;
                break;
            default:
                responseData = currentRound.detectiveResponses.notSuspiciousNeutral;
                break;
        }

        // Debug.Log (responseData);
        var index = Random.Range(0, responseData.Length);

        if (responseData[index].clip != null)
        {
            Debug.Log("LoadDetectiveRespClip: clip HAS been saved before");
            clip = responseData[index].clip;
        }
        else
        {
            // Debug.Log ("LoadDetectiveRespClip: clip has NOT been saved before");
            clip = Resources.Load<AudioClip>(responseData[index].soundFilePath);
            responseData[index].clip = clip;
        }

        subtitle = responseData[index].text;
    }

    private void LoadAllDetectiveResponses(string fileName)
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, fileName);
        // Debug.Log ("responses path: " + filePath);
        if (File.Exists(filePath))
        {
            var dataAsJson = File.ReadAllText(filePath);
            currentRound.detectiveResponses = JsonUtility.FromJson<DetectiveResponses>(dataAsJson);
        }
        else
        {
            Debug.LogError("Cannot load game data: detective responses");
        }
    }

    private void ReadDistanceMap()
    {
        var filePath =
            Path.Combine(Application.streamingAssetsPath,
                DISTANCE_MAPPING_FILE_NAME); // streamingAssetsPath is the folder that stores the json

        if (File.Exists(filePath))
        {
            var dataAsJson = File.ReadAllText(filePath);
            distanceMap = JsonUtility.FromJson<DistanceData>(dataAsJson);
        }
        else
        {
            Debug.LogError("Cannot load distance data!");
        }
    }

    public float ComputeEmotionDistance(string[] expected, EmotionData[] actual, out string closestEmotion)
    {
        return ScoreCalculator.ComputeEmotionDistance(distanceMap, expected, actual, out closestEmotion);
    }


    /*public EmotionData ReadPlayerEmotion (int questionIndex)
    {
        string filePath = Path.Combine (Application.streamingAssetsPath, EXPRESSION_DATA_FILE_NAME); // streamingAssetsPath is the folder that stores the json

        if (File.Exists (filePath)) {
            string dataAsJson = File.ReadAllText (filePath);
            ExpressionData loadedExpressions = JsonUtility.FromJson<ExpressionData> (dataAsJson);

            if (loadedExpressions.emotions.Length < questionIndex + 1) { // index out of bounds???
                Debug.LogError ("Expression data for this question does not exist!");
            }

            EmotionData correspondingEmotion = loadedExpressions.emotions [questionIndex];

            if (correspondingEmotion.questionNo == questionIndex) {
                // Debug.Log ("expression data loaded successfully");
                return correspondingEmotion;
            } else {
                Debug.LogError ("Question index does not match!");
            }
        } else {
            Debug.LogError ("Cannot load expression data!");
        }

        return null;
    }*/

    public EmotionData[] ReadPlayerEmotion()
    {
        var filePath =
            Path.Combine(Application.streamingAssetsPath,
                EXPRESSION_DATA_FILE_NAME); // streamingAssetsPath is the folder that stores the json

        if (File.Exists(filePath))
        {
            var dataAsJson = File.ReadAllText(filePath);
            var loadedExpressions = JsonUtility.FromJson<ExpressionData>(dataAsJson);

            return loadedExpressions.emotions;
        }

        Debug.LogError("Cannot load expression data!");

        return null;
    }

    public Texture2D LoadImage(string fileName)
    {
        Texture2D tex = null;
        byte[] fileData;

        var filePath =
            Path.Combine(Application.streamingAssetsPath,
                fileName); // streamingAssetsPath is the folder that stores the json

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2, TextureFormat.DXT1, false);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        return tex;
    }
}