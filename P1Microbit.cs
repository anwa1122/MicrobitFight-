using System.IO.Ports;
using UnityEngine;
using UnityEngine.UI;
public class P1Microbit : MonoBehaviour
{
    public string portName = "COM3"; // Default เป็น COM3 (P1) COM4 (P2)
    public int baudRate = 115200;

    private SerialPort sp;
    private bool isConnected = false;

    [HideInInspector]
    public int currentBitValue = 0;

    public System.Action<int> OnDataReceived;

    private const float ReconnectDelay = 1.0f;

    [HideInInspector]
    public bool accessP1;

    void Start()
    {
        if (PortSend.Instance != null)
        {
            portName = PortSend.player1PortName;
            Debug.Log($"P1Microbit using port: {portName} from PortSend.");
        }

        // เริ่มพยายามเชื่อมต่อทันที
        Connect();
    }

    void Update()
    {
        if (isConnected && sp.IsOpen)
        {
            ReadData();
            accessP1 = true;
        }
        else
        {
            // ถ้าหลุดการเชื่อมต่อ ให้พยายามเชื่อมต่อใหม่ทุก ReconnectDelay วินาที
            Invoke("Connect", ReconnectDelay);
            accessP1 = false;
        }
    }

    void OnApplicationQuit()
    {
        Disconnect();
    }

    public void Connect()
    {
        // ป้องกันการเชื่อมต่อซ้ำขณะที่กำลังจะเชื่อมต่อ
        if (isConnected && sp != null && sp.IsOpen) return;

        // Disconnect เก่าก่อนเสมอ เพื่อป้องกันปัญหาทรัพยากรพอร์ตถูกล็อค
        Disconnect();

        try
        {
            // 1. ตรวจสอบว่าพอร์ตมีอยู่จริงหรือไม่
            string[] availablePorts = SerialPort.GetPortNames();
            bool portExists = false;
            foreach (string port in availablePorts)
            {
                if (port == portName)
                {
                    portExists = true;
                    break;
                }
            }

            if (portExists)
            {
                sp = new SerialPort(portName, baudRate);
                sp.ReadTimeout = 50;
                sp.Open();
                isConnected = true;
                currentBitValue = 0; // รีเซ็ตค่าเมื่อเชื่อมต่อสำเร็จ
                Debug.Log($"Micro:bit connected successfully on {portName}.");
                CancelInvoke("Connect");
            }
            else
            {
                Debug.LogWarning($"Micro:bit not found on {portName}. Will try again in {ReconnectDelay}s.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to connect to micro:bit on {portName}: {e.Message}. Will try again in {ReconnectDelay}s.");
            isConnected = false;
            Disconnect();
        }
    }

    public void Disconnect()
    {
        if (sp != null)
        {
            if (sp.IsOpen)
            {
                sp.Close();
                Debug.Log($"Micro:bit on {portName} disconnected.");
            }
            sp.Dispose();
            sp = null;
        }
        isConnected = false;
        currentBitValue = 0; // รีเซ็ตค่าเมื่อตัดการเชื่อมต่อ
    }

    private void ReadData()
    {
        try
        {
            string data = sp.ReadExisting();
            if (!string.IsNullOrEmpty(data))
            {
                int receivedValue;
                if (int.TryParse(data, out receivedValue))
                {
                    currentBitValue = receivedValue;

                    OnDataReceived?.Invoke(receivedValue);

                    // Debug.Log($"Microbit {portName} received: {currentBitValue}"); 
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error reading from micro:bit {portName}: {e.Message}");
            Disconnect();
        }
    }
}