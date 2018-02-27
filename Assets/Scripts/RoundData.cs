using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoundData {

	public float suspicionLevel = 0;
	public QuestionData[] questions;
	public SequenceData[] sequence;
	public string responsesPath;
	public DetectiveResponses detectiveResponses;
}
