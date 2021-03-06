using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

//This was built using the Unity multiplayer networking API
//Documentation for the Unity multiplayer networking can be found here:
//https://unity3d.com/learn/tutorials/topics/multiplayer-networking


//Physics and movement controls were modeled on the public unity 2d project found here:
//https://www.assetstore.unity3d.com/en/#!/content/11228


public class PlayerControl : NetworkBehaviour {

	//Standard player movement constants
	private const float maxSpeed = 50f;
	private const float moveForce = 900f;
	private const float jumpForce = 5000f;
	private const float groundRadius = 0.2f; //The radius for which the player should check for ground

	//Conditions for direction faced and ability to jump
	public bool facingRight = true;

	//If some other force is moving the player
	private bool ungrounded = false; //Checks if force should be added to the player in the x direction if player is in air

	//Conditions for checking whether or not the character is grounded
	private Transform groundCheck;

	//Checks if the player is on the ground
	private bool grounded = false;

	//How long until the player can fire again
	private float nextFire;

	//Determines what player can stand on
	public LayerMask whatIsGround;

	//Stores player mouse position
	private Vector3 mousePos;

	//Stores the object of the players camera
	public Camera playerCam;

	//Keeps track of the players health bar
	private HealthBar playerHealthBar;

	//NetTracker keeps track of some extra player variables on the network
	public NetTracker netTracker;

	//Stores the gun prefab for the player
	private Gun gun;

	//Stores the main animation object for this player
	private Animator anim;

	//Used to detect if the player can jump again after jumping
	private bool jumped = false;

	//True if the player is allowed to move using inputs
	private bool canMove;

	// This method is run as soon as the script is compiled
	void Awake () {
		//Keeps the program running when not in focus
		Application.runInBackground = true;

		//Sets the main animation object for the player
		anim = GetComponent<Animator> ();

		//Makes sure the fire cooldown is zero
		nextFire = 0f;

		//Sets the player able to move
		canMove = true;

		//Gets the camera component and sets it to not active
		//This makes sure only the local players camera is active
		playerCam = GetComponentInChildren<Camera> ();
		playerCam.gameObject.SetActive (false);
	}

	//This method is called when the player object is created in the world
	//Only runs on the local client and not the server
	public override void OnStartLocalPlayer(){
		//Initializes a NetTracker object to store networked variables
		this.netTracker = GetComponent<NetTracker> ();

		//Initializes object to check if player is on the ground
		groundCheck = transform.Find("groundCheck");

		//Activates this players camera
		playerCam.gameObject.SetActive(true);
		//Links the camera to this player
		playerCam.GetComponent<CameraFollow> ().setTarget (this.gameObject);

		//Links a health bar object to this player
		playerHealthBar = GetComponent<HealthBar> ();
		playerHealthBar.setPlayer (this.gameObject);

		//Stores the local player's gun component
		gun = GetComponentInChildren<Gun> ();

		//Must force a flip call a fraction of a second after starting the game or else the client 
		//will not register the flip
		StartCoroutine (forceFlip ());

		//Colors sprites white to tell difference between  players
		foreach (var sprite in this.gameObject.GetComponentsInChildren<SpriteRenderer>()) {
			sprite.color = Color.white;
		}
	}


	//Update is called on every action or input that takes place
	void Update() {
		//Makes sure the updates are only being made on the local client
		if (!isLocalPlayer) {
			return;
		}
		//Checks if the player presses ESC to exit the game
		checkExit ();
		//Instantiates a zero vector for the mouse
		checkMouse();
		if (canMove) {
			//Checks that the mouse position is properly set
			if (!getMousePos ().Equals (Vector3.zero)) {
				//Checks if the player shoul flip orientation
				checkFlip ();
				//Sets the gun pointed towards the cursor
				setGunRotation ();
			}
			//Checks the fire input and runs the CmdFire method
			checkFire ();
			//Checks horizontal inputs and applies movements
			applyHorizontalMovement ();
			//Checks jump input and applies jump if grounded
			checkJump ();
		}
	}

	void checkMouse(){
		//Instantiate mouse position to the zero vector 
		Vector3 mousePosition = Vector3.zero;
		//Tries to get the current mouse position on the screen
		try{
			mousePosition = Input.mousePosition;
		}
		catch(Exception e){
			//If the mouse position cannot be set print an error
			print (e);
		}
		//Must set the z axis =/= 0 to use ScreenToWorldPoint
		mousePosition.z = 10;
		//Sets the current mouse position for other methods to use
		setMousePos (mousePosition);
	}

	//Sets the gun rotation facing towards the players cursor
	void setGunRotation(){
		Vector3 mousePosition = playerCam.ScreenToWorldPoint(getMousePos ());
		GameObject gunObject = this.GetComponentInChildren<Gun> ().gameObject;
		float direcY = gunObject.transform.position.y - mousePosition.y;
		float direcX = gunObject.transform.position.x - mousePosition.x;
		float angle = Mathf.Atan2(direcY, direcX) * Mathf.Rad2Deg;
		if (facingRight) {
			angle += 180;
		}
			
		if (angle > 45 && angle <= 100) {
			angle = 45;
		} else if (angle < -45 && angle >= -100) {
			angle = -45;
		}else if (angle > 225 && angle < 320) {
			angle = 320;
		}else if (angle > 90 && angle < 145) {
			angle = 145;
		}
		gunObject.transform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
	}

