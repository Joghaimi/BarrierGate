using Services;
using System.Diagnostics;
using System.IO.Ports;
using System.Net;

namespace Lib
{
    public class BarrierGateHelper
    {
        public static SerialPort DeviceCom = new SerialPort();
        public BarrierGateHelper()
        {

        }
        public bool ConnectToTheGate(SerialPorts comName)
        {
            try
            {
                if (!DeviceCom.IsOpen)
                {
                    DeviceCom.PortName = "/dev/ttyUSB0";// comName.ToString();

                    DeviceCom.BaudRate = 19200;
                    DeviceCom.Parity = Parity.None;
                    DeviceCom.DataBits = 8;
                    DeviceCom.StopBits = StopBits.One;
                    DeviceCom.Open();
                    Console.WriteLine("Gate Connected Sucess");
                }
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;

            }
        }

        //public bool OpenTheGate(byte address = 0x01)
        //{
        //    byte[] command = new byte[] { 0xFD, 0x00, address, 0x03, 0xFD, 0xFA };
        //    DeviceCom.Write(command, 0, command.Length);
        //    Console.WriteLine("Open gate command sent.");
        //    try
        //    {
        //        System.Threading.Thread.Sleep(100);
        //        int bytesToRead = DeviceCom.BytesToRead;
        //        if (bytesToRead >= 6)
        //        {
        //            byte[] response = new byte[bytesToRead];
        //            DeviceCom.Read(response, 0, bytesToRead);
        //            Console.WriteLine("Response: " + BitConverter.ToString(response));

        //            if (response.Length >= 6 &&
        //                response[0] == 0xFD &&
        //                response[1] == 0x00 &&
        //                response[2] == address &&
        //                response[3] == 0x03 &&
        //                response[4] == 0xFD &&
        //                response[5] == 0xFA)
        //            {
        //                return true;
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error reading response: " + ex.Message);
        //    }

        //    return false; // Failed or invalid response
        //}

        //public GateTransaction OpenTheGate(GateTransaction transaction)
        //{
        //    var numberOfOpendByLoopDetector = 0;
        //    byte[] command = new byte[] { 0xFD, 0x00, 0x01, 0x03, 0xFD, 0xFA };
        //    Console.WriteLine("📤 Sending open gate command: " + BitConverter.ToString(command));

        //    try
        //    {
        //        DeviceCom.DiscardInBuffer(); // Clear any old data
        //        DeviceCom.Write(command, 0, command.Length);

        //        var sw = System.Diagnostics.Stopwatch.StartNew();
        //        bool gateOpened = false;
        //        bool gateClosed = false;

        //        while (sw.ElapsedMilliseconds < 10000)
        //        {
        //            if (DeviceCom.BytesToRead >= 6)
        //            {
        //                GateSignalStatus doorStatus = ReadSignalStatus();
        //                if (doorStatus == GateSignalStatus.OpenToUpLimit)
        //                {
        //                    Console.WriteLine("✅ Gate opened.");
        //                    transaction.ReachUpperLimitSwitch ++;
        //                    gateOpened = true;
        //                }
        //                else if (doorStatus == GateSignalStatus.CloseToDownLimit)
        //                {
        //                    Console.WriteLine("✅ Gate fully closed.");
        //                    gateClosed = true;
        //                    transaction.ReachLoweerLimitSwitch++;
        //                    //return (true, numberOfOpendByLoopDetector);
        //                    return transaction; // Return the transaction with updated status
        //                }
        //                else if (doorStatus == GateSignalStatus.OpenByLoopDetector)
        //                {
        //                    transaction.LoopDetector++;
        //                }
        //                else
        //                {
        //                    Console.WriteLine($"⚠️ Unknown signal: {doorStatus}");
        //                }
        //            }

        //            Thread.Sleep(100); // Prevent tight loop
        //        }

        //        Console.WriteLine("⏱️ Timeout waiting for full open-close cycle.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("💥 Error: " + ex.Message);
        //    }

