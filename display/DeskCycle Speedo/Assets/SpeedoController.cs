using UnityEngine;
using System.Collections;
using System.IO.Ports;
using UnityEngine.UI;
using System.ComponentModel;
using System;

public class SpeedoController : MonoBehaviour {
    private SerialPort serialPort = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);
    private string message;
    private string[] messageArr;

    private float speed = 0;
    private float cadence = 0;

    public Text speedoText;
    public Text cadenceText;

    private BackgroundWorker serialMonitor = new BackgroundWorker();

	// Use this for initialization
	void Start () {
        OpenConnection();

        serialMonitor.WorkerSupportsCancellation = true;
        serialMonitor.DoWork += serialMonitor_DoWork;
        serialMonitor.RunWorkerAsync();
	}

    void serialMonitor_DoWork(object sender, DoWorkEventArgs e)
    {
        while (!serialMonitor.CancellationPending)
        {
            try
            {
                message = serialPort.ReadLine();
                if (message != string.Empty)
                {
                    messageArr = message.Split(',');
                    if (messageArr.Length != 0)
                    {
                        speed = Convert.ToSingle(messageArr[0]);
                        cadence = Convert.ToSingle(messageArr[1]);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        speedoText.text = speed.ToString();
        cadenceText.text = cadence.ToString();        
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
        serialMonitor.CancelAsync();
    }
}