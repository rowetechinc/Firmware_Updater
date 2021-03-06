﻿/*
 * Copyright 2017, Rowe Technologies Inc. 
 * All rights reserved.
 * http://www.rowetechinc.com
 * https://github.com/rowetechinc
 * 
 * Redistribution and use in source and binary forms, with or without modification, are
 * permitted provided that the following conditions are met:
 * 
 *  1. Redistributions of source code must retain the above copyright notice, this list of
 *      conditions and the following disclaimer.
 *      
 *  2. Redistributions in binary form must reproduce the above copyright notice, this list
 *      of conditions and the following disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 *      
 *  THIS SOFTWARE IS PROVIDED BY Rowe Technologies Inc. ''AS IS'' AND ANY EXPRESS OR IMPLIED 
 *  WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND
 *  FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL <COPYRIGHT HOLDER> OR
 *  CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 *  CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 *  SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON
 *  ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 *  NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 *  ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *  
 * The views and conclusions contained in the software and documentation are those of the
 * authors and should not be interpreted as representing official policies, either expressed
 * or implied, of Rowe Technologies Inc.
 * 
 * 
 * HISTORY
 * -----------------------------------------------------------------
 * Date            Initials    Version    Comments
 * -----------------------------------------------------------------
 * 03/02/2017      RC          1.0.0      Initial coding
 * 02/14/2020      RC          3.0.0      Added Ethernet Upload.
 * 
 */


using Newtonsoft.Json;
using ReactiveUI.Legacy;
using ReactiveUI;
using RTI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
//using System.Windows;
using AutoUpdaterDotNET;
using System;
using System.Windows.Forms;

namespace Firmware_Updater
{
    /// <summary>
    /// Class to describe the Firmware information.
    /// </summary>
    public class FirmwareInfo
    {
        /// <summary>
        /// Lastest version number.
        /// </summary>
        public string LatestVersion { get; set; }

        /// <summary>
        /// Major firmware revision number.
        /// </summary>
        public int Major { get; set; }

        /// <summary>
        /// Minor firmware revision number.
        /// </summary>
        public int Minor { get; set; }

        /// <summary>
        /// Revision firmware revision number.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// URL to download the latest version.
        /// </summary>
        public string URL { get; set; }

        /// <summary>
        /// Change log for the firmware version.
        /// </summary>
        public string ChangeLog { get; set; }

        /// <summary>
        /// Files to upload to the ADCP.
        /// </summary>
        public string[] Files { get; set; }
 
        /// <summary>
        /// Initialize the object.
        /// </summary>
        public FirmwareInfo()
        {
            LatestVersion = "";
            URL = "";
            ChangeLog = "";
            Major = 0;
            Minor = 0;
            Revision = 0;

            // Default file to at least upload
            Files = new string[1];
            Files[0] = "rtisys1.bin";
        }
    }

    /// <summary>
    /// Update the ADCP with the latest Firmware version.
    /// </summary>
    public class FirmwareUpdateViewModel : Caliburn.Micro.Screen, IDisposable
    {

        #region Variables

        /// <summary>
        /// URL to look for the latest firmware.
        /// </summary>
        private string DOWNLOAD_FIRMWARE_URL = "http://rowetechinc.com/swfw/latest/firmware/LatestFirmwareVersion.json";

        /// <summary>
        /// URL to look for the latest software info.
        /// </summary>
        private string SOFTWARE_UPDATE_URL = "http://rowetechinc.com/swfw/latest/firmware/Firmware_Updater_AppCast.xml";

        /// <summary>
        /// ADCP Serial port.
        /// </summary>
        private AdcpSerialPort _serialPort;

        /// <summary>
        /// ADCP ethernet port.
        /// </summary>
        private AdcpEthernet _ethernetPort;

        /// <summary>
        /// ADCP Serial port options.
        /// </summary>
        private SerialOptions _serialOptions;

        /// <summary>
        /// ADCP Ethernet port options.
        /// </summary>
        private AdcpEthernetOptions _ethernetOptions;

        /// <summary>
        /// The firmware info found on the internet.
        /// </summary>
        private FirmwareInfo _firmwareInfo;

        #endregion

        #region Properties

        #region IsLoading

