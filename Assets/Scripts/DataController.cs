﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class DataController : MonoBehaviour {

	private List<RoundData> allRoundData = new List<RoundData>();

	private DistanceData distanceMap;

	private PlayerProgress playerProgress;

	private const string MENU_SCREEN = "MenuScreen";
	private const string HIGHEST_SCORE_KEY = "highestScore";

	private const string ACT_ONE_QUESTIONS_FILE_NAME = "questionsACT1.json";
	private const string EXPRESSION_DATA_FILE_NAME = "expression_data.json";
	private const string DISTANCEMAP_DATA_FILE_NAME = "distances.json";
	private const string DISTANCE_MAPPING_FILE_NAME = "distances_2d_mapping.json";

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (gameObject); // prevent destroy objects in previous scene that has been unloaded
		LoadGameData(ACT_ONE_QUESTIONS_FILE_NAME);
		LoadPlayerProgress();
		ReadDistanceMap ();

		SceneManager.LoadScene (MENU_SCREEN);
	}

	public RoundData GetCurrentRoundData() {
		return allRoundData [0];
	}

	public void SubmitNewPlayerScore(float newScore) {
		if (newScore > playerProgress.highestScore) {
			playerProgress.highestScore = newScore;
			SavePlayerProgress();
		}
	}

	public float GetHighestPlayerScore() {
		return playerProgress.highestScore;
	}
	
	private void LoadPlayerProgress() {
		playerProgress = new PlayerProgress ();

		if (PlayerPrefs.HasKey (HIGHEST_SCORE_KEY)) { // if we already stored a highest score
			playerProgress.highestScore = PlayerPrefs.GetFloat(HIGHEST_SCORE_KEY);
		}
	}

	private void SavePlayerProgress() {
		PlayerPrefs.SetFloat (HIGHEST_SCORE_KEY, playerProgress.highestScore);
	}

	private void LoadGameData(string fileName){
		string filePath = Path.Combine (Application.streamingAssetsPath, fileName); // streamingAssetsPath is the folder that stores the json

		if (File.Exists (filePath)) {
			string dataAsJson = File.ReadAllText (filePath);
			GameData loadedQuestions = JsonUtility.FromJson<GameData> (dataAsJson);
			RoundData roundData = new RoundData ();

			roundData.questions = loadedQuestions.questions;
			allRoundData.Add (roundData);
		} else {
			Debug.LogError ("Cannot load game data!");
		}
	}

	private void ReadDistanceMap() {
		string filePath = Path.Combine (Application.streamingAssetsPath, DISTANCE_MAPPING_FILE_NAME); // streamingAssetsPath is the folder that stores the json

		if (File.Exists (filePath)) {
			string dataAsJson = File.ReadAllText (filePath);
			distanceMap = JsonUtility.FromJson<DistanceData> (dataAsJson);
		} else {
			Debug.LogError ("Cannot load distance data!");
		}
	}

	public float ComputeEmotionDistance(string[] expected, EmotionData actual) {
		return ScoreCalculator.ComputeEmotionDistance (distanceMap, expected, actual);
	}


	public EmotionData ReadPlayerEmotion(int questionIndex) {
		string filePath = Path.Combine (Application.streamingAssetsPath, EXPRESSION_DATA_FILE_NAME); // streamingAssetsPath is the folder that stores the json

		if (File.Exists (filePath)) {
			string dataAsJson = File.ReadAllText (filePath);
			ExpressionData loadedExpressions = JsonUtility.FromJson<ExpressionData> (dataAsJson);

			if (loadedExpressions.emotions.Length < questionIndex + 1) { // index out of bounds???
				Debug.LogError("Expression data for this question does not exist!");
			}

			EmotionData correspondingEmotion = loadedExpressions.emotions [questionIndex];

			if (correspondingEmotion.questionNo == questionIndex) {
				print ("expression data loaded successfully");
				return correspondingEmotion;
			} else {
				Debug.LogError ("Question index does not match!");
			}
		} else {
			Debug.LogError ("Cannot load expression data!");
		}

		return null;
	}

	public Texture2D LoadImage(string fileName) {
		Texture2D tex = null;
		byte[] fileData;

		string filePath = Path.Combine (Application.streamingAssetsPath, fileName); // streamingAssetsPath is the folder that stores the json

		print ("Loading image: " + filePath);

		if (File.Exists(filePath)) {
			fileData = File.ReadAllBytes(filePath);
			tex = new Texture2D(2, 2, TextureFormat.DXT1, false);
			tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
		}
		return tex;
	}
}
