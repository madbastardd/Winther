using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Xml;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Winther
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        bool globalTemperature = true;    //temperature in faringates = true

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void cityEnter_keyUp(object sender, KeyRoutedEventArgs e) {
            string query = "https://query.yahooapis.com/v1/public/yql?q=select name, country from geo.places(5) where text = '"
                + (sender as TextBox).Text + "'";
            try {
                WebRequest request = WebRequest.Create(query);

                // Get response  
                WebResponse response = await request.GetResponseAsync();
                // Get the response stream  
                StreamReader reader = new StreamReader(response.GetResponseStream());

                using (XmlReader xml = XmlReader.Create(reader)) {
                    ObservableCollection<string> list = new ObservableCollection<string>();
                    while (xml.Read()) {
                        if (xml.NodeType == XmlNodeType.Element) {
                            if (xml.Name == "name") {
                                //add name in list
                                xml.Read();
                                list.Add(xml.Value);
                            }
                            else if (xml.Name == "country") {
                                //append country code to name
                                string last = list[list.Count - 1];
                                list.RemoveAt(list.Count - 1);
                                last += ", " + xml.GetAttribute("code");
                                if (!list.Contains(last))
                                    list.Add(last);
                            }
                        }
                    }
                    TextBlock city = null;
                    foreach (var item in list) {
                        if (city == null)
                            city = city1;
                        else if (city == city1)
                            city = city2;
                        else if (city == city2)
                            city = city3;
                        else if (city == city3)
                            city = city4;
                        else
                            city = city5;

                        city.Text = item;
                    }
                }
            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is System.Net.WebException) {
                //no internet
                notFoundImage.Visibility = Visibility.Visible;
            }
        }

        private string convertFromCtoF(SByte temp, bool FromCtoF = true) {
            return (FromCtoF) ? ((SByte)Math.Round(9.0 / 5.0 * temp + 32)).ToString() : ((SByte)Math.Round(5.0 / 9.0 * (temp - 32))).ToString();
        }

        private string weatherPath(byte forecastCode) {
            if (forecastCode == 0) {
                //tornado
                return "tornado";
            }
            if (forecastCode == 1) {
                //tropical storm
                return "tropical storm";
            }
            if (forecastCode == 5) {
                //rain and snow
                return "rain snow";
            }
            if (forecastCode == 6) {
                //rain and sleet
                return "rain sleet";
            }
            if (forecastCode >= 10 && forecastCode <= 12 || forecastCode == 35) {
                //rain
                return "rain";
            }
            if (forecastCode >= 13 && forecastCode <= 16) {
                //snow
                return "snow";
            }
            if (forecastCode == 24) {
                //windy
                return "windy";
            }
            if (forecastCode >= 26 && forecastCode <= 30
                || (forecastCode == 33 || forecastCode == 34)) {
                //cloudy or fair
                return "cloudy";
            }
            if (forecastCode == 32) {
                //sunny
                return "sunny";
            }
            return "cloudy";
        }

        private string getImagePath(Byte forecastCode) {
            return "ms-appx://App1/Assets/weather-types/" + weatherPath(forecastCode) + ".png";
        }

        private async void button_Click(object sender, RoutedEventArgs e) {
            cityName.Text = textBox.Text;
            byte countday = 1;  //day forecast

            string requestUri = "http://query.yahooapis.com/v1/public/yql?q=select * from weather.forecast where"
                + " woeid in (select woeid from geo.places(1) where text = '"
                + cityName.Text + "')";
            try {
                //string requestUri = "https://www.google.com.ua/";
                //string requestUri = "http://weather.yahooapis.com/forecastrss?w=2502265";
                Windows.Web.Http.HttpClient httpClient = new Windows.Web.Http.HttpClient();

                //Add a user-agent header to the GET request. 
                var headers = httpClient.DefaultRequestHeaders;

                //The safe way to add a header value is to use the TryParseAdd method and verify the return value is true,
                //especially if the header value is coming from user input.
                string header = "ie";
                if (!headers.UserAgent.TryParseAdd(header)) {
                    throw new Exception("Invalid header value: " + header);
                }

                header = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)";
                if (!headers.UserAgent.TryParseAdd(header)) {
                    throw new Exception("Invalid header value: " + header);
                }

                Windows.Web.Http.HttpResponseMessage httpResponse = new Windows.Web.Http.HttpResponseMessage();
                string httpResponseBody = "";

                httpResponse = await httpClient.GetAsync(new Uri(requestUri));
                httpResponse.EnsureSuccessStatusCode();
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();
                byte[] byteArray = Encoding.UTF8.GetBytes(httpResponseBody);
                MemoryStream stream = new MemoryStream(byteArray);
                // Get the response stream  

                using (XmlReader xml = XmlReader.Create(stream)) {
                    bool mph = true;    //speed in mph
                    bool tf = true; //temperature in F
                    bool dm = true; //distance in miles

                    while (xml.Read())
                        if (xml.NodeType == XmlNodeType.Element) {
                            if (xml.Name == "yweather:units") {
                                //set flags
                                tf = xml.GetAttribute("temperature") == "F";
                                mph = xml.GetAttribute("speed") == "mph";
                                dm = xml.GetAttribute("distance") == "mi";
                            }
                            else if (xml.Name == "yweather:condition") {
                                state.Text = xml.GetAttribute("text");
                                try {
                                    var uri = new Uri(getImagePath(byte.Parse(xml.GetAttribute("code"))));
                                    var bitmap = new BitmapImage(uri);
                                    imageState.Source = bitmap;
                                }
                                catch (Exception) {
                                    //nothing to do
                                }
                                if (tf == globalTemperature) {
                                    tempToday.Text = xml.GetAttribute("temp") + ((globalTemperature) ? " F" : " C");
                                }
                                else {
                                    //make conversation
                                    tempToday.Text = convertFromCtoF(SByte.Parse(xml.GetAttribute("temp")),
                                        globalTemperature) + ((globalTemperature) ? " F" : " C");
                                }
                            }
                            else if (xml.Name == "yweather:wind") {
                                Int16 speed = Int16.Parse(xml.GetAttribute("speed")); // wind speed
                                ushort direct = ushort.Parse(xml.GetAttribute("direction"));
                                //choose wind direction
                                if (speed != 0) {
                                    if (direct <= 23)
                                        wind.Text = "Wind N";
                                    else if (direct <= 67)
                                        wind.Text = "Wind NE";
                                    else if (direct <= 113)
                                        wind.Text = "Wind E";
                                    else if (direct <= 157)
                                        wind.Text = "Wind SE";
                                    else if (direct <= 203)
                                        wind.Text = "Wind S";
                                    else if (direct <= 247)
                                        wind.Text = "Wind SW";
                                    else if (direct <= 293)
                                        wind.Text = "Wind W";
                                    else if (direct <= 337)
                                        wind.Text = "Wind NW";
                                    else
                                        wind.Text = "Wind N";

                                    //view speed
                                    if (mph)
                                        wind.Text += " with speed " + xml.GetAttribute("speed") + " mph ("
                                            + (Math.Round((speed * 1.61), 1)).ToString() + " kmph)";
                                    else
                                        wind.Text += " with speed " + xml.GetAttribute("speed") + " kmph ("
                                            + (Math.Round((speed / 1.61), 1)).ToString() + " mph)";
                                }
                                else {
                                    wind.Text = "No wind";
                                }
                            }
                            else if (xml.Name == "yweather:forecast") {
                                DateTime today = DateTime.Now;
                                string date = today.ToString("dd MMM yyyy");
                                if (date == xml.GetAttribute("date")) {
                                    //today forecast
                                    if ((tf == globalTemperature)) {
                                        minToday.Text = "Min: " + xml.GetAttribute("low");
                                        maxToday.Text = "Max: " + xml.GetAttribute("high");
                                    }
                                    else {
                                        //make conversation
                                        minToday.Text = "Min: " + convertFromCtoF(SByte.Parse(xml.GetAttribute("low")),
                                            globalTemperature);
                                        maxToday.Text = "Max: " + convertFromCtoF(SByte.Parse(xml.GetAttribute("high")),
                                            globalTemperature);
                                    }
                                }
                                else {
                                    //another day
                                    DateTime forecastDateTime = DateTime.Parse(xml.GetAttribute("date"));
                                    TextBlock day = null;
                                    TextBlock max = null;
                                    TextBlock min = null;
                                    Image imageday = null;

                                    switch (countday++) {
                                        case 1:
                                            day = day1;
                                            max = max1;
                                            min = min1;
                                            imageday = image1;

                                            break;
                                        case 2:
                                            day = day2;
                                            max = max2;
                                            min = min2;
                                            imageday = image2;

                                            break;
                                        case 3:
                                            day = day3;
                                            max = max3;
                                            min = min3;
                                            imageday = image3;

                                            break;
                                        case 4:
                                            day = day4;
                                            max = max4;
                                            min = min4;
                                            imageday = image4;

                                            break;
                                        default:
                                            break;
                                    }
                                    day.Text = forecastDateTime.DayOfWeek.ToString();
                                    if (tf == globalTemperature) {
                                        max.Text = xml.GetAttribute("high");
                                        min.Text = xml.GetAttribute("low");
                                    }
                                    else {
                                        //make conversation
                                        min.Text = convertFromCtoF(SByte.Parse(xml.GetAttribute("low")),
                                            globalTemperature);
                                        max.Text = convertFromCtoF(SByte.Parse(xml.GetAttribute("high")),
                                            globalTemperature);
                                    }
                                    try {
                                        var uri = new Uri(getImagePath(byte.Parse(xml.GetAttribute("code"))));
                                        var bitmap = new BitmapImage(uri);
                                        imageday.Source = bitmap;
                                    }
                                    catch (Exception) {
                                        //nothing to do
                                    }
                                }
                            }
                        }
                }

            }
            catch (Exception ex) when (ex is System.Runtime.InteropServices.COMException || ex is System.Net.WebException) {
                //no internet
                notFoundImage.Visibility = Visibility.Visible;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            textBox.Text = "Kyiv, UA";

            this.button_Click(button, e);
        }

        private void TextBlock3_MouseLeftButtonUp(object sender, PointerRoutedEventArgs e) {
            SByte temp = SByte.Parse((tempToday.Text as string).Split(' ')[0]);
            SByte low = SByte.Parse((minToday.Text as string).Split(' ')[1]);
            SByte high = SByte.Parse((maxToday.Text as string).Split(' ')[1]);
            globalTemperature = !globalTemperature;

            tempToday.Text = convertFromCtoF(temp, globalTemperature) + ((globalTemperature) ? " F" : " C");

            minToday.Text = "Min: " + convertFromCtoF(low, globalTemperature);
            maxToday.Text = "Max: " + convertFromCtoF(high, globalTemperature);

            foreach (var item in FindVisualChildren<TextBlock>(forecastGrid)) {
                if (item.Name.StartsWith("max") || item.Name.StartsWith("min")) {
                    item.Text = convertFromCtoF(SByte.Parse(item.Text as string), globalTemperature);
                }
            }
        }
        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject {
            if (depObj != null) {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++) {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T) {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child)) {
                        yield return childOfChild;
                    }
                }
            }
        }

        private void notFoundImage_MouseLeftButtonUp(object sender, PointerRoutedEventArgs e) {
            notFoundImage.Visibility = Visibility.Collapsed;
            Window_Loaded(this, e);
        }

        private void textBox_KeyUp(object sender, KeyRoutedEventArgs e) {
            addGrid.Visibility = Visibility.Visible;
            enterCity.Text = textBox.Text;
            enterCity.Focus(FocusState.Pointer);
            enterCity.SelectionStart = enterCity.Text.Length; // add some logic if length is 0
            enterCity.SelectionLength = 0;
        }

        private void city_PointerPressed(object sender, PointerRoutedEventArgs e) {
            textBox.Text = (sender as TextBlock).Text;
            addGrid.Visibility = Visibility.Collapsed;
            button_Click(button, e);
        }
    }
}
