using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace WcfFIARService
{
    enum PlayerColor { Empty, Blue, Red };


    class GameBoard
    {
        private PlayerColor[,] board;
        private PlayerInfo player1;
        private PlayerInfo player2;
        private bool p1Connected;
        public bool p2Connected;
        private bool turnPlayer1; //TODO: MAYBE MAKE ENUM

        public GameBoard(string p1, string p2)
        {

            this.player1 = new PlayerInfo(p1);
            this.player2 = new PlayerInfo(p2);
            p1Connected = true;
            p2Connected = true;
            board = new PlayerColor[7, 6];
            turnPlayer1 = true;
            using (var ctx = new FIARDBContext())
            {
                var g = new Game();
                g.Player_PlayerId = player1.id;
                g.PlayedAgainst_PlayerId = player2.id;
                g.GameStart = DateTime.Now;

                ctx.Games.Add(g);
                ctx.SaveChanges();
                Game g2 = g;
            }
        }

        public MoveResult VerifyMove(string player, int col)
        {
            return MoveResult.GameOn;
        }


        // will throw 
        public bool insertDisk(int col, PlayerColor pc)
        {
            int row = getEmptyInCol(col);
            if (row == -1) //  ??
            {
                IlligaleMove illigalMove = new IlligaleMove
                {
                    Details = "Illigal Move"
                };
                throw new FaultException<IlligaleMove>(illigalMove, new FaultReason("Player made Incorrect move"));
                //need to put on top of the function as [FaultContract(typeof(PlayerAlreadyConnectedFault))] in interface
            }

            board[col, row] = pc;
            return checkIfGameOver(col, row);

        }

        private int getEmptyInCol(int col)
        {
            for (int i = 0; i < 6; i++)
                if (board[col, i] == PlayerColor.Empty)
                    return i;
            return -1;
        }

        private bool checkIfGameOver(int col, int row)
        {
            var leftDig = advInDirection(board[col, row], col, row, 4, -1, -1);
            var leftDownDig = advInDirection(board[col, row], col, row, 4, -1, -1);// added 
            var rightDig = advInDirection(board[col, row], col, row, 4, 1, -1);
            var rightDownDig = advInDirection(board[col, row], col, row, 4, 1, 1);// added
            var down = advInDirection(board[col, row], col, row, 4, 0, -1);

            return leftDig || rightDig || down || leftDownDig || rightDownDig;
        }

        private bool advInDirection(PlayerColor pc, int col, int row, int count, int directionX, int directionY)
        {
            if (col < 0 || col > 6 || row < 0 || row > 5 || count < 1)
                return false;
            if (board[col, row] != pc)
                return false;
            if (count == 1)
                return true;
            return advInDirection(pc, col + directionX, row + directionY, count - 1, directionX, directionY);
        }

        public bool CheckIfPlayerInGame(string username)
        {
            return player1.username == username || player2.username == username;
        }

        public void playerDiscounnected(string username)
        {
            if (username == player1.username)
                p1Connected = false;
            if (username == player2.username)
                p2Connected = false;
        }

    }
}
