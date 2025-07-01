using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

//MonoBehaviour is the base class that every Unity object extends and uses. This is important to have if your script is interacting with Unity objects.
//If your script is a management script like a state machine of some kind, and doesn't modify or interact with Unity objects, you don't need the extension.
//https://developer.mozilla.org/en-US/docs/Glossary/State_machine
public class PlayerMovement : MonoBehaviour
{
	[SerializeField]
	float jumpForce = 300f;
	[SerializeField]
	float jumpCooldown = 0.6f;
	[SerializeField]
	float playerWalkForce = 1200f;
	[SerializeField]
	float maxPlayerSpeed = 5f;
	[SerializeField]
	float airControl = 0.05f;
	[SerializeField]
	float maxWalkableSlope = 45f;
	[SerializeField]
	float minSlopeAngle = 5f;
	[SerializeField]
	float playerSpeedClampMultiplier = 2.0f;

	bool canJump = true;
	bool jumpQueued = false;
	InputAction playerMovementAction; //Look into https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html
	Rigidbody player; //A Rigidbody object - This is the thing that makes the player have friction and gravity - It's given a Mass and Drag to simulate Physics with
	Animator animator;
	Vector2 playerInput;
	Vector3 playerMovementVector; //A 3 dimensional vector in charge of recording player input in a format that makes sense in a 3 dimensional space
	Vector3 groundNormal;
	InputAction playerJump; //Same as playerMovementVector, check Edit -> Project Settings -> Input System Package in the Unity Editor
	Vector3 clampVector;
	float stoppingPower = 0f;
	Vector3 playerLeftFrontOffset;
	Vector3 playerLeftBackOffset;
	Vector3 playerRightFrontOffset;
	Vector3 playerRightBackOffset;
	Vector3 playerForwardOffset;
	Vector3 playerBackwardOffset;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		//InputSystem.actions.FindAction(string ActionName, bool throwErrorOnNotFound) helps get player input in the different formats -
		//This is just binding the action to the action object
		playerMovementAction = InputSystem.actions.FindAction("Player/Move", true);

