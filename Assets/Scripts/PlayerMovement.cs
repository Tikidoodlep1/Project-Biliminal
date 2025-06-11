using System;
using UnityEngine;
using UnityEngine.InputSystem;

//MonoBehaviour is the base class that every Unity object extends and uses. This is important to have if your script is interacting with Unity objects.
//If your script is a management script like a state machine of some kind, and doesn't modify or interact with Unity objects, you don't need the extension.
//https://developer.mozilla.org/en-US/docs/Glossary/State_machine
public class PlayerMovement : MonoBehaviour
{
	[SerializeField]
	int jumpForce = 15;
	[SerializeField]
	float playerWalkForce = 10f;

	bool jumpQueued = false;
	InputAction playerMovementAction; //Look into https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html
	Rigidbody player; //A Rigidbody object - This is the thing that makes the player have friction and gravity - It's given a Mass and Drag to simulate Physics with
	Vector3 playerMovementVector = new Vector3(0.0f, 0.0f, 0.0f); //A 3 dimensional vector in charge of recording player input in a format that makes sense in a 3 dimensional space
	InputAction playerJump; //Same as playerMovementVector, check Edit -> Project Settings -> Input System Package in the Unity Editor

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		//InputSystem.actions.FindAction(string ActionName, bool throwErrorOnNotFound) helps get player input in the different formats -
		//This is just binding the action to the action object
		playerMovementAction = InputSystem.actions.FindAction("Player/Move", true);

		//for this.gameObject check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component-gameObject.html
		//for GetComponent<>() check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component.GetComponent.html
		player = this.gameObject.GetComponent<Rigidbody>();
		playerJump = InputSystem.actions.FindAction("Player/Jump", true);
	}

	private void Update()
	{
		//We need to check if the player jumped within Update() rather than FixedUpdate(). This is because the method WasPerformedThisFrame() is frame based and not physics update based,
		//so sometimes our jump wouldn't jump because Update() runs more frequently than FixedUpdate(), where the input could be missed.
		//Because of this, we use a boolean flag (jumpQueued) to perform the physics within FixedUpdate().

		//Check https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/RespondingToActions.html
		if (playerJump.WasPerformedThisFrame() && isGrounded()) {
			jumpQueued = true;
		}
	}

	// FixedUpdate is called 50 times per second
	// ADDED NOTE FROM ALEX: FixedUpdate() is the physics update method. It runs a specified number of times per second, regardless of player FPS.
	// Update(), which every new script file starts with, is the rendering update method. That should only be used for things that are dependent on player FPS, like visuals.
	void FixedUpdate()
    {
		//Some notes first - playerMovementVector is a 2 dimensional Vector that records an x and y value for player input. In a 3d game our forward and backward movement is the z axis,
		//while vertical movement is the y axis. The x value represents left and right movement, while the y value represents movement forwards and backwards.
		//This is why we read the x value into x and the y value into z. 

		//playerMovementAction will only ever contain 1,0, or -1. player.transform.forward and player.transform.right are relative vectors from the player transform, and playerWalkForce
		//contains the player speed. This is the formula that gets us the corresponding vector that the player moves in.
		playerMovementVector = (player.transform.forward * playerMovementAction.ReadValue<Vector2>().y * playerWalkForce) + (player.transform.right * playerMovementAction.ReadValue<Vector2>().x * playerWalkForce);
		
		if (jumpQueued)
		{
			//Add a vertical force to our movement if we need to jump then set our flag to false.
			//playerMovementVector = playerMovementVector + (player.transform.up * jumpForce);

			//Doing jump force separately to use the VelocityChange force mode
			player.AddForce(player.transform.up * jumpForce, ForceMode.VelocityChange);
			jumpQueued = false;
		}

		//If our x and z axis are both 0, no need to set anything, so culling the excess operations. This is simply a minor optimization.
		if (playerMovementVector != Vector3.zero) {
			//Adding an acceleration force to the player for walking. We use acceleration because acceleration doesn't take mass into account.
			//Check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rigidbody.AddForce.html
			player.AddForce(playerMovementVector, ForceMode.Acceleration);
		}
    }
	
	private bool isGrounded() {
		//Debug lines to visually show the ray we're checking
		if(Physics.Raycast(player.transform.position, player.transform.up * -1, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)) {
			Debug.DrawRay(player.transform.position, player.transform.up * -1, Color.green, 1f);
		}else {
			Debug.DrawRay(player.transform.position, player.transform.up * -1, Color.red, 1f);
		}
		return player.linearVelocity.y == 0 || Physics.Raycast(player.transform.position, player.transform.up * -1, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
	}
}
