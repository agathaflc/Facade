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
}