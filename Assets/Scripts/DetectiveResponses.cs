using System;

[Serializable]
public class DetectiveResponses
{
    public ResponseData[] clarifying;
    public ResponseData[] postClarifying;
    public ResponseData[] notSuspiciousNegative;
    public ResponseData[] notSuspiciousNeutral;
    public ResponseData[] notSuspiciousPositive;
    public ResponseData[] suspiciousNegative;
    public ResponseData[] suspiciousNeutral;
    public ResponseData[] suspiciousPositive;
}