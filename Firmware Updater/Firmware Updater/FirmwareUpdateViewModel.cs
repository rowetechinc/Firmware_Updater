using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Firmware_Updater
{
    class FirmwareUpdateViewModel : Caliburn.Micro.Screen
    {

        /// <summary>
        /// Initialize the view model to update the firmware.
        /// </summary>
        /// <param name="name">Name of the view.</param>
        public FirmwareUpdateViewModel(string name)
        {
            base.DisplayName = name;
        }
    }
}
