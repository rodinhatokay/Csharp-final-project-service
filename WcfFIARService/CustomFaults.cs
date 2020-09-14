using System.Runtime.Serialization;

namespace WcfFIARService
{

    [DataContract]
    internal class PlayerAlreadyConnectedFault
    {
        [DataMember]
        public string Details { get; set; }
    }

    [DataContract]
    internal class PlayerAlreadyExistsInDataBase
    {
        [DataMember]
        public string Details { get; set; }
    }

    [DataContract]
    internal class PlayerDoesntExistInDataBase
    {
        [DataMember]
        public string Details { get; set; }
    }
    [DataContract]
    internal class IlligaleMove
    {
        [DataMember]
        public string Details { get; set; }
    }

    [DataContract]
    internal class OpponentDisconnectedFault
    {
        [DataMember]
        public string Detail { get; set; }
    }

}