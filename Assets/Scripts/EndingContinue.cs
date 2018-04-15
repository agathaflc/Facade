using UnityEngine;
using UnityEngine.UI;

public class EndingContinue : MonoBehaviour {

	public GameObject endreport;
	public GameObject report;

	public void StartScreen(){
		DataController.ResetPlayerData();
		Initiate.Fade ("MenuScreen", Color.black, 0.8f);
	}
	public void PostReport(){
		string finalreport = DataController.Getfinalreport();
		report.GetComponent<Text>().text = finalreport;
		endreport.SetActive (true);
	}

}
