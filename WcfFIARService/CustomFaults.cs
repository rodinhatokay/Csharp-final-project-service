using System.Runtime.Serialization;

namespace WcfFIARService
{
    [DataContract]
    internal class CustomFault
    {
        [DataMember]
        public string Details { get; set; }
    }
    [DataContract]
    internal class PlayerAlreadyConnectedFault : CustomFault
    {
        internal PlayerAlreadyConnectedFault(string username)
        {
            Details = "Player " + username + " already connected";

        }
    }

    [DataContract]
    internal class PlayerAlreadyExistsInDataBase : CustomFault
    {
        internal PlayerAlreadyExistsInDataBase()
        {
            Details = "User name already exists in data base";
        }
    }

    [DataContract]
    internal class PlayerDoesntExistInDataBase : CustomFault
    {


        internal PlayerDoesntExistInDataBase(string username)
        {
            Details = "the username : " + username + "doesnt exists need to register";
        }



    }
    [DataContract]
    internal class IllegalMove : CustomFault
    {

    }

    [DataContract]
    internal class OpponentDisconnectedFault : CustomFault
    {
        internal OpponentDisconnectedFault()
        {
            Details = "The other Player quit";
        }

    }
    [DataContract]
    internal class OpponentNotAvailableFault : CustomFault
    {
        internal OpponentNotAvailableFault()
        {
            Details = "The other Player is not available right now";
        }

    }

}