﻿using CYQ.Data.Tool;
using System;

namespace Taurus.Plugin.MicroService
{
    /// <summary>
    /// 存档请求的客户端信息
    /// </summary>
    internal class HostInfo
    {
        ///// <summary>
        ///// 绑定的主机域名
        ///// </summary>
        //public string Domain { get; set; }
        /// <summary>
        /// 主机地址：http://localhost:8080
        /// </summary>
        public string Host { get; set; }

        /// <summary>
        /// 主机IP
        /// </summary>
        public string HostIP { get; set; }
        /// <summary>
        /// 主机进程ID
        /// </summary>
        public int PID { get; set; }
        /// <summary>
        /// 版本号：用于版本升级。
        /// </summary>
        public int Version { get; set; }

        /// <summary>
        /// 是否虚拟名称【虚拟名称转发不带Name】
        /// </summary>
        public bool IsVirtual { get; set; }
        /// <summary>
        /// 注册时间（最新）
        /// </summary>
        public DateTime RegTime { get; set; }
        /// <summary>
        /// 记录调用时间，用于隔离无法调用的服务，延时调用。
        /// </summary>
        [JsonIgnore]
        public DateTime CallTime { get; set; }
        /// <summary>
        /// 记录调用顺序，用于负载均衡
        /// </summary>
        [JsonIgnore]
        public int CallIndex { get; set; }

        /// <summary>
        /// Host 检测 状态：1- 失败，0 未检测，1 成功。
        /// </summary>
        public int State { get; set; }
    }
}
