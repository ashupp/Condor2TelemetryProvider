using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading;
using ILogger = SimFeedback.log.ILogger;

namespace SimFeedback.telemetry
{
    public sealed class TelemetryProvider : AbstractTelemetryProvider
    {

        private const int _portNum = 55278;
        private const string _ipAddr = "127.0.0.1";
        private bool _isStopped = true;
        private Thread _t;
        private TelemetryData lastTelemetryData;

        public TelemetryProvider()
        {
            Author = "ashupp / ashnet GmbH";
            Version = Assembly.LoadFrom(Assembly.GetExecutingAssembly().Location).GetName().Version.ToString();
            BannerImage = @"img\banner_condor2.png";
            IconImage = @"img\icon_condor2.png";
            TelemetryUpdateFrequency = 60;
        }

        public override string Name => "condor2";

        public override void Init(ILogger logger)
        {
            base.Init(logger);
            Log("Initializing Condor2TelemetryProvider");
            Log("Using Sample Period: " + SamplePeriod);
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

        private void Run()
        {
            lastTelemetryData = new TelemetryData();

            UdpClient socket = new UdpClient {ExclusiveAddressUse = false};
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
                            Thread.Sleep(1000);
                        }
                        continue;
                    }
                    IsConnected = true;
                    IsRunning = true;
                    Byte[] received = socket.Receive(ref endpoint);
                    string resp = Encoding.UTF8.GetString(received);
                    TelemetryData telemetryData = ParseReponse(resp);

