using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Grpc.Core;
using Grpc.Net.Client;
using SlurmxGrpc;
using ConsoleTables;

namespace Sinfo
{
    public class GrpcClient
    {
        public static bool QueryPartitionInfo(
            in string serverAddr, in PartitionInfoQueryRequest partitionInfoQueryRequest, out PartitionInfoQueryReply partitionInfoQueryReply)
        {
            try
            {
                var channel = GrpcChannel.ForAddress(serverAddr);
                var client = new SlurmCtlXd.SlurmCtlXdClient(channel);
                partitionInfoQueryReply = client.QueryPartitioonInfo(partitionInfoQueryRequest, deadline: DateTime.UtcNow.AddSeconds(3));
            }
            catch (RpcException ex)
            {
                Log.Error("Cannot connect to ctlXd server! Exit Status code:"+ex.StatusCode);
                partitionInfoQueryReply = null;
                return false;
            }

            return true;
        }
        
        public static bool QueryNodeInfo(
            in string serverAddr, in NodeInfoQueryRequest nodeInfoQueryRequest, out NodeInfoQueryReply nodeInfoQueryReply)
        {
            try
            {
                var channel = GrpcChannel.ForAddress(serverAddr);
                var client = new SlurmCtlXd.SlurmCtlXdClient(channel);
                nodeInfoQueryReply = client.QueryNodeInfo(nodeInfoQueryRequest, deadline: DateTime.UtcNow.AddSeconds(3));
            }
            catch (RpcException ex)
            {
                Log.Error("Cannot connect to ctlXd server! Exit Status code:"+ex.StatusCode);
                nodeInfoQueryReply = null;
                return false;
            }
            return true;
        }
        public static bool LoadPartitionInfo(in PartitionInfoQueryReply partitionInfoQueryReply)
        {
            if (partitionInfoQueryReply.PartitionsInfo.Count < 1)
            {
                Log.Info("There is no partition information!");
                return true;
            }
            //Store the partitions info in an IEnumerable, which is convenient for printing the form
            IEnumerable<PartitionInfo> parts = Array.Empty<PartitionInfo>();
            foreach (var partitionInfo in partitionInfoQueryReply.PartitionsInfo)
            {
                var nodesInGroups = new List<string>();
                if (partitionInfo.NodesName.Count > 1)
                {
                    //group similar nodes for partitionInfo.NodesName
                    var regex = new Regex(@"^([a-zA-Z]*)([0-9]*)$");
                    IEnumerable<IGrouping<string, string>> nodesGroups =
                    partitionInfo.NodesName.Select(item => new {item, match = regex.Match(item).Groups[1]})
                        .Where(x => x.match.Success)
                        .GroupBy(x => x.match.Value, x => x.item);
                
                    //For each group, we merge nodes with the same prefix 
                    //and store in the nodes_in_groups.
                    foreach (var @group in nodesGroups)
                    {
                        //The prefix as the group_name.
                        var groupName = regex.Match(@group.First()).Groups[1].ToString();
                        //Delete prefix, and add the remaining part into the group.
                        //if node do not have remaining part, we add a "-" into the group.
                        var nodesInAGroup = @group.Select(name => 
                            Regex.Replace(name, "[a-zA-Z]*", ""))
                            .Select(remain => (remain != "") ? remain : "-").ToList();
                        
                        switch (nodesInAGroup.Count)
                        {
                            case > 1:
                                //Surround nodes with "[]"
                                //join Neighbor number 
                                var joinArray = Array.Empty<string>();
                                var joinIndex=0;
                                
                                //If the element "-" is in group, put the element "-" in the first position and move it into joinArray
                                var nodeArray = nodesInAGroup.OrderBy(x=>x).ToArray();
                                if (nodeArray[0] == "-")
                                {
                                    joinArray = joinArray.Concat(new [] {nodeArray[0]}).ToArray();
                                    nodeArray = nodeArray.Skip(1).ToArray();
                                    joinIndex++;
                                }
                                
                                //sort array
                                nodeArray = nodeArray.OrderBy(int.Parse).ToArray();
                                
                                //Determine whether the first number needs to be joined
                                if (int.Parse(nodeArray[1])-int.Parse(nodeArray[0])==1)
                                {
                                    joinArray = joinArray.Concat(new [] {nodeArray[0]}).ToArray();
                                }
                                else
                                {
                                    joinArray = new string[] {nodeArray[0],nodeArray[1]};
                                    joinIndex++;
                                }
                                
                                //join Neighbor number and link with "-"
                                for (var index = 1; index < nodeArray.Length-1; index++)
                                {
                                    if (int.Parse(nodeArray[index+1])-int.Parse(nodeArray[index])==1)
                                        continue;
                                    
                                    joinArray[joinIndex++] += "-"+nodeArray[index];
                                    joinArray = joinArray.Concat(new [] {nodeArray[index+1]}).ToArray();
                                }
                                joinArray[joinIndex] += "-" + nodeArray[^1];
                                
                                //separate joined numbers with "," 
                                groupName += "[" + string.Join(",", joinArray) + "]";
                                break;
                            case 1:
                            {
                                //If there is only one node in the group,
                                //we do not need surround it with "[]".
                                //If the remained one is "-", we just delete it.
                                var first = nodesInAGroup.First();
                                groupName += (first == "-") ? "":first;
                                break;
                            }
                        }
                        nodesInGroups.Add(groupName);
                    }
                }
                else
                    nodesInGroups.Add(partitionInfo.NodesName.First());
                
                //total res
                var total = "";
                total += partitionInfo.ResTotal.CpuCoreLimit + "/";
                total += ReadableMem(partitionInfo.ResTotal.MemoryLimitBytes) + "/";
                total += ReadableMem(partitionInfo.ResTotal.MemorySwLimitBytes);
                
                //available res
                var avail = "";
                avail += partitionInfo.ResAvail.CpuCoreLimit + "/";
                avail += ReadableMem(partitionInfo.ResAvail.MemoryLimitBytes) + "/";
                avail += ReadableMem(partitionInfo.ResAvail.MemorySwLimitBytes);
                
                //res in use
                var inuse = "";
                inuse += partitionInfo.ResInUse.CpuCoreLimit + "/";
                inuse += ReadableMem(partitionInfo.ResInUse.MemoryLimitBytes) + "/";
                inuse += ReadableMem(partitionInfo.ResInUse.MemorySwLimitBytes);
                
                //Fill in partition info
                var partition = new PartitionInfo
                {
                    PARTITION = partitionInfo.PartitionName,
                    TIMELIMIT = partitionInfo.TimeLimitSec,
                    NODES = (UInt32)partitionInfo.NodesName.Count, 
                    NODELIST = string.Join(",", nodesInGroups.ToArray()),
                    TOTAL = total,
                    AVAIL = avail,
                    INUSE = inuse
                };
                //add partition info into IEnumerable
                IEnumerable<PartitionInfo> part = new PartitionInfo[]{partition};
                parts = parts.Concat(part);
            }
            try
            {
                //Print result table
                PrintTable(parts);
            }
            catch (RpcException ex)
            {
                Log.Error("An error occurred while printing the form! Status Codes:"+ex.StatusCode);
                return false;
            }
            return true;
        }
        
