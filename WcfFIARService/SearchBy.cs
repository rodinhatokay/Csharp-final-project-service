using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WcfFIARService
{
    [DataContract]
    public enum SearchBy
    {
        [EnumMember]
        Games,
        [EnumMember]
        Wins,
        [EnumMember]
        Loses,
        [EnumMember]
        Score,
    }
}
