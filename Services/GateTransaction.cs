using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services
{
    public class GateTransaction
    {

        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int numberOfOpenCurrectly { get; set; }
        public int numberOfOpenIllegel { get; set; }
        public bool isSent { get; set; }
    }
}
