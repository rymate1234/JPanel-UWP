using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace JPanel_W10
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StatsPage : Page
    {
        private HttpCookie sessionCookie;
        private HttpClient client;

        private Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;


        public StatsPage()
        {
            this.InitializeComponent();
            Loaded += StatsPage_Loaded;

        }

        private async void StatsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (await AttemptLogin())
            {
#pragma warning disable CS4014 
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(1000);
                        HttpResponseMessage result = await client.GetAsync(new Uri(roamingSettings.Values["panelurl"] + "/stats"));
                        if (result.IsSuccessStatusCode)
                        {
                            var jsonObject = JsonObject.Parse(await result.Content.ReadAsStringAsync());

                            UpdateTicker(jsonObject);
                        }
                    }
                });
#pragma warning restore CS4014
            }
        }

        private async void UpdateTicker(JsonObject jsonObject)
        {
            Debug.WriteLine(jsonObject);
            var freeRam = Math.Round(jsonObject.GetNamedNumber("free") / 1024);
            var totalRam = Math.Round(jsonObject.GetNamedNumber("total") / 1024);
            var cpu = jsonObject.GetNamedNumber("cpu");
            var tps = Math.Round(jsonObject.GetNamedNumber("tps"));

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ramText.Text = "RAM: " + freeRam + "MB / " + totalRam + "MB";
                cpuText.Text = "CPU: " + cpu + "%";
                tpsText.Text = "TPS: " + tps;

                ramBar.Value = (freeRam/totalRam)*100;
                cpuBar.Value = cpu;
                tpsBar.Value = tps;

            });

        }

        private async Task<bool> AttemptLogin()
        {
            var filter = new Windows.Web.Http.Filters.HttpBaseProtocolFilter();
            filter.CacheControl.ReadBehavior =
                Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
            var cookieManager = filter.CookieManager;
            client = new HttpClient(filter);

            Uri loginUri = new Uri(roamingSettings.Values["panelurl"] + "/auth");
            Dictionary<string, string> pairs = new Dictionary<string, string>();

            pairs.Add("username", (string)roamingSettings.Values["username"]);

            pairs.Add("password", (string)roamingSettings.Values["password"]);

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
                        this.sessionCookie = cookie;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
