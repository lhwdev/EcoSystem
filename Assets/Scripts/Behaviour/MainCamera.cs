using System.Linq;
using UnityEditor;
using UnityEngine;

public class MainCamera : MonoBehaviour {
	public float lookSpeedH = 3f;

	public float lookSpeedV = 3f;

	public float zoomSpeed = 4f;

	public float dragSpeed = 8f;

	public float walkSpeed = 9f;

	public bool perspective = true;
	public bool flyWalkToWorld = true;
	public bool followWalker = true;
	public bool followSelection = false;
	bool lastFollow = false;
	public bool trackEntity = true;

	private float yaw = 0f;
	private float pitch = 0f;

	private new Camera camera;
	private GameObject walker;
	private Rigidbody rigidBody;
	private Vector3 lastTargetPosition;

	bool follow => followSelection || followWalker;
	Transform followTarget => followSelection ? Selection.activeTransform : (followWalker ? walker.transform : null);

	private void Start() {
		// Initialize the correct initial rotation
		this.yaw = this.transform.eulerAngles.y;
		this.pitch = this.transform.eulerAngles.x;
		this.walker = GameObject.Find("Walker");
		this.rigidBody = walker.GetComponent<Rigidbody>();
	}

	LivingEntity target() {
		if (trackEntity) {
			return null;
		} else {
			return Selection.GetFiltered<LivingEntity>(SelectionMode.TopLevel).FirstOrDefault();
		}
	}

	private void Update() {
		var position = transform.position;
		var follow = this.follow;
		var followTarget = this.followTarget;

		if (follow && followTarget == null || followTarget == gameObject) return; // not ready yet

		if (follow) { // following

			if (followSelection) {
				if (!lastFollow) {
					lastTargetPosition = followTarget.position;
					transform.localPosition = new Vector3(2f, 2f, 0f);
				}
				// transform.position = followTarget.position;
			}
			if (!lastFollow) {
				if (followWalker) {
					transform.SetParent(walker.transform);
					transform.localPosition = new Vector3(0f, 2f, 0f);
				} else {
					transform.SetParent(null);
				}
				lastFollow = true;
			}

			void TranslateLocal(Vector3 delta) {
				if (followSelection) {
					position += delta;
				} else {
					transform.localPosition += delta;
				}
			}

			if (Input.GetKey(KeyCode.LeftAlt)) {
				// drag camera around with Middle Mouse
				if (Input.GetMouseButton(2)) {
					if (perspective) {
						var vector = new Vector3(Input.GetAxis("Mouse X") * Time.deltaTime * dragSpeed, Input.GetAxis("Mouse Y") * Time.deltaTime * dragSpeed, 0f);
						TranslateLocal(-(transform.rotation * vector));
					} else {
						TranslateLocal(new Vector3(-Input.GetAxisRaw("Mouse X") * Time.deltaTime * dragSpeed, -Input.GetAxisRaw("Mouse Y") * Time.deltaTime * dragSpeed, 0f));
					}

					if (Input.GetMouseButton(1)) {
						// Zoom in and out with Right Mouse
						if (perspective) {
							var vector = new Vector3(0, 0, Input.GetAxisRaw("Mouse X") * this.zoomSpeed * .07f);
							TranslateLocal(transform.rotation * vector);
						} else {
							TranslateLocal(new Vector3(0, 0, Input.GetAxisRaw("Mouse X") * this.zoomSpeed * .07f));
						}
					}
				}
			}
		} else { // not following walker
			if (lastFollow) {
				transform.SetParent(null);
				transform.localPosition = new Vector3(0f, 0f, 0f);
				lastFollow = false;
			}

			// drag camera around with Middle Mouse
			if (Input.GetMouseButton(2)) {
				if (perspective) {
					var vector = new Vector3(Input.GetAxis("Mouse X") * Time.deltaTime * dragSpeed, Input.GetAxis("Mouse Y") * Time.deltaTime * dragSpeed, 0f);
					position += -(transform.rotation * vector);
				} else {
					position += new Vector3(-Input.GetAxisRaw("Mouse X") * Time.deltaTime * dragSpeed, -Input.GetAxisRaw("Mouse Y") * Time.deltaTime * dragSpeed, 0f);
				}
			}

			if (Input.GetMouseButton(1)) {
				// Zoom in and out with Right Mouse
				if (perspective) {
					var vector = new Vector3(0, 0, Input.GetAxisRaw("Mouse X") * this.zoomSpeed * .07f);
					position += transform.rotation * vector;
				} else {
					position += new Vector3(0, 0, Input.GetAxisRaw("Mouse X") * this.zoomSpeed * .07f);
				}
			}
			// Only work with the Left Alt pressed
			// if (Input.GetKey(KeyCode.LeftAlt)) {

		}
		void rotate(Vector3 target) {
			var result = transform.localRotation * target * Time.deltaTime * walkSpeed;
			if (followTarget) {
				result.Scale(new Vector3(1f, 0f, 1f));
				position += result;
			} else {
				var to = followTarget ?? transform;
				result.Scale(new Vector3(1f, 0f, 1f));
				to.transform.Translate(result);
			}
		}

		if (Input.GetKey(KeyCode.Space)) {
			rigidBody.velocity = rigidBody.velocity + new Vector3(0f, 7f, 0f) * Time.deltaTime;
		}
		if (follow) {
			if (Input.GetKey(KeyCode.W)) {
				rotate(followTarget.transform.forward);
			} else if (Input.GetKey(KeyCode.S)) {
				rotate(-followTarget.transform.forward);
			} else if (Input.GetKey(KeyCode.A)) {
				rotate(-followTarget.transform.right);
			} else if (Input.GetKey(KeyCode.D)) {
				rotate(followTarget.transform.right);
			}
		} else {
			if (Input.GetKey(KeyCode.W)) {
				rotate(transform.forward);
			} else if (Input.GetKey(KeyCode.S)) {
				rotate(-transform.forward);
			} else if (Input.GetKey(KeyCode.A)) {
				rotate(-transform.right);
			} else if (Input.GetKey(KeyCode.D)) {
				rotate(transform.right);
			}
		}
		// Look around with Left Mouse
		if (Input.GetMouseButton(0)) {
			this.yaw += this.lookSpeedH * Input.GetAxis("Mouse X");
			this.pitch -= this.lookSpeedV * Input.GetAxis("Mouse Y");

			this.transform.eulerAngles = new Vector3(this.pitch, this.yaw, 0f);
		}
		if (!follow) {
			this.transform.position = position;
		} else {
			if (followSelection) {
				var currentTarget = followTarget.transform.position;
				this.transform.position = position + (currentTarget - lastTargetPosition);
				lastTargetPosition = currentTarget;
			}
		}
		this.transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * zoomSpeed, Space.Self);


		// Zoom in and out with Mouse Wheel
		// this.transform.Translate(0, 0, , Space.Self);
		// this.
		// Input.GetAxis("Mouse ScrollWheel") * this.zoomSpeed;
	}
	// }
}
