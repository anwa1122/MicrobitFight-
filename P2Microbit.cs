using System.IO.Ports;
using UnityEngine;
// กำหนดให้สคริปต์นี้สามารถรันได้แม้ว่า GameObject จะถูกปิดใช้งานชั่วคราว
public class P2Microbit : MonoBehaviour
{
    // ตัวแปรสาธารณะเพื่อให้ตั้งค่า Port Name ใน Inspector ได้
    public string portName = "COM4"; // Default เป็น COM3 (สำหรับ P1) หรือเปลี่ยนเป็น COM4 (สำหรับ P2)
    public int baudRate = 115200;

    private SerialPort sp;
    private bool isConnected = false;

    // ตัวแปรสาธารณะ (ReadOnly) เพื่อให้สคริปต์อื่นมาอ่านค่าได้
    [HideInInspector] // ซ่อนใน Inspector แต่ยัง Public ให้สคริปต์อื่นเข้าถึงได้
    public int currentBitValue = 0;

    // การกำหนด Event/Action เพื่อให้สคริปต์ผู้เล่นรู้ว่ามีข้อมูลเข้ามา
    public System.Action<int> OnDataReceived;

    // Cooldown สำหรับการพยายามเชื่อมต่อใหม่
    private const float ReconnectDelay = 1.0f;

    [HideInInspector]
    public bool accessP2;

    void Start()
    {
        if (PortSend.Instance != null)
        {
            // ใช้ค่าที่ตั้งไว้ใน PortSend.cs
            portName = PortSend.player2PortName; // ⭐ เปลี่ยนเป็น player2PortName ⭐
            Debug.Log($"P2Microbit using port: {portName} from PortSend.");
        }

        // เริ่มพยายามเชื่อมต่อทันที
        Connect();
    }

    void Update()
    {
        if (isConnected && sp.IsOpen)
        {
            ReadData();
            accessP2 = true;
        }
        else
        {
            // ถ้าหลุดการเชื่อมต่อ ให้พยายามเชื่อมต่อใหม่ทุก ReconnectDelay วินาที
            Invoke("Connect", ReconnectDelay);
            accessP2 = false;
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
                CancelInvoke("Connect"); // ยกเลิกการ Invoke ซ้ำๆ เมื่อเชื่อมต่อสำเร็จ
            }
            else
            {
                // ไม่ได้ Debug.LogError เพื่อไม่ให้ Console แดงตลอดเวลา
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
        // ไม่ต้องยกเลิก Invoke ตรงนี้ เพราะ Update จะเรียก Invoke ต่อไปเพื่อพยายามเชื่อมต่อใหม่
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
                    // อัปเดตค่าที่สคริปต์อื่นจะมาอ่าน
                    currentBitValue = receivedValue;

                    // แจ้งให้สคริปต์ผู้เล่นรู้ว่ามีข้อมูลใหม่เข้ามา
                    OnDataReceived?.Invoke(receivedValue);

                    // Debug.Log($"Microbit {portName} received: {currentBitValue}"); 
                }
            }
        }
        catch (System.Exception e)
        {
            // ข้อผิดพลาดในการอ่าน (เช่น ถอดสายออกทันที)
            Debug.LogError($"Error reading from micro:bit {portName}: {e.Message}");
            Disconnect();
        }
    }
}