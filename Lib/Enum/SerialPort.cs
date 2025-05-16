using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lib
{
    public enum SerialPorts
    {
        Serial0,
        Serial2,
        TestPort1,
        TestPort2,
    }
    public enum GateAction
    {
        SearchStatus = 0x00,
        Stop = 0x01,
        Open = 0x03,
        Close = 0x05,
        Lock = 0x07,
        Unlock = 0x08,
        EnableProactiveReporting = 0xA1,
        DisableProactiveReporting = 0xA0
    }
}
