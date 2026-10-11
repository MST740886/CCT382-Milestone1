using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class GymWalker : MonoBehaviour
{
    public Camera eye;
    public Camera overview;
    CharacterController controller;
    float pitch, vertical;
    bool walking;
    void Start() { controller = GetComponent<CharacterController>(); SetMode(false); }
    void SetMode(bool value)
    {
        walking = value;
        eye.gameObject.SetActive(value);
        overview.gameObject.SetActive(!value);
        Cursor.lockState = value ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !value;
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) SetMode(!walking);
        if (Input.GetKeyDown(KeyCode.Escape)) SetMode(false);
        if (!walking) return;
        transform.Rotate(0, Input.GetAxis("Mouse X") * 2, 0);
        pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2, -80, 80);
        eye.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        if (controller.isGrounded && vertical < 0) vertical = -2;
        if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space)) vertical = 6;
        vertical += Physics.gravity.y * Time.deltaTime;
        Vector3 move = Vector3.ClampMagnitude(transform.right * Input.GetAxisRaw("Horizontal") + transform.forward * Input.GetAxisRaw("Vertical"), 1);
        controller.Move((move * (Input.GetKey(KeyCode.LeftShift) ? 7 : 4) + Vector3.up * vertical) * Time.deltaTime);
        if (transform.position.y < -5) { controller.enabled = false; transform.position = new Vector3(-16, 1, -5); controller.enabled = true; vertical = 0; }
    }
    void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 480, 76), "CCT382 / LAB 01 — THREE ROOM TEST GYM\nTAB: overview / walk     WASD: move     Mouse: look\nSPACE: jump     SHIFT: run     ESC: overview");
    }
    void OnDisable() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
}
