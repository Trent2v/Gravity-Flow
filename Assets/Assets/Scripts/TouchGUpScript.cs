﻿using UnityEngine;
using System.Collections;

public class TouchGUpScript : MonoBehaviour {


	// Update is called once per frame
	public void OnTouchBegan () {


			Physics.gravity = new Vector3 (0, 17f, 0);

	}

}
