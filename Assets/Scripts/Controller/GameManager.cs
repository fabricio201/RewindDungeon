﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LevelManager))]
public class GameManager : MonoBehaviour {
	LevelManager levelManager;

	void Awake() {
		if (GameObject.FindGameObjectsWithTag("GM").Length > 1) {
			Destroy(gameObject);
		} else {
			DontDestroyOnLoad(gameObject);
			levelManager = GetComponent<LevelManager>();
		}
	}

}