        //    return transaction; // If not opened and closed in time
        //}
        public GateTransaction OpenTheGate(GateTransaction transaction)
        {
            const int timeoutMs = 10000;
            const int signalLength = 6;
            byte[] openGateCommand = new byte[] { 0xFD, 0x00, 0x01, 0x03, 0xFD, 0xFA };
            Console.WriteLine("📤 Sending open gate command: " + BitConverter.ToString(openGateCommand));

            try
            {
                SendCommandToDevice(openGateCommand);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                bool gateOpened = false;
                bool gateClosed = false;

                while (stopwatch.ElapsedMilliseconds < timeoutMs)
                {
                    if (DeviceCom.BytesToRead >= signalLength)
                    {
                        var doorStatus = ReadSignalStatus();
                        HandleSignalStatus(doorStatus, transaction, ref gateOpened, ref gateClosed);

                        if (gateClosed)
                        {
                            return transaction; // Done successfully
                        }
                    }

                    Thread.Sleep(100); // Prevent tight loop
                }

                Console.WriteLine("⏱️ Timeout waiting for full open-close cycle.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Error: " + ex.Message);
            }

            return transaction; // Return whatever progress has been made
        }

        private void SendCommandToDevice(byte[] command)
        {
            DeviceCom.DiscardInBuffer(); // Clear old data
            DeviceCom.Write(command, 0, command.Length);
        }

        private void HandleSignalStatus(GateSignalStatus status, GateTransaction transaction, ref bool gateOpened, ref bool gateClosed)
        {
            switch (status)
            {
                case GateSignalStatus.OpenToUpLimit:
                    Console.WriteLine("✅ Gate opened.");
                    transaction.ReachUpperLimitSwitch++;
                    gateOpened = true;
                    break;

                case GateSignalStatus.CloseToDownLimit:
                    Console.WriteLine("✅ Gate fully closed.");
                    transaction.ReachLowerLimitSwitch++;
                    gateClosed = true;
                    break;

                case GateSignalStatus.OpenByLoopDetector:
                    Console.WriteLine("↪️ Gate triggered by loop detector.");
                    transaction.numberOfOpenIllegel++;
                    break;
                case GateSignalStatus.AutoCloseAfterVehiclePassed:
                    Console.WriteLine("↪️  AutoCloseAfterVehiclePassed");
                    transaction.LoopDetector++;
                    break;
                default:
                    Console.WriteLine($"⚠️ Unknown signal: {status}");
                    break;
            }
        }



        public (bool, int) OpenTheGate(byte address = 0x01, int timeoutMs = 10000)
        {
            var numberOfOpendByLoopDetector = 0;
            byte[] command = new byte[] { 0xFD, 0x00, address, 0x03, 0xFD, 0xFA };
            Console.WriteLine("📤 Sending open gate command: " + BitConverter.ToString(command));

            try
            {
                DeviceCom.DiscardInBuffer(); // Clear any old data
                DeviceCom.Write(command, 0, command.Length);

                var sw = System.Diagnostics.Stopwatch.StartNew();
                bool gateOpened = false;
                bool gateClosed = false;

                while (sw.ElapsedMilliseconds < timeoutMs)
                {
                    if (DeviceCom.BytesToRead >= 6)
                    {
                        GateSignalStatus doorStatus = ReadSignalStatus();
                        if (doorStatus == GateSignalStatus.OpenToUpLimit)
                        {
                            Console.WriteLine("✅ Gate opened.");
                            gateOpened = true;
                        }
                        else if (doorStatus == GateSignalStatus.CloseToDownLimit)
                        {
                            Console.WriteLine("✅ Gate fully closed.");
                            gateClosed = true;
                            return (true, numberOfOpendByLoopDetector);
                        }
                        else if (doorStatus == GateSignalStatus.OpenByLoopDetector)
                        {
                            numberOfOpendByLoopDetector++;
                        }
                        else
                        {
                            Console.WriteLine($"⚠️ Unknown signal: {doorStatus}");
                        }
                    }

                    Thread.Sleep(100); // Prevent tight loop
                }

                Console.WriteLine("⏱️ Timeout waiting for full open-close cycle.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Error: " + ex.Message);
            }

            return (false, numberOfOpendByLoopDetector); // If not opened and closed in time
        }




