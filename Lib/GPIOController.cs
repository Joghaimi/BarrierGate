using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Device.Gpio;
using System.Runtime.InteropServices;

namespace Lib
{
    public class GPIOController
    {
        GpioController _controller;   
        public GPIOController()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            _controller = new GpioController();
        }
        public void Setup(int pin, PinMode mode)
        {
            _controller.OpenPin(pin, mode);
        }
        public bool Read(int pin)
        {
            return _controller.Read(pin) == PinValue.High ? false : true;
        }
    }
}
