using Lib;
using Models;
using Services;
using System;
using System.Device.Gpio;
using System.IO.Ports;
using System.Net;
using System.Net.NetworkInformation;

Console.WriteLine("Ready ...    ");
/* Test Test */



using var db = new AppDbContext();
SerialPort DeviceCom = new SerialPort();

Task GateBearerTask = Task.Run(() => VistaTransactions());
//Task gateListener = Task.Run(() => GateListener());
GateListener(); // No need for Task.Run
Task dbUpdateTask = Task.Run(() => DbUpdateToWebsiteLoopAsync());
Task.WaitAll(GateBearerTask, dbUpdateTask);


//string response = await gsm.HttpGetAsync(apn, url);

static void VistaTransactions()
{
    const int buttonPin = 27;
    var gpioController = new GPIOController();
    gpioController.Setup(buttonPin, PinMode.InputPullUp);
    while (true)
    {
        if (gpioController.Read(buttonPin))
        {

            var gateAction = new GateActions
            {
                Date = DateTime.Now,
                Actions = GateSignalStatus.VisaTrasnactions,
                isSent = false
            };
            DBHundler.AddGateActionsToDB(gateAction);
            while (gpioController.Read(buttonPin))
                Thread.Sleep(50);
        }
        Console.WriteLine($"Waiting for button press...{gpioController.Read(buttonPin)}");
        Thread.Sleep(50); // Reduce CPU usage
    }
}



//void GateListener()
//{


//    //BarrierGateHelper gate = new BarrierGateHelper();
//    ConnectToTheGate();
//    if (IsGateOpen())
//    {
//        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - ⛔ Gate is open. Closing...");
//        ControlGate(Lib.GateAction.Close, 0x01);
//    }
//    DeviceCom.DataReceived += (s, e) =>
//    {
//        try
//        {
//            byte[] buffer = new byte[DeviceCom.BytesToRead];
//            DeviceCom.Read(buffer, 0, buffer.Length);
//            // Handle buffer here
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine("Error reading from serial: " + ex.Message);
//        }
//    };
//    while (true)
//    {
//        int bytesToRead = DeviceCom.BytesToRead;
//        try
//        {

//            if (bytesToRead >= 6)
//            {
//                byte[] response = new byte[bytesToRead];
//                DeviceCom.Read(response, 0, bytesToRead);
//                Console.WriteLine("Signal received: " + BitConverter.ToString(response));

//                if (response[0] == 0xFD &&
//                    response[1] == 0x00 &&
//                    response[2] == 0x01 &&
//                    response[4] == 0xFD &&
//                    response[5] == 0xFA)
//                {
//                    byte signalCode = response[3];
//                    GateSignalStatus action = Enum.IsDefined(typeof(GateSignalStatus), signalCode)
//                             ? (GateSignalStatus)signalCode
//                             : GateSignalStatus.Unknown;
//                    var gateAction = new GateActions
//                    {
//                        Date = DateTime.Now,
//                        Actions = action,
//                        isSent = false
//                    };

