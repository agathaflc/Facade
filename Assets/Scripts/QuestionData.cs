using System;

[Serializable]
public class QuestionData
{
    public AnswerData[] answers;
    public bool considersEmotion;
    public bool considersFact;
    public float consistencyWeight;
    public float expressionWeight;
    public string pictureFileName;
    public string questionDesc;
    public string questionId;
    public string questionText;
    public int timeLimitInSeconds = 10;
    public string filePath;
    public SpecialEffect[] effects;
    public bool hasLightingEffect;
    public LightingEffect lighting = null;
    
    public bool playCustomBgm;
    public bool dontAdaptLighting;
    public AudioData bgm;
}