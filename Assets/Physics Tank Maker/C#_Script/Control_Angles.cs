using UnityEngine;
using System.Collections;

public class Control_Angles : MonoBehaviour {

	public GameObject left_lever;
	public GameObject right_lever;
	public GameObject hori_crank;
	public GameObject vert_crank;

	HingeJoint left_lever_joint;
	HingeJoint right_lever_joint;
	HingeJoint hori_crank_joint;
	HingeJoint vert_crank_joint;

	Transform left_lever_transform;
	Transform right_lever_transform;
	Transform hori_crank_transform;
	Transform vert_crank_transform;

	void Start() { 
		left_lever_joint = left_lever.GetComponent<HingeJoint> ();
		right_lever_joint = right_lever.GetComponent<HingeJoint> ();
		hori_crank_joint = hori_crank.GetComponent<HingeJoint> ();
		vert_crank_joint = vert_crank.GetComponent<HingeJoint> ();

		left_lever_transform = left_lever.GetComponent<Transform> ();
		right_lever_transform = right_lever.GetComponent<Transform> ();
		hori_crank_transform = hori_crank.GetComponent<Transform> ();
		vert_crank_transform = vert_crank.GetComponent<Transform> ();
	}

	/*void Update() {
		Debug.Log (GetLeftCrankAngle ());
		Debug.Log (GetRightCrankAngle ());
		Debug.Log (GetLeftLeverAngle ());
		Debug.Log (GetRightLeverAngle ());
	}*/

	public float GetLeftLeverAngle() {
		return left_lever_joint.angle;
	}

	public float GetRightLeverAngle() {
		return right_lever_joint.angle;
	}

	public float GetLeftCrankAngle() {
		return hori_crank_joint.angle;
	}

	public float GetRightCrankAngle() {
		return vert_crank_joint.angle;
	}

	public void SetLeftLeverAngle(float localX) {
		left_lever_transform.localEulerAngles = new Vector3 (localX, left_lever_transform.localEulerAngles.y, left_lever_transform.localEulerAngles.z);
	}

	public void SetRightLeverAngle(float localX) {
		right_lever_transform.localEulerAngles = new Vector3 (localX, right_lever_transform.localEulerAngles.y, right_lever_transform.localEulerAngles.z);
	}

	public void SetLeftCrankAngle(float localX) {
		hori_crank_transform.localEulerAngles = new Vector3 (localX, hori_crank_transform.localEulerAngles.y, hori_crank_transform.localEulerAngles.z);
	}

	public void SetRightCrankAngle(float localX) {
		vert_crank_transform.localEulerAngles = new Vector3 (localX, vert_crank_transform.localEulerAngles.y, vert_crank_transform.localEulerAngles.z);
	}


}
