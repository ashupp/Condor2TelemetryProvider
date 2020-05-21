using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using ILogger = SimFeedback.log.ILogger;
using System.Web.Script.Serialization;

namespace SimFeedback.telemetry
{
    public sealed class TelemetryProvider : AbstractTelemetryProvider
    {

        private const int _portNum = 55278;
        private const string _ipAddr = "127.0.0.1";
        private bool _isStopped = true;
        private Thread _t;


        public TelemetryProvider()
        {
            Author = "ashupp / ashnet GmbH";
            Version = "0.0.1.0";
            BannerImage = @"img\banner_condor2.png";
            IconImage = @"img\icon_condor2.png";
            TelemetryUpdateFrequency = 100;
        }

        private static float Rad2Deg(float v) { return (float)(v * 180 / Math.PI); }

        public override string Name => "condor2";

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing Condor2TelemetryProvider");

        }

        public override string[] GetValueList()
        {
            return GetValueListByReflection(typeof(TelemetryData));
        }

        public override void Stop()
        {
            if (_isStopped) return;
            LogDebug("Stopping Condor2TelemetryProvider");
            _isStopped = true;
            if (_t != null) _t.Join();
        }

        public override void Start()
        {
            if (_isStopped)
            {
                LogDebug("Starting Condor2TelemetryProvider");
                _isStopped = false;
                _t = new Thread(Run);
                _t.Start();
            }
        }

        private byte[] ReadBuffer(Stream memoryMappedViewStream, int size)
        {
            using (BinaryReader binaryReader = new BinaryReader(memoryMappedViewStream))
                return binaryReader.ReadBytes(size);
        }


        private void Run()
        {
            TelemetryData lastTelemetryData = new TelemetryData();


            UdpClient socket = new UdpClient();
            socket.ExclusiveAddressUse = false;
            //socket.Client.Bind(new IPEndPoint(IPAddress.Any, _portNum));
            socket.Client.Bind(new IPEndPoint(IPAddress.Parse(_ipAddr),_portNum));
            var endpoint = new IPEndPoint(IPAddress.Parse(_ipAddr), _portNum);
            Stopwatch sw = new Stopwatch();
            sw.Start();

            while (!_isStopped)
            {
                try
                {

                    // get data from game, 
                    if (socket.Available == 0)
                    {
                        if (sw.ElapsedMilliseconds > 500)
                        {
                            IsRunning = false;
                            IsConnected = false;
                            //Thread.Sleep(1000);
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                        continue;
                    }
                    else
                    {
                        IsConnected = true;
                    }

                    // DCS approach....
                    // Byte[] received = socket.Receive(ref _senderIP);
                    // string resp = Encoding.UTF8.GetString(received);
                    //LogDebug(resp);

                    //KK Approach
                    Byte[] received = socket.Receive(ref endpoint);
                    string resp = Encoding.UTF8.GetString(received);
                    TelemetryData telemetryData = ParseReponse(resp);

                    IsRunning = true;

                    //TODO
                    TelemetryEventArgs args = new TelemetryEventArgs(
                        new Condor2TelemetryInfo(telemetryData, lastTelemetryData));
                    RaiseEvent(OnTelemetryUpdate, args);
                    lastTelemetryData = telemetryData;

                    sw.Restart();




                    Thread.Sleep(SamplePeriod);
                }
                catch (Exception e)
                {
                    LogError("Condor2TelemetryProvider Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }
            }

            IsConnected = false;
            IsRunning = false;
        }

        private TelemetryData ParseReponse(string resp)
        {
            TelemetryData telemetryData = new TelemetryData();

            //Log("incoming data: " + resp);
            //File.AppendAllText("testlog.txt", "inc:" + resp);

            string[] lines = resp.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if (lines.Length >= 29)
            {
                telemetryData.Time = float.Parse(lines[0].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.AirSpeed = float.Parse(lines[1].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Yaw = float.Parse(lines[12].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Pitch = float.Parse(lines[13].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Roll = float.Parse(lines[14].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Surge = float.Parse(lines[19].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Sway = float.Parse(lines[20].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.Heave = float.Parse(lines[21].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.RollRate = float.Parse(lines[25].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.PitchRate = float.Parse(lines[26].Split('=')[1], CultureInfo.InvariantCulture);
                telemetryData.YawRate = float.Parse(lines[27].Split('=')[1], CultureInfo.InvariantCulture);
            }

            return telemetryData;
        }
    }
}