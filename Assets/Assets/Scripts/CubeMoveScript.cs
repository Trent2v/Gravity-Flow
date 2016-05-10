using UnityEngine;
using System.Collections;

public class CubeMoveScript : MonoBehaviour {

	public float fuerza;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {

		transform.Translate(new Vector3 (0, 1, 0)*fuerza*Time.deltaTime);
	
	}
}
