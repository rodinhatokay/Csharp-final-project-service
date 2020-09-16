using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WcfFIARService
{
    [DataContract]
    public enum MoveResult
    {
        [EnumMember]
        YouWon,
        [EnumMember]
        Draw,
        [EnumMember]
        NotYourTurn,
        [EnumMember]
        GameOn,
        [EnumMember]
        PlayerLeft
    }
}
