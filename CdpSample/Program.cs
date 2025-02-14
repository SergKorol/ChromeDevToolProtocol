using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json.Linq;

namespace CdpSample
{
    internal static class Program
    {
        static async Task Main()
        {
            await ParseHtml();
        }

        static async Task ParseHtml()
        {
            const int port = 9222;
            string chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            string userDataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            //🚀Step 1: Start Chrome
            Console.WriteLine("Starting a new Chrome instance...");
            Directory.CreateDirectory(userDataDir);

            var psi = new ProcessStartInfo
            {
                FileName = chromePath,
                Arguments = string.Join(" ",
                    $"--remote-debugging-port={port}",
                    "--no-first-run",
                    $"--user-data-dir={userDataDir}",
                    "https://deviceandbrowserinfo.com/are_you_a_bot")
            };

            var chromeProcess = Process.Start(psi);
            if (chromeProcess == null)
            {
                Console.WriteLine("❌Failed to start Chrome.");
                return;
            }

            Console.WriteLine("🚀Chrome started. Waiting for initialization...");
            await Task.Delay(10000);

            try
            {
                //✅Step 2: Get WebSocket Debugger URL
                string? debuggerUrl = await GetPageWebSocketUrl();
                Console.WriteLine(debuggerUrl);
                if (string.IsNullOrEmpty(debuggerUrl))
                {
                    Console.WriteLine("❌Failed to retrieve WebSocket Debugger URL.");
                    return;
                }

                // ⚙️Step 3: Connect to WebSocket
                using var ws = new ClientWebSocket();
                await ws.ConnectAsync(new Uri(debuggerUrl), CancellationToken.None);

                // 🚀Step 4: Send command to retrieve HTML
                var command = new
                {
                    id = 1,
                    method = "Runtime.evaluate",
                    @params = new { expression = "document.querySelector('section.content')?.outerHTML || ''" }
                };
                string message = JsonSerializer.Serialize(command);
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);

                // ✅Step 5: Receive and parse response
                Console.WriteLine("✅Receiving data...");
                using var memoryStream = new MemoryStream();
                var receiveBuffer = new byte[65536];
                WebSocketReceiveResult result;
                do
                {
                    result = await ws.ReceiveAsync(new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    memoryStream.Write(receiveBuffer, 0, result.Count);
                } while (!result.EndOfMessage);

                memoryStream.Position = 0;
                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                string responseText = await reader.ReadToEndAsync();

                // 📝Step 6: Output HTML content
                Console.WriteLine("📝Extracting data...");
                var jsonResponse = JsonSerializer.Deserialize<CdpResponse>(responseText);
                string htmlContent = jsonResponse?.Result?.HtmlResult?.Value ?? "Failed to extract HTML.";

                Console.WriteLine("📄 Page HTML:");
                Console.WriteLine(htmlContent);

                // 📄Step 7: Save HTML to file
                Console.WriteLine("📄 Saving to file...");
                await File.WriteAllTextAsync("site.html", htmlContent, Encoding.UTF8);
                
                // 🌐Step 8: Open the file in the default browser
                Console.WriteLine("🌐 Opening file in the default browser...");
                await NavigateToSavedFileInChrome(ws,"site.html");

                //🚪Step 9: Close
                Console.WriteLine("🚪Press Enter to close...");
                Console.ReadLine();
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                if (!chromeProcess.HasExited)
                {
                    chromeProcess.Kill();
                }
            }
        }

        static async Task<string?> GetPageWebSocketUrl()
        {
            using var httpClient = new HttpClient();
            string json = await httpClient.GetStringAsync("http://localhost:9222/json");
            JArray tabs = JArray.Parse(json);
            var targetTab = tabs.FirstOrDefault(t => t["type"]?.ToString() == "page");
            return targetTab?["webSocketDebuggerUrl"]?.ToString();
        }
        
        static async Task NavigateToSavedFileInChrome(ClientWebSocket ws, string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"❌ File not found: {filePath}");
                    return;
                }

                // 📝Convert the file path to a file URI
                string fileUri = GetValidFileUri(filePath);

                // 🚀Send the navigation command to the existing Chrome tab
                var navigationCommand = new
                {
                    id = 2,
                    method = "Page.navigate",
                    @params = new
                    {
                        url = fileUri
                    }
                };
                string message = JsonSerializer.Serialize(navigationCommand);
                byte[] buffer = Encoding.UTF8.GetBytes(message);
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

                Console.WriteLine($"✅ Navigating to file: {fileUri}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error navigating to file in Chrome: {ex.Message}");
            }
        }
        
        private static string GetValidFileUri(string filePath)
        {
            string absolutePath = Path.GetFullPath(filePath);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                // Windows: file:///C:/path/to/file.html
                return "file:///" + absolutePath.Replace("\\", "/");
            }
            else
            {
                // macOS/Linux: file:///path/to/file.html
                return "file://" + absolutePath;
            }
        }
    }
}