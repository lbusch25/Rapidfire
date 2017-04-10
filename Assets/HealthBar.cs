﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class HealthBar : NetworkBehaviour{

	GUIStyle healthStyle;
	GUIStyle backStyle;

	public GameObject player;
	public float maxHealth;

	[SyncVar]
	public float currentHealth;
	[SyncVar]
	Vector3 pos;

	//Set box coordinates in world space above player then conver all to screen space at the end


//	void Start()
//	{
//		maxHealth = player.GetComponent<PlayerHealth> ().HP;
//		currentHealth = player.GetComponent<PlayerHealth> ().currentHP;
//	}

	void Update() {
		if (player.GetComponent<PlayerHealth> ().currentHP <= 0) {
			currentHealth = 0;
		} else {
			currentHealth = player.GetComponent<PlayerHealth> ().currentHP;
		}
	}


	void OnGUI()
	{
		InitStyles();

		// Draw a Health Bar

		pos = Camera.main.WorldToScreenPoint(transform.position);

		// draw health bar background
		GUI.color = Color.grey;
		GUI.backgroundColor = Color.grey;
		GUI.Box(new Rect(pos.x-26, Screen.height - pos.y - 45, maxHealth/2, 7), ".", backStyle);

		// draw health bar amount
		GUI.color = Color.green;
		GUI.backgroundColor = Color.green;
		//20 for y
		GUI.Box(new Rect(pos.x-25, Screen.height - pos.y - 46, currentHealth/2, 5), ".", healthStyle);
	}

	void InitStyles()
	{
		if( healthStyle == null )
		{
			healthStyle = new GUIStyle( GUI.skin.box );
			healthStyle.normal.background = MakeTex( 2, 2, new Color( 0f, 1f, 0f, 1.0f ) );
		}

		if( backStyle == null )
		{
			backStyle = new GUIStyle( GUI.skin.box );
			backStyle.normal.background = MakeTex( 2, 2, new Color( 0f, 0f, 0f, 1.0f ) );
		}
	}

	Texture2D MakeTex( int width, int height, Color col )
	{
		Color[] pix = new Color[width * height];
		for( int i = 0; i < pix.Length; ++i )
		{
			pix[ i ] = col;
		}
		Texture2D result = new Texture2D( width, height );
		result.SetPixels( pix );
		result.Apply();
		return result;
	}

	public void setPlayer(GameObject p) {
		this.player = p;
	}
}