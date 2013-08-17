using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Devices;
using Microsoft.Devices.Sensors;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Xna.Framework;
using PhoneApp1.Resources;
using Windows.Phone.Media.Capture;
using Windows.ApplicationModel.Store;


namespace PhoneApp1
{
    public partial class MainPage : PhoneApplicationPage
    {
        private Motion motionSensor;
        private PhotoCamera camera;
        private RotateTransform rotation;
        private VideoBrush cameraViewBrush;
        private PurchaseInterface purchaser;
        private double camHeight;
        private int run;
        private double pitch;
        private double objDistance;
        private double objHeight;
        private double run0angle;
        private double run1angle;
        private bool adFailed;
        
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            rotation = new RotateTransform();
            cameraViewBrush = new VideoBrush();
            purchaser = new PurchaseInterface();
            camHeight = 0;
            run = 0;
            pitch = 0;
            adFailed = false;
        }
        //Code for initialization, capture completed, image availability events; also setting the source for the viewfinder.
        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            capture.IsEnabled = false;
            camHeightInput.IsEnabled = false;
            TargetDot2.Opacity = 0;
            checkForActiveProducts();
            // Check to see if the camera is available on the phone.
            if (PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true && Motion.IsSupported == true)
            {
                // Initialize the camera, when available.
                camera = new Microsoft.Devices.PhotoCamera(CameraType.Primary);

                //Set the VideoBrush source to the camera.
                cameraViewBrush.SetSource(camera);
                cameraViewBrush.Stretch = Stretch.UniformToFill;
                cameraView.Background = cameraViewBrush;
                motionSensor = new Motion();
                motionSensor.TimeBetweenUpdates = TimeSpan.FromMilliseconds(20);
                motionSensor.CurrentValueChanged += new EventHandler<SensorReadingEventArgs<MotionReading>>(motionActor);
                motionSensor.Start();
                camHeightInput.IsEnabled = true;
                MessageBox.Show("Hello, welcome to Measure180! To use this app you must roughly know the height you are holding the phone (camHeight),"+
                    " you may use any unit. Hold the phone at your specified height and point the dot at the base of the object and hit capture,"+
                    " that will give you the distance. If the object is not at ground level the distance will be off, you must subtract off the height at which it is off the ground from the camHeight"+
                    " and input that as the new camHeight. To measure height you must move the dot to the top of the of the object after capturing distance and hit capture again. IMPORTANT: YOU MUST NOT MOVE YOUR PHONE from the initial camHeight you set when you captured distance" +
                    " you have to angle the phone until the dot touches the top of the object or else the measurement will be off.\n\n"+
                    " TL;DR: point dot at bottom of object, capture, top of object, capture. If the measurement is horribly off, try reading the tutorial!");
                MessageBox.Show("Instructions were on the last page, don't blame me if you didn't read it and start complaining about bad measurements. This app is as accurate as the user is willing to be, thus a tripod is recommended for super accurate measurements.");
            }
            else
            {
                MessageBox.Show("Motion API and/or primary camera is not supported on this device. Application can not run!");
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
        }

        private void capture_Click(object sender, RoutedEventArgs e)
        {
            if (camHeight <= 0)
            {
                MessageBox.Show("Please enter a valid camera height first! The height may not be less than or equal to 0");
            }
            else if (run == 0)
            {
                run0angle = MathHelper.ToDegrees((float)pitch);
                if (run0angle >= 90||run0angle<0)
                {
                    MessageBox.Show("For distance calculation pitch must be less than 90 degrees and greater than or equal to 0 degrees.");
                    return;
                }
                objDistance = Math.Tan(MathHelper.ToRadians((float)run0angle)) * camHeight;
                run = 1;
                distanceOut.Text = Convert.ToString(objDistance);
                TargetDot2.Opacity = 1;
                heightOut.Text = "";
            }
            else if (run == 1)
            {
                run1angle = MathHelper.ToDegrees((float)pitch) - run0angle;
                double side1 = Math.Sqrt(Math.Pow(camHeight, 2) + Math.Pow(objDistance, 2));
                double angle1 = 180 - (run0angle + run1angle);
                objHeight = side1 * Math.Sin(MathHelper.ToRadians((float)run1angle)) / Math.Sin(MathHelper.ToRadians((float)angle1));
                run = 0;
                heightOut.Text = Convert.ToString(objHeight);
                TargetDot2.Opacity = 0;
            }
        }
        private void motionActor(object sender, SensorReadingEventArgs<MotionReading> e)
        {
            Dispatcher.BeginInvoke(() => motionValue(e.SensorReading));
        }
        private void motionValue(MotionReading e)
        {
            if (motionSensor.IsDataValid)
            {
                if (Orientation==PageOrientation.PortraitUp || Orientation==PageOrientation.PortraitDown)
                {
                    pitch = e.Attitude.Pitch;
                    PitchOut.Text = Convert.ToString(MathHelper.ToDegrees((float)pitch));
                }
                else
                {
                    pitch = Math.Abs(e.Attitude.Roll);
                    PitchOut.Text = Convert.ToString(MathHelper.ToDegrees((float)pitch));
                }
            }
        }

