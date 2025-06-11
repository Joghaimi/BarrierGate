using Lib;
using Services;
using System.Device.Gpio;

Console.WriteLine("Ready ...    ");



Task GateBearerTask = Task.Run(() => GateBearerLoop());
Task dbUpdateTask = Task.Run(() => DbUpdateToWebsiteLoopAsync());

Task.WaitAll(GateBearerTask, dbUpdateTask);


//string response = await gsm.HttpGetAsync(apn, url);

static void GateBearerLoop()
{
    const int buttonPin = 17;
    const int secondPin = 27;

    var gate = new BarrierGateHelper();
    var gpioController = new GPIOController();

    InitializeHardware(gate, gpioController, buttonPin, secondPin);

    while (true)
    {
        if (gpioController.Read(buttonPin))
        {
            Console.WriteLine($"{DateTime.Now:HH:mm:ss} - 🔘 Button Pressed");

            using var db = new AppDbContext();

            var gateTransaction = new GateTransaction
            {
                Date = DateTime.Now,
                isSent = false,
                numberOfOpenCurrectly = 1,
                numberOfOpenIllegel = 0
            };

            var result = gate.OpenTheGate(gateTransaction);

            LogTransaction(result);
            db.Add(result);
            db.SaveChanges();

            // Optional: Wait for button release to avoid repeated triggers
            //WaitForButtonRelease(gpioController, buttonPin);
        }
        else
        {
            Thread.Sleep(50); // Reduce CPU usage
        }
    }
}

static void InitializeHardware(BarrierGateHelper gate, GPIOController gpio, int pin1, int pin2)
{
    gate.ConnectToTheGate(SerialPorts.Serial0);

    gpio.Setup(pin1, PinMode.InputPullUp);
    gpio.Setup(pin2, PinMode.InputPullUp);

    if (gate.IsGateOpen())
    {
        Console.WriteLine($"{DateTime.Now:HH:mm:ss} - ⛔ Gate is open. Closing...");
        gate.ControlGate(GateAction.Close, 0x01);
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



//static void GateBearerLoop()
//{
//    using var db = new AppDbContext();
//    var gate = new BarrierGateHelper();
//    gate.ConnectToTheGate(SerialPorts.Serial0);
//    var gpioController = new GPIOController();
//    gpioController.Setup(17, PinMode.InputPullUp);
//    if (gate.IsGateOpen())
//    {
//        Console.WriteLine("Gate is open");
//        gate.ControlGate(GateAction.Close, 0x01);
//    }
//    while (true)
//    {
//        if (gpioController.Read(17))
//        {
//            Console.WriteLine("Button is Pressed");
//            var newGateTransaction = new GateTransaction
//            {
//                Date = DateTime.Now,
//                isSent = false,
//                numberOfOpenCurrectly = 1,
//                numberOfOpenIllegel = 0
//            };


//            var transaction = gate.OpenTheGate(newGateTransaction);
//            Console.WriteLine($"Gate Status: {transaction.numberOfOpenCurrectly}" +
//                $" number of illegel Tries {transaction.numberOfOpenIllegel} " +
//                $" number of LoopDetector {transaction.LoopDetector} " +
//                $" number of ReachLowerLimitSwitch {transaction.ReachLowerLimitSwitch}" +
//                $" number of ReachUpperLimit {transaction.ReachUpperLimitSwitch}"
//                );
//            db.Add(newGateTransaction);
//            db.SaveChanges();
//        }
//        else
//        {
//            Thread.Sleep(50); // small delay to avoid CPU overuse when button not pressed
//        }
//    }

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
//            numberOfOpenCurrectly = 1,
//            numberOfOpenIllegel = 0
//        };


//        var gateStatus = gate.OpenTheGate();
//        Console.WriteLine($"Gate Status: {gateStatus.Item1} number of illegel Tries {gateStatus.Item2}");
//        newGateTransaction.numberOfOpenIllegel = gateStatus.Item2;
//        db.Add(newGateTransaction);
//        db.SaveChanges();

//        //Thread.Sleep(500);
//        //if (!gate.IsGateOpen())
//        //{
//        //    Console.WriteLine("Gate is open");
//        //    gate.OpenTheGate();
//        //}
//        //Console.WriteLine("waitting");
//        //while (gpioController.Read(27))
//        //{
//        //    Console.Write(".");
//        //    Thread.Sleep(10);
//        //}
//        //Console.WriteLine();
//        //gate.ControlGate(GateAction.Close, 0x01);
//    }
//    else
//    {
//        Thread.Sleep(50); // small delay to avoid CPU overuse when button not pressed
//    }
//}
static async Task DbUpdateToWebsiteLoopAsync()
{
    var gsm = new GSM808L("/dev/ttyAMA0");

    string apn = "zain"; // For Zain Jordan
    string url = "http://145.223.99.117:5112/api/Gate/Open?numberOfIllegelOpenning=";
    int consecutiveFailures = 0;
    while (true)
    {
        try
        {
            using var db = new AppDbContext();

            var unsent = db.GateTransactions.Where(gt => !gt.isSent).ToList();

            foreach (var item in unsent)
            {

                // Call your HTTP GET async method (assume gsm is accessible here)
                Console.WriteLine($"Start Request ");

                url = $"http://145.223.99.117:5112/api/Gate/Update?Id={item.Id}&Date={item.Date:yyyy/MM/dd}" +
                    $"&numberOfOpenCurrectly={item.numberOfOpenCurrectly}" +
                    $"&numberOfOpenIllegel={item.numberOfOpenIllegel}" +
                    $"&ReachUpperLimitSwitch={item.ReachUpperLimitSwitch}" +
                    $"&ReachLowerLimitSwitch={item.ReachLowerLimitSwitch}" +
                    $"&LoopDetector={item.LoopDetector}";


                //url = $"http://145.223.99.117:5112/api/Gate/Open?numberOfIllegelOpenning={item.numberOfOpenIllegel}";
                var success = await gsm.HttpGetAsync("zain", url);
                Console.WriteLine($"End Request ");

                if (success)
                {
                    Console.WriteLine($"{item.Id} Sent transaction {item.Id} successfully.");
                    item.isSent = true;
                    consecutiveFailures = 0;
                    // Optionally save response if needed: item.Response = response;
                    db.SaveChanges();
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
