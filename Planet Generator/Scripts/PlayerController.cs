using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this the MainCamera or Player GameObject to simulate gravity with spherical planets.
// This will work with any number of planets. It will automatically reference anything with a 
// ... planet script attached to it and pull the mass from there to calculate the total graviational
// ... acceleration of all planets within the scene. 
public class Controller : MonoBehaviour
{
    public GameObject playerCamera;
    public float MouseSensitivity = 250f;
    public float MovementSpeed = 10f;
    public float JumpForce = 750f;

    // List of all gravitational sources affecting the player
    private List<Planet> allPlanets = new List<Planet>();

    // Newton would not like it if you changed this! LEAVE IT ALONE
    private const float G = 6.6743E-11f;
    private Rigidbody body; // RigidBody for the Player/MainCamera
    private float localX = 0.0f, localY = 0.0f;
    private bool isGrounded = false;

    void Start()
    {
        UpdatePlanetList();
        Ref_RigidBody();
    }

    void OnValidate()
    {
        UpdatePlanetList();
        Ref_RigidBody();
    }

    void OnCollisionStay(Collision collision)
    {
        isGrounded = true;
    }

    // Reference any object in the scene with a planet script attached to it.
    private void UpdatePlanetList()
    {
        Planet[] nPlanets = GameObject.FindObjectsByType<Planet>(FindObjectsSortMode.InstanceID);
        allPlanets.Clear();
        foreach (Planet planet in nPlanets)
            allPlanets.Add(planet);
    }

    // Find the rigid body attached to the parent of this script. Add one if it does not have one. 
    private void Ref_RigidBody()
    {
        if (gameObject.GetComponent<Rigidbody>() != null)
            body = gameObject.GetComponent<Rigidbody>();
        else
            body = gameObject.AddComponent<Rigidbody>();

        body.freezeRotation = true;
        body.useGravity = false;
    }

    // Orient the player's local up with the nearest planet's surface normal direction
    // ... and add a force to the rigid body to mimic acceleration.
    void Update()
    {
        // Apply gravity
        if (allPlanets.Count == 0) return;
        Vector3 cumulativeAcceleration = Vector3.zero;
        Planet nearestPlanet = allPlanets[0];
        float nearestDistace = Vector3.Distance(allPlanets[0].transform.position, gameObject.transform.position);
        foreach (Planet planet in allPlanets)
        {
            // f = m_1 * a; where f = (G * m_1 * m_2) / r^2; so, a = (G * m_2) / r^2
            float distance = Vector3.Distance(planet.transform.position, gameObject.transform.position);
            if (distance < nearestDistace)
            {
                nearestDistace = distance;
                nearestPlanet = planet;
            }
            Vector3 direction = (planet.transform.position - gameObject.transform.position).normalized;
            cumulativeAcceleration += (G * planet.mass_kg) / Mathf.Pow(distance, 2) * direction;
        }
        body.AddForce(cumulativeAcceleration);
        gameObject.transform.up = (gameObject.transform.position - nearestPlanet.gameObject.transform.position).normalized;

        // Allow for camera rotation using the mouse
        localX += Input.GetAxis("Mouse X") * MouseSensitivity * Time.deltaTime;
        localY -= Input.GetAxis("Mouse Y") * MouseSensitivity * Time.deltaTime;
        localY = Mathf.Clamp(localY, -85f, 75f); // 85deg up and 75deg down
        playerCamera.transform.localRotation = Quaternion.Euler(localY, localX, 0f);

        // Allow for movement using WASD
        float x = Input.GetAxis("Horizontal") * MovementSpeed * Time.deltaTime;
        float z = Input.GetAxis("Vertical") * MovementSpeed * Time.deltaTime;
        Vector3 delta = playerCamera.transform.right * x + playerCamera.transform.forward * z;
        transform.position += delta;

        // Allow for jump using space
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            body.AddForce(gameObject.transform.up * JumpForce);
            isGrounded = false;
        }
    }
}
