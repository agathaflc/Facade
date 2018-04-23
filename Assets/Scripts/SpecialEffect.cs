using System;
using UnityEngine;

[Serializable]
public class SpecialEffect
{
    public string type; // "vignette", "bloom", or "motionBlur"
    
    public string status; // only for motion blur cos everything else can be indicated by intensity
    
    public float intensity; // for bloom (0~infinity (i'd say max 8)), vignette (0~1)

    // for motion blur
    public float shutterAngle; // 0~360
    
    // for vignette
    public Color color;
    public float colorR;
    public float colorG;
    public float colorB;
    public float colorA;
    public float smoothness; // 0~1
    public float roundness; // 0~1
}