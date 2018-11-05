using System;
using System.Drawing;
using System.Windows.Forms;
using System.Text;

using System.Threading;
using System.IO.Ports;
using Microsoft.Lync.Model;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace statusLight
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyApplicationContext());

        }
    }
    public class MyApplicationContext : ApplicationContext
    {
        public NotifyIcon trayIcon;

        public SerialPort com = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);

        public LyncClient lyncClient;

        public int[] colorVal = { 255, 255, 255 };

        public int[] prevColorVal = { 255, 255, 255 };

        public MyApplicationContext()
        {
            // Initialize Tray Icon
            com.Open();
            CreateTextIcon("M", "white");
            com.Write("175,175,175");
     
            try
            {
                lyncClient = LyncClient.GetClient();
            }
            catch (ClientNotFoundException clientNotFoundException)
            {
                Console.WriteLine(clientNotFoundException);
                return;
            }
            catch (NotStartedByUserException notStartedByUserException)
            {
                Console.Out.WriteLine(notStartedByUserException);
                return;
            }
            catch (LyncClientException lyncClientException)
            {
                Console.Out.WriteLine(lyncClientException);
                return;
            }
            catch (SystemException systemException)
            {
                if (IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                    return;
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }

            lyncClient.StateChanged +=
                new EventHandler<ClientStateChangedEventArgs>(Client_StateChanged);

            //Update the user interface
            UpdateUserInterface(lyncClient.State);

            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
            {
                if (e.Reason == SessionSwitchReason.SessionLock)
                {
                    CreateTextIcon("M", "red");
                    com.Write("0,255,0");
                }
                else if (e.Reason == SessionSwitchReason.SessionUnlock)
                {
                    Update(ContactAvailability.None);
                }
            }
        }
       
        public void Update(object state)
        { 

            switch (state)
            {
                case ContactAvailability.Free:
                    CreateTextIcon("M", "green");
                    com.Write("255,0,0");
                    //colorFade(new int[] {255,0,0});
                    break;
                case ContactAvailability.DoNotDisturb:
                    CreateTextIcon("M", "red");
                    com.Write("0,255,0");
                    break;
                case ContactAvailability.Away:
                    CreateTextIcon("M", "orange");
                    com.Write("165,255,0");
                    break;
                case ContactAvailability.Busy:
                    CreateTextIcon("M", "blue");
                    com.Write("0,0,255");
                    break;
                case ContactAvailability.None:
                    CreateTextIcon("M", "White");
                    com.Write("255,255,255");
                    break;
                default:
                    Console.WriteLine("DEFAULT CASE!!!");
                    CreateTextIcon("M", "white");
                    com.Write("175,175,175");
                    break;
            }
            sendState(state);
        }

        public void colorFade(int[] newColor)
        {
            float[] colorDiff = { 0, 0, 0 };
            float[] colorStep = { 0, 0, 0 };
            int numSteps = 50;
            Array.Copy(colorVal, prevColorVal, 3);
            for(int i=0;i<3;i++)
            {
                colorDiff[i] = prevColorVal[i] - newColor[i];
                colorStep[i] = colorDiff[i] / numSteps;
            }
            while(numSteps>0)
            {
                for (int i = 0; i < 3; i++)
                {
                    colorVal[i] = (int)(colorVal[i] - colorStep[i]);
                }
                com.Write( colorVal[0].ToString()+ "," +colorVal[1].ToString()+ "," + colorVal[2].ToString() );
                numSteps--;
                Thread.Sleep(5);//sleep 5ms
            }
            
        }

        public void MenuSelection(object sender, EventArgs e)
        {
            MenuItem item = sender as MenuItem;
            string text = item.Text;
            object state;
            switch (text)
            {
                case "Available":
                    state = ContactAvailability.Free;
                    break;
                case "Do Not Disturb":
                    state = ContactAvailability.DoNotDisturb;
                    break;
                case "Away":
                    state = ContactAvailability.Away;
                    break;
                case "Busy":
                    state = ContactAvailability.Busy;
                    break;
                case "Reset":
                    state = ContactAvailability.None;
                    break;
                default:
                    Console.WriteLine("DEFAULT CASE!!!");
                    state = -1;
                    break;
            }
            if (state!=null)
                Update(state);
        }

        public void sendState(object state)
        {
            //Add the availability to the contact information items to be published
            Dictionary<PublishableContactInformationType, object> newInformation =
                new Dictionary<PublishableContactInformationType, object>();
            newInformation.Add(PublishableContactInformationType.Availability, state);

            //Publish the new availability value
            try
            {
                lyncClient.Self.BeginPublishContactInformation(newInformation, PublishContactInformationCallback, null);
            }
            catch (LyncClientException lyncClientException)
            {
                Console.WriteLine(lyncClientException);
            }
            catch (SystemException systemException)
            {
                if (IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
        }

        void Exit(object sender, EventArgs e)
        {
            // Hide tray icon, otherwise it will remain shown until user mouses over it
            trayIcon.Visible = false;

            Application.Exit();
        }

        public void CreateTextIcon(string text, string color)
        {
            if (trayIcon == null)
                trayIcon = new NotifyIcon()
                {
                    ContextMenu = new ContextMenu(new MenuItem[] {
                        new MenuItem("Available", MenuSelection),
                        new MenuItem("Away", MenuSelection),
                        new MenuItem("Do Not Disturb", MenuSelection),
                        new MenuItem("Busy", MenuSelection),
                        new MenuItem("Reset", MenuSelection),
                        new MenuItem("Exit", Exit)
                    }),
                    Visible = true
                };

            trayIcon.Text = "Lync Status Light";

            Font fontToUse = new Font("Microsoft Sans Serif", 16, FontStyle.Bold, GraphicsUnit.Pixel);
            Brush brushToUse = new SolidBrush(Color.FromName(color));
            Bitmap bitmapText = new Bitmap(16, 16);
            Graphics g = System.Drawing.Graphics.FromImage(bitmapText);

            IntPtr hIcon;

            g.Clear(Color.Black);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
            g.DrawString(text, fontToUse, brushToUse, -2, -2);
            hIcon = (bitmapText.GetHicon());
            trayIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);

        }

        #region Handlers for Lync events
        /// <summary>
        /// Handler for the ContactInformationChanged event of the contact. Used to update the contact's information in the user interface.
        /// </summary>
        private void SelfContact_ContactInformationChanged(object sender, ContactInformationChangedEventArgs e)
        {
            //Only update the contact information in the user interface if the client is signed in.
            //Ignore other states including transitions (e.g. signing in or out).
            if (lyncClient.State == ClientState.SignedIn)
            {
                if (e.ChangedContactInformation.Contains(ContactInformationType.Availability))
                {
                    //Use the current dispatcher to update the contact's availability in the user interface.
                    SetAvailability();
                }
            }
        }

        /// <summary>
        /// Handler for the StateChanged event of the contact. Used to update the user interface with the new client state.
        /// </summary>
        private void Client_StateChanged(object sender, ClientStateChangedEventArgs e)
        {
            //Use the current dispatcher to update the user interface with the new client state.
            UpdateUserInterface(e.NewState);
        }
        #endregion

        #region Callbacks

        /// <summary>
        /// Callback invoked when Self.BeginPublishContactInformation is completed
        /// </summary>
        /// <param name="result">The status of the asynchronous operation</param>
        private void PublishContactInformationCallback(IAsyncResult result)
        {
            lyncClient.Self.EndPublishContactInformation(result);
        }
        #endregion

        /// <summary>
        /// Updates the user interface
        /// </summary>
        /// <param name="currentState"></param>
        private void UpdateUserInterface(ClientState currentState)
        {

            if (currentState == ClientState.SignedIn)
            {
                //Listen for events of changes of the contact's information
                lyncClient.Self.Contact.ContactInformationChanged +=
                    new EventHandler<ContactInformationChangedEventArgs>(SelfContact_ContactInformationChanged);

                //Get the contact's information from Lync and update with it the corresponding elements of the user interface.
                SetAvailability();

            }
        }

        /// <summary>
        /// Gets the contact's current availability value from Lync and updates the corresponding elements in the user interface
        /// </summary>
        private void SetAvailability()
        {
            //Get the current availability value from Lync
            ContactAvailability currentAvailability = 0;
            try
            {
                currentAvailability = (ContactAvailability)lyncClient.Self.Contact.GetContactInformation(ContactInformationType.Availability);
            }
            catch (LyncClientException e)
            {
                Console.WriteLine(e);
            }
            catch (SystemException systemException)
            {
                if (IsLyncException(systemException))
                {
                    // Log the exception thrown by the Lync Model API.
                    Console.WriteLine("Error: " + systemException);
                }
                else
                {
                    // Rethrow the SystemException which did not come from the Lync Model API.
                    throw;
                }
            }
            Update(currentAvailability);


        }

        /// <summary>
        /// Identify if a particular SystemException is one of the exceptions which may be thrown
        /// by the Lync Model API.
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        private bool IsLyncException(SystemException ex)
        {
            return
                ex is NotImplementedException ||
                ex is ArgumentException ||
                ex is NullReferenceException ||
                ex is NotSupportedException ||
                ex is ArgumentOutOfRangeException ||
                ex is IndexOutOfRangeException ||
                ex is InvalidOperationException ||
                ex is TypeLoadException ||
                ex is TypeInitializationException ||
                ex is InvalidComObjectException ||
                ex is InvalidCastException;
        }
    }
    public class SerialCOM
    {
        private SerialPort serialPort;

        public SerialCOM()
        {
            serialPort = new SerialPort("COM4", 9600, Parity.None, 8, StopBits.One);
            serialPort.Handshake = Handshake.None;
            OpenSerial();
        }
        public void OpenSerial()
        {
            if (!serialPort.IsOpen) serialPort.Open();
        }

        public void CloseSerial()
        {
            if (serialPort.IsOpen) serialPort.Close();
        }
        public int SendData(char ch)
        {
            if (char.IsLetter(ch))
                serialPort.Write(ch.ToString());
            return 1;
        }


    }
}