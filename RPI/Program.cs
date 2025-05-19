using Lib;
using Services;
using System.Device.Gpio;
Console.WriteLine("Ready ...    ");
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
        Thread.Sleep(500);
        if (!gate.IsGateOpen())
        {
            Console.WriteLine("Gate is open");
            gate.OpenTheGate();
        }
        Console.WriteLine("waitting");
        while (gpioController.Read(27))
        {
            Console.Write(".");
            // Wait for the button to be released
            Thread.Sleep(10);
        }
        Console.WriteLine();
        gate.ControlGate(GateAction.Close, 0x01);

    }


    //gate.OpenTheGate();
    //Thread.Sleep(1000);
}
