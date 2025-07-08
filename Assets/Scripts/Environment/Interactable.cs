using UnityEngine;

public abstract class Interactable : MonoBehaviour
{
	[SerializeField]
	public string interactableAnimationName = "";
	[SerializeField]
	public Vector3 interactionSize = Vector3.one;
	[SerializeField]
	public Vector3 center = Vector3.zero;

	//private List<GameObject> inCollision = new List<GameObject>();
	private Collider[] inCollision;

	[ExecuteInEditMode]
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;

		Gizmos.matrix = transform.localToWorldMatrix;
		Gizmos.DrawWireCube(center, interactionSize);
		Gizmos.matrix = Matrix4x4.identity;
	}

	public bool CanInteract(GameObject other) {
		//Debug.Log(other.name + " is trying to interact, can it: " + inCollision.Contains(other));
		inCollision = Physics.OverlapBox(center + this.gameObject.transform.position, interactionSize / 2, this.gameObject.transform.rotation, LayerMask.GetMask("Entity"), QueryTriggerInteraction.Ignore);
		//Debug.Log(LayerMask.GetMask("Entity") + ", " + inCollision.Length);

		//foreach(Collider col in inCollision) {
		//	Debug.Log(col.gameObject.name);
		//}

		Debug.Assert(inCollision.Length < 100, "Debug is too long! Look into sorting and searching methods for this array! [Script: Interactable, line 30]");
		
		foreach(Collider c in inCollision) {
			GameObject parent = c.gameObject.GetComponentInParent<Rigidbody>().gameObject;
			if(parent == other) {
				return true;
			}
		}

		return false;
	}

	public abstract void Interact(GameObject interactor);
}
