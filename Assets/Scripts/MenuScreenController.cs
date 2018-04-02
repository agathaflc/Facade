using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuScreenController : MonoBehaviour
{
    public void StartGame()
    {
		Initiate.Fade ("GameScene", Color.black, 0.8f);
        //SceneManager.LoadScene("Game");
    }


}