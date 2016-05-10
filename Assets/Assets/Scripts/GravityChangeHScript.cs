using UnityEngine;
using System.Collections;

public class GravityChangeHScript : MonoBehaviour {

	void Update () {
		
		
		if (Input.GetButtonDown("Jump")) {
			Physics.gravity = new Vector3(13f, 0, 0);}
		
		if (Input.GetButtonDown("Fire2")) {
			Physics.gravity = new Vector3(-13f, 0, 0);}
		
	}
}
