//-----------------------------------------------------------------------
// <copyright file="ControlSystem.cs" company="Crestron">
//     Copyright (c) Crestron Electronics. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crestron.SimplSharp;                       // For Basic SIMPL# Classes
using Crestron.SimplSharp.CrestronIO;            // For Directory
using Crestron.SimplSharpPro;                    // For Basic SIMPL#Pro classes
using Crestron.SimplSharpPro.CrestronThread;     // For Threading
using Crestron.SimplSharpPro.DeviceSupport;      // For Generic Device Support
using Crestron.SimplSharpPro.Diagnostics;        // For System Monitor Access
using Crestron.SimplSharpPro.UI;                 // For xPanelForSmartGraphics

namespace Lab2.CWS
{
    /* Instructors notes
     * 
     */

    /// <summary>
    /// ControlSystem class that inherits from CrestronControlSystem
    /// </summary>
    public class ControlSystem : CrestronControlSystem
    {
        /// <summary>
        /// Used for logging information to error log
        /// </summary>
        private const string LogHeader = "[Device] ";

        /// <summary>
        /// Touchpanel used throughout this exercise
        /// Could also be a Tsw or any other SmartGraphics enabled touchpanel
        /// </summary>
        private XpanelForSmartGraphics tp01;

        /// <summary>
        /// Second touchpanel used throughout this exercise
        /// Could also be a Tsw or any other SmartGraphics enabled touchpanel
        /// </summary>
        private XpanelForSmartGraphics tp02;

