using System;
using UnityEngine;

[Serializable]
public class SpecialEffect
{
    public string type; // "vignette", "bloom", or "motionBlur"
    
    public string status; // only for motion blur cos everything else can be indicated by intensity
    
    public float intensity; // for bloom (0~infinity (i'd say max 8)), vignette (0~1)
    
    // for vignette
    public Color32 color;
    public byte colorR;
    public byte colorG;
    public byte colorB;
    public byte colorA;
    public float smoothness; // 0~1
    public float roundness; // 0~1
}