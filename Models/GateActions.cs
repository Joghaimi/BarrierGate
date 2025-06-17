using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models
{
    public class GateActions
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public GateSignalStatus Actions { get; set; }
        public bool isSent { get; set; }
    }
}