//                    DBHundler.AddGateActionsToDB(gateAction);
//                }
//            }
//            Thread.Sleep(50); // Reduce CPU usage
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"⚠ Error in GateListener loop: {ex.Message}");
//            Thread.Sleep(500); // Delay before retry to avoid rapid crash loops
//        }
//    }
//}
void GateListener()
{
    if (!ConnectToTheGate())
    {
        Console.WriteLine("❌ Could not connect to the gate.");
        return;
    }

    if (IsGateOpen())
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - ⛔ Gate is open. Closing...");
        ControlGate(Lib.GateAction.Close, 0x01);
    }

    DeviceCom.DataReceived += (s, e) =>
    {
        try
        {
            int bytesToRead = DeviceCom.BytesToRead;
            if (bytesToRead >= 6)
            {
                byte[] response = new byte[bytesToRead];
                DeviceCom.Read(response, 0, bytesToRead);
                Console.WriteLine("Signal received: " + BitConverter.ToString(response));

                if (response.Length >= 6 &&
                    response[0] == 0xFD &&
                    response[1] == 0x00 &&
                    response[2] == 0x01 &&
                    response[4] == 0xFD &&
                    response[5] == 0xFA)
                {
                    byte signalCode = response[3];
                    GateSignalStatus action = Enum.IsDefined(typeof(GateSignalStatus), signalCode)
                        ? (GateSignalStatus)signalCode
                        : GateSignalStatus.Unknown;

                    var gateAction = new GateActions
                    {
                        Date = DateTime.Now,
                        Actions = action,
                        isSent = false
                    };

                    DBHundler.AddGateActionsToDB(gateAction);
                }
                else
                {
                    Console.WriteLine($"⚠ Invalid frame received: {BitConverter.ToString(response)}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("💥 Error reading from serial: " + ex.Message);
        }
    };

    Console.WriteLine("📡 Gate listener activated.");
}



bool IsGateOpen(byte address = 0x01)
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

bool ControlGate(Lib.GateAction action, byte address = 0x01)
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

bool ConnectToTheGate()
{
    try
    {
        if (!DeviceCom.IsOpen)
        {
            DeviceCom.PortName = "/dev/ttyUSB1";// comName.ToString();
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


static void LogTransaction(GateTransaction transaction)
{
    Console.WriteLine($"{DateTime.Now:HH:mm:ss} - ✅ Gate Status Report:");
    Console.WriteLine($"  - numberOfOpenCurrectly: {transaction.numberOfOpenCurrectly}");
    Console.WriteLine($"  - numberOfOpenIllegal: {transaction.numberOfOpenIllegel}");
    Console.WriteLine($"  - LoopDetector: {transaction.LoopDetector}");
    Console.WriteLine($"  - ReachLowerLimitSwitch: {transaction.ReachLowerLimitSwitch}");
    Console.WriteLine($"  - ReachUpperLimitSwitch: {transaction.ReachUpperLimitSwitch}");
}

static void WaitForButtonRelease(GPIOController gpio, int pin)
{
    while (gpio.Read(pin))
    {
        Thread.Sleep(50); // Wait until button is released
    }
}




static async Task DbUpdateToWebsiteLoopAsync()
{
    var gsm = new GSM808L("/dev/ttyAMA0");

    string apn = "zain"; // For Zain Jordan
    string url = "http://145.223.99.117:5112/api/Gate/Open?numberOfIllegelOpenning=";
    int consecutiveFailures = 0;
    //while (true)
    //{
    //    try
    //    {
    //        using var db = new AppDbContext();

    //        var unsent = db.GateTransactions.Where(gt => !gt.isSent).ToList();

    //        foreach (var item in unsent)
    //        {

    //            // Call your HTTP GET async method (assume gsm is accessible here)
    //            Console.WriteLine($"Start Request ");

    //            url = $"http://145.223.99.117:5112/api/Gate/Update?Id={item.Id}&Date={item.Date:yyyy/MM/dd}" +
    //                $"&numberOfOpenCurrectly={item.numberOfOpenCurrectly}" +
    //                $"&numberOfOpenIllegel={item.numberOfOpenIllegel}" +
    //                $"&ReachUpperLimitSwitch={item.ReachUpperLimitSwitch}" +
    //                $"&ReachLowerLimitSwitch={item.ReachLowerLimitSwitch}" +
    //                $"&LoopDetector={item.LoopDetector}";


    //            //url = $"http://145.223.99.117:5112/api/Gate/Open?numberOfIllegelOpenning={item.numberOfOpenIllegel}";
    //            var success = await gsm.HttpGetAsync("zain", url);
    //            Console.WriteLine($"End Request ");

    //            if (success)
    //            {
    //                Console.WriteLine($"{item.Id} Sent transaction {item.Id} successfully.");
    //                item.isSent = true;
    //                consecutiveFailures = 0;
    //                // Optionally save response if needed: item.Response = response;
    //                db.SaveChanges();
    //            }
    //            else
    //            {
    //                consecutiveFailures++;
    //                if (consecutiveFailures >= 5)
    //                {
    //                    Console.WriteLine("Too many failures, resetting modem...");
    //                    gsm.SendCommand("AT+CFUN=1,1"); // Reset modem
    //                    await Task.Delay(10000); // Wait for reset
    //                    consecutiveFailures = 0;
    //                }
    //                Console.WriteLine($"Failed to send transaction {item.Id}. Response/Error: ");
    //                // Keep isSent = false, so you retry later
    //            }
    //        }


    //        Console.WriteLine($"Processed {unsent.Count} records at {DateTime.Now}");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Error updating DB: {ex.Message}");
    //    }

    //    Thread.Sleep(5000); // 5 seconds delay
    //}
    while (true)
    {
        try
        {
            using var db = new AppDbContext();
            var unsent = DBHundler.GetUnsentGateActions();

            foreach (var item in unsent)
            {

                // Call your HTTP GET async method (assume gsm is accessible here)
                Console.WriteLine($"Start Request ");

                url = $"http://145.223.99.117:5112/api/Gate/Update?UpdateGateActions={item.Id}&Date={item.Date:yyyy/MM/dd}" +
                    $"&GateSignalStatus={item.Actions}";
                var success = await gsm.HttpGetAsync("zain", url);
                Console.WriteLine($"End Request ");

                if (success)
                {
                    Console.WriteLine($"{item.Id} Sent transaction {item.Id} successfully.");
                    item.isSent = true;
                    consecutiveFailures = 0;
                    // Optionally save response if needed: item.Response = response;
                    DBHundler.UpdateGateActionInDB(item);
                }
                else
                {
                    consecutiveFailures++;
                    if (consecutiveFailures >= 5)
                    {
                        Console.WriteLine("Too many failures, resetting modem...");
                        gsm.SendCommand("AT+CFUN=1,1"); // Reset modem
                        await Task.Delay(10000); // Wait for reset
                        consecutiveFailures = 0;
                    }
                    Console.WriteLine($"Failed to send transaction {item.Id}. Response/Error: ");
                    // Keep isSent = false, so you retry later
                }
            }


            Console.WriteLine($"Processed {unsent.Count} records at {DateTime.Now}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating DB: {ex.Message}");
        }

        Thread.Sleep(5000); // 5 seconds delay
    }
}
//}


//while (true)
//{
//    if (gpioController.Read(17))
//    {
//        Console.WriteLine("Button is Pressed");
//        var newGateTransaction = new GateTransaction
//        {
//            Date = DateTime.Now,
//            isSent = false,
//        };

//        db.Add(newGateTransaction);
//        db.SaveChanges();
//        gate.OpenTheGate();
//        Thread.Sleep(500);
//        if (!gate.IsGateOpen())
//        {
//            Console.WriteLine("Gate is open");
//            gate.OpenTheGate();
//        }
//        Console.WriteLine("waitting");
//        while (gpioController.Read(27))
//        {
//            Console.Write(".");
//            // Wait for the button to be released
//            Thread.Sleep(10);
//        }
//        Console.WriteLine();
//        gate.ControlGate(GateAction.Close, 0x01);

//    }
//}
