using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Threading;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net;
using Newtonsoft.Json;

namespace LiveRateFeed
{
    class Program
    {
       static HttpListenerContext context;
        static async Task Main(string[] args)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();
            Console.WriteLine("WebSocket server started. Listening on http://localhost:8080/");
            while (true)
            {
                context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    ProcessWebSocketRequest(context);
                }
                else
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        static async void ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket socket = webSocketContext.WebSocket;

            try
            {
                while(socket.State == WebSocketState.Open)
                {
                    var liveRate = GetLiveRate();
                    var message = JsonConvert.SerializeObject(liveRate);
                    byte[] buffer = Encoding.UTF8.GetBytes(message);
                    await socket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                    Thread.Sleep(2000); // Simulating some processing time, remove this in a real application
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                if (socket.State != WebSocketState.Closed)
                {
                    socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by the server", CancellationToken.None).Wait();
                }
            }
        }

        public static string nameString = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public static string phoneBase = "0123456789";

        static Random random = new Random();
        private static LiveRateViewModel GetLiveRate()
        {
            ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArguments("--headless");
            string driverPath = AppDomain.CurrentDomain.BaseDirectory;
            WebDriver driver = new ChromeDriver(driverPath, chromeOptions);
            driver.Manage().Timeouts().ImplicitWait = new TimeSpan(10);
            driver.Manage().Window.Maximize();
            driver.Url = "http://sohanbullion.com/liverate.html";

            Thread.Sleep(2000);

            string CustomerName = generateName(8);
            string mobileNumber = generateMobileNumber(9);
            string City = "Kolkata";
            string goldSellingRate;
            string silverSellingRate;

            driver.FindElement(By.XPath("//input[@id='txtotrName']")).SendKeys("CustNam" + CustomerName);
            driver.FindElement(By.XPath("//input[@id='txtotrfirmName']")).SendKeys("FirmNam" + CustomerName);
            driver.FindElement(By.XPath("//input[@id='txtotrmobNumber']")).SendKeys("9" + mobileNumber);
            driver.FindElement(By.XPath("//input[@id='txtotrCity']")).SendKeys("City" + City);
            driver.FindElement(By.XPath("//button[text()='Register']")).Click();
            Thread.Sleep(1500);

            var liverate = new LiveRateViewModel();

            for (int i = 0; i < 5; i++)
            {
                try
                {
                    goldSellingRate = driver.FindElement(By.XPath("//span[text()='CUTTING W.TCS GOLD 9950']/parent::td/following-sibling::td[2]/span[1]")).Text;
                    silverSellingRate = driver.FindElement(By.XPath("//span[text()='SILVER 9999 WITH TCS']/parent::td/following-sibling::td[2]/span[1]")).Text;

                    liverate = new LiveRateViewModel
                    {
                        GoldRate = Convert.ToDecimal(goldSellingRate),
                        SilverRate = Convert.ToDecimal(silverSellingRate)
                    };
                    Console.WriteLine($"Gold Rate : {goldSellingRate}");
                    Console.WriteLine($"Silver Rate: {silverSellingRate}");
                    break;   
                }
                catch
                {
                    continue;
                }
            }

            return liverate;
        }


        private static string generateName(int len)
        {
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(nameString.ElementAt(random.Next(nameString.Length)));
            return sb.ToString();
        }

        private static string generateMobileNumber(int len)
        {
            StringBuilder sb = new StringBuilder(len);
            for (int i = 0; i < len; i++)
                sb.Append(phoneBase.ElementAt(random.Next(phoneBase.Length)));
            return sb.ToString();
        }

        public class LiveRateViewModel
        {
            public decimal GoldRate { get; set; }
            public decimal SilverRate { get; set; }
        }
    }
}
