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
    using var db = new AppDbContext();
    var gate = new BarrierGateHelper();
    gate.ConnectToTheGate(SerialPorts.Serial0);
    var gpioController = new GPIOController();
    gpioController.Setup(17, PinMode.InputPullUp);
    gpioController.Setup(27, PinMode.InputPullUp);
    if (gate.IsGateOpen())
    {
        Console.WriteLine("Gate is open");
        gate.ControlGate(GateAction.Close, 0x01);
    }

    while (true)
    {
        if (gpioController.Read(17))
        {
            Console.WriteLine("Button is Pressed");
            var newGateTransaction = new GateTransaction
            {
                Date = DateTime.Now,
                isSent = false,
            };

            db.Add(newGateTransaction);
            db.SaveChanges();
            gate.OpenTheGate();
            //Thread.Sleep(500);
            //if (!gate.IsGateOpen())
            //{
            //    Console.WriteLine("Gate is open");
            //    gate.OpenTheGate();
            //}
            //Console.WriteLine("waitting");
            //while (gpioController.Read(27))
            //{
            //    Console.Write(".");
            //    Thread.Sleep(10);
            //}
            //Console.WriteLine();
            //gate.ControlGate(GateAction.Close, 0x01);
        }
        else
        {
            Thread.Sleep(50); // small delay to avoid CPU overuse when button not pressed
        }
    }
}
static async Task DbUpdateToWebsiteLoopAsync()
{
    var gsm = new GSM808L("/dev/ttyAMA0");

    string apn = "zain"; // For Zain Jordan
    string url = "http://145.223.99.117:5112/api/Gate/Open";
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
                var success = await gsm.HttpGetAsync("zain", url);
                Console.WriteLine($"End Request ");

                if (success)
                {
                    Console.WriteLine($"{item.Id}Sent transaction {item.Id} successfully.");
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
