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
    //public enum GateSignalStatus : byte
    //{
    //    StopByRemoteControl = 0x02,
    //    OpenByRemoteControl = 0x04,
    //    CloseByRemoteControl = 0x06,
    //    OpenToUpLimit = 0x09,
    //    AutoCloseAfterVehiclePassed = 0x0A,
    //    CloseToDownLimit = 0x0C,
    //    StopByWireControl = 0x11,
    //    OpenByWireControl = 0x13,
    //    CloseByWireControl = 0x15,
    //    OpenByLoopDetector = 0x16,
    //    OpenByInfraredPhotocell = 0x17,
    //    DelayAutoClosing = 0x18,
    //    OpenByAutoReversing = 0x12,
    //    StopOnObstruction = 0x14,
    //    MotorSensorNotDetected = 0xE3,
    //    SpringTensionTooHighOrLifted = 0xE7,
    //    Unknown = 0xFF // default for unknown signals
    //}
}
