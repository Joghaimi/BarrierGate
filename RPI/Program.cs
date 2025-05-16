using Lib;
using System.Device.Gpio;
Console.WriteLine("Ready ...    ");

var gate = new BarrierGateHelper();
gate.ConnectToTheGate(SerialPorts.Serial0);
var gpioController = new GPIOController();
gpioController.Setup(17, PinMode.InputPullUp);
while (true)
{
    if (gpioController.Read(17))
    {
        Console.WriteLine("Button pressed");
        if (!gate.IsGateOpen())
        {
            gate.OpenTheGate();

        }
        else
        {
            gate.ControlGate(GateAction.Close, 0x01);
        }
    }


    //gate.OpenTheGate();
    //Thread.Sleep(1000);
}
