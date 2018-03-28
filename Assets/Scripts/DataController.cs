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

    private const string ACT_ONE_SEQUENCE_FILE_NAME = "ACT1fin.json";
    private const string ACT_ONE_DATA_FILE_NAME = "act1.json";
    private const string EXPRESSION_DATA_FILE_NAME = "expression_data.json";
    private const string DISTANCEMAP_DATA_FILE_NAME = "distances.json";
    private const string DISTANCE_MAPPING_FILE_NAME = "distances_2d_mapping.json";
    private const string ACT_FILE_PATH = "act_files.json";
    private const string FER_FLAG_FILE_NAME = "flag.txt";

    private const string RECORD_EXPRESSION = "record";
    private RoundData currentRound;
    private DistanceData distanceMap;

    private PlayerProgress playerProgress;

    private static int currentAct;
    private string[] actFiles;

    // Use this for initialization
    private void Start()
    {
        DontDestroyOnLoad(gameObject); // prevent destroy objects in previous scene that has been unloaded
        //LoadGameData(ACT_ONE_QUESTIONS_FILE_NAME);
        LoadPlayerProgress();
        ReadDistanceMap();

        currentAct = 0;
        actFiles = ReadActFileNames(ACT_FILE_PATH);

        LoadRoundData(actFiles[currentAct]);
        
        SceneManager.LoadScene(MENU_SCREEN);
    }

    private void StartCurrentAct()
    {
        LoadRoundData(actFiles[currentAct]);
        Initiate.Fade ("Game", Color.black, 0.8f);
    }

    private static string[] ReadActFileNames(string fileName)
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, fileName);

        if (File.Exists(filePath))
        {
            var dataAsJson = File.ReadAllText(filePath);
            var actFileFromJson = JsonUtility.FromJson<ActFile>(dataAsJson);

            return actFileFromJson.actFileNames;
        }

        Debug.LogError("GetActFileNames: cannot read data!");
        return null;
    }

    public RoundData GetCurrentRoundData()
    {
        return currentRound;
    }

    public void SubmitNewPlayerScore(float newScore)
    {
        if (!(newScore > playerProgress.highestScore)) return;
        
        playerProgress.highestScore = newScore;
        SavePlayerProgress();
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

    public static void StartFER()
    {
        var filePath = Path.Combine(Application.streamingAssetsPath, FER_FLAG_FILE_NAME);

        if (File.Exists(filePath))
            File.WriteAllText(filePath, RECORD_EXPRESSION);
        else
            Debug.LogError("FER flag does not exist!");
    }

    public static void StopFER()
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

    private void LoadBgms()
    {
        if (currentRound == null)
        {
            Debug.LogError("current round is null!");
            return;
        }

        currentRound.bgmHappyClip = LoadAudioFile(currentRound.bgmHappyFile);
        currentRound.bgmSadScaredClip = LoadAudioFile(currentRound.bgmSadScaredFile);
        currentRound.bgmNeutralClip = LoadAudioFile(currentRound.bgmNeutralFile);
    }

    public static AudioClip LoadAudioFile(string relativeResourcePath)
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

    public static EmotionData[] ReadPlayerEmotion()
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

    public static Texture2D LoadImage(string fileName)
    {
        Texture2D tex = null;
        byte[] fileData;

        var filePath =
            Path.Combine(Application.streamingAssetsPath,
                fileName); // streamingAssetsPath is the folder that stores the json

        if (!File.Exists(filePath)) return null;
        
        fileData = File.ReadAllBytes(filePath);
        tex = new Texture2D(2, 2, TextureFormat.DXT1, false);
        tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.

        return tex;
    }

    public void StartNextAct()
    {
        currentAct++;
        if (currentAct > actFiles.Length)
        {
            LoadGameOverScreen();
            return;
        }
        
        StartCurrentAct();
    }

    private void LoadGameOverScreen()
    {
        throw new System.NotImplementedException();
    }
}