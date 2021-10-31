using System.Linq;
using UnityEditor;
using UnityEngine;

public class MainCamera : MonoBehaviour {
	public float lookSpeedH = 3f;

	public float lookSpeedV = 3f;

	public float zoomSpeed = 4f;

	public float dragSpeed = 8f;
    
    public float walkSpeed = 9f;

	public bool followWalker = true;
	public bool trackEntity = true;

	private float yaw = 0f;
	private float pitch = 0f;

	private new Camera camera;
    private GameObject walker;
    private Rigidbody rigidBody;

	private void Start() {
		// Initialize the correct initial rotation
		this.yaw = this.transform.eulerAngles.y;
		this.pitch = this.transform.eulerAngles.x;
        this.walker = GameObject.Find("Walker");
        this.rigidBody = walker.GetComponent<Rigidbody>();
	}

	LivingEntity target() {
		if(trackEntity) {
			return null;
		} else {
			return Selection.GetFiltered<LivingEntity>(SelectionMode.TopLevel).FirstOrDefault();
		}
	}

	private void Update() {
		if (followWalker) {
            transform.SetParent(walker.transform);
            transform.localPosition = new Vector3(0f, 2f, 0f);

            void rotate(Vector3 target) {
                var result = transform.localRotation * target * Time.deltaTime * walkSpeed;
                result.Scale(new Vector3(1f, 0f, 1f));
                walker.transform.Translate(result);
            }

            if(Input.GetKey(KeyCode.W)) {
                rotate(walker.transform.forward);
            } else if (Input.GetKey(KeyCode.S)) {
				rotate(-walker.transform.forward);
			} else if (Input.GetKey(KeyCode.A)) {
				rotate(-walker.transform.right);
			} else if (Input.GetKey(KeyCode.D)) {
				rotate(walker.transform.right);
			} else if(Input.GetKey(KeyCode.Space)) {
                rigidBody.velocity = rigidBody.velocity + new Vector3(0f, 7f, 0f) * Time.deltaTime;
            }
		}

		// Only work with the Left Alt pressed
		// if (Input.GetKey(KeyCode.LeftAlt)) {
			//Look around with Left Mouse
			if (Input.GetMouseButton(0)) {
				this.yaw += this.lookSpeedH * Input.GetAxis("Mouse X");
				this.pitch -= this.lookSpeedV * Input.GetAxis("Mouse Y");

				this.transform.eulerAngles = new Vector3(this.pitch, this.yaw, 0f);
			}

			//drag camera around with Middle Mouse
			if (Input.GetMouseButton(2)) {
				transform.Translate(-Input.GetAxisRaw("Mouse X") * Time.deltaTime * dragSpeed, -Input.GetAxisRaw("Mouse Y") * Time.deltaTime * dragSpeed, 0);
			}

			if (Input.GetMouseButton(1)) {
				//Zoom in and out with Right Mouse
				this.transform.Translate(0, 0, Input.GetAxisRaw("Mouse X") * this.zoomSpeed * .07f, Space.Self);
			}

			//Zoom in and out with Mouse Wheel
			// this.transform.Translate(0, 0, , Space.Self);
			// this.
			// Input.GetAxis("Mouse ScrollWheel") * this.zoomSpeed;
		}
	// }
}
