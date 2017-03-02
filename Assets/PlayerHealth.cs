﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class PlayerHealth : MonoBehaviour {

	public int HP = 100;
	public int currentHP;
	public Slider healthSlider;
	public Image hurtImage;
	public Color flashColor = new Color(1f, 0f, 0f, 0.1f);
	public float flashSpeed = 5f;
	public bool hurt = false;
	public bool dead = false;

	public GameObject Canvas;
	private GameObject can;
	private Canvas currentCanvas;


	//	private GameObject sl;
	//	private Slider currentSlider;

	// Use this for initialization
	void Start () {
		currentHP = HP;
//		healthSlider = GameObject.Find ("HUDCanvas/healthGUI/HealthSlider");
//		hurtImage = GameObject.Find ("HUDCanvas/healthGUI/DamageImage");
	}

	// Update is called once per frame
	void Update () {
//		if (hurt) {
//			hurtImage.color = flashColor;
//		} else {
//			hurtImage.color = Color.Lerp (hurtImage.color, Color.clear, flashSpeed * Time.deltaTime);
//		} 
		hurt = false;
	}

	public void Hurt(int damage) {
		hurt = true;
		currentHP -= damage;
		//healthSlider.value = currentHP;
		if (!dead) {
			Death ();
		}
	}

	public void Death() {
		if (HP <= 0) {
			dead = true;
			Destroy (gameObject);
		}
	}
}
