using UnityEngine;
using System.IO.Ports;
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class Player : MonoBehaviour
{
    public float speed = 5f;
    public Projectile laserPrefab;
    private Projectile laser;

    // Serial port settings
    public string portName = "COM6"; // Change this to match your Arduino port
    public int baudRate = 9600;
    // Changed to public for GameManager access
    public SerialPort serialPort;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;
    private bool buttonPressed = false;
    private bool wasButtonReleased = true; // Track if button was released since last press

    // Make the button state accessible to GameManager
    public bool WasButtonPressed()
    {
        if (buttonPressed)
        {
            buttonPressed = false; // Reset after reading
            return true;
        }
        return false;
    }

    private void Start()
    {
        // Initialize and open the serial port
        InitializeSerialPort();
    }

    private void OnEnable()
    {
        // Reset states when the player is enabled/reactivated
        buttonPressed = false;
        wasButtonReleased = true;
        laser = null;
    }

    private void InitializeSerialPort()
    {
        // Only initialize if it's not already open
        if (serialPort != null && serialPort.IsOpen)
            return;

        try
        {
            serialPort = new SerialPort(portName, baudRate);
            serialPort.ReadTimeout = 50; // Short timeout for responsive reading
            serialPort.Open();
            Debug.Log("Connected to Arduino on " + portName);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error opening serial port: " + e.Message);
        }
    }

    private void Update()
    {
        // Try to initialize serial port if it's not open
        if (serialPort == null || !serialPort.IsOpen)
        {
            InitializeSerialPort();
        }

        // Read data from Arduino if the port is open
        if (serialPort != null && serialPort.IsOpen)
        {
            ReadSerialData();
        }

        // Only process movement and shooting if the player is active
        if (gameObject.activeSelf)
        {
            Vector3 position = transform.position;

            // Move based on Arduino tilt sensor input
            if (isMovingLeft)
            {
                position.x -= speed * Time.deltaTime;
            }
            else if (isMovingRight)
            {
                position.x += speed * Time.deltaTime;
            }

            // Keep keyboard controls as fallback
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                position.x -= speed * Time.deltaTime;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                position.x += speed * Time.deltaTime;
            }

            // Clamp the position of the character so they do not go out of bounds
            Vector3 leftEdge = Camera.main.ViewportToWorldPoint(Vector3.zero);
            Vector3 rightEdge = Camera.main.ViewportToWorldPoint(Vector3.right);
            position.x = Mathf.Clamp(position.x, leftEdge.x, rightEdge.x);

            // Set the new position
            transform.position = position;

            // Shoot if we don't have an active laser yet
            if (laser == null && (buttonPressed || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                laser = Instantiate(laserPrefab, transform.position, Quaternion.identity);
                buttonPressed = false; // Reset button state after shooting
                Debug.Log("Shooting laser!");
            }

            // If laser is destroyed, reset the reference so we can shoot again
            if (laser != null && laser.gameObject == null)
            {
                laser = null;
            }
        }
    }

    private void ReadSerialData()
    {
        try
        {
            // Read all available lines to process the most recent commands
            while (serialPort.BytesToRead > 0)
            {
                string data = serialPort.ReadLine().Trim();
                Debug.Log("Arduino data: " + data); // Add debug logging to see data

                // Process the command from Arduino
                switch (data)
                {
                    case "AIM_LEFT":
                        isMovingLeft = true;
                        isMovingRight = false;
                        break;

                    case "AIM_RIGHT":
                        isMovingRight = true;
                        isMovingLeft = false;
                        break;

                    case "NEUTRAL":
                        isMovingLeft = false;
                        isMovingRight = false;
                        // Consider this as button release
                        wasButtonReleased = true;
                        break;

                    case "GAME_BUTTON":
                        // Only register button press if it was released since last press
                        if (wasButtonReleased)
                        {
                            buttonPressed = true;
                            wasButtonReleased = false;
                            Debug.Log("Button pressed!");
                        }
                        break;
                }
            }
        }
        catch (System.TimeoutException)
        {
            // Timeout is normal when no data is available
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error reading serial data: " + e.Message);
        }
    }

    // New public method for sending commands to Arduino
    public bool SendCommand(string command)
    {
        if (serialPort != null && serialPort.IsOpen)
        {
            try
            {
                serialPort.WriteLine(command);
                Debug.Log("Sent command to Arduino: " + command);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Failed to send to Arduino: " + e.Message);
                return false;
            }
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Missile") ||
            other.gameObject.layer == LayerMask.NameToLayer("Invader"))
        {
            GameManager.Instance.OnPlayerKilled(this);
        }
    }

    private void OnDestroy()
    {
        // Close the serial port when the object is destroyed
        if (serialPort != null && serialPort.IsOpen)
        {
            serialPort.Close();
            Debug.Log("Serial port closed");
        }
    }
}