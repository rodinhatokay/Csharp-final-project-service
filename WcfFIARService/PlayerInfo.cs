using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfFIARService
{

    /// <summary>
    /// this class meant for displaying player info for clients 
    /// </summary>
    [DataContract]
    public class PlayerInfo
    {

        public PlayerInfo(Player player)
        {

            init(player);

        }

        public PlayerInfo(string username)
        {
            init(username);



        }
        public PlayerInfo(int id)
        {
            init(id);
        }
        /// <summary>
        /// given id gets the player from the database and sets into the class
        /// might throw fault exception
        /// </summary>
        /// <param name="id"></param>
        private void init(int id)
        {
            using (var ctx = new FIARDBContext())
            {
                var player = (from pl in ctx.Players
                              where pl.PlayerId == id
                              select pl).ToList();
                if (player.Count > 0)
                {
                    init(player[0]);

                }
                else
                {
                    throw new FaultException<PlayerDoesntExistInDataBase>(new PlayerDoesntExistInDataBase(username));
                }
            }
        }
        private void init(string username)
        {
            using (var ctx = new FIARDBContext())
            {
                var player = (from pl in ctx.Players
                              where pl.UserName == username
                              select pl).ToList();
                if (player.Count > 0)
                {
                    init(player[0]);

                }
                else
                {
                    throw new FaultException<PlayerDoesntExistInDataBase>(new PlayerDoesntExistInDataBase(username));
                }
            }
        }
        private void init(Player player)
        {
            id = player.PlayerId;
            username = player.UserName;
            status = (Status)player.Status;

            Wins = 0;
            Loses = 0;
            Score = 0;
            using (var ctx = new FIARDBContext())
            {
                var allGames = (from g in ctx.Games
                                where (g.Player_PlayerId == player.PlayerId || g.PlayedAgainst_PlayerId == player.PlayerId) &&
                                      g.GameOver == true
                                select g).ToList();
                foreach (var game in allGames)
                {
                    if (game.Player1.UserName == player.UserName)
                    {
                        this.Score += game.Player1Points;
                        if (game.Player1Points > game.Player2Points)
                            this.Wins++;
                        else
                            this.Loses++;
                    }

                    else
                    {
                        this.Score += game.Player2Points;
                        if (game.Player2Points > game.Player1Points)
                            this.Wins++;
                        else
                            this.Loses++;
                    }


                }
                this.Games = allGames.Count;
            }

        }
        [DataMember] public int id { get; set; }
        [DataMember] public string username { get; set; }
        [DataMember] public int Wins { get; set; }
        private Status status;

        [DataMember]
        public Status Status
        {
            get { return status; }
            set { status = ChangeStatus(value); }
        }

        [DataMember] public int Loses { get; set; }
        [DataMember] public int Games { get; set; }
        [DataMember] public int Score { get; set; }

        [DataMember]
        public List<string> PlayedAgainst
        {
            get { return GenertatePlayedAgainstList(); }
            set { }
        }

        private List<string> GenertatePlayedAgainstList()
        {
            using (var ctx = new FIARDBContext())
            {
                var allGames = (from g in ctx.Games
                                where (g.Player_PlayerId == this.id || g.PlayedAgainst_PlayerId == this.id) &&
                                      g.GameOver == true
                                select g).ToList();
                var playedAgainst = new List<string>();
                foreach (var game in allGames)
                {
                    var gi = new GameInfo(game);

                    playedAgainst.Add(gi.getPlayerStats(id));
                }

                return playedAgainst;
            }

        }
        private Status ChangeStatus(Status s)
        {

            using (var ctx = new FIARDBContext())
            {
                var player = (from pl in ctx.Players
                              where pl.UserName == username
                              select pl).FirstOrDefault();
                player.Status = (int)s;
                ctx.SaveChanges();
            }

            return s;

        }

        public void Refresh()
        {
            init(username);
        }

        public override bool Equals(object obj)
        {
            if (obj != null)
                return this.id == ((PlayerInfo)obj).id;
            return false;
        }

        public override int GetHashCode()
        {
            return 15235000;
        }
    }

}
