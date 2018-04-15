using UnityEngine;

public class GunCollider : MonoBehaviour
{
	public GameController GameController;
	public Material ShaderMaterial;
	private Material OriginalMaterial;

	private void Start()
	{
		OriginalMaterial = GetComponent<Renderer>().material;
	}

	private void OnMouseDown()
	{
		GameController.EndingScene(true);
	}

	private void OnMouseOver()
	{
		GetComponent<Renderer>().material = ShaderMaterial;
	}

	private void OnMouseExit()
	{
		GetComponent<Renderer>().material = OriginalMaterial;
	}
}