                    TelemetryEventArgs args = new TelemetryEventArgs(new Condor2TelemetryInfo(telemetryData));
                    RaiseEvent(OnTelemetryUpdate, args);
                    lastTelemetryData = telemetryData;
                    sw.Restart();

                }
                catch (Exception e)
                {
                    LogError("Condor2TelemetryProvider Exception while processing data", e);
                    IsConnected = false;
                    IsRunning = false;
                    Thread.Sleep(1000);
                }
            }
            sw.Stop();
            IsConnected = false;
            IsRunning = false;
        }

        private TelemetryData ParseReponse(string resp)
        {
            TelemetryData telemetryData = new TelemetryData();

            string[] lines = resp.Split(new[] { Environment.NewLine }, StringSplitOptions.None);

            if (lines.Length > 15)
            {

                //var dict = new Dictionary<string, float>();
                var dict = new Dictionary<string, double>();
                // Todo: Einlesen in dictionary
                foreach (var line  in lines)
                {
                    var tmpLineItems = line.Split('=');
                    if (tmpLineItems.Length == 2)
                    {
                        //dict.Add(tmpLineItems[0], float.Parse(tmpLineItems[1], CultureInfo.InvariantCulture));
                        dict.Add(tmpLineItems[0], Convert.ToDouble(tmpLineItems[1], CultureInfo.InvariantCulture));
                    }
                }

                dict.TryGetValue("time", out var tmpTime);
                telemetryData.Time = tmpTime;

                dict.TryGetValue("airspeed", out var tmpAirspeed);
                telemetryData.AirSpeed = tmpAirspeed;

                dict.TryGetValue("altitude", out var tmpAltitude);
                telemetryData.Altitude = tmpAltitude;

                dict.TryGetValue("vario", out var tmpVario);
                telemetryData.Vario = tmpVario;

                dict.TryGetValue("evario", out var tmpEvario);
                telemetryData.Evario = tmpEvario;

                dict.TryGetValue("nettovario", out var tmpNettovario);
                telemetryData.Nettovario = tmpNettovario;

                dict.TryGetValue("integrator", out var tmpIntegrator);
                telemetryData.Integrator = tmpIntegrator;

                dict.TryGetValue("compass", out var tmpCompass);
                telemetryData.Compass = tmpCompass;

                dict.TryGetValue("slipball", out var tmpSlipball);
                telemetryData.SlipBall = tmpSlipball;

                dict.TryGetValue("turnrate", out var tmpTurnrate);
                telemetryData.TurnRate = tmpTurnrate;

                dict.TryGetValue("yawstringangle", out var tmpYawstringangle);
                telemetryData.YawStringAngle = tmpYawstringangle;

                dict.TryGetValue("yaw", out var tmpYaw);
                telemetryData.Yaw = tmpYaw;

                dict.TryGetValue("pitch", out var tmpPitch);
                telemetryData.Pitch = tmpPitch;

                dict.TryGetValue("bank", out var tmpRoll);
                telemetryData.Roll = tmpRoll;

                dict.TryGetValue("quaternionx", out var tmpQuaternionx);
                telemetryData.Quaternionx = tmpQuaternionx;

                dict.TryGetValue("quaterniony", out var tmpQuaterniony);
                telemetryData.Quaterniony = tmpQuaterniony;

                dict.TryGetValue("quaternionz", out var tmpQuaternionz);
                telemetryData.Quaternionz = tmpQuaternionz;

                dict.TryGetValue("quaternionw", out var tmpQuaternionw);
                telemetryData.Quaternionw = tmpQuaternionw;

                dict.TryGetValue("ax", out var tmpAx);
                telemetryData.Surge = tmpAx;

                dict.TryGetValue("ay", out var tmpAy);
                telemetryData.Sway = tmpAy;

                dict.TryGetValue("az", out var tmpAz);
                telemetryData.Heave = tmpAz;

                dict.TryGetValue("vx", out var tmpVx);
                telemetryData.SpeedX = tmpVx;

                dict.TryGetValue("vy", out var tmpVy);
                telemetryData.SpeedY = tmpVy;

                dict.TryGetValue("vz", out var tmpVz);
                telemetryData.SpeedZ = tmpVz;

                dict.TryGetValue("rollrate", out var tmpRollrate);
                telemetryData.RollRate = tmpRollrate;

                dict.TryGetValue("pitchrate", out var tmpPitchrate);
                telemetryData.PitchRate = tmpPitchrate;

                dict.TryGetValue("yawrate", out var tmpYawrate);
                telemetryData.YawRate = tmpYawrate;

                dict.TryGetValue("gforce", out var tmpGforce);
                telemetryData.Gforce = tmpGforce;

                dict.TryGetValue("height", out var tmpHeight);
                telemetryData.Height = tmpHeight;

                dict.TryGetValue("wheelheight", out var tmpWheelheight);
                telemetryData.Wheelheight = tmpWheelheight;

                dict.TryGetValue("turbulencestrength", out var tmpTurbulencestrength);
                telemetryData.Turbulencestrength = tmpTurbulencestrength;

                dict.TryGetValue("surfaceroughness", out var tmpSurfaceroughness);
                telemetryData.Surfaceroughness = tmpSurfaceroughness;
                
                // Surge alternative calculation
                if (telemetryData.AirSpeed + telemetryData.Time > 0)
                {
                    if ((telemetryData.Time - lastTelemetryData.Time) > 0)
                    {
                        telemetryData.SurgeAlternative = ((dict["airspeed"] - lastTelemetryData.AirSpeed) / (telemetryData.Time - lastTelemetryData.Time)) / 100;
                        LogDebug("Current time: " + telemetryData.Time + " Last time: " + lastTelemetryData.Time + " Current Speed: " + dict["airspeed"] + " Last Speed: " + lastTelemetryData.AirSpeed + " Current SurgeAlternative: " + telemetryData.SurgeAlternative + " Last SurgeAlternative: " + lastTelemetryData.SurgeAlternative);
                    }
                    else
                    {
                        telemetryData.SurgeAlternative = lastTelemetryData.SurgeAlternative;
                        LogDebug("Division by zero / Using last value for SurgeAlternative");
                    }
                }
                else
                {
                    LogDebug("Airspeed and Time not > 0");
                }
            }

            return telemetryData;
        }
    }
}