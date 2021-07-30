using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandLine;
using CommandLine.Text;
using Grpc.Core;
using Grpc.Net.Client;
using SlurmxGrpc;

namespace Sinfo
{
    public class Program
    {
        private static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<CmdOptions>(args)
                .MapResult(RealMain, _ => 1);
        }
        private static int RealMain(CmdOptions opts)
        {

            var regexAddr = new Regex(@"(^((25[0-5]|(2[0-4]|1[0-9]|[1-9]|)[0-9])(\.(?!$)|$)){4}$)");
            var regexPort = new Regex("^([0-9]{1,4}|[1-5][0-9]{4}|6[0-4][0-9]{3}|65[0-4][0-9]{2}|655[0-2][0-9]|6553[0-5])$");
            var serverAddr = opts.ServerAddr.Split(":");
            if (!regexAddr.Match(serverAddr[0]).Success) {
                Log.Error("CtlXd server address is invalid!{}");
                return 1;
            }
            if (!regexPort.Match(serverAddr[1]).Success) {
                Log.Error("CtlXd server port is invalid!");
                return 1;
            }
            
            if (opts.Nodes!=null)
            {
                var nodesInfoRequest = new NodeInfoQueryRequest();
                var nodes = opts.Nodes.Split(",");
                foreach (var n in nodes)
                {
                    nodesInfoRequest.NodesName.Add(n);     
                }
                
                //Query specified nodes info 
                return (GrpcClient.QueryNodeInfo("http://" + opts.ServerAddr, nodesInfoRequest, out var nodesInfoReply)
                        && GrpcClient.LoadNodeInfo(nodesInfoReply)) == true ? 0 : 1;
            }
            
            //Query all partitions info 
            var partitionsInfoRequest = new PartitionInfoQueryRequest {Version = GlobalConstants.SinfoVersion};
            return (GrpcClient.QueryPartitionInfo("http://"+opts.ServerAddr, partitionsInfoRequest, out var partitionsInfoReply)
                    && GrpcClient.LoadPartitionInfo(partitionsInfoReply))==true ? 0 : 1;
                
        }
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
        
    }
}