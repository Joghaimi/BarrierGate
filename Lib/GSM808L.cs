using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    public class GSM808L
    {
        public static SerialPort DeviceCom = new SerialPort();
        private readonly StringBuilder _responseBuffer = new();

        public GSM808L(string portName, int baudRate = 115200)
        {

            DeviceCom = new SerialPort(portName, baudRate)
            {
                NewLine = "\r\n",
                ReadTimeout = 5000,
                WriteTimeout = 2000,
                Encoding = Encoding.ASCII
            };

            DeviceCom.DataReceived += SerialPort_DataReceived;
            DeviceCom.Open();
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = DeviceCom.ReadExisting();
                lock (_responseBuffer)
                {
                    _responseBuffer.Append(data);
                }
            }
            catch { /* Handle errors if needed */ }
        }

        public void SendCommand(string command)
        {
            _responseBuffer.Clear();
            DeviceCom.Write(command + "\r");
        }

        public async Task<string> WaitForResponseAsync(string expected, int timeoutMs = 10000)
        {
            int waited = 0;
            while (waited < timeoutMs)
            {
                lock (_responseBuffer)
                {
                    //Console.WriteLine($"Waiting for: {_responseBuffer.ToString()}");
                    if (_responseBuffer.ToString().Contains(expected))
                    {
                        return _responseBuffer.ToString();
                    }
                }
                await Task.Delay(100);
                waited += 100;
            }
            return null; // timeout
        }

        public async Task<bool> HttpGetAsync(string apn, string url)
        {
            try
            {
                // Clear the response buffer to avoid stale data
                lock (_responseBuffer)
                {
                    _responseBuffer.Clear();
                }

                // Ensure HTTP session is terminated before starting
                SendCommand("AT+HTTPTERM");
                await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result as it may already be terminated

                // Configure bearer context
                SendCommand("AT+SAPBR=3,1,\"Contype\",\"GPRS\"");
                var r = await WaitForResponseAsync("OK", 5000);
                if (r == null) return false;

                SendCommand($"AT+SAPBR=3,1,\"APN\",\"{apn}\"");
                r = await WaitForResponseAsync("OK", 5000);
                if (r == null) return false;

                // Attempt to open bearer with retries
                int maxRetries = 3;
                int retryCount = 0;
                while (retryCount < maxRetries)
                {
                    SendCommand("AT+SAPBR=1,1");
                    r = await WaitForResponseAsync("OK", 10000);
                    if (r != null) break; // Success

                    retryCount++;
                    Console.Write($".");
                    await Task.Delay(1000); // Short delay before retry

                    // Close bearer before retrying
                    SendCommand("AT+SAPBR=0,1");
                    await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result
                }
                if (r == null) return false;

                // Initialize HTTP session
                SendCommand("AT+HTTPINIT");
                r = await WaitForResponseAsync("OK", 5000);
                if (r == null) return false;

                SendCommand("AT+HTTPPARA=\"CID\",1");
                r = await WaitForResponseAsync("OK", 5000);
                if (r == null) return false;

                SendCommand($"AT+HTTPPARA=\"URL\",\"{url}\"");
                r = await WaitForResponseAsync("OK", 5000);
                if (r == null) return false;

                // Perform HTTP GET
                SendCommand("AT+HTTPACTION=0");
                var httpActionResponse = await WaitForResponseAsync("+HTTPACTION:", 15000);
                if (httpActionResponse == null) return false;

                // Parse HTTP status code
                var lines = httpActionResponse.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                string actionLine = lines.FirstOrDefault(line => line.StartsWith("+HTTPACTION:"));
                if (actionLine == null) return false;

                var parts = actionLine.Split(',');
                if (parts.Length < 3) return false;

                string statusCode = parts[1];
                return statusCode == "200"; // Return true only if status code is 200
            }
            finally
            {
                // Always clean up: terminate HTTP session and close bearer
                SendCommand("AT+HTTPTERM");
                await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result

                SendCommand("AT+SAPBR=0,1");
                await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result
            }
        }

        //public async Task<(bool Success, string Response)> HttpGetAsync(string apn, string url)
        //{
        //    try
        //    {
        //        // Clear the response buffer to avoid stale data
        //        lock (_responseBuffer)
        //        {
        //            _responseBuffer.Clear();
        //        }

        //        // Ensure HTTP session is terminated before starting
        //        SendCommand("AT+HTTPTERM");
        //        await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result as it may already be terminated

        //        // Configure bearer context
        //        SendCommand("AT+SAPBR=3,1,\"Contype\",\"GPRS\"");
        //        var r = await WaitForResponseAsync("OK", 5000);
        //        if (r == null) return (false, "No response to SAPBR Contype");

        //        SendCommand($"AT+SAPBR=3,1,\"APN\",\"{apn}\"");
        //        r = await WaitForResponseAsync("OK", 5000);
        //        if (r == null) return (false, "No response to SAPBR APN");

        //        // Attempt to open bearer with retries
        //        int maxRetries = 3;
        //        int retryCount = 0;
        //        while (retryCount < maxRetries)
        //        {
        //            SendCommand("AT+SAPBR=1,1");
        //            r = await WaitForResponseAsync("OK", 10000);
        //            if (r != null) break; // Success

        //            retryCount++;
        //            Console.Write($".");
        //            await Task.Delay(1000); // Short delay before retry

        //            // Close bearer before retrying
        //            SendCommand("AT+SAPBR=0,1");
        //            await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result
        //        }
        //        if (r == null) return (false, "Failed to open bearer context after retries");

        //        // Initialize HTTP session
        //        SendCommand("AT+HTTPINIT");
        //        r = await WaitForResponseAsync("OK", 5000);
        //        if (r == null) return (false, "No response to HTTPINIT");

        //        SendCommand("AT+HTTPPARA=\"CID\",1");
        //        r = await WaitForResponseAsync("OK", 5000);
        //        if (r == null) return (false, "No response to HTTPPARA CID");

        //        SendCommand($"AT+HTTPPARA=\"URL\",\"{url}\"");
        //        r = await WaitForResponseAsync("OK", 5000);
        //        if (r == null) return (false, "No response to HTTPPARA URL");

        //        // Perform HTTP GET
        //        SendCommand("AT+HTTPACTION=0");
        //        var httpActionResponse = await WaitForResponseAsync("+HTTPACTION:", 15000);
        //        if (httpActionResponse == null) return (false, "No HTTPACTION response");

        //        // Parse HTTP status code and length
        //        var lines = httpActionResponse.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        //        string actionLine = lines.FirstOrDefault(line => line.StartsWith("+HTTPACTION:"));
        //        if (actionLine == null) return (false, "No +HTTPACTION line");

        //        var parts = actionLine.Split(',');
        //        if (parts.Length < 3) return (false, "Invalid +HTTPACTION format");

        //        string statusCode = parts[1];
        //        int dataLength = int.Parse(parts[2]);
        //        if (statusCode != "200") return (false, $"HTTP Error: {statusCode}");

        //        // Read HTTP response
        //        SendCommand("AT+HTTPREAD");
        //        string httpReadResponse = await WaitForResponseAsync("OK", 10000);
        //        if (httpReadResponse == null) return (false, "No HTTPREAD response");

        //        int startIndex = httpReadResponse.IndexOf("\r\n") + 2;
        //        int endIndex = httpReadResponse.LastIndexOf("OK") - 2;
        //        if (startIndex >= 0 && endIndex > startIndex)
        //        {
        //            string data = httpReadResponse.Substring(startIndex, endIndex - startIndex).Trim();
        //            return (true, data);
        //        }

        //        return (false, "Failed to parse HTTPREAD data");
        //    }
        //    finally
        //    {
        //        // Always clean up: terminate HTTP session and close bearer
        //        SendCommand("AT+HTTPTERM");
        //        await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result

        //        SendCommand("AT+SAPBR=0,1");
        //        await WaitForResponseAsync("OK", 2000); // Short timeout, ignore result
        //    }
        //}
        //public async Task<string> HttpGetAsync(string apn, string url)
        //{
        //    // Setup bearer context
        //    SendCommand("AT+SAPBR=3,1,\"Contype\",\"GPRS\"");
        //    await WaitForResponseAsync("OK");
        //    SendCommand($"AT+SAPBR=3,1,\"APN\",\"{apn}\"");
        //    await WaitForResponseAsync("OK");
        //    SendCommand("AT+SAPBR=1,1");
        //    await WaitForResponseAsync("OK");
        //    // Initialize HTTP service
        //    SendCommand("AT+HTTPINIT");
        //    await WaitForResponseAsync("OK");
        //    // Set parameters
        //    SendCommand("AT+HTTPPARA=\"CID\",1");
        //    await WaitForResponseAsync("OK");
        //    SendCommand($"AT+HTTPPARA=\"URL\",\"{url}\"");
        //    await WaitForResponseAsync("OK");
        //    // Start GET action
        //    SendCommand("AT+HTTPACTION=0");
        //    string httpActionResponse = await WaitForResponseAsync("+HTTPACTION:", 15000);
        //    if (httpActionResponse == null)
        //        return null;

        //    // Parse HTTP status code and length
        //    var lines = httpActionResponse.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        //    string actionLine = null;
        //    foreach (var line in lines)
        //    {
        //        if (line.StartsWith("+HTTPACTION:"))
        //        {
        //            actionLine = line;
        //            break;
        //        }
        //    }
        //    if (actionLine == null)
        //        return null;
        //    // Example: +HTTPACTION: 0,200,256
        //    var parts = actionLine.Split(',');
        //    if (parts.Length < 3)
        //        return null;
        //    string statusCode = parts[1];
        //    int dataLength = int.Parse(parts[2]);
        //    if (statusCode != "200")
        //        return $"HTTP Error: {statusCode}";
        //    // Read HTTP data
        //    SendCommand("AT+HTTPREAD");
        //    string httpReadResponse = await WaitForResponseAsync("OK", 10000);
        //    if (httpReadResponse == null)
        //        return null;
        //    // Extract data between the response
        //    // The data is between +HTTPREAD: <len> and OK
        //    int startIndex = httpReadResponse.IndexOf("\r\n") + 2;
        //    int endIndex = httpReadResponse.LastIndexOf("OK") - 2;
        //    if (startIndex >= 0 && endIndex > startIndex)
        //    {
        //        string data = httpReadResponse.Substring(startIndex, endIndex - startIndex).Trim();
        //        return data;
        //    }
        //    return null;
        //}

        public void Dispose()
        {
            if (DeviceCom != null)
            {
                if (DeviceCom.IsOpen)
                {
                    DeviceCom.Close();
                }
                DeviceCom.Dispose();
                DeviceCom = null;
            }
        }

    }
}
