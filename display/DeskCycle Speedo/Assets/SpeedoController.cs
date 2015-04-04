using UnityEngine;
using System.Collections;
using System.IO.Ports;
using UnityEngine.UI;

public class SpeedoController : MonoBehaviour {
    private SerialPort serialPort = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
    private string message;
    private string[] messageArr;

    private string speed;
    private string cadence;

    public Text speedoText;
    public Text cadenceText;

	// Use this for initialization
	void Start () {
        OpenConnection();
	}
	
	// Update is called once per frame
	void Update () {
        message = serialPort.ReadLine();
        
        if (message != string.Empty)
        {
            messageArr = message.Split(',');
            if (messageArr.Length != 0)
            {
                speedoText.text = messageArr[0];
                cadenceText.text = messageArr[1]; 
            }
        }
	}

    public void OpenConnection()
    {
        if (serialPort != null)
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                message = "Closing port, because it was already open!";
            }
            else
            {
                serialPort.Open();
                serialPort.ReadTimeout = 1000;
                message = "Port Opened!";
            }
        }
        else
        {
            if (serialPort.IsOpen)
            {
                print("Port is already open");
            }
            else
            {
                print("Port == null");
            }
        }
    }
    
    void OnApplicationQuit()
    {
        serialPort.Close();
    }

}