        public bool IsGateOpen(byte address = 0x01)
        {
            // Command: fd 00 <address> 00 fd fa
            byte[] command = new byte[] { 0xFD, 0x00, address, 0x00, 0xFD, 0xFA };
            DeviceCom.Write(command, 0, command.Length);
            Console.WriteLine("Status request sent.");

            try
            {
                System.Threading.Thread.Sleep(100); // Wait briefly for response
                int bytesToRead = DeviceCom.BytesToRead;

                if (bytesToRead >= 6)
                {
                    byte[] response = new byte[bytesToRead];
                    DeviceCom.Read(response, 0, bytesToRead);
                    Console.WriteLine("Response: " + BitConverter.ToString(response));

                    if (response[0] == 0xFD &&
                        response[1] == 0x00 &&
                        response[2] == address &&
                        response[4] == 0xFD &&
                        response[5] == 0xFA)
                    {
                        byte statusCode = response[3];

                        if (statusCode == 0x09)
                            return true;  // Gate is fully open
                        else if (statusCode == 0x0C)
                            return false; // Gate is fully closed
                        else
                            Console.WriteLine($"Gate in unknown/intermediate state: 0x{statusCode:X2}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error checking gate status: " + ex.Message);
            }

            return false; // Failed or unknown state
        }

        public string GetGateStatus(byte address = 0x01)
        {
            // Status request command: fd 00 <address> 00 fd fa
            byte[] command = new byte[] { 0xFD, 0x00, address, 0x00, 0xFD, 0xFA };
            DeviceCom.Write(command, 0, command.Length);
            Console.WriteLine("Status request command sent.");

            try
            {
                System.Threading.Thread.Sleep(100); // Wait for device response
                int bytesToRead = DeviceCom.BytesToRead;

                if (bytesToRead >= 6)
                {
                    byte[] response = new byte[bytesToRead];
                    DeviceCom.Read(response, 0, bytesToRead);
                    Console.WriteLine("Response: " + BitConverter.ToString(response));

                    // Expected response: fd 00 <address> <status> fd fa
                    if (response.Length >= 6 &&
                        response[0] == 0xFD &&
                        response[1] == 0x00 &&
                        response[2] == address &&
                        response[4] == 0xFD &&
                        response[5] == 0xFA)
                    {
                        byte statusCode = response[3];

                        // Interpret status code
                        return statusCode switch
                        {
                            0x00 => "Intermediate state",
                            0x09 => "Gate fully open (up limit position)",
                            0x0C => "Gate fully closed (down limit position)",
                            _ => $"Unknown status (code: 0x{statusCode:X2})"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading status: " + ex.Message);
            }

            return "Failed to read gate status";
        }



        public GateSignalStatus ReadSignalStatus(byte address = 0x01)
        {
            try
            {
                Thread.Sleep(100); // Wait briefly to ensure data is received
                int bytesToRead = DeviceCom.BytesToRead;

                if (bytesToRead >= 6)
                {
                    byte[] response = new byte[bytesToRead];
                    DeviceCom.Read(response, 0, bytesToRead);
                    Console.WriteLine("Signal received: " + BitConverter.ToString(response));

                    if (response[0] == 0xFD &&
                        response[1] == 0x00 &&
                        response[2] == address &&
                        response[4] == 0xFD &&
                        response[5] == 0xFA)
                    {
                        byte signalCode = response[3];

                        if (Enum.IsDefined(typeof(GateSignalStatus), signalCode))
                        {
                            return (GateSignalStatus)signalCode;
                        }
                        else
                        {
                            Console.WriteLine($"⚠ Unknown signal code: 0x{signalCode:X2}");
                            return GateSignalStatus.Unknown;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("💥 Error reading signal: " + ex.Message);
            }

            return GateSignalStatus.Unknown;
        }



        //public string ReadSignalStatus(byte address = 0x01)
        //{
        //    try
        //    {
        //        System.Threading.Thread.Sleep(100); // Small delay to ensure data is received
        //        int bytesToRead = DeviceCom.BytesToRead;

        //        if (bytesToRead >= 6)
        //        {
        //            byte[] response = new byte[bytesToRead];
        //            DeviceCom.Read(response, 0, bytesToRead);
        //            //Console.WriteLine("Signal received: " + BitConverter.ToString(response));

        //            if (response[0] == 0xFD &&
        //                response[1] == 0x00 &&
        //                response[2] == address &&
        //                response[4] == 0xFD &&
        //                response[5] == 0xFA)
        //            {
        //                byte signalCode = response[3];

        //                return signalCode switch
        //                {
        //                    0x02 => "Stop by remote control",
        //                    0x04 => "Open by remote control",
        //                    0x06 => "Close by remote control",
        //                    0x09 => "Open to up limit position",
        //                    0x11 => "Stop by wire control",
        //                    0x13 => "Open by wire control",
        //                    0x15 => "Close by wire control",
        //                    0x16 => "Open by loop detector",
        //                    0x17 => "Open by infrared photocell",
        //                    0x0A => "Auto-close after vehicle passed",
        //                    0x0C => "Close to down limit position",
        //                    0x18 => "Delay auto-closing",
        //                    0x12 => "Open by auto-reversing on obstruction",
        //                    0x14 => "Stop on obstruction",
        //                    0xE3 => "Motor sensor not detected",
        //                    0xE7 => "Spring tension too high or lifted",
        //                    _ => $"Unknown signal code: 0x{signalCode:X2}"
        //                };
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("Error reading signal: " + ex.Message);
        //    }

        //    return "No valid signal received";
        //}


        public bool ControlGate(GateAction action, byte address = 0x01)
        {
            // Build command: FD 00 [address] [command] FD FA
            byte[] command = new byte[] { 0xFD, 0x00, address, (byte)action, 0xFD, 0xFA };
            DeviceCom.Write(command, 0, command.Length);
            Console.WriteLine($"{action} gate command sent.");

            try
            {
                System.Threading.Thread.Sleep(100);
                int bytesToRead = DeviceCom.BytesToRead;
                if (bytesToRead >= 6)
                {
                    byte[] response = new byte[bytesToRead];
                    DeviceCom.Read(response, 0, bytesToRead);
                    Console.WriteLine("Response: " + BitConverter.ToString(response));

                    if (response.Length >= 6 &&
                        response[0] == 0xFD &&
                        response[1] == 0x00 &&
                        response[2] == address &&
                        response[3] == (byte)action &&
                        response[4] == 0xFD &&
                        response[5] == 0xFA)
                    {
                        return true; // Command acknowledged successfully
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading response: " + ex.Message);
            }

            return false; // Failed or invalid response
        }


    }
}


//using System.Diagnostics;
//using System.IO.Ports;
//using System.Net;

//namespace Lib
//{
//    public class BarrierGateHelper
//    {
//        public static SerialPort DeviceCom = new SerialPort();
//        public BarrierGateHelper()
//        {

//        }
//        public bool ConnectToTheGate(SerialPorts comName)
//        {
//            try
//            {
//                if (!DeviceCom.IsOpen)
//                {
//                    //DeviceCom.PortName = "/dev/ttyS0";// comName.ToString();
//                    DeviceCom.PortName = "/dev/ttyUSB0";// comName.ToString();
//                    DeviceCom.BaudRate = 19200;
//                    DeviceCom.Parity = Parity.None;
//                    DeviceCom.DataBits = 8;
//                    DeviceCom.StopBits = StopBits.One;
//                    DeviceCom.Open();
//                }
//                return true;
//            }
//            catch (Exception e)
//            {
//                Debug.WriteLine(e.Message);
//                return false;

//            }
//        }

//        //public bool OpenTheGate(byte address = 0x01)
//        //{
//        //    byte[] command = new byte[] { 0xFD, 0x00, address, 0x03, 0xFD, 0xFA };
//        //    DeviceCom.Write(command, 0, command.Length);
//        //    Console.WriteLine("Open gate command sent.");
//        //    try
//        //    {
//        //        System.Threading.Thread.Sleep(100);
//        //        int bytesToRead = DeviceCom.BytesToRead;
//        //        if (bytesToRead >= 6)
//        //        {
//        //            byte[] response = new byte[bytesToRead];
//        //            DeviceCom.Read(response, 0, bytesToRead);
//        //            Console.WriteLine("Response: " + BitConverter.ToString(response));

//        //            if (response.Length >= 6 &&
//        //                response[0] == 0xFD &&
//        //                response[1] == 0x00 &&
//        //                response[2] == address &&
//        //                response[3] == 0x03 &&
//        //                response[4] == 0xFD &&
//        //                response[5] == 0xFA)
//        //            {
//        //                return true;
//        //            }
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Console.WriteLine("Error reading response: " + ex.Message);
//        //    }

//        //    return false; // Failed or invalid response
//        //}
//        public (bool, int) OpenTheGate(byte address = 0x01, int timeoutMs = 20000)
//        {
//            var numberOfOpendByLoopDetector = 0;
//            byte[] command = new byte[] { 0xFD, 0x00, address, 0x03, 0xFD, 0xFA };
//            Console.WriteLine("ðŸ“¤ Sending open gate command: " + BitConverter.ToString(command));

//            try
//            {
//                DeviceCom.DiscardInBuffer(); // Clear any old data
//                DeviceCom.Write(command, 0, command.Length);

//                var sw = System.Diagnostics.Stopwatch.StartNew();
//                bool gateOpened = false;
//                bool gateClosed = false;

//                while (sw.ElapsedMilliseconds < timeoutMs)
//                {
//                    if (DeviceCom.BytesToRead >= 6)
//                    {
//                        GateSignalStatus doorStatus = ReadSignalStatus();
//                        if (doorStatus == GateSignalStatus.OpenToUpLimit)
//                        {
//                            Console.WriteLine("âœ… Gate opened.");
//                            gateOpened = true;
//                        }
//                        else if (doorStatus == GateSignalStatus.CloseToDownLimit)
//                        {
//                            Console.WriteLine("âœ… Gate fully closed.");
//                            gateClosed = true;
//                            return (true, numberOfOpendByLoopDetector);
//                        }
//                        else if (doorStatus == GateSignalStatus.OpenByLoopDetector)
//                        {
//                            numberOfOpendByLoopDetector++;
//                        }
//                        else
//                        {
//                            Console.WriteLine($"âš ï¸ Unknown signal: {doorStatus}");
//                        }
//                    }

//                    Thread.Sleep(100); // Prevent tight loop
//                }

//                Console.WriteLine("â±ï¸ Timeout waiting for full open-close cycle.");
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("ðŸ’¥ Error: " + ex.Message);
//            }

//            return (false, numberOfOpendByLoopDetector); // If not opened and closed in time
//        }




//        public bool IsGateOpen(byte address = 0x01)
//        {
//            // Command: fd 00 <address> 00 fd fa
//            byte[] command = new byte[] { 0xFD, 0x00, address, 0x00, 0xFD, 0xFA };
//            DeviceCom.Write(command, 0, command.Length);
//            Console.WriteLine("Status request sent.");

//            try
//            {
//                System.Threading.Thread.Sleep(100); // Wait briefly for response
//                int bytesToRead = DeviceCom.BytesToRead;

//                if (bytesToRead >= 6)
//                {
//                    byte[] response = new byte[bytesToRead];
//                    DeviceCom.Read(response, 0, bytesToRead);
//                    Console.WriteLine("Response: " + BitConverter.ToString(response));

//                    if (response[0] == 0xFD &&
//                        response[1] == 0x00 &&
//                        response[2] == address &&
//                        response[4] == 0xFD &&
//                        response[5] == 0xFA)
//                    {
//                        byte statusCode = response[3];

//                        if (statusCode == 0x09)
//                            return true;  // Gate is fully open
//                        else if (statusCode == 0x0C)
//                            return false; // Gate is fully closed
//                        else
//                            Console.WriteLine($"Gate in unknown/intermediate state: 0x{statusCode:X2}");
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error checking gate status: " + ex.Message);
//            }

//            return false; // Failed or unknown state
//        }

//        public string GetGateStatus(byte address = 0x01)
//        {
//            // Status request command: fd 00 <address> 00 fd fa
//            byte[] command = new byte[] { 0xFD, 0x00, address, 0x00, 0xFD, 0xFA };
//            DeviceCom.Write(command, 0, command.Length);
//            Console.WriteLine("Status request command sent.");

//            try
//            {
//                System.Threading.Thread.Sleep(100); // Wait for device response
//                int bytesToRead = DeviceCom.BytesToRead;

//                if (bytesToRead >= 6)
//                {
//                    byte[] response = new byte[bytesToRead];
//                    DeviceCom.Read(response, 0, bytesToRead);
//                    Console.WriteLine("Response: " + BitConverter.ToString(response));

//                    // Expected response: fd 00 <address> <status> fd fa
//                    if (response.Length >= 6 &&
//                        response[0] == 0xFD &&
//                        response[1] == 0x00 &&
//                        response[2] == address &&
//                        response[4] == 0xFD &&
//                        response[5] == 0xFA)
//                    {
//                        byte statusCode = response[3];

//                        // Interpret status code
//                        return statusCode switch
//                        {
//                            0x00 => "Intermediate state",
//                            0x09 => "Gate fully open (up limit position)",
//                            0x0C => "Gate fully closed (down limit position)",
//                            _ => $"Unknown status (code: 0x{statusCode:X2})"
//                        };
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error reading status: " + ex.Message);
//            }

//            return "Failed to read gate status";
//        }



//        public GateSignalStatus ReadSignalStatus(byte address = 0x01)
//        {
//            try
//            {
//                Thread.Sleep(100); // Wait briefly to ensure data is received
//                int bytesToRead = DeviceCom.BytesToRead;

//                if (bytesToRead >= 6)
//                {
//                    byte[] response = new byte[bytesToRead];
//                    DeviceCom.Read(response, 0, bytesToRead);
//                    Console.WriteLine("Signal received: " + BitConverter.ToString(response));

//                    if (response[0] == 0xFD &&
//                        response[1] == 0x00 &&
//                        response[2] == address &&
//                        response[4] == 0xFD &&
//                        response[5] == 0xFA)
//                    {
//                        byte signalCode = response[3];

//                        if (Enum.IsDefined(typeof(GateSignalStatus), signalCode))
//                        {
//                            return (GateSignalStatus)signalCode;
//                        }
//                        else
//                        {
//                            Console.WriteLine($"âš  Unknown signal code: 0x{signalCode:X2}");
//                            return GateSignalStatus.Unknown;
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("ðŸ’¥ Error reading signal: " + ex.Message);
//            }

//            return GateSignalStatus.Unknown;
//        }



//        //public string ReadSignalStatus(byte address = 0x01)
//        //{
//        //    try
//        //    {
//        //        System.Threading.Thread.Sleep(100); // Small delay to ensure data is received
//        //        int bytesToRead = DeviceCom.BytesToRead;

//        //        if (bytesToRead >= 6)
//        //        {
//        //            byte[] response = new byte[bytesToRead];
//        //            DeviceCom.Read(response, 0, bytesToRead);
//        //            //Console.WriteLine("Signal received: " + BitConverter.ToString(response));

//        //            if (response[0] == 0xFD &&
//        //                response[1] == 0x00 &&
//        //                response[2] == address &&
//        //                response[4] == 0xFD &&
//        //                response[5] == 0xFA)
//        //            {
//        //                byte signalCode = response[3];

//        //                return signalCode switch
//        //                {
//        //                    0x02 => "Stop by remote control",
//        //                    0x04 => "Open by remote control",
//        //                    0x06 => "Close by remote control",
//        //                    0x09 => "Open to up limit position",
//        //                    0x11 => "Stop by wire control",
//        //                    0x13 => "Open by wire control",
//        //                    0x15 => "Close by wire control",
//        //                    0x16 => "Open by loop detector",
//        //                    0x17 => "Open by infrared photocell",
//        //                    0x0A => "Auto-close after vehicle passed",
//        //                    0x0C => "Close to down limit position",
//        //                    0x18 => "Delay auto-closing",
//        //                    0x12 => "Open by auto-reversing on obstruction",
//        //                    0x14 => "Stop on obstruction",
//        //                    0xE3 => "Motor sensor not detected",
//        //                    0xE7 => "Spring tension too high or lifted",
//        //                    _ => $"Unknown signal code: 0x{signalCode:X2}"
//        //                };
//        //            }
//        //        }
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        Console.WriteLine("Error reading signal: " + ex.Message);
//        //    }

//        //    return "No valid signal received";
//        //}


//        public bool ControlGate(GateAction action, byte address = 0x01)
//        {
//            // Build command: FD 00 [address] [command] FD FA
//            byte[] command = new byte[] { 0xFD, 0x00, address, (byte)action, 0xFD, 0xFA };
//            DeviceCom.Write(command, 0, command.Length);
//            Console.WriteLine($"{action} gate command sent.");

//            try
//            {
//                System.Threading.Thread.Sleep(100);
//                int bytesToRead = DeviceCom.BytesToRead;
//                if (bytesToRead >= 6)
//                {
//                    byte[] response = new byte[bytesToRead];
//                    DeviceCom.Read(response, 0, bytesToRead);
//                    Console.WriteLine("Response: " + BitConverter.ToString(response));

//                    if (response.Length >= 6 &&
//                        response[0] == 0xFD &&
//                        response[1] == 0x00 &&
//                        response[2] == address &&
//                        response[3] == (byte)action &&
//                        response[4] == 0xFD &&
//                        response[5] == 0xFA)
//                    {
//                        return true; // Command acknowledged successfully
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Error reading response: " + ex.Message);
//            }

//            return false; // Failed or invalid response
//        }


//    }
//}
