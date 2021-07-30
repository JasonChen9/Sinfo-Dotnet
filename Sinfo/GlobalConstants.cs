using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Permissions;

namespace Sinfo
{
    public class GlobalConstants
    {
        public const uint SinfoVersion = 1;

    }
    
    public struct PartitionInfo
    {
        public string PARTITION { get; set; }
        public UInt64 TIMELIMIT { get; set; }
        public UInt32 NODES { get; set; }
        public string NODELIST { get; set; }
        public string TOTAL { set; get; }
        public string AVAIL { set; get; }
        public string INUSE { set; get; }
    }
    public struct NodeInfo
    {
        public string NAME { set; get; }
        public UInt32 PORT { set; get; }
        public string PARTITIONNAME { set; get; }
        public string TOTAL { set; get; }
        public string AVAIL { set; get; }
        public string INUSE { set; get; }
        public string IPV4ADDR { set; get; }
    }
}