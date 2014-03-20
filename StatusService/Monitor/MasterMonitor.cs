using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StatusService
{
    class MasterMonitor : BaseMonitor
    {
        public MasterMonitor()
            : base( null )
        {
        }

        protected override void Tick()
        {
            base.Tick();
        }
    }
}
