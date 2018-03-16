using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreenController : MonoBehaviour
{
    public void StartGame()
    {
		Initiate.Fade ("Game", Color.black, 0.8f);
        //SceneManager.LoadScene("Game");
    }


}