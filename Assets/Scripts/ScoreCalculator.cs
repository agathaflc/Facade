using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ScoreCalculator
{
    private const string EMOTION_NEUTRAL = "neutral";

    private const int CORRECT_ANSWER_MULTIPLIER = -1;
    private const int WRONG_ANSWER_MULTIPLIER = 1;
    private const int CORRECT_EXPRESSION_MULTIPLIER = -1;
    private const int WRONG_EXPRESSION_MULTIPLIER = 1;

    private static float ComputeDistanceBetweenTwoPoints(float x1, float y1, float x2, float y2)
    {
        return Mathf.Sqrt(Mathf.Pow(x1 - x2, 2) + Mathf.Pow(y1 - y2, 2));
    }

    public static float ComputeEmotionDistance(DistanceData distanceMap, string[] expected, EmotionData[] actual,
        out string closestEmotion)
    {
        var observedEmotionvector = new List<EmotionMapping>();

        if (actual == null || actual.Length == 0)
            observedEmotionvector.Add(distanceMap.emotions.FirstOrDefault(e => e.type.Equals(EMOTION_NEUTRAL)));
        else
            for (var i = 0; i < actual.Length; i++)
                if (actual[i].emotionScore > 0)
                {
                    Debug.Log(
                        "non zero emotion: " + actual[i].emotion + ", score: " + actual[i].emotionScore);
                    Debug.Log("distancemap: " + distanceMap);
                    var raw = distanceMap.emotions.FirstOrDefault(e => e.type.Equals(actual[i].emotion));
                    var scaled = new EmotionMapping();

                    scaled.x = raw.x * actual[i].emotionScore;
                    scaled.y = raw.y * actual[i].emotionScore;

                    observedEmotionvector.Add(scaled);
                }

        var minDistance = 9999f;
        EmotionMapping expectedMapping;

        var closestEmotionIndex = 0;
        // if multiple expressions are accepted, take the closer one
        for (var i = 0; i < expected.Length; i++)
        for (var j = 0; j < observedEmotionvector.Count; j++)
        {
            expectedMapping = distanceMap.emotions.FirstOrDefault(e => e.type.Equals(expected[i]));
            var currentDistance = ComputeDistanceBetweenTwoPoints(
                observedEmotionvector[j].x,
                observedEmotionvector[j].y,
                expectedMapping.x,
                expectedMapping.y);

            if (currentDistance < minDistance)
            {
                minDistance = currentDistance;
                closestEmotionIndex = i;
            }
        }

        closestEmotion = expected[closestEmotionIndex];

        return minDistance;
    }

    public static float CalculateConsistencyScore(bool consistent, float weight)
    {
        if (consistent)
            return CORRECT_ANSWER_MULTIPLIER * weight;
        return WRONG_ANSWER_MULTIPLIER * weight;
    }

    /**
     * threshold:
     * 	0-0.5 = 3 pts	0.5-1 = 2 pts	1-1.5 = 1 pt
     * 	1.5-2 = 0 pt
     * 	2-2.5 = -1 pt	2.5-3 = -2 pts	> 3 = -3 pts
     **/
    public static float CalculateExpressionScore(float rawDistance, float weight)
    {
        if (rawDistance <= 0.5)
            return 3f * CORRECT_EXPRESSION_MULTIPLIER * weight;
        if (rawDistance <= 1)
            return 2f * CORRECT_EXPRESSION_MULTIPLIER * weight;
        if (rawDistance <= 1.5)
            return 1f * CORRECT_EXPRESSION_MULTIPLIER * weight;
        if (rawDistance <= 2)
            return 0f * weight;
        if (rawDistance <= 2.5)
            return 1f * WRONG_EXPRESSION_MULTIPLIER * weight;
        if (rawDistance <= 3)
            return 2f * WRONG_EXPRESSION_MULTIPLIER * weight;
        return 3f * WRONG_EXPRESSION_MULTIPLIER * weight;
    }
}