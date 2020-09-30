using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;


namespace WcfFIARService
{
    /// <summary>
    /// serivce class for handling all kind of calls for clients
    /// </summary>
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
          ConcurrencyMode = ConcurrencyMode.Multiple)]




    public class FIARService : IFIARService
    {
        /// <summary>
        /// function to display
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="datetime"></param>
        public delegate void MyHostEventHandler(string msg, DateTime datetime);
        MyHostEventHandler _myHostEvent;

        Dictionary<PlayerInfo, IFIARSCallback> clients = new Dictionary<PlayerInfo, IFIARSCallback>();
        List<GameBoard> games = new List<GameBoard>();
        public FIARService()
        {
            Init();
        }

        public FIARService(MyHostEventHandler MyHostEvent)
        {
            this._myHostEvent = MyHostEvent;
            Task t = new Task(() =>
            {

            });
            Init();
            SendStatusMessageEx("server started");

        }

        private void SendStatusMessageEx(string msg)
        {
            if (_myHostEvent != null)
                _myHostEvent(msg, DateTime.Now);
        }


        /// <summary>
        /// sends available players to clients
        /// </summary>
        /// <returns></returns>

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

                SendStatusMessageEx("Call : Get Avalibale Players");
                return pi;
            }

        }


        
        /// <summary>
        /// on creating host we reseting the players status to logged out
        /// </summary>
        private void Init()
        {
            clients = new Dictionary<PlayerInfo, IFIARSCallback>();
            using (var ctx = new FIARDBContext())
            {
                //making all players offline before server starts
                var players = (from p in ctx.Players
                               select p).ToList();
                foreach (var player in players)
                {
                    player.Status = 0;
                }
                //Removes all games that didn't ended in case server crashed
                var gTmp = (from g in ctx.Games
                            where g.Player1Points == 0 && g.Player2Points == 0
                            select g).ToList();
                foreach (var g in gTmp)
                {
                    ctx.Games.Remove(g);
                }

                ctx.SaveChanges();
            }
            checkEveryOne();
        }



        /// <summary>
        /// call for clients to login
        /// if clients doesn't exist or player already connected will throw fault accordingly
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void PlayerLogin(string username, string password)
        {
            using (var ctx = new FIARDBContext())
            {
                var player = (from p in ctx.Players
                              where p.UserName == username && p.Pass == password
                              select p).FirstOrDefault();
                if (player == null)
                {
                    var f = new PlayerDoesntExistInDataBase(username);
                    SendStatusMessageEx(f.Details);
                    throw new FaultException<PlayerDoesntExistInDataBase>(f);
                }
                if (player.Status != 0)
                {
                    PlayerAlreadyConnectedFault f = new PlayerAlreadyConnectedFault(username);
                    SendStatusMessageEx(f.Details);
                    throw new FaultException<PlayerAlreadyConnectedFault>(f);
                }

                player.Status = 1;
                ctx.SaveChanges();
                IFIARSCallback callback = OperationContext.Current.GetCallbackChannel<IFIARSCallback>();
                var getPlayers = GetAvalibalePlayers();
                foreach (var c in clients)
                {
                    c.Value.UpdateClients(getPlayers);
                }
                clients.Add(new PlayerInfo(player), callback);
                SendStatusMessageEx("Login : " + username + " Loged in!");
            }
        }


        /// <summary>
        /// clients logging out 
        /// </summary>
        /// <param name="username"></param>
        public void PlayerLogout(string username)
        {

            try
            {
                foreach (var g in games)
                {
                    var winner = g.PlayerDisconnected(username);
                    if (winner != null)
                        clients[winner].OtherPlayerDisconnected();

                }

                games = games.Where(x => !x.CheckIfPlayerInGame(username)).ToList();

                var player = GetConnectedPlayer(username);
                player.Status = Status.Disconnected;
                clients.Remove(player);
                SendStatusMessageEx("Logout : " + username + " is sending messages");
                var getPlayers = GetAvalibalePlayers();
                foreach (var c in clients)
                {
                    SendStatusMessageEx("updating : " + c.Key.username + " is been updated");
                    c.Value.UpdateClients(getPlayers);
                }

                SendStatusMessageEx("Logout : " + username + " is loged out");

            }
            catch (Exception ex)
            {
                SendStatusMessageEx(ex.Message);
            }
            //clients.Remove(username);
        }


        /// <summary>
        /// returns the games which the given username plays in 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private GameBoard findGame(string username)
        {
            foreach (var g in games)
            {
                if (g.CheckIfPlayerInGame(username))
                    return g;
            }
            return null;
        }


        /// <summary>
        /// function that handles each players move 
        /// it tests if the move was correct and updates the other player about the move was made
        /// </summary>
        /// <param name="username"></param>
        /// <param name="col"></param>
        /// <returns></returns>
        public MoveResult ReportMove(string username, int col)
        {
            try
            {
                GameBoard gb = findGame(username);
                string other_player = (username == gb.player1.username) ? gb.player2.username : gb.player1.username;
                var otherPlayer = GetConnectedPlayer(other_player);
                MoveResult result = gb.VerifyMove(username, col); //if game ended the game will auto update the database accordinly 
                SendStatusMessageEx(result.ToString());
                if (result == MoveResult.NotYourTurn || result == MoveResult.IlligelMove)
                {
                    return result;
                }

                if (result == MoveResult.Draw || result == MoveResult.YouWon)
                {
                    games.Remove(gb);
                    foreach (var p in clients)
                    {
                        if (p.Key.username != username && p.Key.username != other_player)
                        {
                            p.Value.UpdateClients(GetAvalibalePlayers());
                        }
                    }

                }

                clients[otherPlayer].OtherPlayerMoved(result, col);


                SendStatusMessageEx(username + " made a move against " + other_player);
                return result;
            }
            catch (Exception ex)
            {
                SendStatusMessageEx(ex.Message);
                return MoveResult.PlayerLeft;
            }
        }



        /// <summary>
        /// check if all players are alive
        /// </summary>
        private void checkEveryOne()
        {
            Thread t = new Thread(() =>
            {
                while (true)
                {
                    foreach (var c in clients)
                    {

                        IsAlive(c.Key);
                    }
                    Thread.Sleep(10000);
                }
            });
            t.Start();
        }


        /// <summary>
        /// check if given player is alive
        /// </summary>
        /// <param name="player"></param>
        private async void IsAlive(PlayerInfo player)
        {
            var callback = clients[player];
            bool exp = false;
            await Task.Factory.StartNew(() =>
            {
                try
                {
                    callback.IsAlive();
                }
                catch (Exception ex)
                {

                    exp = true;
                }

            });
            if (exp)
            {
                PlayerLogout(player.username);
                SendStatusMessageEx(player.username + " lost connection");
            }

        }


        /// <summary>
        /// function for client to call to register the player to database
        /// if the username already exists will throw fault
        /// </summary>
        /// <param name="username"></param>
        /// <param name="pass"></param>
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
                        var f = new PlayerAlreadyExistsInDataBase();
                        SendStatusMessageEx(f.Details);
                        throw new FaultException<PlayerAlreadyExistsInDataBase>(f);
                    }
                }
            }
            catch (FaultException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                SendStatusMessageEx(ex.Message);
                throw new FaultException(ex.Message);
            }
        }


        /// <summary>
        /// called when players disconnects from game and sets the other player as winner by default
        /// </summary>
        /// <param name="username"></param>
        public void Disconnected(string username)
        {
            try
            {
                SendStatusMessageEx("Disconnected : " + username + " is trying to Disconnect");
                PlayerInfo player = GetConnectedPlayer(username);

                foreach (var g in games)
                {
                    var winner = g.PlayerDisconnected(username);
                    if (winner != null)
                        clients[winner].OtherPlayerDisconnected();

                }
                games = games.Where(x => !x.CheckIfPlayerInGame(username)).ToList();
                foreach (var p in clients)
                {
                    if (p.Key.username != username)
                    {
                        p.Value.UpdateClients(GetAvalibalePlayers());
                        SendStatusMessageEx("sent refresh to  : " + p.Key.username);
                    }
                }


                SendStatusMessageEx("Disconnected : " + username + " Disconnected");
                return;
            }
            catch (Exception ex)
            {
                SendStatusMessageEx(ex.Message);
            }

        }



        /// <summary>
        /// clients call to send invite to player and automatically asks oppoent and return the answer
        /// </summary>
        /// <param name="fromPlayer"></param>
        /// <param name="toPlayer"></param>
        /// <returns></returns>
        public bool InvitationSend(string fromPlayer, string toPlayer)
        {
            var player1 = GetConnectedPlayer(fromPlayer);
            var player2 = GetConnectedPlayer(toPlayer);

            if (player2.Status != Status.Online)
            {
                OpponentNotAvailableFault fault = new OpponentNotAvailableFault();
                SendStatusMessageEx(fault.Details);
                throw new FaultException<OpponentNotAvailableFault>(fault);
            }
            var result = clients[player2].SendInvite(fromPlayer);
            if (result == true)
            {
                GameBoard g = new GameBoard(player1, player2);
                games.Add(g);
                var pl = GetAvalibalePlayers();
                foreach (var p in clients)
                {
                    if (p.Key.id != player1.id && p.Key.id != player2.id)
                    {
                        p.Value.UpdateClients(pl);
                    }
                }
                return true;
            }
            else
            {
                return false;
            }


        }



        /// <summary>
        /// return all players in the database
        /// </summary>
        /// <returns></returns>
        public List<PlayerInfo> GetAllPlayers()
        {

            using (var ctx = new FIARDBContext())
            {
                var players = (from p in ctx.Players

                               select p).ToList();
                List<PlayerInfo> pi = new List<PlayerInfo>();
                foreach (var player in players)
                {
                    pi.Add(new PlayerInfo(player));
                }
                return pi;
            }

        }

        /// <summary>
        /// returns all games going on and ended between given to players
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
        /// <returns></returns>
        public List<GameInfo> GetPlayersGames(string player1, string player2)
        {
            using (var ctx = new FIARDBContext())
            {
                var games = (from g in ctx.Games
                             where (g.Player1.UserName == player1 && g.Player2.UserName == player2 ||
                                   g.Player1.UserName == player2 && g.Player2.UserName == player1)
                             select g).ToList();
                List<GameInfo> gamesLis = new List<GameInfo>();
                foreach (var g in games)
                {
                    gamesLis.Add(new GameInfo(g));
                }
                return gamesLis;

            }
        }


        /// <summary>
        /// return all players currently alive
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        private PlayerInfo GetConnectedPlayer(string username)
        {

            foreach (var p in clients)
            {
                if (p.Key.username == username)
                    return p.Key;
            }
            throw new FaultException<PlayerDoesntExistInDataBase>(new PlayerDoesntExistInDataBase(username));
        }


        /// <summary>
        /// returns all games ended in database
        /// </summary>
        /// <returns></returns>
        public List<GameInfo> GetEndedGames()
        {
            List<GameInfo> gamesList = new List<GameInfo>();
            using (var ctx = new FIARDBContext())
            {
                var games = (from g in ctx.Games
                             where g.GameOver == true
                             select g).ToList();
                foreach (var g in games)
                {
                    gamesList.Add(new GameInfo(g));
                }
                return gamesList;
            }

        }


        /// <summary>
        /// return all ongoing games currently
        /// </summary>
        /// <returns></returns>
        public List<GameInfo> GetOngoingGames()
        {
            List<GameInfo> gamesList = new List<GameInfo>();
            using (var ctx = new FIARDBContext())
            {
                var games = (from g in ctx.Games
                             where g.GameOver == false
                             select g).ToList();
                foreach (var g in games)
                {
                    gamesList.Add(new GameInfo(g));
                }
                return gamesList;
            }
        }


        /// <summary>
        /// client call to check if host is alive
        /// </summary>
        /// <returns></returns>
        public bool ping()
        {
            return true;
        }


        /// <summary>
        /// sets players avaible to play 
        /// </summary>
        /// <param name="username"></param>
        public void SetAsAvailablePlayer(string username)
        {
            var player = clients.Keys.FirstOrDefault(x => x.username == username);
            player.Status = Status.Online;

            foreach (var p in clients)
            {
                try
                {
                    if (p.Key.username != username)
                        p.Value.UpdateClients(GetAvalibalePlayers());
                    else
                    {
                        p.Key.Status = Status.Online;
                    }
                }
                catch (TimeoutException ex)
                {
                    this.PlayerLogout(p.Key.username);
                }

            }
        }
    }
}
