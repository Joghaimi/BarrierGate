using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace API
{
    public class GateTransaction
    {

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int numberOfOpenCurrectly { get; set; }
        public int numberOfOpenIllegel { get; set; }
        public int ReachUpperLimitSwitch { get; set; }
        public int ReachLowerLimitSwitch { get; set; }
        public int LoopDetector { get; set; }
        public bool isSent { get; set; }
    }
}
