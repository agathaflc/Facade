using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class QuestionData {

	public string questionDesc;
	public string questionId;
	public float expressionWeight; 
	public float consistencyWeight;
	public bool considersEmotion;
	public bool considersFact;
	public QVariationData[] variations;
	public int timeLimitInSeconds = 10;
}
