﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Threading;


namespace WcfFIARService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single,
          ConcurrencyMode = ConcurrencyMode.Multiple)]
    public class FIARService : IFIARService
    {
        public delegate void MyHostEventHandler(string msg, DateTime datetime);

        public MyHostEventHandler MyHostEvent;
        Dictionary<PlayerInfo, IFIARSCallback> clients = new Dictionary<PlayerInfo, IFIARSCallback>();
        //private List<PlayerInfo> playersOnline;

        List<GameBoard> games = new List<GameBoard>();
        public FIARService()
        {
            Init();
        }
        public FIARService(MyHostEventHandler MyHostEvent)
        {
            this.MyHostEvent = MyHostEvent;



            Init();
            SendStatusMessageEx("server started");

        }

        private void SendStatusMessageEx(string msg)
        {
            if (MyHostEvent != null)
                MyHostEvent(msg, DateTime.Now);
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

                SendStatusMessageEx("Call : Get Avalibale Players");
                return pi;
            }

        }

        public void Init()
        {
            clients = new Dictionary<PlayerInfo, IFIARSCallback>();
            using (var ctx = new FIARDBContext())
            {
                var players = (from p in ctx.Players
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

        public void PlayerLogout(string username)
        {

            SendStatusMessageEx("Logout : " + username + " is trying to logout");
            //clients.Remove(username);
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

            var getPlayers = GetAvalibalePlayers();
            foreach (var c in clients)
            {
                c.Value.UpdateClients(getPlayers);
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
            try
            {

                //var reporter = GetConnectedPlayer(username);
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

        private PlayerInfo GetConnectedPlayer(string username)
        {

            foreach (var p in clients)
            {
                if (p.Key.username == username)
                    return p.Key;
            }
            throw new FaultException<PlayerDoesntExistInDataBase>(new PlayerDoesntExistInDataBase(username));
        }

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
