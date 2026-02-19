using System.Collections;
using TMPro;
using UnityEngine;
using XboxCtrlrInput;

// Ryan Smith 4 Player Local Multiplayer Game Script

[RequireComponent(typeof(PlayerInfo))]

public class Controller : MonoBehaviour
{
    PlayerInfo playerAssets;

    [Header("Player")]
    public XboxController controller;

    [Header("Player Lives")]
    [SerializeField] private TMP_Text _Lives;

    [Header("Control Variables")]
    public float maxMoveSpeed;
    private Vector3 newPosition;

    [Header("Jump")]
    public float jumpImpulse;
    private bool canJump = false;

    [Header("Body")]
    public Transform playerModel;
    private new Rigidbody rigidbody;

    [SerializeField] private Transform respawnPoint;

    [Header("Abilities")]
    public Transform hammerModel;
    public float dashSpeed = 10f;
    private int executionCount = 0;
    bool isDashing = false;
    bool isSlapping = false;
    bool isHitting = false;
    private bool canMove = false;
    public GameObject Hammer;

    [Header("Push")]
    [SerializeField] private float pushForce;
    [SerializeField] private float pushForce2;
    public float pushRadius;

    //-----------------------------------Start is called once upon creation-------------------------
    void Start()
    {

        playerModel = GetComponentInChildren<AutomaticPrefab>().transform; // Could not think of another way, so it gathers the model as it's attached to the "AutomaticPrefab" script which is empty

        rigidbody = GetComponent<Rigidbody>();

        playerAssets = GetComponent<PlayerInfo>();

        switch (controller)
        {
            case XboxController.First: break;
            case XboxController.Second: break;
            case XboxController.Third: break;
            case XboxController.Fourth: break;
        }


        newPosition = transform.position; // Moves model to the new game position
    }

    //-----------------------------------Update is called once per frame----------------------------
    void Update()
    {

        // Jump (A)
        if (XCI.GetButtonDown(XboxButton.A, controller) && canJump) // Only if on ground 
        {
            canJump = false;
            rigidbody.AddRelativeForce(0.0f, jumpImpulse, 0.0f, ForceMode.Impulse); // Actual 'jump'
        }

        newPosition = transform.position;
        float axisX = XCI.GetAxis(XboxAxis.LeftStickX, controller);
        float axisY = XCI.GetAxis(XboxAxis.LeftStickY, controller);
        Vector2 moveInput = new Vector2(axisX, axisY);

        if (moveInput.magnitude > 0.1f)
        {
            // Rotate the player model to face the direction of movement, as camera can not be used for direction rotation
            Quaternion newTurnVal = Quaternion.LookRotation(new Vector3(-moveInput.x, 0f, -moveInput.y));
            transform.rotation = Quaternion.Lerp(transform.rotation, newTurnVal, 0.2f);

            // Move the player in the direction of movement
            Vector3 movement = new Vector3(moveInput.x, 0f, moveInput.y).normalized * maxMoveSpeed * Time.deltaTime;
            newPosition += movement;
        }

        else
        {

        }

        transform.position = newPosition;

        // Dash
        if (XCI.GetButtonDown(XboxButton.B, controller) && !isDashing)
        {
            isDashing = true;
            rigidbody.velocity = -(transform.forward * dashSpeed); // Boosts forward estentially 
            StartCoroutine(ResetDash());
        }

        // Attack
        if (XCI.GetButtonDown(XboxButton.X, controller) && !isSlapping) // So cannot do it multiple times
        {
            isSlapping = true;

            Collider[] colliders = Physics.OverlapSphere(transform.position, pushRadius);
            foreach (Collider collider in colliders)
            {
                if (collider.CompareTag("Player") && collider.gameObject != gameObject)
                {
                    Rigidbody otherRigidbody = collider.GetComponent<Rigidbody>();
                    if (otherRigidbody != null)
                    {
                        Vector3 pushDirection = (collider.transform.position - transform.position).normalized;
                        pushDirection.y = 0f; // Prevent pushing players upwards but still can happen
                        otherRigidbody.AddForce(pushDirection * pushForce, ForceMode.Impulse);
                    }
                }
            }

            StartCoroutine(ResetSlap());
        }

        // Hammer
        if (XCI.GetButtonDown(XboxButton.Y, controller) && !isHitting) // Reduces spam but for spawning hammer with animation to hit surrounding players to stun
        {
            isHitting = true;
            Hammer.SetActive(true);

            StartCoroutine(ResetHammer());
        }

        // Slam
        if (XCI.GetButtonDown(XboxButton.LeftStick, controller) && !canJump) // Unnecessary but cool
        {
            GetComponent<Rigidbody>().AddRelativeForce(0.0f, (-jumpImpulse * 1.5f), 0.0f, ForceMode.Impulse); // Push players away after the slam
        }

    }

    //-----------------------------------Jump & Stun Check----------------------------
    void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.CompareTag("Ground")) // Simple ground check, not overkill but does work. No double jumping
        {
            canJump = true;

        }
        else
        {
            Rigidbody enemyRig = other.gameObject.GetComponent<Rigidbody>();
            if (enemyRig && isDashing)
                enemyRig.AddForce(rigidbody.velocity.normalized * pushForce);

        }
        {
            Rigidbody enemyRig = other.gameObject.GetComponent<Rigidbody>();
            if (enemyRig && isSlapping)
                enemyRig.AddForce(rigidbody.velocity.normalized * pushForce2);

        }

        if (other.gameObject.CompareTag("Hammer")) // if hit by the hammer, knows to stun
        {
            if (isHitting == false)
            {
                StartCoroutine(Stunned());
            }
        }

    }

    //-----------------------------------Player State----------------------------
    IEnumerator Stunned()
    {
        canMove = false; // Prevents player from moving (purpose of stun)
        yield return new WaitForSeconds(4);
        canMove = true; 
    }

    //-----------------------------------Abilities----------------------------
    private IEnumerator ResetDash()
    {
        yield return new WaitForSeconds(0.5f);
        isDashing = false;
    }

    private IEnumerator ResetHammer()
    {
        yield return new WaitForSeconds(0.5f);
        Hammer.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.5f);
        isHitting = false;

    }

    private IEnumerator ResetSlap()
    {
        yield return new WaitForSeconds(1);
        isSlapping = false;
    }


    //-----------------------------------Off Platform----------------------------
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("KillField"))
        {
            executionCount++;
            gameObject.transform.position = respawnPoint.transform.position;
            _Lives.text = (3 - executionCount).ToString(); // Updates the player life count on HUD

            Rigidbody rb = GetComponent<Rigidbody>();
            rb.velocity = Vector3.zero;

            if (executionCount > 2) // Knows when to change player icon state to dead and stop 'teleporting' back to platfrom
            {
                playerAssets.PlayerDead();
                Transform[] kids = GetComponentsInChildren<Transform>();

                for (int i = 0; i < kids.Length; i++)
                {
                    kids[i].gameObject.SetActive(false);
                }
            }
        }
    }
}