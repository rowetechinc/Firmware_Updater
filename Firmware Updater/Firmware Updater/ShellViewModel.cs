using Caliburn.Micro;

namespace Firmware_Updater {

    /// <summary>
    /// Update the firmware on the ADCP.
    /// </summary>
    public class ShellViewModel : Conductor<object>, IShell
    {
        public ShellViewModel()
        {
            base.DisplayName = "RoweTech Inc. Firmware Updater";

            // Set the view
            var evm = IoC.Get<FirmwareUpdateViewModel>();
            ActivateItem(evm);
        }
    }
}