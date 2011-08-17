using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Audio;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        internal extern static Int32 SetCursorPos(Int32 x, Int32 y);

        double screenWidth = System.Windows.SystemParameters.PrimaryScreenWidth;
        double screenHeight = System.Windows.SystemParameters.PrimaryScreenHeight;

        Runtime nui = new Runtime();
        static int[] right = new int[3];
        static int[] left = new int[3];
        static int[] head = new int[3];



        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            for (var x = 0; x < 3; x++) {
                right[x] = 0;
                left[x] = 0;
                head[x] = 0;
            }


            nui.Initialize(RuntimeOptions.UseSkeletalTracking);
            nui.SkeletonEngine.TransformSmooth = true;

            var parameters = new TransformSmoothParameters { 
                Smoothing = 0.9f,
                Correction = 0.0f,
                Prediction = 0.0f,
                JitterRadius = 0.1f,
                MaxDeviationRadius = 0.04f
            };

            nui.SkeletonEngine.SmoothParameters = parameters;

            nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
            ListenForRequests listen = new ListenForRequests();
            Thread t = new Thread(new ThreadStart(listen.ThreadRun));
            t.Start();
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame allSkeletons = e.SkeletonFrame;

            SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();
            right = ScaleJoint(skeleton.Joints[JointID.HandRight]);
            left = ScaleJoint(skeleton.Joints[JointID.HandLeft]);
            head = ScaleJoint(skeleton.Joints[JointID.Head]);
            textBox1.Text = right[0]+" "+right[1]+" "+right[2] ;
            textBox2.Text = left[0] + " " + left[1] + " " + left[2];
            textBox3.Text = head[0] + " " + head[1] + " " + head[2];
        }

        private int[] ScaleJoint(Joint joint) {
            var scaledJoint = joint.ScaleTo((int)screenWidth, (int)screenHeight, .3f, .2f);
            int[] j = {(int)scaledJoint.Position.X, (int)scaledJoint.Position.Y, (int)scaledJoint.Position.Z};
            return j;
        }

        public class ListenForRequests
        {
            public void ThreadRun()
            {
            int CONNECT_QUEUE_LENGTH = 4;

            Socket listenSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSock.Bind(new IPEndPoint(IPAddress.Any, 2001));
            listenSock.Listen(CONNECT_QUEUE_LENGTH);

                while (true)
                {
                    using (Socket newConnection = listenSock.Accept())
                    {

                        // Send the data.
                        byte[] msg;
                        String tmp = head[0].ToString() + ',' + head[1].ToString() + ',' + head[2].ToString()
                            +':'+left[0].ToString()+','+left[1].ToString()+','+left[2].ToString()
                            +':'+right[0].ToString()+','+right[1].ToString()+','+right[2].ToString();
                        msg = System.Text.Encoding.UTF8.GetBytes(tmp);


                        newConnection.Send(msg, SocketFlags.None);
                    }
                }
            }
        }

        private void SetCursor(Joint joint) {
            var scaledJoint = joint.ScaleTo((int)screenWidth, (int)screenHeight, .3f, .2f);
            SetCursorPos((int)scaledJoint.Position.X, (int)scaledJoint.Position.Y);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            nui.Uninitialize();
        }

        public static void Send(Socket socket, byte[] buffer, int offset, int size, int timeout)
        {
            int startTickCount = Environment.TickCount;
            int sent = 0;  // how many bytes is already sent
            do
            {
                if (Environment.TickCount > startTickCount + timeout)
                    throw new Exception("Timeout.");
                try
                {
                    sent += socket.Send(buffer, offset + sent, size - sent, SocketFlags.None);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock ||
                        ex.SocketErrorCode == SocketError.IOPending ||
                        ex.SocketErrorCode == SocketError.NoBufferSpaceAvailable)
                    {
                        // socket buffer is probably full, wait and try again
                        Thread.Sleep(30);
                    }
                    else
                        throw ex;  // any serious error occurr
                }
            } while (sent < size);
        }

    }
}
