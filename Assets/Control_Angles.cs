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

    float vert_lower = 360;
    float vert_upper = -1800;

    float vert_degree;
    float vert_prev;

    float vert_change;


    void Start() {
        left_lever_joint = left_lever.GetComponent<HingeJoint>();
        right_lever_joint = right_lever.GetComponent<HingeJoint>();
        hori_crank_joint = hori_crank.GetComponent<HingeJoint>();
        vert_crank_joint = vert_crank.GetComponent<HingeJoint>();

        left_lever_transform = left_lever.GetComponent<Transform>();
        right_lever_transform = right_lever.GetComponent<Transform>();
        hori_crank_transform = hori_crank.GetComponent<Transform>();
        vert_crank_transform = vert_crank.GetComponent<Transform>();

        vert_degree = vert_crank_joint.angle;
        vert_prev = vert_degree;
    }

    void Update() {

        float curr = vert_crank_joint.angle;
        if (curr < 0) {
            curr = vert_lower + curr;
        }

        vert_change = Mathf.DeltaAngle(vert_prev, curr); 

        vert_prev = curr;

        vert_degree += vert_change;

        if (vert_degree >= vert_lower) {
            if (vert_change > 0) {
                vert_degree = vert_lower;
                vert_change = 0;
                return;
            }
        } else if (vert_degree <= vert_upper) {
            if (vert_change < 0) {
                vert_degree = vert_upper;
                vert_change = 0;
                return;
            }
        }
        //Debug.Log(vert_degree);


    }

    public float GetLeftLeverAngle() {
        //Debug.Log("Left Lever Angle is " + left_lever_joint.angle);
        return left_lever.transform.localRotation.eulerAngles.x;
    }

    public float GetRightLeverAngle() {
        return right_lever.transform.localRotation.eulerAngles.x;
    }

    public float GetHorCrankAngle() {

        return hori_crank.transform.localRotation.eulerAngles.x;
    }

    public float GetVertCrankAngle() {

        return vert_degree;
    }

    public float GetVertCrankDelta() {
        return vert_change;
    }

    public void SetLeftLeverAngle(float localX) {
        left_lever_transform.localEulerAngles = new Vector3(localX, left_lever_transform.localEulerAngles.y, left_lever_transform.localEulerAngles.z);
    }

    public void SetRightLeverAngle(float localX) {
        right_lever_transform.localEulerAngles = new Vector3(localX, right_lever_transform.localEulerAngles.y, right_lever_transform.localEulerAngles.z);
    }

    public void SetLeftCrankAngle(float localX) {
        hori_crank_transform.localEulerAngles = new Vector3(localX, hori_crank_transform.localEulerAngles.y, hori_crank_transform.localEulerAngles.z);
    }

    public void SetRightCrankAngle(float localX) {
        vert_crank_transform.localEulerAngles = new Vector3(localX, vert_crank_transform.localEulerAngles.y, vert_crank_transform.localEulerAngles.z);
    }


}