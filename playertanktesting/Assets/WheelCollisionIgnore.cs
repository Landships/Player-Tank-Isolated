using UnityEngine;
using System.Collections;

public class WheelCollisionIgnore : MonoBehaviour {
    public GameObject tank;
	// Use this for initialization
	void Start () {
        Physics.IgnoreCollision(this.gameObject.GetComponent<Collider>(), tank.GetComponent<Collider>());
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
