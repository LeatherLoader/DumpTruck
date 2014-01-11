using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeatherLoader.ModList;

namespace DumpTruck
{
    public class DumpTruckInfo : IModInfo
    {
        public DumpTruckInfo()
        {

        }

        public string GetModName()
        {
            return "CANVOX-DumpTruck";
        }

        public string GetModVersion()
        {
            return "1.0.0";
        }

        public string GetPrettyModName()
        {
            return "Dump Truck";
        }

        public string GetPrettyModVersion()
        {
            return "version 1.0";
        }

        public bool CanAcceptModlessClients()
        {
            return true;
        }

        public bool CanConnectToModlessServers()
        {
            return true;
        }

        public string GetCreditString()
        {
            return "By CanVox";
        }
    }
}
