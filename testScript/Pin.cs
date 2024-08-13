using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static testScript.GlobalConstants;

namespace testScript
{
    internal class Pin
    {
        void update()
        {
            IO_WRITE(DO_PIN_SET, 1);
            IO_WRITE(DI_PIN_SNS, 2);
            IO_WRITE("PM1.dBC.SV.Set", 1);
            IO_READ("PM1.dSHT.Sns");
            
            if ( true) 
            { 
                PARAM_READ("PM1.vCHM.CDN");IO_WRITE(DO_SHT_SET, 1);
            }
            else
            {
                IO_WRITE(DI_SHT_SNS, 2);
            }


        }

        public void IO_WRITE(string ioName, int val)
        {
        }

        public void IO_READ(string ioName)
        {
        }

        public void PARAM_READ(string ioName)
        {
        }
    }
}
