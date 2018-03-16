using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    public float mouseSensitively;
    public Transform playerBody;

    private float xAxisClamp;
    private float yAxisClamp = 90;

    private void Awake()
    {
        //Awake called once per game object, problem is that minimise and maximise, will dealocate it
        //Put it in Update function otherwise
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    private void Update()
    {
        RotateCamera();
    }

    private void RotateCamera()
    {
        var mouseX = Input.GetAxis("Mouse X");
        var mouseY = Input.GetAxis("Mouse Y");

        var rotAmountX = mouseX * mouseSensitively;
        var rotAmountY = mouseY * mouseSensitively;

        xAxisClamp -= rotAmountY; // add the rotation amount to determine where the angle is pointing
        yAxisClamp += rotAmountX;

        var targetRotCam = transform.rotation.eulerAngles; //current rotation of game object camera
        var targetRotBody = playerBody.rotation.eulerAngles; //current rotation of game object player


        targetRotCam.x -= rotAmountY; //rotate playerCamera
        targetRotBody.y += rotAmountX; //rotate playerBody, player cam is paired to player body

        targetRotCam.z = 0; //clamping z axis of camera to prevent complete rotation upside down

        //conversion between euler and quaternion angle values, 90 is looking down, -90/270 is looking up
        if (xAxisClamp > 30)
        {
            xAxisClamp = targetRotCam.x = 30;
        }
        else if (xAxisClamp < -30)
        {
            xAxisClamp = -30;
            targetRotCam.x = -30;
        }

        if (yAxisClamp > 135)
            yAxisClamp = targetRotBody.y = 135;
        else if (yAxisClamp < 45) yAxisClamp = targetRotBody.y = 45;

        transform.rotation = Quaternion.Euler(targetRotCam); //assign back new values
        playerBody.rotation = Quaternion.Euler(targetRotBody); //assign back new values
    }
}