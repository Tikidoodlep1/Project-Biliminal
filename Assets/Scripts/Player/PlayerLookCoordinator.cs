using UnityEngine;

public class PlayerLookCoordinator : MonoBehaviour
{

	Rigidbody player;
	Transform playerCamera;
	Vector3 rotateVector = new Vector3(0, 0, 0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		player = this.gameObject.GetComponent<Rigidbody>();

		//Check https://docs.unity3d.com/6000.1/Documentation/ScriptReference/GameObject.Find.html
		playerCamera = this.gameObject.GetComponentInChildren<Camera>().transform;
    }

	//Done on Update instead of FixedUpdate because it only affects visual objects, not physics related.
    void Update()
    {
		//Get the y axis of our camera rotation for the rotation of the player body on the horizontal axis
		float y = playerCamera.rotation.eulerAngles.y;

		//No need to rotate if we're already facing the correct direction
		if(player.transform.eulerAngles.y != y) {
			//Put the value into a vector to avoid creating objects every frame
			rotateVector.y = y;

			//Set the Euler Angles of the player transform to our rotation vector, which visually rotates our body
			player.transform.eulerAngles = rotateVector;
		}
    }
}
