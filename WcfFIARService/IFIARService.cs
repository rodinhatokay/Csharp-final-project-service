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
        bool InvatationSend(string from, string to); //receive invite from client to send to client



        [OperationContract]
        [FaultContract(typeof(PlayerDoesntExistInDataBase))]
        [FaultContract(typeof(PlayerAlreadyConnectedFault))]
        void PlayerLogin(string username, string password);


        #region searchs
        [OperationContract]
        [FaultContract(typeof(PlayerDoesntExistInDataBase))]
        List<PlayerInfo> Search(string username);

        [OperationContract]
        [FaultContract(typeof(PlayerDoesntExistInDataBase))]
        List<PlayerInfo> Search(string player1,string player2);

        [OperationContract]
        List<PlayerInfo> Search(SearchBy searchBy ,int count);

        #endregion

        [OperationContract]
        void PlayerLogout(string username);

        [OperationContract]
        void Init();

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
        bool SendInvite(string username); // send invite to client

        [OperationContract(IsOneWay = true)]
        void OtherPlayerMoved(MoveResult result, int col);

        [OperationContract(IsOneWay = true)]
        void UpdateClients(List<PlayerInfo> players); // update clients list that are not playing


        [OperationContract(IsOneWay = true)]
        void StartGame();

    }
}
