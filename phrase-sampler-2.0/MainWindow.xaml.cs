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
    using System.Windows.Media;
    using System.Windows.Shapes;
    using Microsoft.Kinect.VisualGestureBuilder;
    using System.Windows.Forms;
    using System.Threading;
    using System.Windows.Media.Imaging;
    using System.IO;
    using System.Diagnostics;
    using System.Text;
   //#using Microsoft.Xna.Framework;
    //using SlimDX;
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

        /// <summary>
        ///  Instantiate a Reader for the RGB and Depth Profiles
        /// </summary>
        private MultiSourceFrameReader _reader;

        /// <summary> Current status text to display </summary>
        private string statusText = null;

        /// <summary> KinectBodyView object which handles drawing the Kinect bodies to a View box in the UI </summary>
        private KinectBodyView kinectBodyView = null;
        
        /// <summary> List of gesture detectors, there will be one detector created for each potential body (max of 6) </summary>
        private List<GestureDetector> gestureDetectorList = null;

        private ClientInterface clientInterface = null;

        private bool startMode = false;
        private bool leftHanded = false;

        private static Queue<BitmapFrame> imageQueue = new Queue<BitmapFrame>();
        static ReaderWriterLockSlim _rw = new ReaderWriterLockSlim();

        private string[] phrase_list = 
        {
            "Alligator_behind_black_wall","Alligator_behind_blue_wagon","Alligator_behind_chair","Alligator_behind_orange_wagon","Alligator_behind_wall","Alligator_in_box","Alligator_in_orange_flowers","Alligator_in_wagon","Alligator_on_bed","Alligator_on_blue_wall","Alligator_under_green_bed","Black_Alligator_behind_orange_wagon","Black_cat_behind_green_bed","Black_cat_in_blue_wagon","Black_cat_on_green_bed","Black_Snake_under_blue_chair","Black_Spider_in_white_flowers","Blue_Alligator_on_green_wall","Blue_Spider_on_green_box","cat_behind_orange_bed","Cat_behind_bed","Cat_behind_box","Cat_behind_flowers","Cat_on_blue_bed","Cat_on_green_wall","Cat_on_wall","Cat_under_blue_bed","Cat_under_chair","cat_under_orange_chair","Green_Alligator_under_blue_flowers","Green_Snake_under_blue_chair","Green_snake_under_blue_chair","Green_Spider_under_orange_chair","Orange_Alligator_in_green_flowers","Orange_Snake_under_blue_flowers","Orange_Spider_in_green_box","Orange_spider_under_green_flowers","Snake_behind_wall","Snake_in_flowers","Snake_in_green_wagon","Snake_on_box","Snake_under_bed","Snake_under_black_chair","Snake_under_blue_chair","Snake_under_blue_flowers","Snake_under_chair","Spider_under_bed","Spider_in_blue_box","Spider_in_box","Spider_in_green_box","Spider_in_orange_flowers","Spider_on_chair","Spider_on_wall","Spider_on_white_wall","Spider_under_blue_chair","Spider_under_wagon","White_snake_in_blue_flowers","White_Alligator_on_blue_wall","White_cat_in_green_box","White_cat_on_orange_wall"
        };

        private int current_phrase_index = 0;

        private int session_number;

        private bool paused = false;

        private ColorFrameWriter colorFrameWriter;
        private DepthFrameWriter depthFrameWriter;
        private JointDataWriter jointDataWriter;

        private String mainDir;

        private int totalCapturedFrames_joints;
        private int totalCapturedFrames_color;
        private int totalCapturedFrames_depth;

        private Queue<byte[]> colorQueue = new Queue<byte[]>();
        private Queue<ushort[]> depthQueue = new Queue<ushort[]>();
        int dimension = 0, widthD = 0, heightD = 0;
        ushort minDepth = 0, maxDepth = 0;
        //############# PHRASE NAME ########################### PHRASE NAME ########################## PHRASE NAME ########################################

        private String phrase_name = "Alligator_behind_chair";
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

            this._reader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth);
            this._reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

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
            //this.clientInterface = ClientInterface.getClientInstance();
            //clientInterface.connect();

            //Console.WriteLine("connect to the client interface \n " + clientInterface.GetHashCode() + "\n");            
            //clientInterface.disconnect();

            // create a gesture detector for each body (6 bodies => 6 detectors) and create content controls to display results in the UI
            //int col0Row = 0, col1Row = 0;

            this.colorFrameWriter = new ColorFrameWriter();
            this.depthFrameWriter = new DepthFrameWriter();
            this.jointDataWriter = new JointDataWriter();
            this.totalCapturedFrames_joints = 0;
            this.totalCapturedFrames_color = 0;
            this.totalCapturedFrames_depth = 0;

            session_number = 1;

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

            prevDeleteButton.Click += deletePreviousSample;
            currentPhraseName.Text = phrase_list[current_phrase_index];

            String current_phrase = phrase_list[current_phrase_index];
            char[] delims = { '_' };
            String[] words = current_phrase.Split(delims);

            StringBuilder builder = new StringBuilder();
            foreach (string s in words)
            {
                builder.Append(s.ToLower()).Append(" ");
            }
            String cleanedPhrase = builder.ToString().TrimEnd(new char[] { ' ' });
            cleanedPhrase += ".png";
            //Console.WriteLine("!!!!!!!!!!!!!@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + cleanedPhrase);

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(System.IO.Path.Combine(
                @"C:\Users\aslr\Documents\GitHub\SignLanguageRecognition\phrase-sampler-2.0\phrase_images", cleanedPhrase));
            image.EndInit();
            phraseImage.Source = image;

            phrase_name = phrase_list[current_phrase_index];
            /*clientInterface.sendData("new_phrase");
            clientInterface.sendData(phrase_name);*/

            //String mainDir = System.IO.Path.Combine(@"C:\Users\aslr\Documents\aslr-data", phrase_name);
            //String colorDir = System.IO.Path.Combine(mainDir, "color");
            //String depthDir = System.IO.Path.Combine(mainDir, "depth");
            //Console.WriteLine("&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&&& " + kinect.DepthStream.FrameWidth);\

            mainDir = System.IO.Path.Combine(@"D:\z-alsr-data", phrase_name);
            //String colorDir = System.IO.Path.Combine(mainDir, "color");
            //String depthDir = System.IO.Path.Combine(mainDir, "depth");
            System.IO.Directory.CreateDirectory(mainDir);
            //System.IO.Directory.CreateDirectory(colorDir);
            //System.IO.Directory.CreateDirectory(depthDir);

            //System.IO.Directory.CreateDirectory(mainDir);
            //System.IO.Directory.CreateDirectory(colorDir);
            //System.IO.Directory.CreateDirectory(depthDir);

            colorFrameWriter.setCurrentPhrase(phrase_name);
            depthFrameWriter.setCurrentPhrase(phrase_name);
            jointDataWriter.setCurrentPhrase(phrase_name);
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

        

        // Handles the image and depth frame data arriving from the sensor
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (Keyboard.IsKeyDown(Key.Z))
            {
                if (!scrClicked)
                    scrClicked = true;
            }
            if (Keyboard.IsKeyDown(Key.X))
            {
                if (scrClicked)
                    scrClicked = false;
            }

            var reference = e.FrameReference.AcquireFrame();
            //ColorFrame tempFrame;
            /// Handle the colour frame
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if ((!paused && startMode) || scrClicked)
                    {              
                        int width = frame.FrameDescription.Width;
                        int height = frame.FrameDescription.Height;
                       
                        PixelFormat format = PixelFormats.Bgr32;

                        byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

                        if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
                        {
                            frame.CopyRawFrameDataToArray(pixels);
                        }
                        else
                        {
                            frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
                        }

                        int stride = width * format.BitsPerPixel / 8;
                        //_rw.EnterWriteLock();
                        //imageQueue.Enqueue(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride)));
                        //Thread.Sleep(100);
                        //_rw.ExitWriteLock();
                        colorQueue.Enqueue(pixels);
                        //this.colorFrameWriter.ProcessWrite(pixels);
                        //saveRGB(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride)));
                    }
                }
            }

            /// Handle the depth frame 
            using (var frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if ((!paused && startMode) || scrClicked)
                    {
                        widthD = frame.FrameDescription.Width;
                        //Console.WriteLine("!!!!!!!!!!!!!!!!!!%$^&***********************************" + width);
                        heightD = frame.FrameDescription.Height;
                        //Console.WriteLine("!!!!!!!!!!!!!!!!!!%$^&****************************HIEIGHT*" + height);

                        PixelFormat format = PixelFormats.Bgr32;

                        minDepth = frame.DepthMinReliableDistance;
                        maxDepth = frame.DepthMaxReliableDistance;

                        ushort[] pixelData = new ushort[widthD * heightD];
                        //byte[] pixels = new byte[width * height * (format.BitsPerPixel + 7) / 8];
                        dimension = widthD * heightD * (format.BitsPerPixel + 7) / 8;
                        frame.CopyFrameDataToArray(pixelData);

                        depthQueue.Enqueue(pixelData);

                        /*
                        int colorIndex = 0;
                        for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
                        {
                            ushort depth = pixelData[depthIndex];

                            byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                            pixels[colorIndex++] = intensity; // Blue
                            pixels[colorIndex++] = intensity; // Green
                            pixels[colorIndex++] = intensity; // Red

                            ++colorIndex;
                        }

                        int stride = width * format.BitsPerPixel / 8;
                        this.depthFrameWriter.ProcessWrite(pixels);
                         * */
                    }
                    /*
                    BitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride)));

                    string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                    string path = System.IO.Path.Combine(myPhotos, "Image 2.png");
                    try
                    {
                        using (FileStream fs = new FileStream(path, FileMode.Create))
                        {
                            encoder.Save(fs);
                        }


                    }
                    catch (IOException details)
                    {
                        Console.Write(details.ToString());

                    }
                    if (path == null)
                        System.Console.WriteLine("Image was not taken.");

                    //return BitmapSource.Create(width, height, 96, 96, format, null, pixels, stride);
                    */
                }
            }
            //saveData(colorQueue, depthQueue, dimension, minDepth, maxDepth, widthD, heightD);
        }

        public void saveData(Queue<byte[]> colorQueue, Queue<ushort[]> depthQueue, int depthArrDimension, ushort minDepth, ushort maxDepth, int widthD, int heightD) {
            String filePathColor = mainDir + "\\" + session_number + "\\color\\";
            System.IO.Directory.CreateDirectory(filePathColor);

            String filePathDepth = mainDir + "\\" + session_number + "\\depth\\";
            System.IO.Directory.CreateDirectory(filePathDepth);

            int size = colorQueue.Count;
            for (int x = 0; x < size; x++)
            {
                byte[] pixels = colorQueue.Dequeue();
                this.colorFrameWriter.ProcessWrite(pixels, session_number);
            }

            int size2 = depthQueue.Count;
            for (int x = 0; x < size2; x++)
            {
                ushort[] pixelData = depthQueue.Dequeue();
                byte[] pixels = new byte[depthArrDimension];
                int colorIndex = 0;
                for (int depthIndex = 0; depthIndex < pixelData.Length; ++depthIndex)
                {
                    ushort depth = pixelData[depthIndex];

                    byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? depth : 0);

                    pixels[colorIndex++] = intensity; // Blue
                    pixels[colorIndex++] = intensity; // Green
                    pixels[colorIndex++] = intensity; // Red

                    ++colorIndex;
                }

                PixelFormat format = PixelFormats.Bgr32;
                int stride = widthD * format.BitsPerPixel / 8;
                this.depthFrameWriter.ProcessWrite(pixels, session_number); 
            }
            session_number++;
        }

        static int imageCounter = 0;
        static int imageCounter1 = 0;
        
        /// <summary>
        /// Thread to push the RGB frames
        /// </summary>
        /// <param name="frame"></param>
        private void saveRGB(BitmapFrame b)
        {
            /*
            string myPhotos = "C:\\Users\\ASLR\\Documents\\z-aslr-data";//Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //Console.WriteLine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
            //Console.WriteLine(myPhotos);
            string path = System.IO.Path.Combine(myPhotos, "Image_" + imageCounter + ".png");
            imageCounter++;
            Console.WriteLine("Image counter = " + imageCounter);
            imageCounter1++;
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Create))
                {
                            
                    if (imageQueue.Count == 0)
                    {
                        BitmapEncoder encoder = new PngBitmapEncoder();
                        //_rw.EnterReadLock();
                        //encoder.Frames.Add(imageQueue.Dequeue());
                        encoder.Frames.Add(b);
                        //Thread.Sleep(100);
                        //_rw.ExitReadLock();
                        encoder.Save(fs);
                        fs.Flush();
                        fs.Close();
                    }
                }

            }
            catch (IOException details)
            {
                Console.Write(details.ToString());

            }
            */
            Console.WriteLine("Color..........." + totalCapturedFrames_color++);
            //this.colorFrameWriter.ProcessWrite(b);

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

            if (this.bodies != null)
            {
                int maxBodies = this.kinectSensor.BodyFrameSource.BodyCount;
                for (int i = 0; i < maxBodies; ++i)
                {
                    Body body = this.bodies[i];
                    SolidColorBrush gSolidColor = new SolidColorBrush();
                    gSolidColor.Color = Color.FromRgb(0, 255, 0);
                    SolidColorBrush rSolidColor = new SolidColorBrush();
                    rSolidColor.Color = Color.FromRgb(255, 0, 0);

                    Joint handr = body.Joints[JointType.HandRight];         //11
                    Joint handl = body.Joints[JointType.HandLeft];          //7
                    Joint thumbr = body.Joints[JointType.ThumbRight];       //24
                    Joint thumbl = body.Joints[JointType.ThumbLeft];        //22
                    Joint tipr = body.Joints[JointType.HandTipRight];       //23
                    Joint tipl = body.Joints[JointType.HandTipLeft];        //21

                    Joint hipr = body.Joints[JointType.HipRight];           //16
                    Joint hipl = body.Joints[JointType.HipLeft];            //12
                    Joint spinebase = body.Joints[JointType.SpineBase];     //0
                    Joint spinemid = body.Joints[JointType.SpineMid];

                    if(!paused)
                    {
                        double spineDifferenceY = Math.Abs(spinebase.Position.Y - spinemid.Position.Y);
                        double distFromBase = (spineDifferenceY * 2.0) / 3.0; //Take 2/3rds the distance from the spine base.
                        double threshold = spinebase.Position.Y + distFromBase;

                        double handlY = handl.Position.Y;
                        double handrY = handr.Position.Y;

                        double trig_hand, non_trig_hand;

                        bool value = (bool) dominantHand.IsChecked;
                        if (value)
                        {
                            leftHanded = true;
                            dominantHandText.Text = "Left Handed.";
                        }
                        else
                        {
                            leftHanded = false;
                            dominantHandText.Text = "Right Handed.";
                        }

                        if (leftHanded)
                        {
                            trig_hand = handlY;
                            non_trig_hand = handrY;
                        }
                        else
                        {
                            trig_hand = handrY;
                            non_trig_hand = handlY;
                        }

                        if (threshold > trig_hand)
                        {
                            //Console.WriteLine("YESS!");
                            rectangleFlag.Fill = rSolidColor;
                            if (textFlag.Text == "Stopped.")
                            {
                                //First time, when beginning
                                textFlag.Text = "Ready!";
                            }
                            else if (textFlag.Text == "Started!")
                            {
                                if (!raisedLeftHand)
                                {
                                    //Erase the session data

                                    this.jointDataWriter.deleteLastSample(session_number); //clientInterface.sendData("delete");
                                    colorQueue.Clear();
                                    depthQueue.Clear();
                                    startMode = false;
                                    textFlag.Text = "Erased data, and ready!";
                                    raisedLeftHand = false;
                                }
                                else if (raisedLeftHand)
                                {
                                    //Save the session data
                                    startMode = false;
                                    this.jointDataWriter.endPhrase(); //clientInterface.sendData("end");
                                    Console.WriteLine("\nEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE\n");
                                    saveData(colorQueue, depthQueue, dimension, minDepth, maxDepth, widthD, heightD);
                                    textFlag.Text = "Saved data, and ready!";
                                    raisedLeftHand = false;
                                }

                            }
                        }
                        else if (threshold < trig_hand && textFlag.Text != "Stopped.")
                        {
                            //Begin the data collection.
                            if (!startMode)
                            {
                                textFlag.Text = "Started!";
                                startMode = true;
                                String filePath = mainDir + "\\" + session_number;
                                System.IO.Directory.CreateDirectory(filePath);
                                this.jointDataWriter.startNewPhrase(session_number); //clientInterface.sendData("start");
                                //clientInterface.sendData(phrase_name);
                                Console.WriteLine("\nSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSSS\n");
                            }
                            rectangleFlag.Fill = gSolidColor;
                            textFlag.Text = "Started!";
                            if (threshold < non_trig_hand)
                            {
                                raisedLeftHand = true;
                            }
                        }

                    }
                    else
                    {
                        this.jointDataWriter.pause(); //clientInterface.sendData("paused...");
                    }
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
                            if (startMode)
                            {
                                Console.WriteLine("Joints.........." + totalCapturedFrames_joints++);
                            }
                            this.jointDataWriter.writeData(msg + "\n"); //clientInterface.sendData(msg);

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

        bool raisedLeftHand = false;
        private String checkForHandLocation(Body body)
        {
            
            return "";
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
            
            double l0 = Math.Round(Math.Sqrt(Math.Pow((neck.Position.X - shoulderl.Position.X), 2) + Math.Pow((neck.Position.Y - shoulderl.Position.Y), 2) + Math.Pow((neck.Position.Z - shoulderl.Position.Z), 2)), 5);
            double r0 = Math.Round(Math.Sqrt(Math.Pow((neck.Position.X - shoulderr.Position.X), 2) + Math.Pow((neck.Position.Y - shoulderr.Position.Y), 2) + Math.Pow((neck.Position.Z - shoulderr.Position.Z), 2)), 5);
            double l1 = Math.Round(Math.Sqrt(Math.Pow((shoulderl.Position.X - elbowl.Position.X), 2) + Math.Pow((shoulderl.Position.Y - elbowl.Position.Y), 2) + Math.Pow((shoulderl.Position.Z - elbowl.Position.Z), 2)), 5);
            double r1 = Math.Round(Math.Sqrt(Math.Pow((shoulderr.Position.X - elbowr.Position.X), 2) + Math.Pow((shoulderr.Position.Y - elbowr.Position.Y), 2) + Math.Pow((shoulderr.Position.Z - elbowr.Position.Z), 2)), 5);
            double l2 = Math.Round(Math.Sqrt(Math.Pow((elbowl.Position.X - wristl.Position.X), 2) + Math.Pow((elbowl.Position.Y - wristl.Position.Y), 2) + Math.Pow((elbowl.Position.Z - wristl.Position.Z), 2)), 4);
            double r2 = Math.Round(Math.Sqrt(Math.Pow((elbowr.Position.X - wristr.Position.X), 2) + Math.Pow((elbowr.Position.Y - wristr.Position.Y), 2) + Math.Pow((elbowr.Position.Z - wristr.Position.Z), 2)), 4);

            double norm = (l0 + l1 + l2 + r0 + r1 + r2) / 2.0;

            Joint[] joints = { head, neck, shoulderr, shoulderl, spinesh, elbowr, elbowl, wristr, wristl, handr, handl, thumbr, thumbl, tipr, tipl, hipr, hipl, spinebase, kneer, kneel };
            String msg_points = "";
            foreach(Joint j in joints){
                msg_points += "" + Math.Round(j.Position.X, 5) + " " + Math.Round(j.Position.Y, 5) + " " + Math.Round(j.Position.Z, 5) + " ";
            }
            Console.WriteLine(msgCount++ +" | " + msg.Length);

            //------------------------------------------------------------------------------------------------------------------------------------
            //------------------------------------------------------------------------------------------------------------------------------------
            JointType[] joint_types = {JointType.Head, JointType.Neck, JointType.ShoulderRight, JointType.ShoulderLeft, JointType.SpineShoulder, JointType.ElbowRight, JointType.ElbowLeft, JointType.WristRight, JointType.WristLeft, JointType.HandRight, JointType.HandLeft, JointType.ThumbRight, JointType.ThumbLeft, JointType.HandTipRight, JointType.HandTipLeft, JointType.HipRight, JointType.HipLeft, JointType.SpineBase };//, JointType.KneeRight, JointType.KneeLeft };
            int joint_count = 0;
            foreach (JointType j in joint_types)
            {
                Microsoft.Kinect.Vector4 quat = body.JointOrientations[j].Orientation;
                double msg_w = Math.Round( quat.W, 7 );
                double msg_x = Math.Round( quat.X, 7 );
                double msg_y = Math.Round( quat.Y, 7 );
                double msg_z = Math.Round( quat.Z, 7 );
                //double msg_x = Math.Round((j.Position.X - neck.Position.X) / norm, 5);double msg_y = Math.Round((j.Position.Y - neck.Position.Y) / norm, 5);double msg_z = Math.Round((j.Position.Z - neck.Position.Z) / norm, 5);
                msg += "" + msg_w + " " + msg_x + " " + msg_y + " " + msg_z + " ";
                joint_count++;
            }
            //Console.WriteLine(msgCount++ +" | " + msg.Length + " | " + joint_count);

            msg = msg + " ||| " + msg_points;
            return msg;
        }


        private float my_clamp(float val, float min, float max)
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        private void deletePreviousSample(object sender, RoutedEventArgs e)
        {
            this.jointDataWriter.deleteLastSample(session_number); //clientInterface.sendData("delete");
            startMode = false;
            textFlag.Text = "Erased previous sample, and ready!";
        }

        private void changePhraseName(object sender, RoutedEventArgs e)
        {
            ShowDialog("Change Phrase name", "Phrase Change");
        }

        public string ShowDialog(string text, string caption)
        {
            Form prompt = new Form()
            {
                Width = 500,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterScreen
            };
            System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label() { Left = 50, Top = 20, Text = text };
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox() { Left = 50, Top = 50, Width = 400 };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "Ok", Left = 350, Width = 100, Top = 70, DialogResult = System.Windows.Forms.DialogResult.OK };
            confirmation.Click += (sender, e) => { prompt.Close(); };
            prompt.Controls.Add(textBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK ? textBox.Text : "";
        }

        private void nextPhrase_Click(object sender, RoutedEventArgs e)
        {
            current_phrase_index++;
            if (current_phrase_index == phrase_list.Length)
                current_phrase_index = 0;
            currentPhraseName.Text = phrase_list[current_phrase_index];
            phrase_name = phrase_list[current_phrase_index];
            /*clientInterface.sendData("new_phrase");
            clientInterface.sendData(phrase_name);*/

            String current_phrase = phrase_list[current_phrase_index];
            char[] delims = { '_' };
            String[] words = current_phrase.Split(delims);

            StringBuilder builder = new StringBuilder();
            foreach (string s in words)
            {
                builder.Append(s.ToLower()).Append(" ");
            }
            String cleanedPhrase = builder.ToString().TrimEnd(new char[] { ' ' });
            cleanedPhrase += ".png";
            //Console.WriteLine("!!!!!!!!!!!!!@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@" + cleanedPhrase);

            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(System.IO.Path.Combine(
                @"C:\Users\aslr\Documents\GitHub\SignLanguageRecognition\phrase-sampler-2.0\phrase_images", cleanedPhrase));
            image.EndInit();
            phraseImage.Source = image;

            session_number = 1;
            mainDir = System.IO.Path.Combine(@"D:\z-alsr-data", phrase_name);

            Directory.CreateDirectory(System.IO.Path.Combine(@"D:\z-alsr-data", phrase_name));
            colorFrameWriter.setCurrentPhrase(phrase_name);
            depthFrameWriter.setCurrentPhrase(phrase_name);
            jointDataWriter.setCurrentPhrase(phrase_name);
        }

        private void pauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!paused)
            {
                paused = true;
                SolidColorBrush bSolidColor = new SolidColorBrush();
                bSolidColor.Color = Color.FromRgb(0, 0, 0);
                rectangleFlag.Fill = bSolidColor;
            }
            else
            {
                paused = false;
                SolidColorBrush rSolidColor = new SolidColorBrush();
                rSolidColor.Color = Color.FromRgb(255, 0, 0);
                rectangleFlag.Fill = rSolidColor;
            }
        }

        private bool scrClicked = false;
      
        private void take_screenshot(object sender, RoutedEventArgs e)
        {
            scrClicked = true;            
        }

        private void off_screenshot(object sender, RoutedEventArgs e)
        {
            scrClicked = false;
        }


    }

}

