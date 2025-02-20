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
            await InteractionWithDom();
        }

        static async Task InteractionWithDom()
        {
            const int port = 9222;
            var chromePath = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome";
            var userDataDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            //🚀Step 1: Start Chrome
            Console.WriteLine("🚀Starting a new Chrome instance...");
            Directory.CreateDirectory(userDataDir);

            var psi = new ProcessStartInfo
            {
                FileName = chromePath,
                Arguments = string.Join(" ",
                    $"--remote-debugging-port={port}",
                    "--no-first-run",
                    $"--user-data-dir={userDataDir}",
                    "https://selectorshub.com/xpath-practice-page/")
            };

            var chromeProcess = Process.Start(psi);
            if (chromeProcess == null)
            {
                Console.WriteLine("❌Failed to start Chrome.");
                return;
            }

            Console.WriteLine("🚀Chrome started. Waiting for initialization...");
            await Task.Delay(5000);
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

                // ✅Step 4: Get input element and set text
                var companyInputSelector =
                    "#content > div > div.elementor.elementor-1097 > section.elementor-section.elementor-top-section.elementor-element.elementor-element-0731668.elementor-section-boxed.elementor-section-height-default > div > div.elementor-column.elementor-col-50.elementor-top-column.elementor-element.elementor-element-b7d792b > div > div.elementor-element.elementor-element-459c920.elementor-widget__width-inherit.elementor-widget.elementor-widget-html > div > div > div:nth-child(11) > div > div > div > input[type=\"text\"]:nth-child(3)";
                
                var companyInputObjectId = await QuerySelector(ws, companyInputSelector, 1);
                if(string.IsNullOrEmpty(companyInputObjectId)) return;
                await InsertText(companyInputObjectId, ws, "dev.to", 1);
                
                // ✅Step 5: Get input element and set text
                var userNameObjectId = await QuerySelector(ws, "#userName", 2);
                if (string.IsNullOrEmpty(userNameObjectId)) return;
                var shadowRootId = await GetShadowRootId(ws, userNameObjectId, 2);
                if (string.IsNullOrEmpty(shadowRootId)) return;
                var inputObjectId = await QuerySelectorInShadowRoot(ws, shadowRootId, "#kils", 2);
                if (string.IsNullOrEmpty(inputObjectId)) return;
                await InsertText(inputObjectId, ws, "John Doe", 2);
                
                var projectRoot = Directory.GetParent(AppContext.BaseDirectory)?.Parent?.Parent?.Parent?.FullName;
                var screenshotPath = Path.Combine(projectRoot ?? ".", "screenshot.png");
                await CaptureScreenshot(ws, screenshotPath, 3);

                //🚪Step 9: Close
                Console.WriteLine("🚪Press Enter to close...");
                Console.ReadLine();
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None);
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

        private static async Task InsertText(string? objectId, ClientWebSocket ws, string text, int id)
        {
            var command = new
            {
                id,
                method = "Runtime.callFunctionOn",
                @params = new
                {
                    objectId,
                    functionDeclaration = @"
                async function simulateTyping(text) {
                    this.focus();
                    for (let char of text) {
                        this.value += char;
                        this.dispatchEvent(new InputEvent('input', { bubbles: true }));
                        this.dispatchEvent(new KeyboardEvent('keydown', { bubbles: true }));
                        await new Promise(resolve => setTimeout(resolve, 100));
                    }
                    this.dispatchEvent(new KeyboardEvent('keyup', { bubbles: true }));
                    this.dispatchEvent(new Event('change', { bubbles: true }));
                }",
                    arguments = new[]
                    {
                        new { value = text }
                    },
                    userGesture = true
                }
            };

            var response = await SendAndReceive(ws, command);
            Console.WriteLine($"📝 Text Insertion Response: {response}");
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
                string htmlContent = jsonResponse?.Result?.ResultValue.ToString() ?? "Failed to extract HTML.";

                Console.WriteLine("📄 Page HTML:");
                Console.WriteLine(htmlContent);

                // 📄Step 7: Save HTML to file
                Console.WriteLine("📄 Saving to file...");
                await File.WriteAllTextAsync("site.html", htmlContent, Encoding.UTF8);

                // 🌐Step 8: Open the file in the default browser
                Console.WriteLine("🌐 Opening file in the default browser...");
                await NavigateToSavedFileInChrome(ws, "site.html");

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
                await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);

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

        private static async Task<string?> QuerySelector(ClientWebSocket ws, string selector, int id)
        {
            var command = new
            {
                id,
                method = "Runtime.evaluate",
                @params = new
                {
                    expression = $"document.querySelector('{selector}')",
                    returnByValue = false
                }
            };

            var response = await SendAndReceive(ws, command);


            using var doc = JsonDocument.Parse(response);

            if (doc.RootElement.TryGetProperty("result", out var result) &&
                result.GetProperty("result").TryGetProperty("objectId", out var objectId))
            {
                return objectId.GetString();
            }

            throw new Exception($"❌Selector: '{selector}' not found");
        }
        
        private static async Task<string?> GetShadowRootId(ClientWebSocket ws, string hostObjectId, int id)
        {
            var command = new
            {
                id,
                method = "Runtime.callFunctionOn",
                @params = new
                {
                    objectId = hostObjectId,
                    functionDeclaration = "function() { return this.shadowRoot || null; }",
                    returnByValue = false
                }
            };

            var response = await SendAndReceive(ws, command);
            Console.WriteLine($"Shadow Root Response: {response}");

            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.GetProperty("result").GetProperty("result")
                    .TryGetProperty("objectId", out var shadowRootId))
                throw new Exception("❌ Shadow root not found after retries.");
            Console.WriteLine("✅ Shadow root found.");
            return shadowRootId.GetString();

        }

        private static async Task<string?> QuerySelectorInShadowRoot(ClientWebSocket ws, string elementId, string selector, int id)
        {
            var command = new
            {
                id,
                method = "Runtime.callFunctionOn",
                @params = new
                {
                    objectId = elementId,
                    functionDeclaration = $"function() {{return this.querySelector('{selector}');}}",
                    returnByValue = false
                }
            };

            var response = await SendAndReceive(ws, command);

            using var doc = JsonDocument.Parse(response);

            if (!doc.RootElement.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("result", out var innerResult) ||
                !innerResult.TryGetProperty("objectId", out var objectId))
                throw new Exception($"❌ Element '{selector}' not found");
            Console.WriteLine($"🎯 Element '{selector}' found.");
            return objectId.GetString();
        }

        private static async Task<string> ReceiveMessage(ClientWebSocket ws)
        {
            var buffer = new ArraySegment<byte>(new byte[4096]);
            var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
            if (buffer.Array != null) return Encoding.UTF8.GetString(buffer.Array, 0, result.Count);
            throw new Exception("❌Buffer is empty.");
        }
        
        private static async Task<string> SendAndReceive(ClientWebSocket ws, object command)
        {
            string message = JsonSerializer.Serialize(command);
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, CancellationToken.None);

            var response = await ReceiveMessage(ws);
            Console.WriteLine($"✅[Response]: {response}");
            return response;
        }
        
        private static async Task<string> SendAndReceiveLarge(ClientWebSocket ws, object command)
        {
            var message = JsonSerializer.Serialize(command);
            var bytes = Encoding.UTF8.GetBytes(message);
            await ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);

            var buffer = new byte[8192];
            var responseBuilder = new StringBuilder();

            WebSocketReceiveResult result;
            do
            {
                result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                responseBuilder.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            }
            while (!result.EndOfMessage);

            return responseBuilder.ToString();
        }
        
        private static async Task CaptureScreenshot(ClientWebSocket ws, string filePath, int id)
        {
            await Task.Delay(3000);
            var command = new
            {
                id,
                method = "Page.captureScreenshot",
                @params = new
                {
                    format = "png"
                }
            };

            var response = await SendAndReceiveLarge(ws, command);

            using var doc = JsonDocument.Parse(response);
            if (!doc.RootElement.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("data", out var imageData))
            {
                throw new Exception("❌ Failed to capture screenshot.");
            }

            var imageBytes = Convert.FromBase64String(imageData.GetString() ?? string.Empty);
            await File.WriteAllBytesAsync(filePath, imageBytes);
            Console.WriteLine($"📸 Screenshot saved to: {filePath}");
        }
    }
}