	//Checks what inputs are down and applies horizontal movement
	void applyHorizontalMovement(){
		float moveH = Input.GetAxisRaw ("Horizontal");

		//If the player is on the ground and there is not horizontal movement, set x axis velocity to zero
		if (!ungrounded && moveH == 0) {
			GetComponent<Rigidbody2D>().velocity = new Vector2(0, GetComponent<Rigidbody2D>().velocity.y);
		}

		//Sets the speed of the movement animation
		anim.SetFloat ("Speed", Mathf.Abs(moveH));

		//If horizontal velocity is < maxspeed
		if(moveH * GetComponent<Rigidbody2D>().velocity.x < maxSpeed)
			//Increase horizontal velocity by adding a force to player
			GetComponent<Rigidbody2D>().AddForce(Vector2.right * moveH * moveForce);

		// If horizontal velocity > maxspeed
		if(Mathf.Abs(GetComponent<Rigidbody2D>().velocity.x) > maxSpeed)
			//Set horizontal velocity to maxspeed
			GetComponent<Rigidbody2D>().velocity = new Vector2(Mathf.Sign(GetComponent<Rigidbody2D>().velocity.x) * maxSpeed, GetComponent<Rigidbody2D>().velocity.y);
		
	}

	//Checks if the player is on the ground and calls jump
	void checkJump(){
		grounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, whatIsGround);
		//Using GetButton instead of GetButtonDown
		if (Input.GetButton ("Jump") && grounded && !jumped) {
			//Runs the jump process with a coroutine so the IEnumerator can run the timer to reset jump	(inefficient)
			StartCoroutine(applyJump ());
		}
	}

	//Applies the jump force to the player
	IEnumerator applyJump(){

		//Triggers the jump animation to play
		anim.SetTrigger ("Jump");
		//Adds force vector upwards to player
		GetComponent<Rigidbody2D> ().AddForce (new Vector2 (0f, jumpForce));

		//Sets a timer so the player cannot jump again for .01s
		jumped = true;
		yield return new WaitForSeconds(0.01f);
		jumped = false;
	}

	//Checks if the the player should flip its sprite direction
	void checkFlip(){
		GameObject player = this.gameObject;
		Vector3 mouse = playerCam.ScreenToWorldPoint (getMousePos ());
		bool mouseRight = true;
		//If mouse is actually to the left of the player then set mouseRight to false
		if(mouse.x < player.transform.position.x) {
			mouseRight = false;
		}
		//If the mouse is on the wrong side of the player then flip the player
		if (mouseRight != facingRight) {
			Flip (player, mouseRight);
		}

	}
	//Tells the NetTracker component to switch the direction the player is facing
	void Flip(GameObject player, bool facing) {
		//Sets the client facing varibale
		facingRight = facing;
		//Tells the server to flip this player
		netTracker.CmdFlipSprite(this.gameObject, facingRight);
	}

	//Forces the player to flip when the game starts after .05s
	IEnumerator forceFlip(){
		yield return new WaitForSeconds(0.05f);
		checkFlip ();
	}


	//Runs the fire command if the player has pressed fire
	void checkFire(){
		if (Time.time > nextFire && Input.GetButton ("Fire1")) {
			CmdFire (this.gameObject, playerCam.ScreenToWorldPoint(getMousePos()));
			nextFire = Time.time + gun.getFireRate();
		}
	}

	//Command methods are run on the server but called on the client. This method instantiates a bullet object from the
	//referenced player's gun in the direction that their mouse is facing and uses NetworkServer.Spawn to spawn
	//the bullet on the server
	[Command]
	void CmdFire(GameObject player, Vector3 cursor){

		//Get the players gun object
		Gun cg = player.GetComponentInChildren<Gun> ();

		//Gets the position of the gunTip object
		Vector3 gunTip = cg.getGunTipPos();

		//Creates the position for the bullet to spawn from
		Vector3 bulletSpawn = gunTip;
		//Creates the direction vector on which the bullet will travel and normalizes it
		Vector3 direction = cursor - bulletSpawn;
		direction.z = 0;
		direction.Normalize();

		//Creates the bullet projectile
		var projectile = Instantiate(cg.bullet, bulletSpawn, Quaternion.identity);

		//Sets the angle of travel and rotation for the bullet
		var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
		//angle = gun.transform.eulerAngles
		projectile.transform.rotation = Quaternion.AngleAxis (angle,Vector3.forward);

		//Sets the source of the bullet to this player
		projectile.GetComponent<Bullet> ().setSource(player);

		//Sets the velocity of the bullet
		//direction = gunTip - gun.transform.position;
		projectile.GetComponent<Rigidbody2D>().velocity = (direction * cg.speed) * 10;

		//Tells the NetworkServer to spawn the bullet and keep track of it for all clients
		NetworkServer.Spawn (projectile);
	}

	public void checkExit(){
		if (Input.GetButton ("Cancel")) {
			if (isServer) {
				RpcEndGame ();
			} else {
				CmdEndGame ();
			}
		}
	}

	[Command]
	public void CmdEndGame(){
		RpcEndGame ();
	}

	[ClientRpc]
	public void RpcEndGame(){
		//Stops the client connection to the server
		NetworkLobbyManager.singleton.StopClient ();
		//Closes the network manager HUD
		NetworkLobbyManager.singleton.GetComponent<NetworkManagerHUD> ().enabled = false;
		//Loads the end game screen
		SceneManager.LoadScene (3);
	}
		
	//Sets the player unable to move
	public void setCanMove(bool canMove) {
		this.canMove = canMove;
	}

	//Sets the mouse position
	private void setMousePos(Vector3 m){
		mousePos = m;
	}

	//Returns the current position of the mouse
	public Vector3 getMousePos(){
		return(mousePos);
	}
}