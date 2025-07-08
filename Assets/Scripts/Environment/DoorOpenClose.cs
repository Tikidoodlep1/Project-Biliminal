using UnityEngine;

public class DoorOpenClose : Interactable
{
	private const bool STATE_CLOSED = false;
	private const bool STATE_OPEN = true;

	[SerializeField]
	[Header("false = Closed, true = Open")]
	private bool state = STATE_CLOSED;
	Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(state != STATE_CLOSED && state != STATE_OPEN) {
			Debug.LogError("Door [" + this.gameObject + "] state was set to " + state + ", which is invalid! Must be 0 or 1!");
			state = STATE_CLOSED;
		}
		animator = this.gameObject.GetComponent<Animator>();
    }

	public override void Interact(GameObject interactor) {
		//Debug.Log(interactor.name + " is interacting with Interactable animation " + this.interactableAnimationName + " with current state " + state);
		if(state == STATE_CLOSED) {
			animator.SetTrigger("Open");
		}else {
			animator.SetTrigger("Close");
		}
		state = !state;
	}
}
