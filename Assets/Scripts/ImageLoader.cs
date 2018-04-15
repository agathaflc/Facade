using UnityEngine;
using UnityEngine.UI;

public class ImageLoader : MonoBehaviour
{
    private Material material;

    // Use this for initialization
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
    }

    public void LoadImage(Texture2D tex)
    {
        material = GetComponent<Image>().material;
        material.mainTexture = tex;
    }

    public void DestroyMaterial()
    {
        if (material != null) Destroy(material.mainTexture);
    }
}