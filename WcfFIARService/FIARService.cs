using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading;

namespace WcfFIARService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
          ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FIARService : IFIARService
    {

        Dictionary<string, IFIARSCallback> clients = new Dictionary<string, IFIARSCallback>();
        //Dictionary<string, bool> clientIngame = new Dictionary<string, bool>();
        List<GameBoard> games = new List<GameBoard>();

        public FIARService()
        {
            Init();

        }

        public List<PlayerInfo> GetAvalibalePlayers()
        {
            using (var ctx = new FIARDBContext())
            {
                var players = (from p in ctx.Players
                               where p.Status == 1
                               select p).ToList();
                List<PlayerInfo> pi = new List<PlayerInfo>();
                foreach (var player in players)
                {
                    pi.Add(new PlayerInfo(player));
                }
                return pi;
            }

        }

        public void Init()
        {
            using (var ctx = new FIARDBContext())
            {
                var players = (from p in ctx.Players
                               where p.Status == 1
                               select p).ToList();
                foreach (var player in players)
                {
                    player.Status = 0;
                }
                ctx.SaveChanges();
            }
        }

        public void PlayerLogin(string username, string password)
        {
            using (var ctx = new FIARDBContext())
            {
                var player = (from p in ctx.Players
                              where p.UserName == username && p.Pass == password
                              select p).FirstOrDefault();
                if (player == null)
                {
                    PlayerDoesntExistInDataBase fault = new PlayerDoesntExistInDataBase
                    {
                        Details = "Player " + username + "doesnt exists need to register"
                    };
                    throw new FaultException<PlayerDoesntExistInDataBase>(fault, new FaultReason("Player Doesnt exist in database"));
                }
                if (player.Status != 0)
                {
                    PlayerAlreadyConnectedFault userAlreadyConnected = new PlayerAlreadyConnectedFault
                    {
                        Details = "Player " + username + " already connected"
                    };
                    throw new FaultException<PlayerAlreadyConnectedFault>(userAlreadyConnected, new FaultReason("Player already connected"));
                }

                player.Status = 1;
                ctx.SaveChanges();
                IFIARSCallback callback = OperationContext.Current.GetCallbackChannel<IFIARSCallback>();
                var connectedClients = GetAvalibalePlayers();
                foreach (var cli in clients.Values)// send to all others to update thier list
                {
                    cli.UpdateClients(connectedClients);
                }
                clients.Add(username, callback);
                //clientIngame.Add(username, false);
            }
        }

        public void PlayerLogout(string username)
        {
            clients.Remove(username);
            //clientIngame.Remove(username);
            using (var ctx = new FIARDBContext())
            {
                var player = (from p in ctx.Players
                              where p.UserName == username
                              select p).FirstOrDefault();
                player.Status = 0;
                ctx.SaveChanges();
            }
            games.ForEach(g => g.playerDiscounnected(username));
            var connectedClients = GetAvalibalePlayers();
            foreach (var client in clients)
            {
                try
                {
                    client.Value.UpdateClients(connectedClients); // need to surrond with try and catch i think to 
                }
                catch (Exception ex)
                {

                }
            }
        }

        private GameBoard findGame(string username)
        {
            foreach (var g in games)
            {
                if (g.CheckIfPlayerInGame(username))
                    return g;
            }

            return null;

        }
        public MoveResult ReportMove(string username, int col)
        {
            GameBoard gb = findGame(username);
            string other_player = (username == gb.player1.username) ? gb.player2.username : gb.player1.username;
            MoveResult result = findGame(username).VerifyMove(username, col); //if game ended the game will auto update the database accordinly 
            if(result == MoveResult.NotYourTurn || result == MoveResult.IlligelMove)
            {
                return result;
            }
            
            if (result == MoveResult.PlayerLeft)
            {
                //TODO :update game ended or delete game in db
                OpponentDisconnectedFault fault = new OpponentDisconnectedFault();
                fault.Detail = "The other Player quit";
                throw new FaultException<OpponentDisconnectedFault>(fault);
            }
            if(result == MoveResult.Draw || result == MoveResult.YouWon)
            {
                
                games.Remove(gb);
            }
            
            Thread updateOtherPlayerThread = new Thread(() =>
            {
                clients[other_player].OtherPlayerMoved(result, col); // result may : Draw, youWon, GameOn, PlayerLeft
            });
            updateOtherPlayerThread.Start();



            return result;
        }

        public void RegisterPlayer(string username, string pass)
        {

            try
            {
                using (var ctx = new FIARDBContext())
                {
                    var player = (from p in ctx.Players
                                  where p.UserName == username
                                  select p).FirstOrDefault();
                    if (player == null)
                    {
                        ctx.Players.Add(new Player
                        {
                            UserName = username,
                            Pass = pass
                        });
                        ctx.SaveChanges();
                    }
                    else
                    {
                        PlayerAlreadyExistsInDataBase fault = new PlayerAlreadyExistsInDataBase
                        {
                            Details = "User already exists in data base"
                        };
                        throw new FaultException<PlayerAlreadyExistsInDataBase>(fault);
                    }
                }
            }
            catch (FaultException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new FaultException(ex.Message);
            }
        }

        public void Disconnected(string username)
        {
            return;
        }

        public bool InvatationSend(string from, string to)
        {
            //var other_player = clients[to];
            if (!clients.ContainsKey(to))
            {
                OpponentDisconnectedFault fault = new OpponentDisconnectedFault();
                fault.Detail = "The Player is offline";
                throw new FaultException<OpponentDisconnectedFault>(fault);
            }

            var result = clients[to].SendInvite(from);
            if (result == true)
            {
                GameBoard g = new GameBoard(from, to);
                games.Add(g);
                return true;
            }
            else
            {
                return false;
            }

        }
    }
}
