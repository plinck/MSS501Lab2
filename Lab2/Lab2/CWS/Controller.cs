//-----------------------------------------------------------------------
// <copyright file="Controller.cs" company="Crestron">
//     Copyright (c) Crestron Electronics. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.WebScripting;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Lab2.CWS
{
    /* Instructors notes
     */

    /// <summary>
    /// CWS controller class that handles all CWS communication back and forth
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// Used for logging information to error log
        /// </summary>
        private const string LogHeader = "[PAULLINCK2 CWS] ";

        /// <summary>
        /// Optional CWS path to be added
        /// Base path for this exercise is http://[ipaddres]/VirtualControl/Rooms/[roomname]/cws/
        /// Where [ipaddress] is the ip address or hostname of the VC-4 server
        /// And [roomname] is the name used when creating a room instance of this program
        /// </summary>
        //private string cwsPath = "http://ec2-18-188-67-188.us-east-2.compute.amazonaws.com/VirtualControl/Rooms/PAULLINCK2/cws/";
        private string cwsPath;

        /// <summary>
        /// Locking object for CWS Server
        /// </summary>
        private CCriticalSection cwsServerLock = new CCriticalSection();

        /// <summary>
        /// The HTTP CWS server
        /// </summary>
        private HttpCwsServer cwsServer;

        /// <summary>
        /// XpanelForSmartGraphics object used to send feedback to user
        /// </summary>
        private XpanelForSmartGraphics tp;
        
        // Object type for the request
        public class Request
        {
            public string text { get; set; }
        }
        public class SliderRequest
        {
            public ushort value { get; set; }
        }

        public class ButtonStatus
        {
            public Boolean button { get; set; }
            public ButtonStatus(Boolean state)
            {
                this.button = state;
            }
        }
        public class InterlockResponse
        {
            public List<ButtonStatus> status { get; set; }
            
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Controller" /> class.
        /// </summary>
        /// <param name="tp">The XpanelForSmartGraphics object</param>
        /// <param name="cwsPath">Additional CWS path. May be empty</param>
        public Controller(XpanelForSmartGraphics tp, string cwsPath)
        {
            ErrorLog.Notice($"{LogHeader} Running CWS COntroller Constructor");
            
            this.cwsPath = cwsPath;

            this.StartServer();

            this.tp = tp;
        }

        /// <summary>
        /// Start the CWS server with the previously set path
        /// </summary>
        public void StartServer()
        {
            ErrorLog.Notice($"{LogHeader} Starting CWS API Server");

            try
            {
                this.cwsServerLock.Enter();
                if (this.cwsServer == null)
                {
                    ErrorLog.Notice($"{LogHeader} Starting CWS API Server");
                    this.cwsServer = new HttpCwsServer(this.cwsPath);

                    this.cwsServer.ReceivedRequestEvent += new HttpCwsRequestEventHandler(this.ReceivedRequestEvent);

                    // Example GET route
                    // There are a couple of things to keep in mind:
                    // {data} defines a variable that can be used later in your code
                    // DUMMY is a shorter name that can be used to "find" your route later on
                    
                    //this.cwsServer.Routes.Add(new HttpCwsRoute("dummygetroute/{data}") { Name = "DUMMYGET" });

                    // TODO: Level1. Add your own HTTP CWS Route called "helloworld" 
                    this.cwsServer.Routes.Add(new HttpCwsRoute("helloworld/{data}") {Name = "HELLOWORLD"});

                    // Example POST route
                    // Not necessary for Level1, this was added as a reference
                    this.cwsServer.Routes.Add(new HttpCwsRoute("holamundo") { Name = "holamundo" });
                    
                    //Level2
                    this.cwsServer.Routes.Add(new HttpCwsRoute("interlockstatus") {Name = "interlockstatus"});
                    
                    // Level 3
                    this.cwsServer.Routes.Add(new HttpCwsRoute("getslider") {Name = "getslider"});
                    this.cwsServer.Routes.Add(new HttpCwsRoute("postslider") { Name = "postslider" });
                    this.cwsServer.Routes.Add(new HttpCwsRoute("log") {Name = "log"});
 
                    // register the server
                    this.cwsServer.Register();
                    ErrorLog.Notice($"{LogHeader} Started CWS API Server");
                }
                else
                {
                    throw new InvalidOperationException("CWS API Server is already running");
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error(LogHeader + "Exception in StartServer(): {0}", e.Message);
            }
            finally
            {
                this.cwsServerLock.Leave();
            }
        }

        /// <summary>
        /// The received request handler for the CWS server
        /// </summary>
        /// <param name="sender">optional sender object</param>
        /// <param name="args">The HttpCwsRequestEventArgs arguments containing information about this request like the HTTP method</param>
        public void ReceivedRequestEvent(object sender, HttpCwsRequestEventArgs args)
        {
            ErrorLog.Notice($"{LogHeader} ReceivedRequestEvent running ...");
            
            try
            {
                if (args.Context.Request.RouteData == null)
                {
                    args.Context.Response.StatusCode = 200;
                    args.Context.Response.ContentType = "text/html";
                    switch (args.Context.Request.Path.ToUpper())
                    {
                        // not used, for demo/temp purposes
                        case "/WHATEVER":
                            break;
                        default:
                            args.Context.Response.StatusCode = 200;
                            args.Context.Response.Write(
                                JsonConvert.SerializeObject(
                                new Response
                                {
                                    Status = "Error",
                                    Message = this.GetApiHelp()
                                }, 
                                Formatting.Indented), 
                                true);
                            break;
                    }
                }
                else
                {
                    args.Context.Response.StatusCode = 200;
                    args.Context.Response.ContentType = "application/json";

                    // When we get a "GET" request
                    if (args.Context.Request.HttpMethod == "GET")
                    {
                        switch (args.Context.Request.RouteData.Route.Name.ToUpper())
                        {
                            // TODO: Level1. Not really a TODO, but we wanted to show you were you use the short name
                            // This was defined on line 90 of this class
                            case "HELLOWORLD":
                                // Get the data from the GET request by making use of the "data" variable
                                // that was defined on line 90 of this class
                                
                                // TODO: Level1. Create your own code to handle the "helloworld" CWS route
                                string data = args.Context.Request.RouteData.Values["data"].ToString();
                                    
                                // TODO: Level1. From that code, send the received data to serial join 11
                                this.tp.StringInput[11].StringValue = data;

                                // TODO: Level1. Implement WriteWithAppend() in the FileControl.cs file to write the received data to User/logfile.txt
                                string appDir = Directory.GetApplicationRootDirectory(); 
                                FileControl.WriteWithAppend(data, $"{appDir}/User/logfile.txt");

                                // TODO: Level1. Return the received text as a response to this request
                                args.Context.Response.Write("Hello Atlanta!", true);

                                // For these exercises, take a good look at the supplied DUMMYGET route that is defined a few lines above.
                                break;
                            
                            case "INTERLOCKSTATUS":
                                ErrorLog.Notice($"{LogHeader} ReceivedRequestEvent INTERLOCKSTATUS running ...");
                                
                                var interlockResponse = new InterlockResponse();
                                interlockResponse.status = new List<ButtonStatus>();
                                interlockResponse.status.Add(new ButtonStatus(tp.BooleanInput[22].BoolValue));
                                interlockResponse.status.Add(new ButtonStatus(tp.BooleanInput[23].BoolValue));
                                interlockResponse.status.Add(new ButtonStatus(tp.BooleanInput[24].BoolValue));

                                string JSONResponseString = JsonConvert.SerializeObject(interlockResponse, Formatting.Indented);
                                ErrorLog.Notice($"{LogHeader} returning interlock status {JSONResponseString}");
                                args.Context.Response.Write(JSONResponseString, true);

                                break;
                            
                            case "GETSLIDER":
                                // Level 3
                                ErrorLog.Notice($"{LogHeader} ReceivedRequestEvent SLIDER running ...");
                                ushort percentage = Convert.ToUInt16(tp.UShortInput[31].UShortValue / 65535 * 100);
                                JSONResponseString = $"{{\"value\": {percentage}%}}";
                                args.Context.Response.Write(JSONResponseString, true);
                                
                                break;
                            
                            case "LOG":
                                // Level 3
                                ErrorLog.Notice($"{LogHeader} GET Request LOG running ...");
                                
                                JSONResponseString = FileControl.ReadFile($"{Directory.GetApplicationRootDirectory()}/User/logfile.txt");
                                args.Context.Response.Write($"{{ \"log:\" : {JSONResponseString} }}", true);
                                
                                break;
    
                            default:
                                break;
                        }
                    }

                    // When we get a "POST" request, we receive information from the frontend
                    if (args.Context.Request.HttpMethod == "POST")
                    {
                        string contents;

                        using (Crestron.SimplSharp.CrestronIO.Stream inputStream = args.Context.Request.InputStream)
                        {
                            using (StreamReader readStream = new StreamReader(inputStream, Encoding.UTF8))
                            {
                                contents = readStream.ReadToEnd();
                            }
                        }

                        switch (args.Context.Request.RouteData.Route.Name.ToUpper())
                        {
                            case "HOLAMUNDO":
                                ErrorLog.Notice($"{LogHeader} ReceivedRequestEvent HOLAMUNDO running ...");
                                string JSONBody = contents;
                                // echo request in response (for initial testing)
                                // args.Context.Response.Write(JSONBody, true);
                                
                                Request request = JsonConvert.DeserializeObject<Request>(JSONBody);

                                string data = request.text;
                                ErrorLog.Notice($"{LogHeader} Adding {data} to end of file {Directory.GetApplicationRootDirectory()}/User/logfile.txt ...");
                                FileControl.WriteWithAppend(data, $"{Directory.GetApplicationRootDirectory()}/User/logfile.txt");
                                
                                // set the button properly to send back
                                string JSONResponseString = this.tp.BooleanInput[21].BoolValue ? "{\"button\": true}" : "{\"button\": false}";
                                
                                args.Context.Response.Write(JSONResponseString, true);

                                break;
                            
                            case "POSTSLIDER":
                                // Level 3 payload {"value": 50}
                                ErrorLog.Notice($"{LogHeader} POST Request SLIDER running ...");
                                SliderRequest sliderRequest = JsonConvert.DeserializeObject<SliderRequest>(contents);

                                var sliderString = $"{sliderRequest.value}";
                                ErrorLog.Notice($"{LogHeader} Adding {sliderRequest.value} to end of file {Directory.GetApplicationRootDirectory()}/User/logfile.txt ...");
                                FileControl.WriteWithAppend(sliderString, $"{Directory.GetApplicationRootDirectory()}/User/logfile.txt");
                                tp.UShortInput[31].UShortValue = (ushort)(sliderRequest.value / 65535 * 100);

                                args.Context.Response.Write($"{{\"statusvalue\": \"{sliderRequest.value}\"}}", true);
                                
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                args.Context.Response.ContentType = "application/json";
                args.Context.Response.StatusCode = 401;
                args.Context.Response.Write(
                    JsonConvert.SerializeObject(
                    new Response
                    {
                        Status = "Error",
                        Message = this.GetApiError(ex)
                    }, 
                    Formatting.Indented), 
                    true);
            }
        }

        /// <summary>
        /// Stop the CWS server
        /// </summary>
        public void StopServer()
        {
            ErrorLog.Notice($"{LogHeader} StopServer() running ...");
            
            try
            {
                this.cwsServerLock.Enter();
                ErrorLog.Notice(LogHeader + "Stopping CWS API Server");
                if (this.cwsServer != null)
                {
                    this.cwsServer.Unregister();
                    this.cwsServer = null;
                    ErrorLog.Notice(LogHeader + "Stopped CWS API Server");
                }
                else
                {
                    ErrorLog.Error(LogHeader + "CWS API Server was not running!");
                }
            }
            finally
            {
                this.cwsServerLock.Leave();
            }
        }

        /// <summary>
        /// Returns the API Help
        /// </summary>
        /// <returns>List of possible commands</returns>
        private List<string> GetApiHelp()
        {
            var apiCommands = new List<string>();

            apiCommands.Add("[GET] Here you can put information regarding GET routes");
            apiCommands.Add("[POST] Here you can put information regarding POST routes\n");
            return apiCommands;
        }

        /// <summary>
        /// Returns any exception that occured to the user
        /// </summary>
        /// <param name="e">Exception message / stacktrace</param>
        /// <returns>List with the exception to be written back to the user</returns>
        private List<string> GetApiError(Exception e)
        {
            var apiError = new List<string>();
            apiError.Add(string.Format("Message: {0} \n", e.Message));
            apiError.Add(string.Format("Trace: {0}", e.StackTrace));
            return apiError;
        }
    }
}