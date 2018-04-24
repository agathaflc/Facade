using System;

[Serializable]
public class SequenceData
{
    public string filePath;
    public string sequenceType;
    public string subtitleText;
    public bool playCustomBgm;
    public AudioData bgm;
    public bool readExpression;
    public float scoreWeight;
    public string[] expectedExpressions;
    public QuestionData[] questions;
    
    public int animationNo;
    public int animatorLayer;

    public bool ending;
    public bool earlyFade;

    public SpecialEffect[] effects;
    public bool hasLightingEffect;
    public bool usemaxBgm;
    public bool turnOnSpotlight;
    public LightingEffect lighting = null;
}