        /// <summary>
        /// The CWS controller used for Lab 2
        /// </summary>
        private CWS.Controller controller;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlSystem" /> class.
        /// Use the constructor to:
        /// * Initialize the maximum number of threads (max = 400)
        /// * Register devices
        /// * Register event handlers
        /// * Add Console Commands
        /// Please be aware that the constructor needs to exit quickly; if it doesn't
        /// exit in time, the SIMPL#Pro program will exit.
        /// You cannot send / receive data in the constructor
        /// </summary>
        public ControlSystem()
            : base()
        {
            try
            {
                Thread.MaxNumberOfUserThreads = 20;

                // Subscribe to the controller events (System, Program, and Ethernet)
                CrestronEnvironment.SystemEventHandler += new SystemEventHandler(this.ControlSystem_ControllerSystemEventHandler);
                CrestronEnvironment.ProgramStatusEventHandler += new ProgramStatusEventHandler(this.ControlSystem_ControllerProgramEventHandler);
                CrestronEnvironment.EthernetEventHandler += new EthernetEventHandler(this.ControlSystem_ControllerEthernetEventHandler);

                if (this.SupportsEthernet)
                {
                    this.tp01 = new XpanelForSmartGraphics(0x03, this);

                    // YS: We will leave this in so they can understand what is happening
                    this.tp01.SigChange += new SigEventHandler(this.Xpanel_SigChange);

                    // YS: We will comment this out so they can create this eventhandler with all the logic on their own
                    this.tp01.OnlineStatusChange += this.Xpanel_OnlineStatusChange;

                    string sgdPath = string.Format($"{Directory.GetApplicationDirectory()}/XPanel_Masters2020.sgd");

                    if (this.tp01.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
                    {
                        ErrorLog.Error(string.Format(
                            $"{LogHeader} Error registering XPanel: {this.tp01.RegistrationFailureReason}"));
                    }
                    else
                    {
                        this.tp01.LoadSmartObjects(sgdPath);
                        ErrorLog.Error(string.Format($"{LogHeader} Loaded SmartObjects: {this.tp01.SmartObjects.Count}"));
                        foreach (KeyValuePair<uint, SmartObject> smartObject in this.tp01.SmartObjects)
                        {
                            smartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(this.Xpanel_SO_SigChange);
                        }
                    }
                }

                this.controller = new CWS.Controller(this.tp01, "");
            }
            catch (Exception e)
            {
                ErrorLog.Error(string.Format(LogHeader + "Error in the constructor: {0}", e.Message));
            }
        }

        /// <summary>
        /// Eventhandler for boolean/ushort/string sigs
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains the SigType, Sig.Number and Sig.Value and more</param>
        public void Xpanel_SigChange(BasicTriList currentDevice, SigEventArgs args)
        {
            switch (args.Sig.Type)
            {
                case eSigType.Bool:
                    // ErrorLog.Notice(string.Format(LogHeader + "Boolean Received from Touch Panel: {0}, {1}", args.Sig.Number, args.Sig.BoolValue));
                    switch (args.Sig.Number)
                    {
                        // YS: Level 1, exercise 2
                        // Hello world button
                        case 12:
                            if (args.Sig.BoolValue == true)
                            {
                                currentDevice.StringInput[11].StringValue = "Hello World!";
                            }
                            else
                            {
                                currentDevice.StringInput[11].StringValue = string.Empty;
                            }

                            break;

                        // YS: Level 2, exercise 1
                        // toggle button
                        case 21:
                            if (args.Sig.BoolValue == true)
                            {
                                // toggle it, easy way
                                currentDevice.BooleanInput[21].BoolValue = !currentDevice.BooleanInput[21].BoolValue;
                                if (currentDevice.BooleanInput[21].BoolValue == true)
                                {
                                    currentDevice.StringInput[21].StringValue = "Hello World!";
                                }
                                else
                                {
                                    currentDevice.StringInput[21].StringValue = string.Empty;
                                }
                            }

                            break;

                        // YS: Level 2, exercise 2
                        // interlock
                        case 22:
                        case 23:
                        case 24:
                            if (args.Sig.BoolValue == true)
                            {
                                // Loop through the possible interlocked buttons
                                for (ushort i = 22; i <= 24; i++)
                                {
                                    currentDevice.BooleanInput[i].BoolValue = false;
                                }

                                // Set only the pressed button feedback to high
                                currentDevice.BooleanInput[args.Sig.Number].BoolValue = true;

                                // Set the correct text
                                if (currentDevice.BooleanInput[22].BoolValue == true)
                                {
                                    currentDevice.StringInput[21].StringValue = "Hello World!";
                                }
                                else if (currentDevice.BooleanInput[23].BoolValue == true)
                                {
                                    currentDevice.StringInput[21].StringValue = "Hallo Wereld!";
                                }
                                else if (currentDevice.BooleanInput[24].BoolValue == true)
                                {
                                    currentDevice.StringInput[21].StringValue = "Hola Mundo!";
                                }
                            }

                            break;
                        case 25:
                            if (args.Sig.BoolValue == true)
                            {
                                // Loop through the possible interlocked buttons
                                for (ushort i = 22; i <= 24; i++)
                                {
                                    currentDevice.BooleanInput[i].BoolValue = false;
                                }

                                // Clear text field
                                currentDevice.StringInput[21].StringValue = string.Empty;
                            }

                            break;

                        // YS: Level 3, exercise 2
                        // Register new touchpanel
                        case 31:
                            if (args.Sig.BoolValue == true)
                            {
                                // let's first flip the switch
                                currentDevice.BooleanInput[31].BoolValue = !currentDevice.BooleanInput[31].BoolValue;

                                // then use the value of the button to either register or unregister the touchpanel
                                this.RegisterUnregisterXpanel(currentDevice.BooleanInput[31].BoolValue, currentDevice.ID + 1);
                            }

                            break;
                    }

                    break;
                case eSigType.UShort:
                    // ErrorLog.Error(string.Format(LogHeader + "Ushort Received from Touch Panel: {0}, {1}", args.Sig.Number, args.Sig.UShortValue));
                    // YS: Level 3, exercise 1
                    if (args.Sig.Number == 31)
                    {
                        ushort percentage = Convert.ToUInt16(args.Sig.UShortValue * 100 / 65535);

                        // send it right back to analog join 32 after converting 0->65535 to 0->100
                        currentDevice.UShortInput[32].UShortValue = percentage;

                        currentDevice.UShortInput[31].UShortValue = args.Sig.UShortValue;

                        if (percentage == 0)
                        {
                            currentDevice.UShortInput[33].UShortValue = 0;
                        }
                        else if (percentage > 0 && percentage <= 33)
                        {
                            currentDevice.UShortInput[33].UShortValue = 1;
                        }
                        else if (percentage > 33 && percentage <= 66)
                        {
                            currentDevice.UShortInput[33].UShortValue = 2;
                        }
                        else if (percentage > 66 && percentage <= 100)
                        {
                            currentDevice.UShortInput[33].UShortValue = 3;
                        }
                    }

                    break;
                case eSigType.String:
                    // ErrorLog.Notice(string.Format(LogHeader + "String Received from Touch Panel: {0}, {1}", args.Sig.Number, args.Sig.StringValue));
                    break;
            }
        }

        /// <summary>
        /// Online/Ofline event handler for Xpanel
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains DeviceOnline for status feedback</param>
        public void Xpanel_OnlineStatusChange(GenericBase currentDevice, OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                // if it was tp01 that triggered the event
                if (currentDevice == this.tp01)
                {
                    // ErrorLog.Notice(string.Format(LogHeader + "{0} is online", tp01.Type));
                    this.tp01.BooleanInput[11].BoolValue = args.DeviceOnLine;
                }
            }
            else
            {
                // ErrorLog.Notice(string.Format(LogHeader + "{0} is offline", currentDevice.Description));
            }
        }

        /// <summary>
        /// Specific event handler for Smart Objects (not used in this exercise)
        /// </summary>
        /// <param name="currentDevice">The device that triggered the event</param>
        /// <param name="args">Contains args.Sig.Type, args.Sig.Name, args.SmartObjectArgs.ID and more</param>
        public void Xpanel_SO_SigChange(GenericBase currentDevice, SmartObjectEventArgs args)
        {
            // ErrorLog.Notice(string.Format(LogHeader + "Event Type: {0}, Signal: {1}, from SmartObject: {2}", args.Sig.Type, args.Sig.Name, args.SmartObjectArgs.ID));
        }

