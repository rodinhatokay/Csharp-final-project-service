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
        [FaultContract(typeof(OpponentNotAvailableFault))]
        bool InvitationSend(string fromPlayer, string toPlayer); //receive invite from client to send to client



        [OperationContract]
        [FaultContract(typeof(PlayerDoesntExistInDataBase))]
        [FaultContract(typeof(PlayerAlreadyConnectedFault))]
        void PlayerLogin(string username, string password);


        [OperationContract(IsOneWay = true)]
        void SetAsAvailablePlayer(string username);


        #region searchs
        [OperationContract]
        List<PlayerInfo> GetAllPlayers();

        [OperationContract]
        List<GameInfo> GetPlayersGames(string player1, string player2);


        #endregion

        [OperationContract(IsOneWay = true)]
        void PlayerLogout(string username);

        [OperationContract]
        void Init();

        [OperationContract(IsOneWay = true)]
        void Disconnected(string username);//middle game or something


        [OperationContract]
        [FaultContract(typeof(OpponentDisconnectedFault))]
        MoveResult ReportMove(string username, int col);

        [OperationContract]
        List<PlayerInfo> GetAvalibalePlayers();

        [OperationContract]
        List<GameInfo> GetEndedGames();

        [OperationContract]
        List<GameInfo> GetOngoingGames();


    }
    [ServiceContract]
    public interface IFIARSCallback
    {
        [OperationContract]
        bool SendInvite(string username); // send invite to client

        [OperationContract(IsOneWay = true)]
        void OtherPlayerMoved(MoveResult result, int col);

        [OperationContract(IsOneWay = true)]
        void OtherPlayerDisconnected();

        [OperationContract(IsOneWay = true)]
        void UpdateClients(List<PlayerInfo> players);


        [OperationContract(IsOneWay = true)]
        void StartGame();

        [OperationContract]
        bool IsAlive();

    }
}
