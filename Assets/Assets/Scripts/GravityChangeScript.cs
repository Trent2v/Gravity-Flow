using UnityEngine;
using System.Collections;

public class GravityChangeScript : MonoBehaviour {
		// Use this for initialization
	void Start (){
		Physics.gravity = new Vector3(0, -17F, 0);
	}

	
	// Update is called once per frame
	void Update () {


		if (Input.GetButtonDown("Jump")) {
			Physics.gravity = new Vector3(0, 17F, 0);}

		if (Input.GetButtonDown("Fire2")) {
			Physics.gravity = new Vector3(0, -17F, 0);}

	}


}
