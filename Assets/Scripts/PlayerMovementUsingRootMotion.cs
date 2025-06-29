using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementUsingRootMotion : MonoBehaviour
{
	[Header("IK Settings")]
	public bool useRootMotion = true;
	public float raycastDistance = 1.2f;
	public float footOffset = 0.1f;
	public float pelvisAdjustmentSpeed = 5f;

	private float lastPelvisY;
	private float pelvisOffset;

	Animator animator;
	Rigidbody player;

	InputAction playerMovementAction;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		animator = this.gameObject.GetComponent<Animator>();
		player = this.gameObject.GetComponent<Rigidbody>();

		playerMovementAction = InputSystem.actions.FindAction("Player/Move", true);
	}

    // Update is called once per frame
    void Update()
    {
        
    }

	private void FixedUpdate()
	{
		Vector3 input = playerMovementAction.ReadValue<Vector2>();

		animator.SetFloat("dirX", input.x);
		animator.SetFloat("dirZ", input.y);
		animator.SetBool("grounded", isGrounded());
	}

	private void OnAnimatorMove()
	{
		if(useRootMotion) {
			Vector3 deltaPosition = animator.deltaPosition;
			Quaternion deltaRotation = animator.deltaRotation;

			//Debug.Log(deltaPosition);

			player.MovePosition(player.position + deltaPosition);
			player.MoveRotation(player.rotation * deltaRotation);
		}
	}

	//NOT BEING USED RN
	private void OnAnimatorIK(int layerIndex)
	{
		//Debug.Log("Using IK");
		AdjustFootTarget(AvatarIKGoal.LeftFoot);
		AdjustFootTarget(AvatarIKGoal.RightFoot);
		AdjustHipHeight();
	}

	private void AdjustFootTarget(AvatarIKGoal foot) {
		int footIndex = (foot == AvatarIKGoal.LeftFoot) ? 0 : 1;

		Vector3 footPosition = animator.GetIKPosition(foot);
		Vector3 origin = footPosition + Vector3.up;
		Ray ray = new Ray(origin, Vector3.down);

		if(Physics.Raycast(ray, out RaycastHit hit, raycastDistance, LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)) {
			Vector3 targetPosition = hit.point + Vector3.up * footOffset;
			animator.SetIKPositionWeight(foot, 1f);
			animator.SetIKPositionWeight(foot, 1f);
			animator.SetIKPosition(foot, targetPosition);
			animator.SetIKRotation(foot, Quaternion.LookRotation(transform.forward, hit.normal));

			float footHeight = transform.InverseTransformPoint(targetPosition).y;
			if(foot == AvatarIKGoal.LeftFoot) {
				lastPelvisY = footHeight;
			}else {
				pelvisOffset = Mathf.Min(lastPelvisY, footHeight);
			}
		}else {
			animator.SetIKPositionWeight(foot, 0f);
			animator.SetIKRotationWeight(foot, 0f);
		}
	}
	
	private void AdjustHipHeight() {
		Vector3 bodyPosition = animator.bodyPosition;
		bodyPosition.y += pelvisOffset;
		animator.bodyPosition = Vector3.Lerp(animator.bodyPosition, bodyPosition, Time.deltaTime * pelvisAdjustmentSpeed);
	}

	private bool isGrounded()
	{
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
}
