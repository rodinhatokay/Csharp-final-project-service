using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace WcfFIARService
{
    [DataContract]
    public class GameInfo
    {
        public GameInfo(Game g)
        {
            this.GameId = g.GameId;
            this.GameOver = g.GameOver;
            this.Player1_id = g.Player1.PlayerId;
            this.Player2_id = g.Player2.PlayerId;
            this.Player1_name = g.Player1.UserName;
            this.Player2_name = g.Player2.UserName;
            this.Winner_id = g.Winner != null ? g.Winner.PlayerId : 0;
            this.Winner = Winner_id == 0 ? "" : this.Winner_id == this.Player1_id ? this.Player1_name : this.Player2_name;
            this.WinnerScore = Winner_id == 0 ? "" : this.Winner_id == this.Player1_id ? this.Player1Points.ToString() : this.Player2Points.ToString();
            this.Player1Points = g.Player1Points;
            this.Player2Points = g.Player2Points;
            this.GameStartTime = g.GameStart.ToString("t");
            this.GameStartDate = g.GameStart.ToString("d");
        }


        [DataMember]
        public string Winner { get; set; }
        [DataMember]
        public string WinnerScore { get; set; }
        [DataMember]
        public int GameId { get; set; }
        [DataMember]
        public bool GameOver { get; set; }
        [DataMember]
        public int Player1_id { get; set; }
        [DataMember]
        public int Player2_id { get; set; }
        [DataMember]
        public string Player1_name { get;  set; }
        [DataMember]
        public string Player2_name { get; set; }
        [DataMember]
        public int Winner_id { get; set; }


        [DataMember]
        public int Player1Points { get; set; }
        [DataMember]
        public int Player2Points { get; set; }
        
        [DataMember]
        public string GameStartTime { get; set; }
        [DataMember]
        public string GameStartDate { get;  set; }

        public string getPlayerStats(int id)
        {
            var player1 = new PlayerInfo(Player1_id);
            var player2 = new PlayerInfo(Player2_id);

            if (id == Player1_id)
            {
                string str = "played against " + player2.username + " and ";
                if (Winner_id == Player1_id)
                    str += "won";
                else if (Winner_id == Player2_id)
                    str += "lost";
                else
                    str += "draw";
                return str;

            }
            else
            {
                string str = "played against " + player1.username + " and ";
                if (Winner_id == Player2_id)
                    str += "won";
                else if (Winner_id == Player1_id)
                    str += "lost";
                else
                    str += "draw";

                return str;
            }


        }
    }
}
