﻿using System.Runtime.Serialization;

namespace GameCloud.UCenter.Common.Portable.Models.AppClient
{
    [DataContract]
    public class AccountWeChatOAuthInfo
    {
        [DataMember]
        public string AppId { get; set; }
        [DataMember]
        public string Code { get; set; }
    }
}