        private void camHeightInput_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (run == 0 && camHeightInput.Text!="" && System.Text.RegularExpressions.Regex.IsMatch(camHeightInput.Text,"^(\\d+)?(\\.\\d+)?$"))
            {
                String test = camHeightInput.Text;
                camHeight = Convert.ToDouble(camHeightInput.Text);
                capture.IsEnabled = true;
            }
            else if(run==0)
            {
                MessageBox.Show("Invalid camHeight please try again.");
                capture.IsEnabled = false;
            }
        }

        private void camHeightInput_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (run == 0)
            {
                camHeightInput.IsReadOnly = false;
            }
            else if (run == 1)
            {
                camHeightInput.IsReadOnly = true;
                MessageBox.Show("You may not change this value until after the 2nd capture.");
            }
        }
        private void phoneApplicationPage_OrientationChanged(object sender, OrientationChangedEventArgs e)
        {
            if (e.Orientation == PageOrientation.LandscapeLeft || e.Orientation == PageOrientation.LandscapeRight)
            {
                ApplicationBar.IsVisible = false;

                TopRec.Width = 780;
                BottomRec.Width = 780;
                BottomRec.Margin = new Thickness(0, 0, 0, 0);

                cameraView.Margin = new Thickness(0, 20, 0, 20);
                cameraView.HorizontalAlignment = HorizontalAlignment.Left;
                cameraView.Height = 373;
                cameraView.Width = 456;
                rotation.CenterX = 0;
                rotation.CenterY = 0;
                if (e.Orientation == PageOrientation.LandscapeLeft)
                {
                    rotation.Angle = 0;
                }
                else
                {
                    rotation.Angle = 180;
                }
                cameraView.RenderTransform = rotation;

                Grid.SetColumnSpan(TargetDot, 1);
                Grid.SetColumnSpan(TargetDot2, 1);
                TargetDot.Margin = new Thickness(108, 0, 0, 0);
                TargetDot2.Margin = new Thickness(108, 0, 0, 0);

                Grid.SetRow(capture, 1);
                capture.HorizontalAlignment = HorizontalAlignment.Right;
                capture.VerticalAlignment = VerticalAlignment.Center;
                capture.Margin = new Thickness(0, 0, 40, 0);

                Grid.SetRow(camHeightInput, 1);
                Grid.SetRowSpan(camHeightInput, 1);
                camHeightInput.Margin = new Thickness(0, 10, 45, 0);
                camHeightInput.VerticalAlignment = VerticalAlignment.Top;

                Grid.SetRow(CamHeightLabel, 1);
                CamHeightLabel.HorizontalAlignment = HorizontalAlignment.Center;
                CamHeightLabel.Margin = new Thickness(-20, 30, 30, 0);

                Grid.SetRow(PitchOut, 1);
                Grid.SetColumn(PitchOut, 1);
                PitchOut.HorizontalAlignment = HorizontalAlignment.Center;
                PitchOut.VerticalAlignment = VerticalAlignment.Center;
                PitchOut.Margin = new Thickness(15, 0, 20, 0);

                Grid.SetRow(PitchLabel, 1);
                Grid.SetColumn(PitchLabel, 1);
                PitchLabel.VerticalAlignment = VerticalAlignment.Center;
                PitchLabel.Margin = new Thickness(115, 0, 20, 0);
                PitchLabel.Text = "Roll:";

                Grid.SetRow(DistanceLabel, 1);
                Grid.SetColumn(DistanceLabel, 1);
                DistanceLabel.Margin = new Thickness(115, 0, 20, 125);

                Grid.SetRow(distanceOut, 1);
                Grid.SetColumn(distanceOut, 1);
                distanceOut.Margin = new Thickness(0, 0, 75, 124);

                Grid.SetRow(HeightLabel, 1);
                HeightLabel.Margin = new Thickness(115, 0, 20, 60);

                Grid.SetRow(heightOut, 1);
                heightOut.Margin = new Thickness(0, 0, 93, 60);
                if (!adFailed)
                {
                    Ad.Visibility = Visibility.Visible;
                } 
            }
            else
            {
                if (!checkForActiveProducts())
                {
                    ApplicationBar.IsVisible = true;
                }
                ApplicationBar.Opacity = .5;

                Thickness blankThickness = new Thickness(0, 0, 0, 0);
                TopRec.Width = 480;
                BottomRec.Width = 480;
                BottomRec.Margin = blankThickness;

                cameraView.Margin = blankThickness;
                cameraView.HorizontalAlignment = HorizontalAlignment.Center;
                cameraView.Width = 373;
                cameraView.Height = 456;
                rotation.Angle=90;
                cameraView.RenderTransform = rotation;
                cameraView.Margin = blankThickness;

                Grid.SetColumnSpan(TargetDot, 2);
                Grid.SetColumnSpan(TargetDot2, 2);
                TargetDot.Margin = blankThickness;
                TargetDot2.Margin = blankThickness;

                Grid.SetRow(capture, 2);
                capture.HorizontalAlignment = HorizontalAlignment.Center;
                capture.VerticalAlignment = VerticalAlignment.Top;
                capture.Margin = blankThickness;

                Grid.SetRow(camHeightInput, 2);
                Grid.SetRowSpan(camHeightInput, 2);
                camHeightInput.Margin = new Thickness(0, 0, 10, 63);
                camHeightInput.VerticalAlignment = VerticalAlignment.Bottom;

                Grid.SetRow(CamHeightLabel, 3);
                CamHeightLabel.HorizontalAlignment = HorizontalAlignment.Left;
                CamHeightLabel.Margin = blankThickness;

                Grid.SetRow(PitchOut, 3);
                Grid.SetColumn(PitchOut, 0);
                PitchOut.HorizontalAlignment = HorizontalAlignment.Right;
                PitchOut.VerticalAlignment = VerticalAlignment.Top;
                PitchOut.Margin = new Thickness(0, 0, 100, 0);

                Grid.SetRow(PitchLabel, 3);
                Grid.SetColumn(PitchLabel, 0);
                PitchLabel.VerticalAlignment = VerticalAlignment.Top;
                PitchLabel.Margin = new Thickness(30, 0, 0, 0);
                PitchLabel.Text = "Pitch:";

                Grid.SetRow(DistanceLabel, 3);
                Grid.SetColumn(DistanceLabel, 0);
                DistanceLabel.Margin = new Thickness(30, 83, 0, 0);

                Grid.SetRow(distanceOut, 3);
                Grid.SetColumn(distanceOut, 0);
                distanceOut.Margin = new Thickness(0, 0, 38, 0);

                Grid.SetRow(HeightLabel, 3);
                HeightLabel.Margin = blankThickness;

                Grid.SetRow(heightOut, 3);
                heightOut.Margin = new Thickness(0, 0, 102, 0);
                if (!adFailed)
                {
                    Ad.Visibility = Visibility.Visible;
                }              
            }
        }

        private void Ad_ErrorOccurred(object sender, Microsoft.Advertising.AdErrorEventArgs e)
        {
            adFailed = true;
        }

        private void RemoveAds_Click(object sender, System.EventArgs e)
        {
            purchaser.invokePurchaceInterface(LayoutRoot,"RmAdsPID");
            checkForActiveProducts();
        }
        private bool checkForActiveProducts()
        {
            var licences = CurrentApp.LicenseInformation.ProductLicenses;
            if (licences["RmAdsPID"].IsActive)
            {
                Ad.IsEnabled = false;
                Ad.Visibility = Visibility.Collapsed;
                adFailed = true;
                ApplicationBar.IsVisible = false;
                return true;
            }
            return false;
        }
    }
}