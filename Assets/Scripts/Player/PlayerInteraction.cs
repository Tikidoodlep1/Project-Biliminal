using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
	private InputAction interact;

	private Transform playerCamera;

	private List<GameObject> touchingInteractable = new();
	private Interactable target;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		interact = InputSystem.actions.FindAction("Player/Interact", true);

		playerCamera = this.gameObject.GetComponentInChildren<Camera>().transform;
    }

    // Update is called once per frame
    void Update()
    {
        if(touchingInteractable.Count > 0 && interact.WasPerformedThisFrame()) {
			//Debug.Log("Trying Interaction");
			GameObject closestToFacing = getClosestInteractableToFacing();
			//Debug.Log("Closest Interactable set to " + closestToFacing);
			if(closestToFacing != null) {
				target = closestToFacing.GetComponent<Interactable>();
				//Debug.Log("Can Player Interact: " + target.CanInteract(this.gameObject));
				if(target.CanInteract(this.gameObject)) {
					target.Interact(this.gameObject);
				}
			}
		}
    }

	private GameObject getClosestInteractableToFacing() {
		GameObject closestToFacing = null;
		foreach (GameObject obj in touchingInteractable)
		{
			if (closestToFacing == null)
			{
				closestToFacing = obj;
			}
			else
			{
				if (Vector3.Angle(playerCamera.forward, obj.transform.position) < Vector3.Angle(playerCamera.forward, closestToFacing.transform.position))
				{
					closestToFacing = obj;
				}
			}
		}

		return closestToFacing;
	}

	private void OnTriggerEnter(Collider other)
	{
		Interactable parent = other.gameObject.GetComponentInParent<Interactable>();
		//Debug.Log(parent);
		
		if(parent != null && !touchingInteractable.Contains(parent.gameObject)) {
			touchingInteractable.Add(parent.gameObject);
		}

		//Debug.Log("Entering:" + touchingInteractable.Count);
	}

	private void OnTriggerExit(Collider other) 
	{
		Interactable parent = other.gameObject.GetComponentInParent<Interactable>();

		if (parent != null && touchingInteractable.Contains(parent.gameObject))
		{
			touchingInteractable.Remove(parent.gameObject);
		}

		//Debug.Log("Leaving: " + touchingInteractable.Count);
	}
}
