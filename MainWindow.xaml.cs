using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Windows;
using InputValidator;
using System.Windows.Media;

namespace SetSystemTime
{
    public partial class MainWindow : Window
    {
        private string APIToken = "";
        private ValidatorExtensions Validator = new ValidatorExtensions();
        private SolidColorBrush Danger = new SolidColorBrush(Color.FromRgb(255, 0, 0));
        private SolidColorBrush Gray = new SolidColorBrush(Color.FromRgb(171, 173, 179));
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool SetLocalTime(ref SystemTime sysTime);
        public struct SystemTime
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMiliseconds;
        }
        public MainWindow()
        {
            InitializeComponent();
        }

        private void SiginAPI_Click(object sender, RoutedEventArgs e)
        {
            Message.Text = "";
            bool CanSend = true;
            if (Validator.IsValidHttpAddress(WebServer.Text))
            {
                WebServer.BorderBrush = Gray;
            }
            else
            {
                CanSend = false;
                Message.Text += "Please input url.\r";
                WebServer.BorderBrush = Danger;
            }
            if (Validator.IsValidEmailAddress(user.Text))
            {
                user.BorderBrush = Gray;
            }
            else
            {
                CanSend = false;
                Message.Text += "Please input email address.\r";
                user.BorderBrush = Danger;
            }
            if (password.Password == "")
            {
                CanSend = false;
                Message.Text += "Please input password.\r";
                password.BorderBrush = Danger;
            }
            else
            {
                password.BorderBrush = Gray;
            }
            if (!CanSend) return;
            HttpClient Client = new HttpClient();
            JObject jObject;
            try
            {
                Client.BaseAddress = new Uri(WebServer.Text);
                Client.Timeout = TimeSpan.FromMinutes(500);
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                FormUrlEncodedContent formContent = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("email", user.Text),
                    new KeyValuePair<string, string>("password", password.Password),
                    new KeyValuePair<string, string>("source", "SetSystemTime")
                });
                HttpResponseMessage ResponseMessage = Client.PostAsync("api/login", formContent).Result;
                string responseJson = ResponseMessage.Content.ReadAsStringAsync().Result;
                jObject = JObject.Parse(responseJson);
            }
            catch (Exception)
            {
                string responseJson = @"{
                    ""success"" : false,
                    ""message"" : ""Can't connect Web server.""
                }";
                jObject = JObject.Parse(responseJson);
            }
            if ((bool)jObject["success"])
            {
                APIToken = jObject["data"]["token"].ToString();
                SiginAPI.IsEnabled = false;
            }
            else
            {
                Message.Text = "Sigin Web server fail";
            }

        }

        public void GetTime_Click(object sender, RoutedEventArgs e)
        {
            Message.Text = "";
            if (APIToken == "")
            {
                Message.Text = "First sigin API then set time.";
                return;
            }
            JObject jObject;
            HttpClient Client = new HttpClient();
            Client.BaseAddress = new Uri(WebServer.Text);
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", APIToken);
            HttpResponseMessage ResponseMessage = Client.PostAsync("api/synctime", null).Result;
            string responseJson = ResponseMessage.Content.ReadAsStringAsync().Result;
            jObject = JObject.Parse(responseJson);
            if ((bool)jObject["success"])
            {
                string ServerTime = jObject["message"].ToString();
                Message.Text = ServerTime;
                
                if (!SetLocalTimeByStr(ServerTime))
                {

                    Message.Text = "Set system time fail.";
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                }
                
            }
            else
            {
                Message.Text = "Get system time fail";
            }
        }
        public static bool SetLocalTimeByStr(string timestr)
        {
            bool successful = false;
            SystemTime sysTime = new SystemTime();
            try
            {
                DateTime dt = Convert.ToDateTime(timestr);
                sysTime.wYear = Convert.ToUInt16(dt.Year);
                sysTime.wMonth = Convert.ToUInt16(dt.Month);
                sysTime.wDay = Convert.ToUInt16(dt.Day);
                sysTime.wHour = Convert.ToUInt16(dt.Hour);
                sysTime.wMinute = Convert.ToUInt16(dt.Minute);
                sysTime.wSecond = Convert.ToUInt16(dt.Second);
                successful = SetLocalTime(ref sysTime);
            }
            catch (Exception ex)
            {
                MessageBox.Show("SetLocalTimeByStr Error" + ex.Message);
            }
            return successful;
        }

        private void Exitprogram_Click(object sender, RoutedEventArgs e)
        {
            if (APIToken != "")
            {
                HttpClient Client = new HttpClient();
                Client.BaseAddress = new Uri(WebServer.Text);
                Client.DefaultRequestHeaders.Accept.Clear();
                Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", APIToken);
                HttpResponseMessage ResponseMessage = Client.PostAsync("api/logout", null).Result;
            }
            Environment.Exit(0);
        }
    }
}
