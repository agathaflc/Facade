﻿using System;

[Serializable]
public class SequenceData
{
    public string filePath;
    public string sequenceType;
    public string subtitleText;
    public string effect;
    public AudioData bgm;
    public bool readExpression;
    public float scoreWeight;
    public string[] expectedExpressions;
    public QuestionData[] questions;
    
    public int animationNo;
    public int animatorLayer;

    public bool ending;
    public bool earlyFade;
}