using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DetectiveResponses {

	public ResponseData[] suspiciousPositive;
	public ResponseData[] suspiciousNegative;
	public ResponseData[] suspiciousNeutral;
	public ResponseData[] notSuspiciousPositive;
	public ResponseData[] notSuspiciousNegative;
	public ResponseData[] notSuspiciousNeutral;
}
