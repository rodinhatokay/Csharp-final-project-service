using System;
using System.Collections.Generic;
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
        private int player1;
        private int player2;

        public GameBoard(int player1, int player2)
        {
            this.player1 = player1;
            this.player2 = player2;

            board = new PlayerColor[7, 6];
        }

        public MoveResult VerifyMove(string player, int col)
        {
            return MoveResult.Draw;
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
    }
}