        /// <summary>
        /// Flag for loading.
        /// </summary>
        private bool _IsLoading;
        /// <summary>
        /// Flag for loading.
        /// </summary>
        public bool IsLoading
        {
            get { return _IsLoading || _IsCheckingAdcp; }
            set
            {
                _IsLoading = value;
                this.NotifyOfPropertyChange(() => this.IsLoading);
                this.NotifyOfPropertyChange(() => this.CanUpdate);
                this.NotifyOfPropertyChange(() => this.IsNotLoading); 
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }

        public bool IsNotLoading
        {
            get { return !IsLoading; }
        }

        /// <summary>
        /// Flag for checking ADCP.
        /// </summary>
        private bool _IsCheckingAdcp;
        /// <summary>
        /// Flag for checking ADCP.
        /// </summary>
        public bool IsCheckingAdcp
        {
            get { return _IsCheckingAdcp; }
            set
            {
                _IsCheckingAdcp = value;
                this.NotifyOfPropertyChange(() => this.IsCheckingAdcp);
                this.NotifyOfPropertyChange(() => this.IsLoading);
                this.NotifyOfPropertyChange(() => this.IsNotLoading);
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }


        /// <summary>
        /// Flag for checking ADCP.
        /// </summary>
        public bool IsAdcpSerialConnectAvail
        {
            get { return !IsLoading && _IsSerialSelected; }

        }

        /// <summary>
        /// Flag for checking ADCP.
        /// </summary>
        public bool IsAdcpPingAvail
        {
            get { return !IsLoading && _IsEthernetSelected; }

        }



        #endregion

        #region Files

        /// <summary>
        /// Local file tooltip.
        /// </summary>
        private string _LocalFileTooltip;
        /// <summary>
        /// Local file tooltip.
        /// </summary>
        public string LocalFileTooltip
        {
            get { return _LocalFileTooltip; }
            set
            {
                _LocalFileTooltip = value;
                this.NotifyOfPropertyChange(() => this.LocalFileTooltip);
            }
        }

        /// <summary>
        /// Internet file tooltip.
        /// </summary>
        private string _InternetFileTooltip;
        /// <summary>
        /// Internet file tooltip.
        /// </summary>
        public string InternetFileTooltip
        {
            get { return _InternetFileTooltip; }
            set
            {
                _InternetFileTooltip = value;
                this.NotifyOfPropertyChange(() => this.InternetFileTooltip);
            }
        }

        /// <summary>
        /// Local file tooltip.
        /// </summary>
        private string _LocalFilePath;
        /// <summary>
        /// Local file.
        /// </summary>
        public string LocalFilePath
        {
            get { return _LocalFilePath; }
            set
            {
                _LocalFilePath = value;
                this.NotifyOfPropertyChange(() => this.LocalFilePath);
            }
        }

        /// <summary>
        /// File found on the internet.
        /// </summary>
        private string _InternetFile;
        /// <summary>
        /// File found on the internet.
        /// </summary>
        public string InternetFile
        {
            get { return _InternetFile; }
            set
            {
                _InternetFile = value;
                this.NotifyOfPropertyChange(() => this.InternetFile);
            }
        }

        #endregion

        #region Checkboxes

        /// <summary>
        /// Is the local file checkbox selected.
        /// </summary>
        private bool _IsLocalFileSelected;
        /// <summary>
        /// Is the local file checkbox selected.
        /// </summary>
        public bool IsLocalFileSelected
        {
            get { return _IsLocalFileSelected; }
            set
            {
                _IsLocalFileSelected = value;

                // Make the other option the opposite
                if (_IsInternetSelected && value)
                {
                    IsInternetSelected = !value;
                }

                // Make the other option the opposite
                if (!_IsInternetSelected && !value)
                {
                    IsInternetSelected = !value;
                }

                this.NotifyOfPropertyChange(() => this.IsLocalFileSelected);
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }

        /// <summary>
        /// Is the internet file checkbox selected.
        /// </summary>
        private bool _IsInternetSelected;
        /// <summary>
        /// Is the internet file checkbox selected.
        /// </summary>
        public bool IsInternetSelected
        {
            get { return _IsInternetSelected; }
            set
            {
                _IsInternetSelected = value;

                // Make the other option the opposite
                if (_IsLocalFileSelected && value)
                {
                    IsLocalFileSelected = !value;
                }

                if (!_IsLocalFileSelected && !value)
                {
                    IsLocalFileSelected = !value;
                }

                this.NotifyOfPropertyChange(() => this.IsInternetSelected);
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }

        /// <summary>
        /// Select serial port to upload the data.
        /// </summary>
        private bool _IsSerialSelected;
        /// <summary>
        /// Select serial port to upload the data.
        /// </summary>
        public bool IsSerialSelected
        {
            get { return _IsSerialSelected; }
            set
            {
                _IsSerialSelected = value;

                // Make the other option the opposite
                if (_IsEthernetSelected && value)
                {
                    IsEthernetSelected = !value;
                }

                // Make the other option the opposite
                if (!_IsEthernetSelected && !value)
                {
                    IsEthernetSelected = !value;
                }

                this.NotifyOfPropertyChange(() => this.IsSerialSelected);
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }

        /// <summary>
        /// Select serial port to upload the data.
        /// </summary>
        private bool _IsEthernetSelected;
        /// <summary>
        /// Select serial port to upload the data.
        /// </summary>
        public bool IsEthernetSelected
        {
            get { return _IsEthernetSelected; }
            set
            {
                _IsEthernetSelected = value;

                // Make the other option the opposite
                if (_IsSerialSelected && value)
                {
                    IsSerialSelected = !value;
                }

                // Make the other option the opposite
                if (!_IsSerialSelected && !value)
                {
                    IsSerialSelected = !value;
                }

                this.NotifyOfPropertyChange(() => this.IsEthernetSelected);
                this.NotifyOfPropertyChange(() => this.IsAdcpSerialConnectAvail);
                this.NotifyOfPropertyChange(() => this.IsAdcpPingAvail);
            }
        }

        #endregion

        #region Ports

        /// <summary>
        /// List of all the comm ports on the computer.
        /// </summary>
        private List<string> _CommPortList;
        /// <summary>
        /// List of all the comm ports on the computer.
        /// </summary>
        public List<string> CommPortList
        {
            get { return _CommPortList; }
            set
            {
                _CommPortList = value;
                this.NotifyOfPropertyChange(() => this.CommPortList);
            }
        }

        /// <summary>
        /// List of all the baud rate options.
        /// </summary>
        public List<int> BaudRateList { get; set; }

        /// <summary>
        /// Selected COMM Port.
        /// </summary>
        private string _SelectedCommPort;
        /// <summary>
        /// Selected COMM Port.
        /// </summary>
        public string SelectedCommPort
        {
            get { return _SelectedCommPort; }
            set
            {
                _SelectedCommPort = value;
                this.NotifyOfPropertyChange(() => this.SelectedCommPort);

                // Set the serial options
                _serialOptions.Port = value;

                // Reconnect the ADCP
                //ReconnectAdcpSerial(_serialOptions);

                // Reset check to update
                this.NotifyOfPropertyChange(() => this.CanUpdate);
            }
        }

        /// <summary>
        /// Selected baud rate.
        /// </summary>
        private int _SelectedBaud;
        /// <summary>
        /// Selected baud rate.
        /// </summary>
        public int SelectedBaud
        {
            get { return _SelectedBaud; }
            set
            {
                _SelectedBaud = value;
                this.NotifyOfPropertyChange(() => this.SelectedBaud);

                // Set the serial options
                _serialOptions.BaudRate = value;

                // Reconnect the ADCP
                //ReconnectAdcpSerial(_serialOptions);

                // Reset check to update
                this.NotifyOfPropertyChange(() => this.CanUpdate);
            }
        }

        #endregion

        #region Ethernet


        /// <summary>
        /// Selected ADCP Ethernet Address A.
        /// </summary>
        public string EtherAddressA
        {
            get
            {
                return _ethernetOptions.IpAddrA.ToString();
            }
            set
            {
                // ADCP Ethernet Option Address A
                _ethernetOptions.IpAddrA = uint.Parse(value);

                this.NotifyOfPropertyChange(() => this.EtherAddressA);
            }
        }

        /// <summary>
        /// Selected ADCP Ethernet Address B.
        /// </summary>
        public string EtherAddressB
        {
            get
            {
                return _ethernetOptions.IpAddrB.ToString();
            }
            set
            {   
                // Ethernet options
                _ethernetOptions.IpAddrB = uint.Parse(value); ;

                this.NotifyOfPropertyChange(() => this.EtherAddressB);
            }
        }

        /// <summary>
        /// Selected ADCP Ethernet Address C.
        /// </summary>
        public string EtherAddressC
        {
            get
            {
                return _ethernetOptions.IpAddrC.ToString();
            }
            set
            {
                // ADCP Ethernet Option Address C
                _ethernetOptions.IpAddrC = uint.Parse(value);

                this.NotifyOfPropertyChange(() => this.EtherAddressC);
            }
        }

        /// <summary>
        /// Selected ADCP Ethernet Address D.
        /// </summary>
        public string EtherAddressD
        {
            get
            {
                return _ethernetOptions.IpAddrD.ToString();
            }
            set
            {
                // ADCP Ethernet Option Address D
                _ethernetOptions.IpAddrD = uint.Parse(value); ;

                this.NotifyOfPropertyChange(() => this.EtherAddressD);
            }
        }

        /// <summary>
        /// Ethernet Port
        /// </summary>
        public uint EtherPort
        {
            get
            {
                return _ethernetOptions.Port;
            }
            set
            {
                // Set the ethernet port
                _ethernetOptions.Port = value;
                this.NotifyOfPropertyChange(() => this.EtherPort);
            }
        }

        #endregion

        #region Serial Output

        /// <summary>
        /// Local file tooltip.
        /// </summary>
        private string _SerialOutput;
        /// <summary>
        /// Local file tooltip.
        /// </summary>
        public string SerialOutput
        {
            get { return _SerialOutput; }
            set
            {
                _SerialOutput = value;
                this.NotifyOfPropertyChange(() => this.SerialOutput);
            }
        }
        

        #endregion

        #region Upload Data

        /// <summary>
        /// The current number of bytes written uploaded.
        /// </summary>
        private long _uploadFilePogress;
        /// <summary>
        /// The current number of bytes written uploaded.
        /// </summary>
        public long UploadFileProgress
        {
            get { return _uploadFilePogress; }
            set
            {
                _uploadFilePogress = value;
                this.NotifyOfPropertyChange(() => this.UploadFileProgress);
            }
        }

        /// <summary>
        /// The size of the file being uploaded in bytes.
        /// </summary>
        private long _uploadFileSize;
        /// <summary>
        /// The size of the file being uploaded in bytes.
        /// </summary>
        public long UploadFileSize
        {
            get { return _uploadFileSize; }
            set
            {
                _uploadFileSize = value;
                this.NotifyOfPropertyChange(() => this.UploadFileSize);
            }
        }

        #endregion

        #region Download Progress

        /// <summary>
        /// The current number of bytes written downloaded.
        /// </summary>
        private int _DownloadProgress;
        /// <summary>
        /// The current number of bytes written downloaded.
        /// </summary>
        public int DownloadProgress
        {
            get { return _DownloadProgress; }
            set
            {
                _DownloadProgress = value;
                this.NotifyOfPropertyChange(() => this.DownloadProgress);
            }
        }


        #endregion

        #region Firmware Version

        /// <summary>
        /// Latest firmware version.
        /// </summary>
        private string _LatestFirmwareVersion;
        /// <summary>
        /// Latest firmware version.
        /// </summary>
        public string LatestFirmwareVersion
        {
            get { return _LatestFirmwareVersion; }
            set
            {
                _LatestFirmwareVersion = value;
                this.NotifyOfPropertyChange(() => this.LatestFirmwareVersion);
            }
        }

        /// <summary>
        /// Latest firmware changelog.
        /// </summary>
        private string _ChangeLog;
        /// <summary>
        /// Latest firmware changelog.
        /// </summary>
        public string ChangeLog
        {
            get { return _ChangeLog; }
            set
            {
                _ChangeLog = value;
                this.NotifyOfPropertyChange(() => this.ChangeLog);
            }
        }

        
        #endregion

        #region Status

        /// <summary>
        /// Status of the update process.
        /// </summary>
        private string _FimwareUpdateStatus;
        /// <summary>
        /// Status of the update process.
        /// </summary>
        public string FirmwareUpdateStatus
        {
            get { return _FimwareUpdateStatus; }
            set
            {
                _FimwareUpdateStatus = value;
                this.NotifyOfPropertyChange(() => this.FirmwareUpdateStatus);
            }
        }

        /// <summary>
        /// Flag if we can update.
        /// </summary>
        public bool CanUpdate
        {
            get 
            { 
                return _IsAdcpFound && !_IsLoading; 
            }
        }

        /// <summary>
        /// Flag if ADCP is found.
        /// </summary>
        private bool _IsAdcpFound;
        /// <summary>
        /// Flag if ADCP is found.
        /// </summary>
        public bool IsAdcpFound
        {
            get { return _IsAdcpFound; }
            set
            {
                _IsAdcpFound = value;
                this.NotifyOfPropertyChange(() => this.IsAdcpFound);
                this.NotifyOfPropertyChange(() => this.CanUpdate);
            }
        }

        #endregion

        #region ADCP Status

        /// <summary>
        /// Status of the ADCP connection.
        /// </summary>
        private string _AdcpStatus;
        /// <summary>
        /// Status of the ADCP connection.
        /// </summary>
        public string AdcpStatus
        {
            get { return _AdcpStatus; }
            set
            {
                _AdcpStatus = value;
                this.NotifyOfPropertyChange(() => this.AdcpStatus);
            }
        }

        /// <summary>
        /// Current verison of the ADCP firwmare.
        /// </summary>
        private string _AdcpFirmwareVersion;
        /// <summary>
        /// Current verison of the ADCP firwmare.
        /// </summary>
        public string AdcpFirmwareVersion
        {
            get { return _AdcpFirmwareVersion; }
            set
            {
                _AdcpFirmwareVersion = value;
                this.NotifyOfPropertyChange(() => this.AdcpFirmwareVersion);
            }
        }

        /// <summary>
        /// Status of the ADCP fimrware.  Is the
        /// firwmare up to date.
        /// </summary>
        private string _AdcpFirmwareStatus;
        /// <summary>
        /// Status of the ADCP fimrware.  Is the
        /// firwmare up to date.
        /// </summary>
        public string AdcpFirmwareStatus
        {
            get { return _AdcpFirmwareStatus; }
            set
            {
                _AdcpFirmwareStatus = value;
                this.NotifyOfPropertyChange(() => this.AdcpFirmwareStatus);
            }
        }

        #endregion

        #region Software Version
       
        /// <summary>
        /// Current Software Version
        /// </summary>
        private string _CurrentSoftwareVersion;
        /// <summary>
        /// Current Software Version
        /// </summary>
        public string CurrentSoftwareVersion
        {
            get { return _CurrentSoftwareVersion; }
            set
            {
                _CurrentSoftwareVersion = value;
                this.NotifyOfPropertyChange(() => this.CurrentSoftwareVersion);
            }
        }

        /// <summary>
        /// New Software Version
        /// </summary>
        private string _NewSoftwareVersion;
        /// <summary>
        /// New Software Version
        /// </summary>
        public string NewSoftwareVersion
        {
            get { return _NewSoftwareVersion; }
            set
            {
                _NewSoftwareVersion = value;
                this.NotifyOfPropertyChange(() => this.NewSoftwareVersion);
            }
        }

        #endregion

        #endregion

        #region Commands

        /// <summary>
        /// Command to scan for available ADCP.
        /// </summary>
        public ReactiveCommand<object> ScanAdcpCommand { get; protected set; }

        /// <summary>
        /// Command to scan for available ADCP.
        /// </summary>
        public ReactiveCommand<object> BrowseFileCommand { get; protected set; }

        /// <summary>
        /// Command to update the firwmare.
        /// </summary>
        public ReactiveCommand<object> UpdateFirmwareCommand { get; protected set; }

        /// <summary>
        /// Connect the ADCP Serial Port.
        /// </summary>
        public ReactiveCommand<object> ConnectAdcpCommand { get; protected set; }

        /// <summary>
        /// Ping the ADCP Ethernet Port.
        /// </summary>
        public ReactiveCommand<object> PingAdcpEthernetPortCommand { get; protected set; }

        /// <summary>
        /// Open Adcp Connect Popup.
        /// </summary>
        public ReactiveCommand<object> OpenAdcpConnectPopupCommand { get; protected set; }
        

        #endregion

        /// <summary>
        /// Initialize the view model to update the firmware.
        /// </summary>
        /// <param name="name">Name of the view.</param>
        public FirmwareUpdateViewModel(string name)
        {
            base.DisplayName = name;

            Init();

            // Scan for ADCP command
            ScanAdcpCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            ScanAdcpCommand.Subscribe(_ => ScanForAdcp());

            BrowseFileCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            BrowseFileCommand.Subscribe(_ => BrowseForFile());

            //UpdateFirmwareCommand = ReactiveUI.Legacy.ReactiveCommand.Create(this.WhenAny(_ => _.CanUpdate, x => x.Value));
            UpdateFirmwareCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            UpdateFirmwareCommand.Subscribe(_ => Task.Run(() => UpdateFirmware()));

            ConnectAdcpCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            ConnectAdcpCommand.Subscribe(_ => Task.Run(() => ReconnectAdcp()));

            PingAdcpEthernetPortCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            PingAdcpEthernetPortCommand.Subscribe(_ => Task.Run(() => PingAdcpEthernet()));

            OpenAdcpConnectPopupCommand = ReactiveUI.Legacy.ReactiveCommand.Create();
            OpenAdcpConnectPopupCommand.Subscribe(_ => Task.Run(() => OpenAdcpConnectPopup()));
        }

        /// <summary>
        /// Dispose the viewmodel.
        /// </summary>
        public void Dispose()
        {
            if(_serialPort != null)
            {
                DisconnectAdcpSerial();
            }
        }

        /// <summary>
        /// Initialize the values.
        /// </summary>
        public void Init()
        {
            _serialOptions = new SerialOptions();
            _ethernetOptions = new AdcpEthernetOptions();

            CommPortList = SerialOptions.PortOptions;
            BaudRateList = SerialOptions.BaudRateOptions;
            SelectedBaud = 115200;

            IsLoading = false;
            IsCheckingAdcp = false;

            FirmwareUpdateStatus = "";
            AdcpStatus = "ADCP NOT CONNECTED";
            IsLoading = false;
            IsAdcpFound = false;
            AdcpFirmwareStatus = "ADCP NOT CONNECTED";

            _firmwareInfo = new FirmwareInfo();
            UploadFileSize = 100;           // Init to not 0

            LatestFirmwareVersion = "";
            ChangeLog = "";

            LocalFileTooltip = "Select the firmware zip file";
            InternetFileTooltip = "Get the latest file from the internet.  This will download a zip file and store it to a temporary location.";
            IsInternetSelected = true;

            IsSerialSelected = true;

            // Get the latest firmware version
            GetFirmwareVersion();

            // Check for software update
            CheckForUpdates();
        }


        #region ADCP

        /// <summary>
        /// Scan for any ADCP connected to the computer.
        /// Wait for the good serial port to be found.
        /// </summary>
        /// <returns>List of all good serial ports found.</returns>
        private async Task<List<AdcpSerialPort.AdcpSerialOptions>> ScanForAdcp()
        {
            IsCheckingAdcp = true;

            List<AdcpSerialPort.AdcpSerialOptions> serialConnOptions = new List<AdcpSerialPort.AdcpSerialOptions>();

            // Do this for serial ADCP connection
            if (_IsSerialSelected)
            {
                FirmwareUpdateStatus = "Looking for an ADCP";

                if (_serialPort != null)
                {
                    // Scan all the serial ports
                    await Task.Run(() => serialConnOptions = _serialPort.ScanSerialConnection());

                    // If any good serial ports were found, use the first serial port
                    if (serialConnOptions.Count > 0)
                    {
                        // Set the selected ports
                        _SelectedCommPort = serialConnOptions.First().SerialOptions.Port;
                        _serialOptions.Port = serialConnOptions.First().SerialOptions.Port;
                        _SelectedBaud = serialConnOptions.First().SerialOptions.BaudRate;
                        _serialOptions.BaudRate = serialConnOptions.First().SerialOptions.BaudRate;
                        this.NotifyOfPropertyChange(() => this.SelectedCommPort);
                        this.NotifyOfPropertyChange(() => this.SelectedBaud);

                        // Reconnect the ADCP serial connection
                        ReconnectAdcpSerial(_serialOptions);

                        // Set the status
                        AdcpStatus = "ADCP Connected";
                        AdcpFirmwareStatus = "ADCP Connected";

                        // Set flag
                        IsAdcpFound = true;

                        // Get the ADCP Configuration
                        await Task.Run(() => GetAdcpConfiguration());
                    }
                    else
                    {
                        // Set flag
                        IsAdcpFound = false;
                    }
                }
            }

            IsCheckingAdcp = false;

            return serialConnOptions;
        }

        /// <summary>
        /// Verify the ADCP is connected.
        /// </summary>
        /// <returns>TRUE = ADCP found.</returns>
        private bool VerifyAdcpConnection()
        {
            AdcpFirmwareStatus = "Verifying ADCP Connection";

            try
            {
                if (_IsSerialSelected)
                {
                    if (!string.IsNullOrEmpty(_SelectedCommPort))
                    {
                        return _serialPort.TestSerialPortConnection();
                    }
                }
                else
                {
                    if(!string.IsNullOrEmpty(EtherAddressA) &&
                       !string.IsNullOrEmpty(EtherAddressB) &&
                       !string.IsNullOrEmpty(EtherAddressC) &&
                       !string.IsNullOrEmpty(EtherAddressD))
                    {
                        return _ethernetPort.TestEthernetConnection();
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }

            AdcpFirmwareStatus = "ADCP Not Found";
            AdcpFirmwareVersion = "Verify ADCP connection settings are correct";

            return false;
        }


        /// <summary>
        /// Get the ADCP configuration.
        /// </summary>
        private void GetAdcpConfiguration()
        {
            // Set the status
            FirmwareUpdateStatus = "Getting ADCP Configuration";

            AdcpConfiguration config = null;
            if (_IsSerialSelected)
            {
                // Get the ADCP configuration
                config = _serialPort.GetAdcpConfiguration();
            }
            else
            {
                config = _ethernetPort.GetAdcpConfiguration();
            }

            // Set the ADCP Firmware
            AdcpFirmwareVersion = config.AdcpSerialOptions.Firmware.ToString();

            // Check if the lastest version matches the ADCP
            if (_firmwareInfo.Major > config.AdcpSerialOptions.Firmware.FirmwareMajor)
            {
                AdcpFirmwareStatus = "ADCP Firmware Update Available";
            }
            else if (_firmwareInfo.Major == config.AdcpSerialOptions.Firmware.FirmwareMajor)
            {
                if (_firmwareInfo.Minor > config.AdcpSerialOptions.Firmware.FirmwareMinor)
                {
                    AdcpFirmwareStatus = "ADCP Firmware Update Available";
                }
                else if (_firmwareInfo.Minor == config.AdcpSerialOptions.Firmware.FirmwareMinor)
                {
                    if (_firmwareInfo.Revision > config.AdcpSerialOptions.Firmware.FirmwareRevision)
                    {
                        AdcpFirmwareStatus = "ADCP Firmware Update Available";
                    }
                    else if (_firmwareInfo.Revision <= config.AdcpSerialOptions.Firmware.FirmwareRevision)
                    {
                        AdcpFirmwareStatus = "ADCP Firmware Up To Date";
                    }
                }
            }
            FirmwareUpdateStatus = "Ready...";
        }

        /// <summary>
        /// Use background worker to get the configuration.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TryGetConfiguration(object sender, DoWorkEventArgs e)
        {
            IsLoading = true;

            // If the ADCP is found, get configuration
            if (VerifyAdcpConnection())
            {
                // Set the status
                AdcpStatus = "ADCP Connected";
                AdcpFirmwareStatus = "ADCP Connected";

                // Set flag
                IsAdcpFound = true;

                GetAdcpConfiguration();
            }
            else
            {
                AdcpFirmwareStatus = "ADCP Not Found";
                AdcpFirmwareVersion = "Verify ADCP connection settings are correct";
            }

            IsLoading = false;
        }

        #endregion

        #region Serial Connection

        public async void ReconnectAdcp()
        {
            IsLoading = true;

            // Reconnect the ADCP
            await Task.Run(() => ReconnectAdcpSerial(_serialOptions));
        }

        /// <summary>
        /// Create a connection to the ADCP serial port with
        /// the given options.  If no options are given, return null.
        /// </summary>
        /// <param name="options">Options to connect to the serial port.</param>
        /// <returns>Adcp Serial Port based off the options</returns>
        public AdcpSerialPort ConnectAdcpSerial(SerialOptions options)
        {
            // If there is a connection, disconnect
            if (_serialPort != null)
            {
                DisconnectAdcpSerial();
            }

            if (options != null)
            {
                // Set the connection
                //Status.Status = eAdcpStatus.Connected;

                // Create the connection and connect
                _serialPort = new AdcpSerialPort(options);
                _serialPort.Connect();

                // Force the baud rate to 115200
                _serialPort.SendForceBreak();
                _serialOptions.BaudRate = 115200;

                // Subscribe to receive ADCP data
                _serialPort.ReceiveAdcpSerialDataEvent += new AdcpSerialPort.ReceiveAdcpSerialDataEventHandler(ReceiveAdcpSerialData);
                _serialPort.UploadProgressEvent += new AdcpSerialPort.UploadProgressEventHandler(On_UploadProgressEvent);
                _serialPort.UploadCompleteEvent += new AdcpSerialPort.UploadCompleteEventHandler(On_UploadCompleteEvent);
                _serialPort.UploadFileSizeEvent += new AdcpSerialPort.UploadFileSizeEventHandler(On_UploadFileSizeEvent);

                // Publish that the ADCP serial port is new
                //PublishAdcpSerialConnection();

                Debug.WriteLine(string.Format("ADCP Connect: {0}", _serialPort.ToString()));

                // Try to get the configuration
                BackgroundWorker bg = new BackgroundWorker();
                bg.DoWork += TryGetConfiguration;
                bg.RunWorkerAsync();

                return _serialPort;
            }

            return null;
        }

        /// <summary>
        /// Shutdown the ADCP serial port.
        /// This will stop all the read threads
        /// for the ADCP serial port.
        /// </summary>
        public void DisconnectAdcpSerial()
        {
            try
            {
                if (_serialPort != null)
                {
                    Debug.WriteLine(string.Format("ADCP Disconnect: {0}", _serialPort.ToString()));

                    // Disconnect the serial port
                    _serialPort.Disconnect();

                    // Unscribe to ADCP SerialPort events
                    _serialPort.ReceiveAdcpSerialDataEvent -= ReceiveAdcpSerialData;
                    _serialPort.UploadProgressEvent -= On_UploadProgressEvent;
                    _serialPort.UploadCompleteEvent -= On_UploadCompleteEvent;
                    _serialPort.UploadFileSizeEvent -= On_UploadFileSizeEvent;

                    // Publish that the ADCP serial conneciton is disconnected
                    //PublishAdcpSerialDisconnection();

                    // Shutdown the serial port
                    _serialPort.Dispose();
                }
                //Status.Status = eAdcpStatus.NotConnected;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error disconnecting the serial port.", e);
            }
        }

        /// <summary>
        /// Disconnect then connect with the new options given.
        /// </summary>
        /// <param name="options">Options to connect the ADCP serial port.</param>
        public void ReconnectAdcpSerial(SerialOptions options)
        {
            // Disconnect
            DisconnectAdcpSerial();

            // Wait for Disconnect to finish
            Thread.Sleep(RTI.AdcpSerialPort.WAIT_STATE);

            // Connect
            ConnectAdcpSerial(options);
        }

        /// <summary>
        /// Return if the Adcp Serial port is open and connected.
        /// </summary>
        /// <returns>TRUE = Is connected.</returns>
        public bool IsAdcpSerialConnected()
        {
            // See if the connection is open
            if (_serialPort != null && _serialPort.IsOpen())
            {
                return true;
            }

            return false;
        }


        #endregion

        #region Upload File

        /// <summary>
        /// Populate the list of available files to download.
        /// </summary>
        private void BrowseForFile()
        {
            try
            {
                // Get the file
                System.Windows.Forms.OpenFileDialog openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
                openFileDialog1.InitialDirectory = "";
                openFileDialog1.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*|bin files (*.bin)|*.bin";//"txt files (*.txt)|*.txt|All files (*.*)|*.*";
                openFileDialog1.FilterIndex = 2;
                openFileDialog1.Multiselect = false;

                UploadFileSize = 0;
                UploadFileProgress = 0;

                if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    // Set the local file
                    LocalFilePath = openFileDialog1.FileName;

                    // Set the flag to use the local file
                    IsLocalFileSelected = true;
                }
            }
            catch (AccessViolationException)
            {
                //log.Error("Error trying to open firmware file", ae);
            }
            catch (Exception)
            {
                //log.Error("Error trying to open firmware file", e);
            }
        }

        #endregion

        #region Download Data

        /// <summary>
        /// Download the firmware zip file.
        /// </summary>
        /// <param name="downloadURL">URL to download the data.</param>
        /// <param name="storagePath">Path to store the downloaded data.</param>
        private void DownloadData(string downloadURL, string storagePath)
        {
            // Reset progressbar
            DownloadProgress = 0;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    wc.DownloadProgressChanged += wc_DownloadProgressChanged;
                    wc.DownloadFile(new System.Uri(downloadURL), storagePath);
                }
                catch(Exception)
                {
                    // No Internet
                }
            }
        }

        /// <summary>
        /// Show the progress of downloading the data.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void wc_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress = e.ProgressPercentage;
        }

        #endregion

        #region Firmware Download

        /// <summary>
        /// Get the latest firmware version.
        /// </summary>
        private async void GetFirmwareVersion()
        {
            try
            {
                string filePath = Path.GetTempPath() + @"\LatestFirmwareVersion.json";

                // Remove the file if it currently exist
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Download the firmware version JSON file
                await Task.Run(() => DownloadData(DOWNLOAD_FIRMWARE_URL, filePath));

                // Check if the file could be downloaded
                if (File.Exists(filePath))
                {
                    // Read the firmware info
                    using (StreamReader r = new StreamReader(filePath))
                    {
                        string json = r.ReadToEnd();
                        _firmwareInfo = JsonConvert.DeserializeObject<FirmwareInfo>(json);
                    }

                    // Set the latest firmware version
                    LatestFirmwareVersion = _firmwareInfo.LatestVersion;
                    InternetFile = _firmwareInfo.URL;

                    // Download the changelog
                    filePath = Path.GetTempPath() + @"\firmware_changelog.txt";
                    await Task.Run(() => DownloadData(_firmwareInfo.ChangeLog, filePath));

                    // Set changelog
                    ChangeLog = File.ReadAllText(filePath);
                }
                else
                {
                    // No internet, so select Local file
                    IsInternetSelected = false;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error downloading the latest version JSON", e);
            }
        }

        #endregion

        #region Update Firwmare

        /// <summary>
        /// Update the firmware based off selected option.
        /// </summary>
        private async Task<bool> UpdateFirmware()
        {
            IsLoading = true;

            // Verify an ADCP is connected and the correct baud rate
            bool adcpVerify = false;
            await Task.Run(() => adcpVerify = VerifyAdcpConnection());
            if(!adcpVerify)
            {
                // Can for the ADCP
                await Task.Run(() => ScanForAdcp());

                // Verify if ADCP is now connected and if not
                // Give a warning and stop
                await Task.Run(() => adcpVerify = VerifyAdcpConnection());
                if(!adcpVerify)
                {
                    MessageBox.Show("Error talking to the ADCP.  Check your baud rate and comm port.", "No ADCP Connected", MessageBoxButtons.OK, MessageBoxIcon.Error);

                    IsLoading = false;
                    return false;
                }
            }

            // Use the local file
            if(IsLocalFileSelected)
            {
                if (string.IsNullOrEmpty(_LocalFilePath))
                {
                    MessageBox.Show("Firmware zip file not selected.", "No Firmware Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    FirmwareUpdateStatus = "Error: Select a firmware file.";
                }
                else
                {
                    // Update the firmware from a local file
                    await Task.Run(() => UpdateFirmwareLocal());

                    // Get the ADCP Configuration
                    await Task.Run(() => GetAdcpConfiguration());
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_InternetFile))
                {
                    MessageBox.Show("Firmware zip file could not be downloaded.", "No Firmware Selected", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    FirmwareUpdateStatus = "Error: Select a firmware file.";
                }
                else
                {

                    // Use the downloaded zip file
                    await Task.Run(() => UpdateFirmwareRemote());

                    // Get the ADCP Configuration
                    await Task.Run(() => GetAdcpConfiguration());
                }
            }

            FirmwareUpdateStatus = "Firmware Update Complete";
            AdcpFirmwareStatus = "Firmware Update Complete";

            IsLoading = false;

            return true;
        }

        /// <summary>
        /// Update the ADCP firmware with a local zip file.
        /// </summary>
        private void UpdateFirmwareLocal()
        {
            // Download the file from the internet
            try
            {
                // File name
                string fileName = System.IO.Path.GetFileName(LocalFilePath);

                if (!string.IsNullOrEmpty(fileName))
                {
                    // File path to save the file
                    string zipPath = LocalFilePath;

                    // Set the status
                    AdcpFirmwareStatus = "Uploading Firmware";

                    // Upload the data to the ADCP
                    UploadToAdcp(zipPath, fileName);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error uploading the files to the ADCP: " + e.Message);
            }
        }

        /// <summary>
        /// Download the firmware from internet.  Then unzip the file and upload it
        /// to the internet.
        /// </summary>
        private void UpdateFirmwareRemote()
        {
            // Download the file from the internet
            try
            {
                // File URL
                Uri uri = new Uri(_firmwareInfo.URL);
                string fileName = System.IO.Path.GetFileName(uri.LocalPath);

                if (!string.IsNullOrEmpty(fileName))
                {
                    // File path to save the file
                    string zipPath = Path.GetTempPath() + fileName;

                    // Set the status
                    FirmwareUpdateStatus = "Downloading Firmware";

                    // Download the firmware version JSON file
                    DownloadData(_firmwareInfo.URL, zipPath);

                    // Set the status
                    AdcpFirmwareStatus = "Uploading Firmware";

                    // Upload the downloaded data to the ADCP
                    UploadToAdcp(zipPath, fileName);
                }
            }
            catch(Exception e)
            {
                System.Windows.MessageBox.Show("Error downloading latest firmware." + e.Message, "Error Downloading Firmware", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Debug.WriteLine("Error uploading the files to the ADCP: " + e.Message);
            }
        }

        #endregion

        #region Upload To ADCP

        /// <summary>
        /// Extract the zip file to a temporary folder.  Then upload the
        /// files listed in the firmware update to the ADCP.
        /// </summary>
        /// <param name="zipPath">Path to the zip file containing the latest firmware.</param>
        /// <param name="fileName">File name of the firmware.  Used to create a temp folder.</param>
        private void UploadToAdcp(string zipPath, string fileName)
        {
            try
            {
                // Set the extract path
                string extractPath = Path.GetTempPath() + @"\rti_firmware";

                // Delete the previous directory
                if (Directory.Exists(extractPath))
                {
                    //var dir = new DirectoryInfo(extractPath);
                    //dir.Delete(true);
                    Directory.Delete(extractPath, true);
                }

                try
                {
                    // Set Status
                    FirmwareUpdateStatus = "Extracting Firmware";

                    // Unzip the file
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                }
                catch(Exception e)
                {
                    System.Windows.MessageBox.Show("Error unzipping file to " + extractPath + ". " + e.Message, "Error Unzipping firmware folder", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                // Get the folder path of the firmware in the temp folder
                FileInfo fi = new FileInfo(fileName);
                string firmwareFolder = Path.GetFileNameWithoutExtension(fi.Name);

                // Create a list to update
                List<string> fileList = new List<string>();
                foreach (var file in _firmwareInfo.Files)
                {
                    if (File.Exists(extractPath + @"\" + firmwareFolder + @"\" + file))
                    {
                        fileList.Add(extractPath + @"\" + firmwareFolder + @"\" + file);
                    }
                    else if(File.Exists(extractPath + @"\" + file))
                    {
                        fileList.Add(extractPath + @"\" + file);
                    }
                }

                // Set Status
                FirmwareUpdateStatus = "Uploading Firmware to ADCP";

                // Serial Port Update Firmware
                if (_IsSerialSelected)
                {
                    // Update the firmware with the file list
                    _serialPort.UpdateFirmware(fileList.ToArray());
                }
                else
                {
                    // Ethernet Port Update Firmware
                    _ethernetPort.UpdateFirmware(fileList.ToArray());
                }

                FirmwareUpdateStatus = "Firmware Update Complete";
                AdcpFirmwareStatus = "Firmware Update Complete";

                // Check the firmware
            }
            catch(Exception e)
            {
                Debug.WriteLine("Error uploading the files to the ADCP: " + e.Message);
                System.Windows.MessageBox.Show("Error uploading to the ADCP. " + e.Message, "Error Uploading to the ADCP", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        #endregion

        #region ADCP Ethernet Port

        private void PingAdcpEthernet()
        {
            if (_ethernetPort == null ||
                _ethernetPort.Options.IpAddrA != _ethernetOptions.IpAddrA ||
                _ethernetPort.Options.IpAddrB != _ethernetOptions.IpAddrB ||
                _ethernetPort.Options.IpAddrC != _ethernetOptions.IpAddrC ||
                _ethernetPort.Options.IpAddrD != _ethernetOptions.IpAddrD)
            {
                _ethernetPort = new AdcpEthernet(_ethernetOptions);
                _ethernetPort.ReceiveEthernetDataEvent += _ethernetPort_ReceiveEthernetDataEvent;
                _ethernetPort.UploadProgressEvent += new AdcpEthernet.UploadProgressEventHandler(On_UploadProgressEvent);
                _ethernetPort.UploadCompleteEvent += new AdcpEthernet.UploadCompleteEventHandler(On_UploadCompleteEvent);
                _ethernetPort.UploadFileSizeEvent += new AdcpEthernet.UploadFileSizeEventHandler(On_UploadFileSizeEvent);
            }

            // Try to get the configuration
            BackgroundWorker bg = new BackgroundWorker();
            bg.DoWork += TryGetConfiguration;
            bg.RunWorkerAsync();
        }

        /// <summary>
        /// Pass the ADCP data from the port to the serial output.
        /// </summary>
        /// <param name="data"></param>
        private void _ethernetPort_ReceiveEthernetDataEvent(string data)
        {
            SerialOutput += data;
        }

        #endregion

        #region Popups

        /// <summary>
        /// Adcp Connect Popup is Opened.
        /// </summary>
        public void OpenAdcpConnectPopup()
        {
            // Reset the list with the latest ports available
            CommPortList = SerialOptions.PortOptions;
        }

        #endregion

        #region Event Handler

        #region Serial Port

        /// <summary>
        /// Receive binary data from the ADCP serial port.
        /// Then pass the binary data to the codec to decode the
        /// data into ensembles.
        /// 
        /// The data could be binary or dvl data.
        /// The data will go to both codec and
        /// if the codec can process the data it will.
        /// </summary>
        /// <param name="data">Data to decode.</param>
        public void ReceiveAdcpSerialData(byte[] data)
        {
            SerialOutput += System.Text.Encoding.Default.GetString(data);
        }



        #endregion

        #region Upload Progress

        /// <summary>
        /// Event handler when a file has completed being
        /// downloaded.
        /// </summary>
        /// <param name="fileName">File name of the completed download.</param>
        /// <param name="goodDownload">Flag set to determine if the download was good or bad.</param>
        private void On_UploadCompleteEvent(string fileName, bool goodDownload)
        {
            //if (UploadCompleteEvent != null)
            //{
            //    UploadCompleteEvent(fileName, goodDownload);
            //}

            FirmwareUpdateStatus = "Upload Complete: " + fileName;
        }

        /// <summary>
        /// Set the file size for the file uploading.
        /// </summary>
        /// <param name="fileName">File Name.</param>
        /// <param name="fileSize">Size of the file in bytes.</param>
        private void On_UploadFileSizeEvent(string fileName, long fileSize)
        {
            //if (UploadFileSizeEvent != null)
            //{
            //    UploadFileSizeEvent(fileName, fileSize);
            //}

            UploadFileSize = fileSize;
            FirmwareUpdateStatus = "Uploading " + fileName;
        }

        /// <summary>
        /// Progress of the uploading file.  This will give the number
        /// of bytes currently written to the file.
        /// </summary>
        /// <param name="fileName">File name of file in progress.</param>
        /// <param name="bytesWritten">Number of bytes written to file.</param>
        private void On_UploadProgressEvent(string fileName, long bytesWritten)
        {
            //if (UploadProgressEvent != null)
            //{
            //    UploadProgressEvent(fileName, bytesWritten);
            //}

            UploadFileProgress = bytesWritten;
        }

        #endregion


        #endregion

        #region AutoUpdater

        /// <summary>
        /// Check for update to the software.
        /// </summary>
        private void CheckForUpdates()
        {
            AutoUpdater.Start(SOFTWARE_UPDATE_URL);
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
        }

        /// <summary>
        /// Event Hnadler for Updater.
        /// </summary>
        /// <param name="args"></param>
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null)
            {
                // Current version of the software
                CurrentSoftwareVersion = args.InstalledVersion.ToString();

                if (args.IsUpdateAvailable)
                {
                    // Set the new software warning
                    NewSoftwareVersion = "There is new version " + args.CurrentVersion.ToString() + " available";

                    DialogResult dialogResult;
                    if (args.Mandatory)
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {args.InstalledVersion}. This is required update. Press Ok to begin updating the application.", @"Update Available",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                    }
                    else
                    {
                        dialogResult =
                            MessageBox.Show(
                                $@"There is new version {args.CurrentVersion} available. You are using version {
                                        args.InstalledVersion
                                    }. Do you want to update the application now?", @"Update Available",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Information);
                    }

                    // Uncomment the following line if you want to show standard update dialog instead.
                    // AutoUpdater.ShowUpdateForm();

                    if (dialogResult.Equals(DialogResult.Yes) || dialogResult.Equals(DialogResult.OK))
                    {
                        try
                        {
                            if (AutoUpdater.DownloadUpdate())
                            {
                                Application.Exit();
                            }
                        }
                        catch (Exception exception)
                        {
                            MessageBox.Show(exception.Message, exception.GetType().ToString(), MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
                        }
                    }
                }
                else
                {
                    //MessageBox.Show(@"There is no update available please try again later.", @"No update available",
                    //    MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                //MessageBox.Show(
                //        @"There is a problem reaching update server please check your internet connection and try again later.",
                //        @"Update check failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

    }

    #endregion
}
