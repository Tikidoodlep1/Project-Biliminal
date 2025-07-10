using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

//MonoBehaviour is the base class that every Unity object extends and uses. This is important to have if your script is interacting with Unity objects.
//If your script is a management script like a state machine of some kind, and doesn't modify or interact with Unity objects, you don't need the extension.
//https://developer.mozilla.org/en-US/docs/Glossary/State_machine
public class PlayerMovement : MonoBehaviour
{
	[SerializeField]
	public float jumpForce = 300f;
	[SerializeField]
	public float jumpCooldown = 0.6f;
	[SerializeField]
	public float acceleration = 1000f;
	[SerializeField]
	public float walkSpeed = 10f;
	[SerializeField]
	public float sprintSpeed = 15f;
	[SerializeField]
	public float airControl = 0.05f;
	[SerializeField]
	public float maxWalkableSlope = 45f;
	[SerializeField]
	public float minSlopeAngle = 5f;
	
	
	bool canJump = true;
	bool jumpQueued = false;
	InputAction playerMovementAction; //Look into https://docs.unity3d.com/Packages/com.unity.inputsystem@1.14/manual/index.html
	Rigidbody player; //A Rigidbody object - This is the thing that makes the player have friction and gravity - It's given a Mass and Drag to simulate Physics with
	Animator animator;
	Vector2 playerInput;
	//Vector3 playerMovementVector; //A 3 dimensional vector in charge of recording player input in a format that makes sense in a 3 dimensional space
	Vector3 groundNormal;
	InputAction playerJump; //Same as playerMovementVector, check Edit -> Project Settings -> Input System Package in the Unity Editor
	InputAction sprint;
	bool isRunning = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		//InputSystem.actions.FindAction(string ActionName, bool throwErrorOnNotFound) helps get player input in the different formats -
		//This is just binding the action to the action object
		playerMovementAction = InputSystem.actions.FindAction("Player/Move", true);

		//for this.gameObject check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component-gameObject.html
		//for GetComponent<>() check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Component.GetComponent.html
		player = this.gameObject.GetComponent<Rigidbody>();
		animator = this.gameObject.GetComponent<Animator>();
		playerJump = InputSystem.actions.FindAction("Player/Jump", true);
		sprint = InputSystem.actions.FindAction("Player/Sprint", true);
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

		if(sprint.IsPressed()) {
			isRunning = true;
		}else {
			isRunning = false;
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
		Vector3 playerMovementVector = ((player.transform.forward * playerInput.y) + (player.transform.right * playerInput.x)).normalized;
		
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
		if (playerMovementVector != Vector3.zero)
		{
			//Adding an acceleration force to the player for walking. We use acceleration because acceleration doesn't take mass into account.
			//Check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Rigidbody.AddForce.html

			float targetSpeed = isRunning ? sprintSpeed : walkSpeed;
			Vector3 currentVelocity = player.linearVelocity;
			currentVelocity.y = 0f;
			currentVelocity = Vector3.ProjectOnPlane(currentVelocity, Vector3.up);
			Vector3 deltaVelocity = (playerMovementVector * targetSpeed) - currentVelocity;
			float control = isGrounded() ? 1f : airControl;
			Vector3 accelerationStep = Vector3.ClampMagnitude(deltaVelocity, acceleration * Time.fixedDeltaTime);

			//If the player is grounded, we want to make slopes feel more natural to walk on by projecting the player movement vector onto that plane.
			//To explain it in less technical terms, make the player movement vector parallel to the ground.
			//We also set a maxWalkableSlope. If the slope is above that angle, we don't want to make the player movement vector parallel to the ground,
			//otherwise they could climb walls and stuff by walking into it.
			groundNormal = control == 1f ? getGroundNormal() : Vector3.up;
			if (Vector3.Angle(groundNormal, Vector3.up) <= maxWalkableSlope && Vector3.Angle(groundNormal, Vector3.up) > minSlopeAngle)
			{
				accelerationStep = Vector3.ProjectOnPlane(accelerationStep, groundNormal);
				player.AddForce(groundNormal * (9.81f * 0.5f), ForceMode.Force);
			}

			player.AddForce(accelerationStep, ForceMode.Acceleration);
		}

		Vector3 localVelocity = player.transform.InverseTransformDirection(player.linearVelocity);
		animator.SetFloat("speed", Mathf.Lerp(animator.GetFloat("speed"), player.linearVelocity.magnitude / (sprintSpeed * 0.45f), Time.fixedDeltaTime));
		animator.SetFloat("dirX", Mathf.Lerp(animator.GetFloat("dirX"), localVelocity.x / (walkSpeed * 0.45f), Time.fixedDeltaTime)); //playerInput.x
		animator.SetFloat("dirZ", Mathf.Lerp(animator.GetFloat("dirZ"), localVelocity.z / (walkSpeed * 0.45f), Time.fixedDeltaTime)); //playerInput.y
		animator.SetBool("grounded", isGrounded());
	}

	private void OnDrawGizmos()
	{
		if(player != null && player.transform != null) {
			if (isGrounded())
			{
				Gizmos.color = Color.yellow;
			}
			else
			{
				Gizmos.color = Color.red;
			}

			Gizmos.DrawWireSphere(player.transform.position + (player.transform.up * 0.6f), 0.45f);
			Gizmos.DrawWireSphere(player.transform.position + player.transform.up * -1, 0.45f);
		}
	}

	private bool isGrounded() {
		RaycastHit hit;
		//Debug lines to visually show the ray we're checking
		if (Physics.SphereCast(player.transform.position + (player.transform.up * 0.6f), 0.45f, player.transform.up * -1f, out hit, 0.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore))
		{
			//Debug.DrawRay(player.transform.position + (player.transform.up * 0.5f), player.transform.up * -1, Color.green, 0.5f);
		}
		else
		{
			//Debug.DrawRay(player.transform.position + (player.transform.up * 0.5f), player.transform.up * -1, Color.red, 0.5f);
		}

		return player.linearVelocity.y == 0 || Physics.SphereCast(player.transform.position + (player.transform.up * 0.6f), 0.45f, player.transform.up * -1f, out hit, 0.5f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);
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

		RaycastHit velocityOffsetRay;

		bool rayVO = Physics.Raycast(player.transform.position + player.linearVelocity.normalized, Vector3.down, out velocityOffsetRay, 1f, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore);

		//Shoot a ray down, and if it hits something (a face or surface on the "Default" Unity Layer), then return the normal vector of the surface.
		if (rayVO)
		{
			return velocityOffsetRay.normal;
		}

		return Vector3.up;
	}
}
