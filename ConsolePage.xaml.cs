using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web;
using Windows.Web.Http;
using Microsoft.ApplicationInsights;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace JPanel_W10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ConsolePage : Page
    {
        private Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

        private string protocol;

        private Uri consoleUri;

        private MessageWebSocket messageWebSocket;
        private DataWriter messageWriter;
        private HttpClient client;

        public ObservableCollection<string> consoleEntries { get; set; }


        public ConsolePage()
        {
            this.InitializeComponent();
            this.protocol = ((bool)roamingSettings.Values["ssl"] == true) ? "wss" : "ws";

            Loaded += ConsolePage_Loaded;
            consoleEntries = new ObservableCollection<string>();
        }

        private async void ConsolePage_Loaded(object sender, RoutedEventArgs e)
        {
            string authkey = await AttemptLogin();
            string wsport = await GetPort();
            consoleUri = new Uri(protocol + "://" + roamingSettings.Values["domain"] + ":" + wsport + "/", UriKind.Absolute);

            consoleView.ItemsSource = consoleEntries;
            consoleEntries.CollectionChanged += (s, args) => ScrollToBottom();
            try
            {
                // Make a local copy to avoid races with Closed events.
                MessageWebSocket webSocket = messageWebSocket;

                // Have we connected yet?
                if (webSocket == null)
                {
                    Uri server = consoleUri;

                    webSocket = new MessageWebSocket();

                    webSocket.SetRequestHeader("Cookie", "loggedin" + "=" + authkey);

                    // MessageWebSocket supports both utf8 and binary messages.
                    // When utf8 is specified as the messageType, then the developer
                    // promises to only send utf8-encoded data.
                    webSocket.Control.MessageType = SocketMessageType.Utf8;
                    // Set up callbacks
                    webSocket.MessageReceived += MessageReceived;
                    webSocket.Closed += Closed;

                    await webSocket.ConnectAsync(server);
                    messageWebSocket = webSocket; // Only store it after successfully connecting.
                    messageWriter = new DataWriter(webSocket.OutputStream);
                }

            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                Debug.WriteLine("Error with connecting: " + status);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }


        private async Task<string> AttemptLogin()
        {
            var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            var cookieManager = filter.CookieManager;
            client = new HttpClient(filter);

            Uri loginUri = new Uri(roamingSettings.Values["panelurl"] + "/auth");
            Dictionary<string, string> pairs = new Dictionary<string, string>();

            pairs.Add("username", (string) roamingSettings.Values["username"]);
            
            pairs.Add("password", (string) roamingSettings.Values["password"]);

            HttpFormUrlEncodedContent formContent = new HttpFormUrlEncodedContent(pairs);
            HttpResponseMessage response = await client.PostAsync(loginUri, formContent);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();

                var responseCookies = cookieManager.GetCookies(loginUri);

                foreach (HttpCookie cookie in responseCookies)
                {
                    if (cookie.Name == "loggedin")
                    {
                        return cookie.Value;
                    }
                }
            }
            return "";
        }

        private async Task<string> GetPort()
        {
            HttpResponseMessage result = await client.GetAsync(new Uri(roamingSettings.Values["panelurl"] + "/wsport"));
            if (result.IsSuccessStatusCode)
            {
                return await result.Content.ReadAsStringAsync();
            }
            return "9003";
        }

        private void ScrollToBottom()
        {
            if (consoleScroll != null)
            {
                consoleScroll.UpdateLayout();
                consoleScroll.ChangeView(0.0f, double.MaxValue, 1.0f);
            }
        }

        private void Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            //throw new NotImplementedException();
        }

        private async void MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                using (DataReader reader = args.GetDataReader())
                {
                    reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
                    string read = reader.ReadString(reader.UnconsumedBufferLength);
                    Regex ansiFilter = new Regex("\\e\\[[\\d;]*[^\\d;]");
                    read = ansiFilter.Replace(read, "");
                    await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => consoleEntries.Add(read));
                }
            }
            catch (Exception ex) // For debugging
            {
                WebErrorStatus status = WebSocketError.GetStatus(ex.GetBaseException().HResult);
                Debug.WriteLine("Error with recieving a message: " + status);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
            }
        }

        private async void CmdInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {

            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string message = cmdBox.Text;

                // Buffer any data we want to send.
                messageWriter.WriteString(message);

                // Send the data as one complete message.
                await messageWriter.StoreAsync();
                cmdBox.Text = "";
            }
        }
    }
}
