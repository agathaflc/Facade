using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EmotionData {

	public string emotion;
	public float emotionScore;

	public EmotionData(string e, float score) {
		emotion = e;
		emotionScore = score;
	}
}
