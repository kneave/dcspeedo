using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class SpeedoController : MonoBehaviour {
    private SerialPort serialPort;

    //  For some reason the COM searching code isn't working on my work machine
    //  temporarily disabled until I get it working again.
    //private string port = string.Empty;
    private string port = "COM6";

    private string message;
    private string[] messageArr;

    //  Units are MPH, RPM, Miles
    private float speed = 0;
    private float cadence = 0;
    private float distance = 0;
    private TimeSpan duration = new TimeSpan(0);

    private float sevenDayDistance = 0;
    private TimeSpan sevenDayDuration = new TimeSpan(0);

    public Text speedoText;
    public Text cadenceText;
    public Text aveSpeedoText;
    public Text aveCadenceText;
    public Text distanceText;
    public Text timeText;
    public Text totalDistanceText;
    public Text totalTimeText;

    private float averageSpeed = 0f;
    private float averageCadence = 0f;

    private List<float> speeds = new List<float>();
    private List<float> cadences = new List<float>();

    private DateTime lastReading = DateTime.MinValue;

    private BackgroundWorker serialMonitor = new BackgroundWorker();
    private BackgroundWorker mathsWorker = new BackgroundWorker();

	// Use this for initialization
    void Start()
    {
        OpenLogs();

        Debug.Log(string.Format("7day distance: {0}", sevenDayDistance));
        Debug.Log(string.Format("7day duration: {0}", sevenDayDuration));

        serialMonitor.WorkerSupportsCancellation = true;
        serialMonitor.DoWork += serialMonitor_DoWork;
        serialMonitor.RunWorkerAsync();

        mathsWorker.WorkerSupportsCancellation = true;
        mathsWorker.DoWork += mathsWorker_DoWork;
    }

    void mathsWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        float totalSpeed = 0;
        float totalCadence = 0;

        foreach (float s in speeds)
            totalSpeed += s;

        foreach (float c in cadences)
            totalCadence += c;

        averageSpeed = totalSpeed / speeds.Count;
        averageCadence = totalCadence / cadences.Count;
    }

    void serialMonitor_DoWork(object sender, DoWorkEventArgs e)
    {
        TimeSpan timeDelta = new TimeSpan();

        //  this is temporary, related to the COM searching code bug.
        serialPort = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);

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
                                        duration += timeDelta;
                                    }
                                    lastReading = DateTime.Now;
                                    distance += speed * (float)timeDelta.TotalHours; 
                                }
                                else
                                {
                                    //  speed is zero therefore stopped peddling
                                    //  reset lastReading to prevent duration from being screwed up
                                    lastReading = DateTime.MinValue;
                                }

                                if (speed > 0)
                                {
                                    WriteLog(); 
                                }
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
            duration = new TimeSpan(0);

            speeds.Clear();
            cadences.Clear();
            OpenLogs();

            // Create a file to write to. 
            using (StreamWriter sw = File.CreateText(filename))
            {
                sw.WriteLine("DateTime,Speed (MPH),Cadence (RPM),Distance (Miles),Duration (HH:MM:SS)");
            }
        }

        speeds.Add(speed);
        cadences.Add(cadence);
        if (!mathsWorker.IsBusy)
            mathsWorker.RunWorkerAsync();

        if (File.Exists(filename))
        {
            // Create a file to write to. 
            using (StreamWriter sw = File.AppendText(filename))
            {
                sw.WriteLine("{0},{1},{2},{3},{4}",
                    DateTime.Now.ToString(),
                    speed,
                    cadence,
                    distance,
                    string.Format("{0:00}:{1:00}:{2:00}",
                        duration.Hours, duration.Minutes, duration.Seconds));
            }
        }
    }

    private void OpenLogs()
    {
        try
        {
            string lastLine = string.Empty;
            DateTime fileNameTime;

            //  Opening the logs, reset the counters
            sevenDayDuration = new TimeSpan(0);
            sevenDayDistance = 0;

            for (int i = 0; i < 7; i++)
            {
                fileNameTime = DateTime.Now.AddDays(-i);
                string filename = string.Format(@"Data\{0:00}{1:00}{2:0000}.csv",
                    fileNameTime.Day,
                    fileNameTime.Month,
                    fileNameTime.Year);

                lastLine = GetLastResults(filename);
                
                if (string.IsNullOrEmpty(lastLine))
                    continue;

                string[] csv = lastLine.Split(',');
                if (csv[0] != "DateTime")
                {
                    if (i != 0)
                    {
                        sevenDayDistance += Convert.ToSingle(csv[3]);

                        string durStr = csv[4];
                        string[] durStrArr = durStr.Split(':');
                        sevenDayDuration += new TimeSpan(
                            Convert.ToInt32(durStrArr[0]),
                            Convert.ToInt32(durStrArr[1]),
                            Convert.ToInt32(durStrArr[2])); 
                    }
                    else
                    {
                        distance += Convert.ToSingle(csv[3]);

                        string durStr = csv[4];
                        string[] durStrArr = durStr.Split(':');
                        duration += new TimeSpan(
                            Convert.ToInt32(durStrArr[0]),
                            Convert.ToInt32(durStrArr[1]),
                            Convert.ToInt32(durStrArr[2]));
                    }
                } 
            }

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private string GetLastResults(string filename)
    {
        string lastLine = string.Empty;
        string currentLine = string.Empty;
     
        if (File.Exists(filename))
        {
            // Create a file to write to. 
            using (StreamReader sr = new StreamReader(filename))
            {
                while ((currentLine = sr.ReadLine()) != null)
                {
                    lastLine = currentLine;
                }
            }
        }

        return lastLine;
    }
	
	// Update is called once per frame
	void Update () {
        speedoText.text = speed.ToString();
        cadenceText.text = cadence.ToString();

        aveSpeedoText.text = string.Format("{0:F}", averageSpeed);
        aveCadenceText.text = string.Format("{0:F}", averageCadence);

        distanceText.text = distance.ToString("F");
        timeText.text = string.Format("{0:00}:{1:00}:{2:00}",
            duration.Hours, duration.Minutes, duration.Seconds);

        float tempDist = sevenDayDistance + distance;
        TimeSpan tempDur = sevenDayDuration + duration;

        totalDistanceText.text = tempDist.ToString("F");
        if (tempDur.TotalHours >= 24)
            totalTimeText.text = string.Format("{0}:{1:00}:{2:00}:{3:00}",
                tempDur.Days, tempDur.Hours, tempDur.Minutes, tempDur.Seconds); 
        else
            totalTimeText.text = string.Format("{0:00}:{1:00}:{2:00}",
                tempDur.Hours, tempDur.Minutes, tempDur.Seconds); 
	}

    public void OpenConnection(string portName)
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
                    serialPort.ReadTimeout = 5000;
                    serialPort.WriteTimeout = 500;

	                if(!serialPort.IsOpen)
                    {
                        serialPort.Open();
                        serialPort.WriteLine("h");
                        string response = serialPort.ReadLine();
                        Debug.Log(response);
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