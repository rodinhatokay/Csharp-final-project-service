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
            this.Winner_id = g.Winner != null ? g.Winner.PlayerId : 0;
            this.Player1Points = g.Player1Points;
            this.Player2Points = g.Player2Points;
        }
        [DataMember]
        public int GameId { get; set; }
        [DataMember]
        public bool GameOver { get; set; }
        [DataMember]
        public int Player1_id { get; set; }
        [DataMember]
        public int Player2_id { get; set; }
        [DataMember]
        public int Winner_id { get; set; }
        [DataMember]
        public int Player1Points { get; set; }
        [DataMember]
        public int Player2Points { get; set; }

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
