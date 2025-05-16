using Lib;
using System.Device.Gpio;

var gate = new BarrierGateHelper();
gate.ConnectToTheGate(SerialPorts.Serial0);
var gpioController = new GPIOController();
gpioController.Setup(17, PinMode.InputPullUp);
while (true)
{
    if (gpioController.Read(17))
    {
        Console.WriteLine("Button pressed");
      
        gate.OpenTheGate();
    }
    else
    {
        Console.WriteLine("Button not pressed");
    }

    //gate.OpenTheGate();
    //Thread.Sleep(1000);
}
