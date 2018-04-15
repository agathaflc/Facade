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

	public void StartScreen(){
		Initiate.Fade ("MenuScreen", Color.black, 0.8f);
	}
	public void PostReport(){
		//Initiate.Fade ("Ending1", Color.black, 0.8f);
	}

}