        public static bool LoadNodeInfo(in NodeInfoQueryReply nodeInfoQueryReply)
        {
            if (nodeInfoQueryReply.NodesInfo.Count < 1)
            {
                Log.Info("There is no node information!");
                return true;
            }
            
            IEnumerable<NodeInfo> nodes = Array.Empty<NodeInfo>();
            foreach (var nodeInfo in nodeInfoQueryReply.NodesInfo)
            {
                //total res
                var total = "";
                total += nodeInfo.ResTotal.CpuCoreLimit + "/";
                total += ReadableMem(nodeInfo.ResTotal.MemoryLimitBytes) + "/";
                total += ReadableMem(nodeInfo.ResTotal.MemorySwLimitBytes);
                //available res
                var avail = "";
                avail += nodeInfo.ResAvail.CpuCoreLimit + "/";
                avail += ReadableMem(nodeInfo.ResAvail.MemoryLimitBytes) + "/";
                avail += ReadableMem(nodeInfo.ResAvail.MemorySwLimitBytes);
                //res in use
                var inuse = "";
                inuse += nodeInfo.ResInUse.CpuCoreLimit + "/";
                inuse += ReadableMem(nodeInfo.ResInUse.MemoryLimitBytes) + "/";
                inuse += ReadableMem(nodeInfo.ResInUse.MemorySwLimitBytes);
                var node = new NodeInfo
                {
                    PORT = nodeInfo.Port, 
                    NAME = nodeInfo.NodeName, 
                    PARTITIONNAME = nodeInfo.PartitionName,
                    TOTAL = total,
                    AVAIL = avail,
                    INUSE = inuse,
                    IPV4ADDR = nodeInfo.Ipv4Addr
                };
                
                IEnumerable<NodeInfo> n = new NodeInfo[]{node};
                nodes = nodes.Concat(n);

            }
            try
            {
                //Print result table
                PrintTable(nodes);
            }
            catch (RpcException ex)
            {
                Log.Error("An error occurred while printing the form!! Status Codes:"+ex.StatusCode);
                return false;
            }
            return true;
        }
        private static void PrintTable<T>(IEnumerable<T> values)
        {
            //Custom form style
            ConsoleTable.From<T>(values).Write(format:Format.Minimal);
        }

        //return readable memory string
        private static string ReadableMem(UInt64 res)
        {
            return res switch
            {
                < 1024 => res.ToString() + "B",
                < 1024 * 1024 => (res / 1024).ToString() + "K",
                < 1024 * 1024 * 1024 => (res / (1024 * 1024)).ToString() + "M",
                _ => (res / (1024 * 1024 * 1024)).ToString() + "G"
            };
        }

        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod()?.DeclaringType);
    }
}