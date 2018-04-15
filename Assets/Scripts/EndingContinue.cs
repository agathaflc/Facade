using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using UnityEngine.Playables;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;

public class EndingContinue : MonoBehaviour {

	public GameObject endreport;
	public GameObject report;

	public void StartScreen(){
		Initiate.Fade ("MenuScreen", Color.black, 0.8f);
	}
	public void PostReport(){
		string finalreport = DataController.Getfinalreport();
		report.GetComponent<Text>().text = finalreport;
		endreport.SetActive (true);
	}

}
