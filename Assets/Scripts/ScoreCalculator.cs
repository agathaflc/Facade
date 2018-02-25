using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ScoreCalculator {
	private const string EMOTION_NEUTRAL = "neutral";

	private static float ComputeDistanceBetweenTwoPoints(float x1, float y1, float x2, float y2) {
		return Mathf.Sqrt (Mathf.Pow ((x1 - x2), 2) + Mathf.Pow ((y1 - y2), 2));
	}

	public static float ComputeEmotionDistance(DistanceData distanceMap, string[] expected, EmotionData actual) {
		// TODO implement ComputeEmotionDistance
		EmotionMapping observedEmotionvector;

		if (actual == null) {
			// assume neutral expression
			observedEmotionvector = distanceMap.emotions.FirstOrDefault (e => e.type.Equals(EMOTION_NEUTRAL));
		} else {
			observedEmotionvector = distanceMap.emotions.FirstOrDefault (e => e.type.Equals (actual.emotion));
			observedEmotionvector.x = observedEmotionvector.x * actual.emotionScore / 100.0f;
			observedEmotionvector.y = observedEmotionvector.y * actual.emotionScore / 100.0f;
		}

		float minDistance = 9999f;
		EmotionMapping expectedMapping;

		// if multiple expressions are accepted, take the closer one
		for (int i = 0; i < expected.Length; i++) {
			expectedMapping = distanceMap.emotions.FirstOrDefault (e => e.type.Equals (expected [i]));
			float currentDistance = ComputeDistanceBetweenTwoPoints (observedEmotionvector.x, observedEmotionvector.y, expectedMapping.x, expectedMapping.y);
			if (currentDistance < minDistance) {
				minDistance = currentDistance;
			}
		}

		return minDistance;
	}
}