		//for this.gameObject check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component-gameObject.html
		//for GetComponent<>() check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component.GetComponent.html
		player = this.gameObject.GetComponent<Rigidbody>();
		animator = GameObject.Find("Player/PlayerBody/Walking").GetComponent<Animator>();
		playerJump = InputSystem.actions.FindAction("Player/Jump", true);
	}

	private void Update()
	{
		//We need to check if the player jumped within Update() rather than FixedUpdate(). This is because the method WasPerformedThisFrame() is frame based and not physics update based,
		//so sometimes our jump wouldn't jump because Update() runs more frequently than FixedUpdate(), where the input could be missed.
		//Because of this, we use a boolean flag (jumpQueued) to perform the physics within FixedUpdate().

		//Check https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/RespondingToActions.html
		if (canJump && playerJump.WasPerformedThisFrame() && isGrounded()) {
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

		playerInput = playerMovementAction.ReadValue<Vector2>();

		//playerMovementAction will only ever contain 1,0, or -1. player.transform.forward and player.transform.right are relative vectors from the player transform, and playerWalkForce
		//contains the player speed. This is the formula that gets us the corresponding vector that the player moves in.
		playerMovementVector = (player.transform.forward * playerInput.y) + (player.transform.right * playerInput.x);
		
		if (jumpQueued)
		{
			//Add a vertical force to our movement if we need to jump then set our flag to false.
			//playerMovementVector = playerMovementVector + (player.transform.up * jumpForce);

			//Doing jump force separately to use the Impulse force mode
			player.AddForce(player.transform.up * jumpForce, ForceMode.Impulse);
			jumpQueued = false;
			canJump = false;
			StartCoroutine(JumpCooldown());
		}

		//If our x and z axis are both 0, no need to set anything, so culling the excess operations. This is simply a minor optimization.
		if (playerMovementVector != Vector3.zero) {
			//Adding an acceleration force to the player for walking. We use acceleration because acceleration doesn't take mass into account.
			//Check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rigidbody.AddForce.html

			//Here, we check if the player is grounded or not. If they aren't, we want to make them unable to fully control their movement because they're airborn.
			//To do this, we just multiply our movement vector by our airControl variable, which is set to 0.05 by default. This means that the player has 0.5% of their normal movement in the air.
			if(!isGrounded()) {
				player.AddForce(playerMovementVector * airControl, ForceMode.Force);
			}else {
				//If the player IS grounded, we want to make slopes feel more natural to walk on by projecting the player movement vector onto that plane.
				//To explain it in less technical terms, make the player movement vector parallel to the ground.
				//We also set a maxWalkableSlope. If the slope is above that angle, we don't want to make the player movement vector parallel to the ground,
				//otherwise they could climb walls and stuff by walking into it.
				groundNormal = getGroundNormal();
				if(Vector3.Angle(groundNormal, Vector3.up) <= maxWalkableSlope && Vector3.Angle(groundNormal, Vector3.up) > minSlopeAngle) {
					playerMovementVector = Vector3.ProjectOnPlane(playerMovementVector, groundNormal).normalized;
					//player.AddForce(groundNormal * (9.81f/2), ForceMode.Acceleration);
				}

				playerMovementVector = playerMovementVector.normalized * playerWalkForce;
				player.AddForce(playerMovementVector, ForceMode.Force);
			}
		}

		//Clamping the Player Speed to a maximum speed.
		//To do this, we get the horizontal player velocity. We set the y field to 0 because otherwise the magnitude of the y axis could be greater than our x and z,
		//clamping our horizonal movement based on vertical movement.
		Vector3 horizontal = player.linearVelocity;
		horizontal.y = 0f;
		//We use the magnitude because it's just the length of the vector - or the rate at which the player is moving. We check that against maxPlayerSpeed to see if we should clamp speed.
		if (horizontal.magnitude > maxPlayerSpeed)
		{
			//We create a Vector3 equal to the opposite of the players linear velocity, then normalize it. We normalize it because we want the direction, but NOT the speed from the player.
			//We set stoppingPower equal to the magnitude (or the speed) of the player minus the maximum speed our player should be going. This gives us the amount we need to slow the player by.
			clampVector = -horizontal.normalized;
			stoppingPower = horizontal.magnitude - maxPlayerSpeed;

			//We add our force in the direction of clampVector using the speed of stoppingPower and apply it as a VelocityChange so we don't use the mass of the player.
			player.AddForce(clampVector * stoppingPower * playerSpeedClampMultiplier, ForceMode.Acceleration);
		}

		animator.SetBool("acending", player.linearVelocity.y > 0.3);
		animator.SetFloat("horizVelocity", Mathf.Abs(player.linearVelocity.x) + Mathf.Abs(player.linearVelocity.z));
	}
	
	private bool isGrounded() {
		RaycastHit hit;
		//Debug lines to visually show the ray we're checking
		if (Physics.SphereCast(player.transform.position + (player.transform.up * 0.5f), 0.45f, player.transform.up * -1f, out hit, 0.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
		{
			Debug.DrawRay(player.transform.position + (player.transform.up * 0.5f), player.transform.up * -1, Color.green, 0.5f);
		}
		else
		{
			Debug.DrawRay(player.transform.position + (player.transform.up * 0.5f), player.transform.up * -1, Color.red, 0.5f);
		}

		return player.linearVelocity.y == 0 || Physics.SphereCast(player.transform.position + (player.transform.up * 0.5f), 0.45f, player.transform.up * -1f, out hit, 0.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
	}

	//IEnumerator uses the 'Using' System.Collections and is used to essentially loop over something without blocking the active thread.
	//If you have questions about Thread Blocking or Threads in general, feel free to ask Alex as they can get confusing.
	//the 'yield' keyword is used to tell the code to execute the returned method, runnable, etc. until it's completed. After that, the rest of the method runs.
	private IEnumerator JumpCooldown() {
		yield return new WaitForSeconds(jumpCooldown);
		canJump = true;
	}

	//This function gets the normal vector of a surface - This is a Vector3 that points perpendicular to the plane it's on, so a ground normal vector would come back as something like [0, 1, 0] if it's flat.
	private Vector3 getGroundNormal() {

		playerLeftFrontOffset = -(player.transform.right) + (player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);
		playerLeftBackOffset = -(player.transform.right) + -(player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);
		playerRightFrontOffset = (player.transform.right) + (player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);
		playerRightBackOffset = (player.transform.right) + -(player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);
		playerForwardOffset = (player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);
		playerBackwardOffset = -(player.transform.forward) + player.transform.position + (player.transform.up * 0.25f);

		RaycastHit leftFront;
		RaycastHit leftBack;
		RaycastHit rightFront;
		RaycastHit rightBack;
		RaycastHit front;
		RaycastHit back;

		bool rayLF = Physics.Raycast(playerLeftFrontOffset, Vector3.down, out leftFront, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
		bool rayLB = Physics.Raycast(playerLeftBackOffset, Vector3.down, out leftBack, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
		bool rayRF = Physics.Raycast(playerRightFrontOffset, Vector3.down, out rightFront, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
		bool rayRB = Physics.Raycast(playerRightBackOffset, Vector3.down, out rightBack, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
		bool rayF = Physics.Raycast(playerForwardOffset, Vector3.down, out front, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
		bool rayB = Physics.Raycast(playerBackwardOffset, Vector3.down, out back, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);

		//Debug.DrawLine(leftFront.normal + player.transform.position, leftBack.normal, Color.cyan, 0.5f);
		//Debug.DrawLine(leftFront.normal + player.transform.position, rightFront.normal, Color.cyan, 0.5f);
		//Debug.DrawLine(rightFront.normal + player.transform.position, rightBack.normal, Color.cyan, 0.5f);
		//Debug.DrawLine(leftBack.normal + player.transform.position, rightBack.normal, Color.cyan, 0.5f);

		int avg = 0;
		Vector3 normalAverage = Vector3.zero;

		if(rayLF) {
			normalAverage += leftFront.normal;
			avg++;
			Debug.DrawRay(playerLeftFrontOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (rayLB)
		{
			normalAverage += leftBack.normal;
			avg++;
			Debug.DrawRay(playerLeftBackOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (rayRF)
		{
			normalAverage += rightFront.normal;
			avg++;
			Debug.DrawRay(playerRightFrontOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (rayRB)
		{
			normalAverage += rightBack.normal;
			avg++;
			Debug.DrawRay(playerRightBackOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (rayF)
		{
			normalAverage += front.normal;
			avg++;
			Debug.DrawRay(playerForwardOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (rayB)
		{
			normalAverage += back.normal;
			avg++;
			Debug.DrawRay(playerBackwardOffset, Vector3.down, Color.cyan, 0.5f);
		}

		if (normalAverage != Vector3.zero)
		{
			Debug.Log((normalAverage / avg) + ", " + Vector3.Angle(normalAverage / avg, Vector3.up) + " === left front:" + leftFront.normal + " === right front:" + rightFront.normal + " === left back:" + leftBack.normal + " === right back:" + rightBack.normal + " === front:" + front.normal + " === back:" + back.normal);
			return normalAverage / avg;
		}

		//RaycastHit hit;
		////Shoot a ray down, and if it hits something (a face or surface on the "Default" Unity Layer), then return the normal vector of the surface.
		//if (Physics.SphereCast(player.transform.position + (player.transform.up * 0.1f), 0.45f, Vector3.down, out hit, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
		//{
		//	return hit.normal;
		//}

		return Vector3.up;
	}
}
