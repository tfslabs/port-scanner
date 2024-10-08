﻿using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Scanner
{
    class Program
    {
        static Mutex fileMutex = new Mutex();
        public static void Usage()
        {
            Console.WriteLine(
             @"
PortScanner is a part of Windows Server Management Tools
Currently maintained by TheFlightSims

See more at: https://github.com/TheFlightSims/windowsserver-mgmttools

PortScanner.exe {hosts} {ports} [timeout]

DESCRIPTION:
    PortScanner is used to scan any opening ports of the target computer
    The commands are depend on how you use, but general mapping are still 
    can be seen above.

GENERAL COMMANDS:
    hosts         - Target computers. Can use FQDN or IP. Seperate by comma

    ports         - Ports that needs to be scanned. Seperate by comma. 
                    You can use defined ports. See below for more info.
                    Port range is not supported in this version

    timeout       - Maximum scanning time per port. Useful when scanning on
                    large number of ports

DEFINED PORTS:

    Defined ports are the ports containing specific roles, providing specific 
    features on computer. The list below shows all defined ports on PortScanner
    
    admin         - 135, 139, 445, 3389, 5985, 5986, 8000, 8080.

    web           - 21, 23, 25, 80, 443, 8080.

    top20         - 21, 22, 23, 25, 53, 80, 110, 111, 135, 139, 
                    143, 443, 445, 993, 995, 1723, 3306, 3389, 
                    5900, 8080.

    server-common - 7, 9, 13, 17, 19, 25, 42, 80, 88, 110, 111, 
                    119, 135, 149, 389, 443, 445, 465, 563, 587, 
                    636, 808, 993, 995, 1433, 1688, 1801, 3268, 
                    3269, 3387, 3388, 3389, 4044, 6516, 6881, 
                    8000, 8080, 8800, 8391, 8443, 8530, 8531, 
                    9389

    all-ports    - All ports in range 1, 65535

SAMPLES:
    PortScanner.exe hosts=127.0.0.1,google.com ports=21,22,23 timeout=5000
    PortScanner.exe hosts=localhost ports=admin
              ");
        }

        static Dictionary<string, object> results = new Dictionary<string, object>();
        public static Dictionary<string, object> ArgParser(string[] args)
        {
            string[] keys = { "timeout", "hosts", "ports"};
            string[] requiredKeys = { "hosts", "ports" };
            results.Add("timeout", 500);

            foreach (string arg in args)
            {
                string[] parts = arg.Split('=');
                if (parts.Length != 2)
                {
                    Console.WriteLine("[Error] Invalid argument passed: {0}", arg);
                    Usage();
                    Environment.Exit(1);
                }
                if (!keys.Contains(parts[0]))
                {
                    Console.WriteLine("[Error] Unknown argument passed: {0}", parts[0]);
                    continue;
                }
                switch (parts[0])
                {
                    case "hosts":
                        var hostArray = parts[1].Split(',');
                        var val = hostArray;
                        results.Add("hosts", val);
                        break;
                    case "timeout":
                        var val2 = parts[1];
                        if (Convert.ToInt32(val2) < 500)
                        {
                            Console.WriteLine("[Warning] Timeout will be set to the minimum at 500 milliseconds.\n");
                        }
                        results.Remove("timeout");
                        results.Add("timeout", val2);
                        break;
                    case "ports":
                        if (parts[1] == "admin")
                        {
                            Console.WriteLine("[Notification] The following ports will be scanned: 135, 139, 445, 3389, 5985, 5986, 8000, 8080. \n");
                            results["ports"] = new object[] { 135, 139, 445, 3389, 5985, 5986, 8000, 8080 };
                        }
                        else if (parts[1] == "web")
                        {
                            Console.WriteLine("[Notification] The following ports will be scanned: 21, 23, 25, 80, 443, 8080. \n");
                            results["ports"] = new object[] { 21, 23, 25, 80, 443, 8080 };
                        }
                        else if (parts[1] == "top20")
                        {
                            Console.WriteLine("[Notification] The following ports will be scanned: 21, 22, 23, 25, 53, 80, 110, 111, 135, 139, 143, 443, 445, 993, 995, 1723, 3306, 3389, 5900, 8080. \n");
                            results["ports"] = new object[] { 21, 22, 23, 25, 53, 80, 110, 111, 135, 139, 143, 443, 445, 993, 995, 1723, 3306, 3389, 5900, 8080 };
                        }
                        else if (parts[1] == "server-common")
                        {
                            Console.WriteLine("[Notification] The following ports will be scanned: 7, 9, 13, 17, 19, 25, 42, 80, 88, 110, 111, 119, 135, 149, 389, 443, 445, 465, 563, 587, 636, 808, 993, " +
                                "995, 1433, 1688, 1801, 3268, 3269, 3387, 3388, 3389, 4044, 6516, 6881, 8000, 8080, 8800, 8391, 8443, 8530, 8531, 9389 \n");
                            results["ports"] = new object[] { 7, 9, 13, 17, 19, 25, 42, 80, 88, 110, 111, 119, 135, 149, 389, 443, 445, 465, 563, 587, 636, 808, 993, 995, 1433, 1688, 1801, 3268, 3269, 3387, 3388, 3389, 4044, 6516, 6881, 8000, 8080, 8800, 8391, 8443, 8530, 8531, 9389 };
                        }
                        else if (parts[1] == "all-ports")
                        {
                            Console.WriteLine("[Notification] All ports of the destination computer will be scanned\n");
                            results["ports"] = Enumerable.Range(1, 65535).Select(x => (object)x).ToArray();
                        }
                        else
                        {
                            var portArray = parts[1].Split(',');
                            var val5 = portArray;
                            results.Add("ports", val5);
                        }
                        break;
                    default:
                        Console.WriteLine("[Error] Unknown parameter passed: {0}", parts[0]);
                        break;
                }
            }
            foreach (string requiredKey in requiredKeys)
            {
                if (!results.ContainsKey(requiredKey))
                {
                    Console.WriteLine("[Error] Missing required parameter: {0}", requiredKey);
                    Usage();
                    Environment.Exit(1);
                }
            }
            return results;
        }

        public static bool IsPortOpen(string host, object ports1)
        {
            bool bRet;
            try
            {
                Socket s = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);
                int Time = Convert.ToInt32(results["timeout"]);
                s.ReceiveTimeout = Time;
                s.Connect(host, Convert.ToInt32(ports1));
                bRet = true;
            }
            catch (Exception)
            {
                bRet = false;
            }
            return bRet;
        }

        public static void PortScan(string host)
        {
            bool scanresult;
            string content;
            Parallel.ForEach((object[])results["ports"], ports => {
                scanresult = IsPortOpen(host, ports);
                if (scanresult)
                {
                    content = String.Format("{0}: port {1} is {2}", host, ports, "opening");
                    Console.WriteLine(String.Format("{0} : port {1} is {2}", host, ports, "opening"));
                }
            });
            Console.WriteLine("[Notification] All unlisted ports are not open, nor timed out");
        }

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Usage();
            }

            else
            {
                ArgParser(args);
                foreach (object host in (object[])results["hosts"])
                {
                    PortScan(host.ToString());
                    Console.WriteLine("[Notification] Scanner complete.");
                }
            }
        }
    }
}
