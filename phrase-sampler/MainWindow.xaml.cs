//---------------------------------------------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//
// <Description>
// This program tracks up to 6 people simultaneously.
// If a person is tracked, the associated gesture detector will determine if that person is seated or not.
// If any of the 6 positions are not in use, the corresponding gesture detector(s) will be paused
// and the 'Not Tracked' image will be displayed in the UI.
// </Description>
//----------------------------------------------------------------------------------------------------

namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Controls;
    using Microsoft.Kinect;
    using Microsoft.Kinect.VisualGestureBuilder;

    /// <summary>
    /// Interaction logic for the MainWindow
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        /// <summary> Active Kinect sensor </summary>
        private KinectSensor kinectSensor = null;
        
        /// <summary> Array for the bodies (Kinect will track up to 6 people simultaneously) </summary>
        private Body[] bodies = null;

        /// <summary> Reader for body frames </summary>
        private BodyFrameReader bodyFrameReader = null;

        /// <summary> Current status text to display </summary>
        private string statusText = null;

        /// <summary> KinectBodyView object which handles drawing the Kinect bodies to a View box in the UI </summary>
        private KinectBodyView kinectBodyView = null;
        
        /// <summary> List of gesture detectors, there will be one detector created for each potential body (max of 6) </summary>
        private List<GestureDetector> gestureDetectorList = null;

        private ClientInterface clientInterface = null;

        private bool startMode = false;

        //############# PHRASE NAME ################
        private String phrase_name = "car-crash";
        /// <summary>
        /// Initializes a new instance of the MainWindow class
        /// </summary>
        public MainWindow()
        {
            // only one sensor is currently supported
            this.kinectSensor = KinectSensor.GetDefault();
            
            // set IsAvailableChanged event notifier
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;

            // open the sensor
            this.kinectSensor.Open();

            // set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.NoSensorStatusText;

            // open the reader for the body frames
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            // set the BodyFramedArrived event notifier
            this.bodyFrameReader.FrameArrived += this.Reader_BodyFrameArrived;

            // initialize the BodyViewer object for displaying tracked bodies in the UI
            this.kinectBodyView = new KinectBodyView(this.kinectSensor);

            // initialize the gesture detection objects for our gestures
            this.gestureDetectorList = new List<GestureDetector>();

            // initialize the MainWindow
            this.InitializeComponent();

            // set our data context objects for display in UI
            this.DataContext = this;
            this.kinectBodyViewbox.DataContext = this.kinectBodyView;

            // connect to htk server via tcpClient
            this.clientInterface = ClientInterface.getClientInstance();
            clientInterface.connect();

            Console.WriteLine("connect to the client interface \n " + clientInterface.GetHashCode() + "\n");            
            //clientInterface.disconnect();

            // create a gesture detector for each body (6 bodies => 6 detectors) and create content controls to display results in the UI
            int col0Row = 0;
            int col1Row = 0;
            int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;
            for (int i = 0; i < maxBodies; ++i)
            {
                GestureResultView result = new GestureResultView(i, false, false, 0.0f);
                GestureDetector detector = new GestureDetector(this.kinectSensor, result);
                this.gestureDetectorList.Add(detector);                
                
                // split gesture results across the first two columns of the content grid
                ContentControl contentControl = new ContentControl();
                contentControl.Content = this.gestureDetectorList[i].GestureResultView;
                /*
                if (i % 2 == 0)
                {
                    // Gesture results for bodies: 0, 2, 4
                    Grid.SetColumn(contentControl, 0);
                    Grid.SetRow(contentControl, col0Row);
                    ++col0Row;
                }
                else
                {
                    // Gesture results for bodies: 1, 3, 5
                    Grid.SetColumn(contentControl, 1);
                    Grid.SetRow(contentControl, col1Row);
                    ++col1Row;
                }

                this.contentGrid.Children.Add(contentControl);*/
            }
        }

        /// <summary>
        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the current status text to display
        /// </summary>
        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.FrameArrived -= this.Reader_BodyFrameArrived;
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.gestureDetectorList != null)
            {
                // The GestureDetector contains disposable members (VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader)
                foreach (GestureDetector detector in this.gestureDetectorList)
                {
                    detector.Dispose();
                }

                this.gestureDetectorList.Clear();
                this.gestureDetectorList = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.IsAvailableChanged -= this.Sensor_IsAvailableChanged;
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        /// <summary>
        /// Handles the event when the sensor becomes unavailable (e.g. paused, closed, unplugged).
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            // on failure, set the status text
            this.StatusText = this.kinectSensor.IsAvailable ? Properties.Resources.RunningStatusText
                                                            : Properties.Resources.SensorNotAvailableStatusText;
        }

        /// <summary>
        /// Handles the body frame data arriving from the sensor and updates the associated gesture detector object for each body
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_BodyFrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        // creates an array of 6 bodies, which is the max number of bodies that Kinect can track simultaneously
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    // As long as those body objects are not disposed and not set to null in the array,
                    // those body objects will be re-used.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (Keyboard.IsKeyDown(Key.Return))
            {
                if (!startMode)
                {
                    startMode = true;
                    clientInterface.sendData("start");
                    clientInterface.sendData(phrase_name);
                    Console.WriteLine("\nSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS\n");
                }
            }

            if (Keyboard.IsKeyDown(Key.X))
            {
                if (startMode)
                {
                    startMode = false;
                    clientInterface.sendData("end");
                    Console.WriteLine("\nEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE\n");
                }
            }

            if (dataReceived)
            {
                // visualize the new body data
                this.kinectBodyView.UpdateBodyFrame(this.bodies);

                // we may have lost/acquired bodies, so update the corresponding gesture detectors
                if (this.bodies != null)
                {
                    // loop through all bodies to see if any of the gesture detectors need to be updated
                    int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;
                    for (int i = 0; i < maxBodies; ++i)
                    {
                        Body body = this.bodies[i];
                        ulong trackingId = body.TrackingId;
                        
                        if (trackingId != 0)
                        {
                            
                            String msg = prepareTcpMessage(body);
                            
                            clientInterface.sendData(msg);
                            /*
                            GestureDetector gd = this.gestureDetectorList[i];
                            if (gd.sw2Done)
                            {
                                Console.WriteLine("SW2 DONE YAAAAYYYY!!!!");
                                gd.sw1Done = gd.ct1Done  = gd.sw2Done = false;
                                sign.Text = "A snake is behind the wall!";
                                clientInterface.sendData("A snake is behind the wall!");
                                clientInterface.disconnect();
                            }
                            if (gd.ct2Done)
                            {
                                sign.Text = "A car crashes into the tree!";
                                Console.WriteLine("CT2 DONE YAAAAYYYY!!!!");
                                gd.sw1Done = gd.ct1Done = gd.ct2Done = false;
                                clientInterface.sendData("A car crashes into the tree!");
                                
                            } */   
                            //Console.WriteLine("FRAME COUNT: " + this.gestureDetectorList[i].frameCount);
                        }
                           
                        // if the current body TrackingId changed, update the corresponding gesture detector with the new value
                        if (trackingId != this.gestureDetectorList[i].TrackingId)
                        {
                            this.gestureDetectorList[i].TrackingId = trackingId;
                            
                            // if the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
                            // if the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
                            this.gestureDetectorList[i].IsPaused = trackingId == 0;
                        }
                    }
                }
            }
        }

        private static int msgCount = 0;
        private String prepareTcpMessage(Body body)
        {
            String msg = "";
            Joint head = body.Joints[JointType.Head];               //3
            Joint neck = body.Joints[JointType.Neck];               //2
            Joint shoulderr = body.Joints[JointType.ShoulderRight]; //8
            Joint shoulderl = body.Joints[JointType.ShoulderLeft];  //4
            Joint spinesh = body.Joints[JointType.SpineShoulder];   //20

            Joint elbowr = body.Joints[JointType.ElbowRight];       //9
            Joint elbowl = body.Joints[JointType.ElbowLeft];        //5
            Joint wristr = body.Joints[JointType.WristRight];       //10
            Joint wristl = body.Joints[JointType.WristLeft];        //6
            Joint handr = body.Joints[JointType.HandRight];         //11
            Joint handl = body.Joints[JointType.HandLeft];          //7
            Joint thumbr = body.Joints[JointType.ThumbRight];       //24
            Joint thumbl = body.Joints[JointType.ThumbLeft];        //22
            Joint tipr = body.Joints[JointType.HandTipRight];       //23
            Joint tipl = body.Joints[JointType.HandTipLeft];        //21

            Joint hipr = body.Joints[JointType.HipRight];           //16
            Joint hipl = body.Joints[JointType.HipLeft];            //12
            Joint spinebase = body.Joints[JointType.SpineBase];     //0
            Joint kneer = body.Joints[JointType.KneeRight];         //17
            Joint kneel = body.Joints[JointType.KneeLeft];          //13

            Joint[] joints = { head, neck, shoulderr, shoulderl, spinesh, elbowr, elbowl, wristr, wristl, handr, handl, thumbr, thumbl, tipr, tipl, hipr, hipl, spinebase, kneer, kneel };
            foreach(Joint j in joints){
                msg += "" + Math.Round(j.Position.X, 5) + " " + Math.Round(j.Position.Y, 5) + " " + Math.Round(j.Position.Z, 5) + " ";
            }
            Console.WriteLine(msgCount++ +" | " + msg.Length);
            return msg;
        }



    }
}
