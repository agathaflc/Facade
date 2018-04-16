using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActData
{
    public AudioClip bgmHappyClip;
    public AudioClip bgmNeutralClip;
    public AudioClip bgmSadScaredClip;
    public AudioClip bgmAngrySurprisedClip;

    public bool useBgmLevels;
    public AudioData[] bgmLevels;
    public List<AudioClip> bgmLevelClips;

    public bool showGun;

    public AudioData bgmHappyFile;
    public AudioData bgmNeutralFile;
    public AudioData bgmSadScaredFile;
    public AudioData bgmAngrySurprisedFile;

    public DetectiveResponses detectiveResponses;
    public string responsesPath;
    public SequenceData[] sequence;
    public float suspicionLevel = 0;
}