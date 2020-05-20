﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace FlightSimulatorApp.Model
{
    public class SimulatorModel : ISimulatorModel
    {

        public event PropertyChangedEventHandler PropertyChanged;
        ITelnetClient telnetClient;
        volatile Boolean stop;
        private Queue<String> setCommandsQueue;
        private static Mutex mutex = new Mutex();

        // Dashboard data members.
        private string heading="";
        private string verticalSpeed = "";
        private string groundSpeed = "";
        private string airSpeed = "";
        private string gpsAltitude = "";
        private string roll = "";
        private string pitch = "";
        private string altitude = "";

        // Map location data members.
        private string latitude = "";
        private string longitude = "";
        private string location = "";

        // Errors data memebers.
        private string wrongLocation;
        private string connectionStatus;

        // Ctor.
        public SimulatorModel(ITelnetClient telnetClient)
        {
            this.telnetClient = telnetClient;
            stop = false;
            this.setCommandsQueue = new Queue<String>();
        }

        // Connect the model to the simulator.
        public void Connect(string ip, string port)
        {
            stop = false;
            ConnectionStatus = telnetClient.Connect(ip, port);
            if (ConnectionStatus != "Status: Cannot connect (invalid ip or port)")
            {
                Start();
                SetJoystickSliders();
            }
        }

        // Disconnect the model from the simulator.
        public void Disconnect()
        {
            stop = true;
            ConnectionStatus = telnetClient.Disconnect();
        }

        // Send a command (of get or set) to the simulator.
        private string SendCommand(string command, string currVal)
        {
            string returnValWrite = telnetClient.Write(command);
            if (returnValWrite == "Status: Server has disconnected")
            {   
                ConnectionStatus = returnValWrite;
                return currVal;
            }

            string returnValRead = telnetClient.Read();
            if (returnValRead == "Status: Server timeout")
            {
                if (connectionStatus == "Status: Connected to server")
                {
                    ConnectionStatus = returnValRead;
                }
                return currVal;
            }

            return returnValRead;
        }

        // update dashbord values
        private void GetDashboardValuesFromServer()
        {
            Heading = SendCommand("get /instrumentation/heading-indicator/indicated-heading-deg\n", heading);
            VerticalSpeed = SendCommand("get /instrumentation/gps/indicated-vertical-speed\n", verticalSpeed);
            GroundSpeed = SendCommand("get /instrumentation/gps/indicated-ground-speed-kt\n", groundSpeed);
            AirSpeed = SendCommand("get /instrumentation/airspeed-indicator/indicated-speed-kt\n", airSpeed);
            GPSAltitude = SendCommand("get /instrumentation/gps/indicated-altitude-ft\n", gpsAltitude);
            Roll = SendCommand("get /instrumentation/attitude-indicator/internal-roll-deg\n", roll);
            Pitch = SendCommand("get /instrumentation/attitude-indicator/internal-pitch-deg\n", pitch);
            Altitude = SendCommand("get /instrumentation/altimeter/indicated-altitude-ft\n", altitude);
        }

        // get airplane new location
        private string GetUpdatedLocationFromServer()
        {
            Latitude = SendCommand("get /position/latitude-deg\n", latitude);
            string oldLong = longitude;
            Longitude = SendCommand("get /position/longitude-deg\n", longitude);

            return oldLong;
        }

        // Start sending all the get commands to the simulator, and update the sensors properties.
        public void Start()
        {
            new Thread(delegate ()
            {
                double lat;
                double lon;

                while (!stop)
                {
                    try
                    {
                        mutex.WaitOne();

                        GetDashboardValuesFromServer();

                        string oldLong = GetUpdatedLocationFromServer();

                        if (IsValidValue(longitude) && IsValidValue(latitude))
                        {
                            lat = double.Parse(latitude);
                            lon = double.Parse(longitude);

                            // Limiting the airplane to stay inside the map when arrived to the boundary.
                            if (double.Parse(latitude) < -94)
                            {
                                lat = -94;
                                lon = LimitLongitudeToMapBorders(oldLong);
                            }
                            else if (double.Parse(latitude) > 83.25)
                            {
                                lat = 83.25;
                                lon = LimitLongitudeToMapBorders(oldLong);
                            }
                            else
                            {
                                WrongLocation = " ";
                            }

                            string newLocation = lat + ", " + lon;

                            if (location != newLocation)
                            {
                                Location = newLocation;
                            }
                        }
                        Thread.Sleep(250);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }).Start();
        }

        // the function check if the value is valid
        private bool IsValidValue(string value)
        {
            if (value != null && value != "" && value != "ERR" && value != "ERR\n" && value != "\n")
                return true;
            return false;
        }

        //the function prevent from the airplane to cross borders map
        private double LimitLongitudeToMapBorders(string oldLong)
        {
            Longitude = oldLong;
            double lon = double.Parse(oldLong);
            WrongLocation = "Error: Airplane is stuck - reached map's coordinates boundary.";
            return lon;
        }

        // Sensors properties.
        public string Heading
        {
            set
            {
                if (IsValidValue(value))
                {
                    heading = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    heading = value;
                }
                NotifyPropertyChanged("Heading");
            }
            get { return heading; }
        }
        public string VerticalSpeed
        {
            set
            {
                if (IsValidValue(value))
                {
                    verticalSpeed = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    verticalSpeed = value;
                }
                NotifyPropertyChanged("VerticalSpeed");
            }
            get { return verticalSpeed; }
        }
        public string GroundSpeed
        {
            set
            {
                if (IsValidValue(value))
                {
                    groundSpeed = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    groundSpeed = value;
                }
                NotifyPropertyChanged("GroundSpeed");
            }
            get { return groundSpeed; }
        }
        public string AirSpeed
        {
            set
            {
                if (IsValidValue(value))
                {
                    airSpeed = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    airSpeed = value;
                }
                NotifyPropertyChanged("AirSpeed");
            }
            get { return airSpeed; }
        }
        public string GPSAltitude
        {
            set
            {
                if (IsValidValue(value))
                {
                    gpsAltitude = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    gpsAltitude = value;
                }
                NotifyPropertyChanged("GPSAltitude");
            }
            get { return gpsAltitude; }
        }

        public string Roll
        {
            set
            {
                if (IsValidValue(value))
                {
                    roll = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    roll = value;
                }
                NotifyPropertyChanged("Roll");
            }
            get { return roll; }
        }

        public string Pitch
        {
            set
            {
                if (IsValidValue(value))
                {
                    pitch = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    pitch = value;
                }
                NotifyPropertyChanged("Pitch");
            }
            get { return pitch; }
        }

        public string Altitude
        {
            set
            {
                if (IsValidValue(value))
                {
                    altitude = string.Format("{0:F3}", double.Parse(value));
                }
                else
                {
                    altitude = value;
                }

                NotifyPropertyChanged("Altitude");
            }
            get { return altitude; }
        }

        public string Latitude
        {
            set
            {
                latitude = value;
                NotifyPropertyChanged("Latitude");
            }
            get { return latitude; }
        }

        public string Longitude
        {
            set
            {
                longitude = value;
                NotifyPropertyChanged("Longitude");
            }
            get { return longitude; }
        }

        public string Location
        {
            set
            {
                location = value;
                NotifyPropertyChanged("Location");

            }
            get { return location; }
        }

        public string WrongLocation
        {
            set
            {
                wrongLocation = value;
                NotifyPropertyChanged("WrongLocation");
            }
            get { return wrongLocation; }
        }

        public string ConnectionStatus
        {
            set
            {
                connectionStatus = value;
                NotifyPropertyChanged("ConnectionStatus");
            }
            get { return connectionStatus; }
        }

        // Notify the coresponding view model when a property changed.
        public void NotifyPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propName));
        }

        // Setting the joystick and sliders according to the user mouse movements.
        public void SetJoystickSliders()
        {
            string x = "";
            new Thread(delegate ()
            {
                while (!stop)
                {
                    try
                    {
                        mutex.WaitOne();
                        while ((this.setCommandsQueue.Count > 0) && !stop)
                        {
                            string command = this.setCommandsQueue.Dequeue();
                            if (command != null && command != "")
                            {
                                x = SendCommand(command, "");
                            }
                        }
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }).Start();
        }


        // Setting new value to the rudder.
        public void MoveRudder(double rudder)
        {
            this.setCommandsQueue.Enqueue("set /controls/flight/rudder " + rudder.ToString() + "\n");
        }

        // Setting new value to the elevator.
        public void MoveElevator(double elevator)
        {
            this.setCommandsQueue.Enqueue("set /controls/flight/elevator " + elevator.ToString() + "\n");
        }

        // Setting new value to the aileron.
        public void MoveAileron(double aileron)
        {
            this.setCommandsQueue.Enqueue("set /controls/flight/aileron " + aileron.ToString() + "\n");
        }

        // Setting new value to the throttle.
        public void MoveThrottle(double throttle)
        {
            this.setCommandsQueue.Enqueue("set /controls/engines/current-engine/throttle " + throttle.ToString() + "\n");
        }
    }
}
