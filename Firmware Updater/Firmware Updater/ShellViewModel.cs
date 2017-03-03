using Caliburn.Micro;

namespace Firmware_Updater {

    /// <summary>
    /// Update the firmware on the ADCP.
    /// </summary>
    public class ShellViewModel : Conductor<object>, IShell, IDeactivate
    {
        public ShellViewModel()
        {
            base.DisplayName = "RoweTech Inc. Firmware Updater";

            // Set the view
            var vm = IoC.Get<FirmwareUpdateViewModel>();
            ActivateItem(vm);
        }

        /// <summary>
        /// Shutdown the view model.
        /// </summary>
        /// <param name="close"></param>
        void IDeactivate.Deactivate(bool close)
        {
            var vm = IoC.Get<FirmwareUpdateViewModel>();
            if(vm != null)
            {
                vm.Dispose();
            }
        }

    }
}