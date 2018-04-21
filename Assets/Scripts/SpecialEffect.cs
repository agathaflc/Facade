using System;
using UnityEngine;

[Serializable]
public class SpecialEffect
{
    public string type; // "vignette", "bloom", or "motionBlur"
    public float intensity; // for bloom and vignette

    // specifically for vignette
    public Color color;
    public float colorR;
    public float colorG;
    public float colorB;
    public float colorA;
    public float smoothness;
    public float roundness;
}