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

	public string bgmPositiveFile;
	public string bgmNegativeFile;
	public string bgmNormalFile;

	public AudioClip bgmPositiveClip;
	public AudioClip bgmNegativeClip;
	public AudioClip bgmNormalClip;
}
