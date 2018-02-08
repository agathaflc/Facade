using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class DataController : MonoBehaviour {

	private List<RoundData> allRoundData = new List<RoundData>();

	private PlayerProgress playerProgress;

	private const string MENU_SCREEN = "MenuScreen";
	private const string HIGHEST_SCORE_KEY = "highestScore";

	private const string ACT_ONE_QUESTIONS_FILE_NAME = "questionsACT1.json";


	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (gameObject); // prevent destroy objects in previous scene that has been unloaded
		LoadGameData(ACT_ONE_QUESTIONS_FILE_NAME);
		LoadPlayerProgress();

		SceneManager.LoadScene (MENU_SCREEN);
	}

	public RoundData GetCurrentRoundData() {
		return allRoundData [0];
	}

	public void SubmitNewPlayerScore(int newScore) {
		if (newScore > playerProgress.highestScore) {
			playerProgress.highestScore = newScore;
			SavePlayerProgress();
		}
	}

	public int GetHighestPlayerScore() {
		return playerProgress.highestScore;
	}
	
	private void LoadPlayerProgress() {
		playerProgress = new PlayerProgress ();

		if (PlayerPrefs.HasKey (HIGHEST_SCORE_KEY)) { // if we already stored a highest score
			playerProgress.highestScore = PlayerPrefs.GetInt(HIGHEST_SCORE_KEY);
		}
	}

	private void SavePlayerProgress() {
		PlayerPrefs.SetInt (HIGHEST_SCORE_KEY, playerProgress.highestScore);
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
}
