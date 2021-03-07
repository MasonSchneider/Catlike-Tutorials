using UnityEngine;

public class MovingSphere : MonoBehaviour
{
    [SerializeField, Range(0f, 100f)]
    float maxSpeed = 10f;

    [SerializeField, Range(0f, 100f)]
    float maxAcceleration = 10f, maxAirAcceleration = 1f;

    [SerializeField, Range(0f, 10f)]
    float jumpHeight = 2f;

    [SerializeField, Range(0, 5)]
    int maxAirJumps = 0;

    [SerializeField, Range(0f, 90f)]
    float maxGroundAngle = 25f;

    float minGroundDotProduct;

    bool desiredJump;
    int jumpPhase;
    int groundContactCount;
    bool OnGround => groundContactCount > 0;
    Vector3 contactNormal;

    Vector3 velocity, desiredVelocity;

    Rigidbody body;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        OnValidate();
    }

    private void OnValidate()
    {
        minGroundDotProduct = Mathf.Cos(maxGroundAngle * Mathf.Deg2Rad);
    }

    private void Update()
    {
        Vector2 playerInput;
        playerInput.x = Input.GetAxis("Horizontal");
        playerInput.y = Input.GetAxis("Vertical");
        desiredJump |= Input.GetButtonDown("Jump");
        playerInput = Vector2.ClampMagnitude(playerInput, 1f);
        desiredVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;
        GetComponent<Renderer>().material.SetColor(
            "_Color", Color.white * (groundContactCount * 0.25f)
        );
    }

    private void FixedUpdate()
    {
        UpdateState();
        AdjustVelocity();
        if (desiredJump)
        {
            desiredJump = false;
            Jump();
        }
        body.velocity = velocity;
        ClearState();
    }

    private void ClearState()
    {
        groundContactCount = 0;
        contactNormal = Vector3.zero;
    }

    private void AdjustVelocity()
    {
        var xAxis = ProjectOnContactPlane(Vector3.right).normalized;
        var zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

        var currentX = Vector3.Dot(velocity, xAxis);
        var currentZ = Vector3.Dot(velocity, zAxis);

        var acceleration = OnGround ? maxAcceleration : maxAirAcceleration;
        var maxSpeedChange = acceleration * Time.deltaTime;
        var newX = Mathf.MoveTowards(currentX, desiredVelocity.x, maxSpeedChange);
        var newZ = Mathf.MoveTowards(currentZ, desiredVelocity.z, maxSpeedChange);

        velocity += xAxis * (newX - currentX) + zAxis * (newZ - currentZ);
    }

    private Vector3 ProjectOnContactPlane(Vector3 vector)
    {
        return vector - contactNormal * Vector3.Dot(vector, contactNormal);
    }

    private void UpdateState()
    {
        velocity = body.velocity;
        if (OnGround)
        {
            jumpPhase = 0;
            if (groundContactCount > 1)
            {
                contactNormal.Normalize(); 
            }
        }
        else
        {
            contactNormal = Vector3.up;
        }
    }

    private void Jump()
    {
        if (OnGround || jumpPhase < maxAirJumps)
        {
            jumpPhase++;
            var jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
            var alignedSpeed = Vector3.Dot(velocity, contactNormal);
            if (alignedSpeed > 0f)
            {
                jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
            }
            velocity += contactNormal * jumpSpeed;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        EvaluateCollision(collision);
    }

    private void EvaluateCollision(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            var normal = collision.GetContact(i).normal;
            if (normal.y >= minGroundDotProduct)
            {
                groundContactCount += 1;
                contactNormal += normal;
            }
        }
    }
}
