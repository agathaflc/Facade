using System;
using UnityEngine;

[Serializable]
public class RoundData
{
    public AudioClip bgmNegativeClip;
    public string bgmNegativeFile;
    public AudioClip bgmNormalClip;
    public string bgmNormalFile;

    public AudioClip bgmPositiveClip;

    public string bgmPositiveFile;
    public DetectiveResponses detectiveResponses;
    public QuestionData[] questions;
    public string responsesPath;
    public SequenceData[] sequence;
    public float suspicionLevel = 0;
}