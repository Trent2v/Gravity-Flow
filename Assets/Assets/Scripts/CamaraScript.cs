using UnityEngine;
using System.Collections;

public class CamaraScript : MonoBehaviour {


	public GameObject player;

		
	void Start() {
		player = GameObject.FindGameObjectWithTag ("Player");
	}
		
	void Update() {
			
		if(player != null)
		{
			transform.position = new Vector3 (player.transform.position.x+10, 6, -20);
		}

		
		
		
	}
	
	
}
