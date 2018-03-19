using System;
using UnityEngine;

[Serializable]
public class RoundData
{
    public AudioClip bgmHappyClip;
    public AudioClip bgmNeutralClip;
    public AudioClip bgmSadScaredClip;
    public AudioClip bgmAngrySurprisedClip;

    public string bgmHappyFile;
    public string bgmNeutralFile;
    public string bgmSadScaredFile;
    public string bgmAngrySurprisedFile;

    public DetectiveResponses detectiveResponses;
    public string responsesPath;
    public SequenceData[] sequence;
    public float suspicionLevel = 0;
}