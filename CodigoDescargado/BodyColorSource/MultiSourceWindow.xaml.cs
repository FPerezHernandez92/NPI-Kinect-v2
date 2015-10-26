using System.Windows.Controls;

namespace BodyColorSource
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.Collections.Generic;

    public partial class MultiSourceWindow : Window, INotifyPropertyChanged
    {
         /// <summary>
        /// Indicates opaque in an opacity mask
        /// </summary>
        private const int OpaquePixel = -1;

        /// <summary>
        /// Size of the RGB pixel in the bitmap
        /// </summary>
        private readonly int bytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;

        /// <summary>
        /// Active Kinect sensor
        /// </summary>
        private KinectSensor kinectSensor = null;

        /// <summary>
        /// Coordinate mapper to map one type of point to another
        /// </summary>
        private CoordinateMapper coordinateMapper = null;

        /// <summary>
        /// Reader for depth/color/body index frames
        /// </summary>
        private MultiSourceFrameReader reader = null;

        /// <summary>
        /// Bitmap to display
        /// </summary>
        private WriteableBitmap bitmap = null;

        /// <summary>
        /// Intermediate storage for receiving depth frame data from the sensor
        /// </summary>
        private ushort[] depthFrameData = null;

        /// <summary>
        /// Intermediate storage for receiving color frame data from the sensor
        /// </summary>
        private byte[] colorFrameData = null;

        /// <summary>
        /// Intermediate storage for receiving body index frame data from the sensor
        /// </summary>
        private byte[] bodyIndexFrameData = null;

        /// <summary>
        /// Intermediate storage for frame data converted to color
        /// </summary>
        private byte[] displayPixels = null;

        /// <summary>
        /// Intermediate storage for the depth to color mapping
        /// </summary>
        private ColorSpacePoint[] colorPoints = null;

        /// <summary>
        /// The time of the first frame received
        /// </summary>
        private long startTime = 0;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;

        /// <summary>
        /// Next time to update FPS/frame time status
        /// </summary>
        private DateTime nextStatusUpdate = DateTime.MinValue;

        /// <summary>
        /// Number of frames since last FPS/frame time status
        /// </summary>
        private uint framesSinceUpdate = 0;

        private DrawingGroup drawingGroup;


        /// <summary>
        /// Timer for FPS calculation
        /// </summary>
        private Stopwatch stopwatch = null;
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly Brush handClosedBrush = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as opened
        /// </summary>
        private readonly Brush handOpenBrush = new SolidColorBrush(Color.FromArgb(128, 0, 255, 0));

        /// <summary>
        /// Brush used for drawing hands that are currently tracked as in lasso (pointer) position
        /// </summary>
        private readonly Brush handLassoBrush = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255));

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently tracked
        /// </summary>
        private readonly Pen trackedBonePen = new Pen(Brushes.Green, 6);

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);
        private const double JointThickness = 3;
        private const double HandSize = 30;
        /// <summary>
        /// Gets the bitmap to display
        /// </summary>
        public ImageSource ImageSource
        {
            get
            {
                return this.bitmap;
            }
        }
        private Body[] bodies = null;
        private byte[] bodyBytespixels = null;
        DrawingVisual drawingVisual;

        RenderTargetBitmap bmp;
        Grid rootGrid;
        Image img;
        Image img2;

        private readonly WriteableBitmap _colorWriteableBitmap;
        private readonly WriteableBitmap _bodyWriteableBitmap;
        public MultiSourceWindow()
        {
             // for Alpha, one sensor is supported
            this.kinectSensor = KinectSensor.Default;

            if (this.kinectSensor != null)
            {
                // get the coordinate mapper
                this.coordinateMapper = this.kinectSensor.CoordinateMapper;

                // open the sensor
                this.kinectSensor.Open();

                FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;

                int depthWidth = depthFrameDescription.Width;
                int depthHeight = depthFrameDescription.Height;
                this.bodies = new Body[this.kinectSensor.BodyFrameSource.BodyCount];

                // allocate space to put the pixels being received and converted
                this.depthFrameData = new ushort[depthWidth*depthHeight];
                this.bodyIndexFrameData = new byte[depthWidth*depthHeight];
                this.displayPixels = new byte[depthWidth*depthHeight*this.bytesPerPixel];
                this.colorPoints = new ColorSpacePoint[depthWidth*depthHeight];

                // create the bitmap to display
                this.bitmap = new WriteableBitmap(depthWidth, depthHeight, 96.0, 96.0, PixelFormats.Bgra32, null);

                FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;

                int colorWidth = colorFrameDescription.Width;
                int colorHeight = colorFrameDescription.Height;

                // allocate space to put the pixels being received
                this.colorFrameData = new byte[colorWidth*colorHeight*this.bytesPerPixel];

                this.reader =
                    this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color |
                                                                 FrameSourceTypes.BodyIndex);

                this.bodyBytespixels = new byte[colorFrameDescription.Width * colorFrameDescription.Height * this.bytesPerPixel];

                // create the bitmap to display
                this.bitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
                drawingVisual = new DrawingVisual();
                this.drawingGroup = new DrawingGroup();
                bmp = new RenderTargetBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Pbgra32);
                rootGrid = new Grid();

                _colorWriteableBitmap = BitmapFactory.New(colorFrameDescription.Width, colorFrameDescription.Height);
                _bodyWriteableBitmap = BitmapFactory.New(colorFrameDescription.Width, colorFrameDescription.Height); 
            }

            this.DataContext = this;

            // set the status text
            InitializeComponent();

            Image.Source = _colorWriteableBitmap;            

        }


        /// <summary>
        /// Execute start up tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.reader != null)
            {
                this.reader.MultiSourceFrameArrived += this.Reader_MultiSourceFrameArrived;
            }
        }

        /// <summary>
        /// Execute shutdown tasks
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.reader != null)
            {
                // MultiSourceFrameReder is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrameReference frameReference = e.FrameReference;

            MultiSourceFrame multiSourceFrame = frameReference.AcquireFrame();
            DepthFrame depthFrame = null;
            ColorFrame colorFrame = null;
            BodyIndexFrame bodyIndexFrame = null;
            BodyFrame bodyFrame = null;

            try
            {
                multiSourceFrame = frameReference.AcquireFrame();

                if (multiSourceFrame != null)
                {
                    // MultiSourceFrame is IDisposable
                    using (multiSourceFrame)
                    {
                        DepthFrameReference depthFrameReference = multiSourceFrame.DepthFrameReference;
                        ColorFrameReference colorFrameReference = multiSourceFrame.ColorFrameReference;
                        BodyIndexFrameReference bodyIndexFrameReference = multiSourceFrame.BodyIndexFrameReference;
                        BodyFrameReference bodyFrameReference = multiSourceFrame.BodyFrameReference;

                        depthFrame = depthFrameReference.AcquireFrame();
                        colorFrame = colorFrameReference.AcquireFrame();
                        bodyIndexFrame = bodyIndexFrameReference.AcquireFrame();
                        bodyFrame = bodyFrameReference.AcquireFrame();


                        if ((depthFrame != null) && (colorFrame != null) && (bodyIndexFrame != null) && (bodyFrame !=null))
                        {
                            this.framesSinceUpdate++;

                            FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                            FrameDescription colorFrameDescription = colorFrame.FrameDescription;
                            FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;

                            int depthWidth = depthFrameDescription.Width;
                            int depthHeight = depthFrameDescription.Height;

                            int colorWidth = colorFrameDescription.Width;
                            int colorHeight = colorFrameDescription.Height;

                            int bodyIndexWidth = bodyIndexFrameDescription.Width;
                            int bodyIndexHeight = bodyIndexFrameDescription.Height;

                            // verify data and write the new registered frame data to the display bitmap
                            if (((depthWidth * depthHeight) == this.depthFrameData.Length) &&
                                ((colorWidth * colorHeight * this.bytesPerPixel) == this.colorFrameData.Length) &&
                                ((bodyIndexWidth * bodyIndexHeight) == this.bodyIndexFrameData.Length))
                            {
                                depthFrame.CopyFrameDataToArray(this.depthFrameData);
                                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                {
                                    colorFrame.CopyRawFrameDataToArray(this.colorFrameData);
                                }
                                else
                                {
                                    colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
                                }

                                bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);

                                this.coordinateMapper.MapDepthFrameToColorSpace(this.depthFrameData, this.colorPoints);

                                Array.Clear(this.displayPixels, 0, this.displayPixels.Length);

                                // loop over each row and column of the depth
                                //PlayerToDepth(depthHeight, depthWidth, colorWidth, colorHeight);


                                this.bitmap.WritePixels(
                                    new Int32Rect(0, 0, depthWidth, depthHeight),
                                    this.displayPixels,
                                    depthWidth * this.bytesPerPixel,
                                    0);

                                using (DrawingContext dc = this.drawingGroup.Open())
                                {
                                    // Draw a transparent background to set the render size

                                    dc.DrawRectangle(Brushes.Transparent, null, new Rect(0.0, 0.0, depthWidth, depthHeight));

                                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                                    // As long as those body objects are not disposed and not set to null in the array,
                                    // those body objects will be re-used.
                                    bodyFrame.GetAndRefreshBodyData(this.bodies);

                                    foreach (Body body in this.bodies)
                                    {
                                        if (body.IsTracked)
                                        {
                                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                                            // convert the joint points to depth (display) space
                                            var jointPoints = new Dictionary<JointType, Point>();
                                            foreach (JointType jointType in joints.Keys)
                                            {
                                                ColorSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToColorSpace(joints[jointType].Position);
                                                jointPoints[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);
                                            }

                                            this.DrawBody(joints, jointPoints, dc);

                                            this.DrawHand(body.HandLeftState, jointPoints[JointType.HandLeft], dc);
                                            this.DrawHand(body.HandRightState, jointPoints[JointType.HandRight], dc);
                                        }
                                    }

                                    // prevent drawing outside of our render area
                                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, depthWidth, depthHeight));
                                    img2 = new Image { Source = new DrawingImage(drawingGroup), Width = depthWidth, Height = depthHeight, Stretch = Stretch.UniformToFill };
                                    rootGrid.Children.Clear();
                                    rootGrid.Children.Add(img2);
                                    rootGrid.Measure(new Size(img2.Width, img2.Height));
                                    rootGrid.Arrange(new Rect(0, 0, img2.Width, img2.Height));
                                    bmp.Clear();
                                    bmp.Render(rootGrid);
                                    bmp.CopyPixels(this.bodyBytespixels, depthWidth * this.bytesPerPixel,
                                        0);
                                    _bodyWriteableBitmap.FromByteArray(this.bodyBytespixels);
                                }

                                _colorWriteableBitmap.FromByteArray(this.displayPixels);
                                var rec = new Rect(0, 0, depthWidth, depthHeight);
                                using (_colorWriteableBitmap.GetBitmapContext())
                                {
                                    using (_bodyWriteableBitmap.GetBitmapContext())
                                    {
                                        _colorWriteableBitmap.Blit(rec, _bodyWriteableBitmap, rec, WriteableBitmapExtensions.BlendMode.Additive);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                // ignore if the frame is no longer available
            }
            finally
            {
                // MultiSourceFrame, DepthFrame, ColorFrame, BodyIndexFrame are IDispoable
                if (depthFrame != null)
                {
                    depthFrame.Dispose();
                    depthFrame = null;
                }

                if (colorFrame != null)
                {
                    colorFrame.Dispose();
                    colorFrame = null;
                }

                if (bodyIndexFrame != null)
                {
                    bodyIndexFrame.Dispose();
                    bodyIndexFrame = null;
                }

                if (multiSourceFrame != null)
                {
                    multiSourceFrame.Dispose();
                    multiSourceFrame = null;
                }
            }
        }

        private void PlayerToDepth(int depthHeight, int depthWidth, int colorWidth, int colorHeight)
        {
            for (int y = 0; y < depthHeight; ++y)
            {
                for (int x = 0; x < depthWidth; ++x)
                {
                    // calculate index into depth array
                    int depthIndex = (y*depthWidth) + x;

                    byte player = this.bodyIndexFrameData[depthIndex];

                    // if we're tracking a player for the current pixel, sets its color and alpha to full
                    if (player != 0xff)
                    {
                        // retrieve the depth to color mapping for the current depth pixel
                        ColorSpacePoint colorPoint = this.colorPoints[depthIndex];

                        // make sure the depth pixel maps to a valid point in color space
                        int colorX = (int) Math.Floor(colorPoint.X + 0.5);
                        int colorY = (int) Math.Floor(colorPoint.Y + 0.5);
                        if ((colorX >= 0) && (colorX < colorWidth) && (colorY >= 0) && (colorY < colorHeight))
                        {
                            // calculate index into color array
                            int colorIndex = ((colorY*colorWidth) + colorX)*this.bytesPerPixel;

                            // set source for copy to the color pixel
                            int displayIndex = depthIndex*this.bytesPerPixel;
                            this.displayPixels[displayIndex] = this.colorFrameData[colorIndex];
                            this.displayPixels[displayIndex + 1] = this.colorFrameData[colorIndex + 1];
                            this.displayPixels[displayIndex + 2] = this.colorFrameData[colorIndex + 2];
                            this.displayPixels[displayIndex + 3] = 0xff;
                        }
                    }
                }
            }
        }

        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext drawingContext)
        {
            // Draw the bones

            // Torso
            this.DrawBone(joints, jointPoints, JointType.Head, JointType.Neck, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.Neck, JointType.SpineShoulder, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.SpineMid, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineMid, JointType.SpineBase, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineShoulder, JointType.ShoulderLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.SpineBase, JointType.HipLeft, drawingContext);

            // Right Arm    
            this.DrawBone(joints, jointPoints, JointType.ShoulderRight, JointType.ElbowRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.ElbowRight, JointType.WristRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.HandRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.HandRight, JointType.HandTipRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristRight, JointType.ThumbRight, drawingContext);

            // Left Arm
            this.DrawBone(joints, jointPoints, JointType.ShoulderLeft, JointType.ElbowLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.ElbowLeft, JointType.WristLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.HandLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.HandLeft, JointType.HandTipLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.WristLeft, JointType.ThumbLeft, drawingContext);

            // Right Leg
            this.DrawBone(joints, jointPoints, JointType.HipRight, JointType.KneeRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeRight, JointType.AnkleRight, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleRight, JointType.FootRight, drawingContext);

            // Left Leg
            this.DrawBone(joints, jointPoints, JointType.HipLeft, JointType.KneeLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.KneeLeft, JointType.AnkleLeft, drawingContext);
            this.DrawBone(joints, jointPoints, JointType.AnkleLeft, JointType.FootLeft, drawingContext);

            // Draw the joints
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                if (trackingState == TrackingState.Tracked)
                {
                    drawBrush = this.trackedJointBrush;
                }
                else if (trackingState == TrackingState.Inferred)
                {
                    drawBrush = this.inferredJointBrush;
                }

                if (drawBrush != null)
                {
                    drawingContext.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }

        /// <summary>
        /// Draws one bone of a body (joint to joint)
        /// </summary>
        /// <param name="joints">joints to draw</param>
        /// <param name="jointPoints">translated positions of joints to draw</param>
        /// <param name="jointType0">first joint of bone to draw</param>
        /// <param name="jointType1">second joint of bone to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBone(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, JointType jointType0, JointType jointType1, DrawingContext drawingContext)
        {
            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == TrackingState.NotTracked ||
                joint1.TrackingState == TrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == TrackingState.Inferred &&
                joint1.TrackingState == TrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = this.inferredBonePen;
            if ((joint0.TrackingState == TrackingState.Tracked) && (joint1.TrackingState == TrackingState.Tracked))
            {
                drawPen = this.trackedBonePen;
            }

            drawingContext.DrawLine(drawPen, jointPoints[jointType0], jointPoints[jointType1]);
        }

        /// <summary>
        /// Draws a hand symbol if the hand is tracked: red circle = closed, green circle = opened; blue circle = lasso
        /// </summary>
        /// <param name="handState">state of the hand</param>
        /// <param name="handPosition">position of the hand</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawHand(HandState handState, Point handPosition, DrawingContext drawingContext)
        {
            switch (handState)
            {
                case HandState.Closed:
                    drawingContext.DrawEllipse(this.handClosedBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Open:
                    drawingContext.DrawEllipse(this.handOpenBrush, null, handPosition, HandSize, HandSize);
                    break;

                case HandState.Lasso:
                    drawingContext.DrawEllipse(this.handLassoBrush, null, handPosition, HandSize, HandSize);
                    break;
            }
        }
    }
}
