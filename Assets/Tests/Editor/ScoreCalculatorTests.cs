using UnityEngine;
using System.IO;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Tests.Editor {
	public class ScoreCalculatorTests : IPrebuildSetup {
		private static DistanceData distanceMap;

		private const string DISTANCE_MAPPING_FILE_NAME = "distance_2d_mapping_test.json";

		public void Setup() {
			string filePath = Path.Combine (Application.streamingAssetsPath, DISTANCE_MAPPING_FILE_NAME); // streamingAssetsPath is the folder that stores the json

			if (File.Exists (filePath)) {
				string dataAsJson = File.ReadAllText (filePath);
				distanceMap = JsonUtility.FromJson<DistanceData> (dataAsJson);
			} else {
				Debug.LogError ("Cannot load distance data!");
			}
		}

		[Test]
		[PrebuildSetup(typeof(ScoreCalculatorTests))]
		public void CalculatesCorrectHorizontalDistanceBetweenPoints() {
			string[] expected = {"happy"};
			string closestEmotion;
			EmotionData[] emotionDataArray = new EmotionData[1];
			EmotionData angry = new EmotionData("angry", 1);

			emotionDataArray[0] = angry;

			Assert.AreEqual (2, ScoreCalculator.ComputeEmotionDistance(distanceMap, expected, emotionDataArray, out closestEmotion));
			Assert.AreEqual (closestEmotion, expected [0]);
		}
	}
}