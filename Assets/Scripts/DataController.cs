using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class DataController : MonoBehaviour {

	public RoundData[] allRoundData;

	private PlayerProgress playerProgress;

	// Use this for initialization
	void Start () {
		DontDestroyOnLoad (gameObject); // prevent destroy objects in previous scene that has been unloaded
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
}
