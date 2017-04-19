﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class PlayerHealth : NetworkBehaviour {

	public int HP = 100;

	[SyncVar]
	public int currentHP;

	public Image hurtImage;
	public Color flashColor = new Color(1f, 0f, 0f, 0.1f);
	public float flashSpeed = 5f;

	private bool hurt = false;
	private bool dead = false;

	private Animator anim;

	public HealthBar playerHealthBar;

	// Use this for initialization
	void Start () {
		currentHP = HP;
		playerHealthBar = this.gameObject.GetComponent<HealthBar> ();
		playerHealthBar.maxHealth = HP;
		playerHealthBar.currentHealth = currentHP;
		hurtImage = GameObject.Find ("HUDCanvas/Image").GetComponent<Image>();

		anim = GetComponent<Animator> ();
	}

	// Update is called once per frame
	void Update () {
		if (hurt) {
			hurtImage.color = flashColor;
		} else {
			hurtImage.color = Color.Lerp (hurtImage.color, Color.clear, flashSpeed * Time.deltaTime);
		} 
		hurt = false;
	}

	public void Hurt(int damage) {
		if (!isServer) {
			return;
		}
		hurt = true;
		currentHP -= damage;
		playerHealthBar.currentHealth = currentHP;
		if (!dead) {
			Death ();
		}
	}

	public void Death() {
		if (!isServer) {
			return;
		}
		if (currentHP <= 0) {
			playerHealthBar.currentHealth = 0;
			dead = true;

			anim.SetTrigger ("Death");

			//WaitForSeconds (5.0f);
			RPCDeath();
			//SceneManager.LoadScene (3);
			//NetworkServer.Destroy (gameObject);
		}
	}
	[ClientRpc]
	public void RPCDeath() {
		SceneManager.LoadScene (3);
	}
}
