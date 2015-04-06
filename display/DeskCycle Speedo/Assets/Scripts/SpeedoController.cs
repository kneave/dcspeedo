using System;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class SpeedoController : MonoBehaviour {
    private SerialPort serialPort;
    private string port = string.Empty;

    private string message;
    private string[] messageArr;

    //  Units are MPH, RPM, Miles
    private float speed = 0;
    private float cadence = 0;
    private float distance = 0;
    private TimeSpan time = new TimeSpan(0);

    public Text speedoText;
    public Text cadenceText;
    public Text distanceText;
    public Text timeText;

    private DateTime lastReading = DateTime.MinValue;

    private BackgroundWorker serialMonitor = new BackgroundWorker();

	// Use this for initialization
    void Start()
    {
        serialMonitor.WorkerSupportsCancellation = true;
        serialMonitor.DoWork += serialMonitor_DoWork;
        serialMonitor.RunWorkerAsync();
    }

    void serialMonitor_DoWork(object sender, DoWorkEventArgs e)
    {
        TimeSpan timeDelta = new TimeSpan();

        while (!serialMonitor.CancellationPending)
        {
            try
            {
                if(string.IsNullOrEmpty(port))
                    port = FindSpeedo();
                else
                {
                    if (!serialPort.IsOpen)
                        OpenConnection(port);
                    else
                    {
                        serialPort.Write("b");
                        message = serialPort.ReadLine();
                        if (message != string.Empty)
                        {
                            messageArr = message.Split(',');
                            if (messageArr.Length != 0)
                            {
                                speed = Convert.ToSingle(messageArr[0]);
                                cadence = Convert.ToSingle(messageArr[1]);

                                if (speed != 0)
                                {
                                    if (lastReading != DateTime.MinValue)
                                    {
                                        //  this gives us the time since the last reading
                                        //  we will use this to calculat how far we have travelled at our current speed
                                        timeDelta = DateTime.Now - lastReading;
                                        time += timeDelta;
                                    }
                                    lastReading = DateTime.Now;
                                    distance += speed * (float)timeDelta.TotalHours; 
                                }

                                WriteLog();
                            }
                        }
                    }
                }

                Thread.Sleep(500);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    private void WriteLog()
    {
        string filename = string.Format(@"Data\{0:00}{1:00}{2:0000}.csv",
            DateTime.Now.Day,
            DateTime.Now.Month,
            DateTime.Now.Year);
        
        if (!File.Exists(filename))
        {
            //  Reset the distance with each new file.  This means each new day will be reset.
            distance = 0;

            // Create a file to write to. 
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.WriteLine("DateTime,Speed,Cadence,Distance");
            }
        }

        if (File.Exists(filename))
        {
            // Create a file to write to. 
            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine("{0},{1},{2},{3}",
                    DateTime.Now.ToString(),
                    speed,
                    cadence,
                    distance);
            }
        }
    }
	
	// Update is called once per frame
	void Update () {
        speedoText.text = speed.ToString();
        cadenceText.text = cadence.ToString();
        distanceText.text = distance.ToString("F");
        timeText.text = string.Format("{0:00}:{1:00}:{2:00}",
            time.Hours, time.Minutes, time.Seconds);
	}

    public void OpenConnection(string portName)
    {
        if(!serialPort.IsOpen)
            serialPort = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One);

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

    private string FindSpeedo()
    {
        foreach (string portname in SerialPort.GetPortNames())
        {
            Debug.Log(string.Format("Testing {0}", portname));
            try
            {
                serialPort = new SerialPort(portname, 9600, Parity.None, 8, StopBits.One);
                using(serialPort)
                {
                    serialPort.ReadTimeout = 2000;
                    serialPort.WriteTimeout = 2000;

	                if(!serialPort.IsOpen)
                    {
                        serialPort.Open();
                        serialPort.WriteLine("h");
                        string response = serialPort.ReadLine();
                        if (response.Contains("DeskCycle Speedo"))
                        {
                            Debug.Log(string.Format("Speedo found on port {0}", portname));
                            return portname;
                        } 
                    } 
                }
                
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        return string.Empty;
    }

    void OnApplicationQuit()
    {
        serialPort.Close();
        serialMonitor.CancelAsync();
    }
}