        /// <summary>
        /// InitializeSystem - this method gets called after the constructor 
        /// has finished. 
        /// Use InitializeSystem to:
        /// * Start threads
        /// * Configure ports, such as serial and verisports
        /// * Start and initialize socket connections
        /// Send initial device configurations
        /// Please be aware that InitializeSystem needs to exit quickly also; 
        /// if it doesn't exit in time, the SIMPL#Pro program will exit.
        /// </summary>
        public override void InitializeSystem()
        {
        }

        /// <summary>
        /// Method to easily register and register the additional touchpanel for Lab 1, Level 3, exercise 2
        /// </summary>
        /// <param name="registration">True for registration, false for unregistration</param>
        /// <param name="id">IPID we want to register this touchpanel on</param>
        public void RegisterUnregisterXpanel(bool registration, uint id)
        {
            this.tp02 = new XpanelForSmartGraphics(id, this);

            // YS: We will leave this in so they can understand what is happening
            this.tp02.SigChange += new SigEventHandler(this.Xpanel_SigChange);

            // YS: We will comment this out so they can create this eventhandler with all the logic on their own
            this.tp02.OnlineStatusChange += this.Xpanel_OnlineStatusChange;

            string sgdPath = string.Format(@"{0}/XPanel_Masters2020.sgd", Directory.GetApplicationDirectory());

            if (this.tp02.Register() != eDeviceRegistrationUnRegistrationResponse.Success)
            {
                ErrorLog.Error(string.Format(LogHeader + "Error registering XPanel: {0}", this.tp02.RegistrationFailureReason));
            }
            else
            {
                this.tp02.LoadSmartObjects(sgdPath);
                ErrorLog.Notice(string.Format(LogHeader + "Loaded SmartObjects: {0}", this.tp02.SmartObjects.Count));
                foreach (KeyValuePair<uint, SmartObject> smartObject in this.tp02.SmartObjects)
                {
                    smartObject.Value.SigChange += new SmartObjectSigChangeEventHandler(this.Xpanel_SO_SigChange);
                }
            }
        }

        /// <summary>
        /// Event Handler for Ethernet events: Link Up and Link Down. 
        /// Use these events to close / re-open sockets, etc. 
        /// </summary>
        /// <param name="ethernetEventArgs">This parameter holds the values 
        /// such as whether it's a Link Up or Link Down event. It will also indicate 
        /// wich Ethernet adapter this event belongs to.
        /// </param>
        public void ControlSystem_ControllerEthernetEventHandler(EthernetEventArgs ethernetEventArgs)
        {
            switch (ethernetEventArgs.EthernetEventType)
            {
                // Determine the event type Link Up or Link Down
                case eEthernetEventType.LinkDown:
                    // Next need to determine which adapter the event is for. 
                    // LAN is the adapter is the port connected to external networks.
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                    }

                    break;
                case eEthernetEventType.LinkUp:
                    if (ethernetEventArgs.EthernetAdapter == EthernetAdapterType.EthernetLANAdapter)
                    {
                    }

                    break;
            }
        }

        /// <summary>
        /// Event Handler for Programmatic events: Stop, Pause, Resume.
        /// Use this event to clean up when a program is stopping, pausing, and resuming.
        /// This event only applies to this SIMPL#Pro program, it doesn't receive events
        /// for other programs stopping
        /// </summary>
        /// <param name="programStatusEventType">Stop, resume or pause</param>
        public void ControlSystem_ControllerProgramEventHandler(eProgramStatusEventType programStatusEventType)
        {
            switch (programStatusEventType)
            {
                case eProgramStatusEventType.Paused:
                    // ErrorLog.Notice(string.Format("Program Paused"));
                    break;
                case eProgramStatusEventType.Resumed:
                    // ErrorLog.Notice(string.Format("Program Resumed"));
                    break;
                case eProgramStatusEventType.Stopping:
                    // ErrorLog.Notice(string.Format("Program Stopping"));
                    break;
            }
        }

        /// <summary>
        /// Event Handler for system events, Disk Inserted/Ejected, and Reboot
        /// Use this event to clean up when someone types in reboot, or when your SD /USB
        /// removable media is ejected / re-inserted.
        /// </summary>
        /// <param name="systemEventType">Inserted, Removed, Rebooting</param>
        public void ControlSystem_ControllerSystemEventHandler(eSystemEventType systemEventType)
        {
            switch (systemEventType)
            {
                case eSystemEventType.DiskInserted:
                    // Removable media was detected on the system
                    break;
                case eSystemEventType.DiskRemoved:
                    // Removable media was detached from the system
                    break;
                case eSystemEventType.Rebooting:
                    // The system is rebooting. 
                    // Very limited time to preform clean up and save any settings to disk.
                    break;
            }
        }
    }
}
