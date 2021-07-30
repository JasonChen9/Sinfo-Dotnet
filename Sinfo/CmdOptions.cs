using System.Collections.Generic;
using CommandLine;
using System;
using CommandLine.Text;

namespace Sinfo
{
    public class CmdOptions
    {
        [Option('s', Required = true,HelpText = "SlurmCtlXd address format: <IP>:<port>")] public string ServerAddr { get; set; }
        [Option('n', SetName = "nodes", Required = false,HelpText = "Print information about the specified node(s). " +
                                                                    "Multiple nodes using comma separated")] 
        #nullable enable
        public string? Nodes { get; set; }
        #nullable disable

    }
    

}