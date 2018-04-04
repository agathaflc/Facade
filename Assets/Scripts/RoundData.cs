﻿using System;
using UnityEngine;

[Serializable]
public class RoundData
{
    public AudioClip bgmHappyClip;
    public AudioClip bgmNeutralClip;
    public AudioClip bgmSadScaredClip;
    public AudioClip bgmAngrySurprisedClip;

    public AudioData bgmHappyFile;
    public AudioData bgmNeutralFile;
    public AudioData bgmSadScaredFile;
    public AudioData bgmAngrySurprisedFile;

    public DetectiveResponses detectiveResponses;
    public string responsesPath;
    public SequenceData[] sequence;
    public float suspicionLevel = 0;
}