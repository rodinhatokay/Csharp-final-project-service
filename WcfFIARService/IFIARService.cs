using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfFIARService
{
    //(CallbackContract = typeof(IFIARSCallback))
    
    [ServiceContract(CallbackContract = typeof(IFIARSCallback))]
    public interface IFIARService
    {

        [OperationContract]
        [FaultContract(typeof(PlayerAlreadyExistsInDataBase))]
        void RegisterPlayer(string username, string pass);

        [OperationContract]
        [FaultContract(typeof(OpponentDisconnectedFault))]
        bool InvatationSend(string from, string to);



        [OperationContract]
        [FaultContract(typeof(PlayerDoesntExistInDataBase))]
        [FaultContract(typeof(PlayerAlreadyConnectedFault))]
        void PlayerLogin(string username, string password);

        [OperationContract]
        void PlayerLogout(string username);



        [OperationContract]
        void Disconnected(string username);//middle game or something


        [OperationContract]
        [FaultContract(typeof(OpponentDisconnectedFault))]
        MoveResult ReportMove(string username, int col);

        [OperationContract]
        List<PlayerInfo> GetAvalibalePlayers();




    }
    public interface IFIARSCallback
    {
        [OperationContract(IsOneWay = false)]
        bool SendInvite(string username);

        [OperationContract(IsOneWay = true)]
        void OtherPlayerMoved(MoveResult result, int col);


        [OperationContract(IsOneWay = true)]
        void StartGame();

    }
}
