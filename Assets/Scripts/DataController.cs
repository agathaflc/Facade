using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;

public class DataController : MonoBehaviour {

	private RoundData[] allRoundData;

	private PlayerProgress playerProgress;
	private string gameDataFileName = "data.json";

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (gameObject); // prevent destroy objects in previous scene that has been unloaded
		LoadGameData();
		LoadPlayerProgress();

		SceneManager.LoadScene ("MenuScreen");
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

		if (PlayerPrefs.HasKey ("highestScore")) { // if we already stored a highest score
			playerProgress.highestScore = PlayerPrefs.GetInt("highestScore");
		}
	}

	private void SavePlayerProgress() {
		PlayerPrefs.SetInt ("highestScore", playerProgress.highestScore);
	}

	private void LoadGameData(){
		string filePath = Path.Combine (Application.streamingAssetsPath, gameDataFileName); // streamingAssetsPath is the folder that stores the json

		if (File.Exists (filePath)) {
			string dataAsJson = File.ReadAllText (filePath);
			GameData loadedData = JsonUtility.FromJson<GameData> (dataAsJson); // turn from json to an object

			allRoundData = loadedData.allRoundData;
		} else {
			Debug.LogError ("Cannot load game data!");
		}
	}
}
