using Lib;

var gate = new BarrierGateHelper();
gate.ConnectToTheGate(SerialPorts.Serial0);
while (true)
{
    gate.OpenTheGate();
    Thread.Sleep(1000);
}
