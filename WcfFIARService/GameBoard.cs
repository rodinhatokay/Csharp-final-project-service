using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Globalization;
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
        public Game game { get; }
        private PlayerColor[,] board;
        public PlayerInfo player1 { get; }
        public PlayerInfo player2 { get; }
        private bool turnPlayer1;




        public GameBoard(PlayerInfo player1, PlayerInfo player2)
        {

            this.player1 = player1;
            this.player2 = player2;



            player1.Status = Status.Playing;
            player2.Status = Status.Playing;

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
                game = g;
            }


        }

        public MoveResult VerifyMove(string player, int col)
        {
            //need to check if all filled to make draw
            //if move result == you won its better to update here database instead outside.


            if (turnPlayer1 && player1.username != player || !turnPlayer1 && player2.username != player)
                return MoveResult.NotYourTurn;
            int row = getEmptyInCol(col);
            if (row != -1)
            {
                board[col, row] = turnPlayer1 ? PlayerColor.Red : PlayerColor.Blue;
                if (CheckIfGameOver(col, row))
                {
                    EndGame(player);
                    return MoveResult.YouWon;
                }
                if (AllfilledbyPlayers())
                {
                    EndGame(null);
                    return MoveResult.Draw;
                }
                turnPlayer1 = !turnPlayer1;
                return MoveResult.GameOn;
            }

            return MoveResult.IlligelMove;
        }


        private void EndGame(string player) // draw or the player made move won
        {
            if (player == null)// its a draw 
            {
                using (var ctx = new FIARDBContext())
                {
                    var game = (from g in ctx.Games
                                where g.GameId == this.game.GameId
                                select g).FirstOrDefault();
                    game.GameOver = true;
                    game.Winner_PlayerId = null;
                    game.Player2Points = calcLoserPoints(player2.username) + checkIfAllCollsFilled(player2.username);
                    game.Player1Points = calcLoserPoints(player1.username) + checkIfAllCollsFilled(player1.username);
                    ctx.SaveChanges();
                }
            }
            else // ther is a winner!
            {
                using (var ctx = new FIARDBContext())
                {
                    var game = (from g in ctx.Games
                                where g.GameId == this.game.GameId
                                select g).FirstOrDefault();

                    game.GameOver = true;
                    if (player == player1.username)
                    {
                        game.Winner_PlayerId = player1.id;
                        game.Player1Points = 1000 + checkIfAllCollsFilled(player1.username);
                        game.Player2Points = calcLoserPoints(player2.username) + checkIfAllCollsFilled(player1.username);
                    }
                    else
                    {
                        game.Winner_PlayerId = player2.id;
                        game.Player2Points = 1000 + checkIfAllCollsFilled(player2.username);
                        game.Player1Points = calcLoserPoints(player1.username) + checkIfAllCollsFilled(player1.username);
                    }
                    ctx.SaveChanges();
                }
            }
        }

        private int getEmptyInCol(int col)
        {
            for (int i = 0; i < 6; i++)
                if (board[col, i] == PlayerColor.Empty)
                    return i;
            return -1;
        }

        private bool AllfilledbyPlayers()
        {
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (board[j, i] == PlayerColor.Empty)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        private bool CheckIfGameOver(int col, int row)
        {
            var leftDig = advInDirection(board[col, row], col, row, 4, -1, -1);
            var left = advInDirection(board[col, row], col, row, 4, -1, 0);
            var down = advInDirection(board[col, row], col, row, 4, 0, -1);
            var right = advInDirection(board[col, row], col, row, 4, 1, 0);
            var rightDig = advInDirection(board[col, row], col, row, 4, 1, -1);

            return leftDig || rightDig || down || right || left;
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

        public void PlayerDisconnected(string username)
        {
            PlayerInfo winner;
            PlayerInfo loser;
            if (username == player1.username)
            {
                winner = player2;
                loser = player1;
            }
            else if (username == player2.username)
            {
                winner = player1;
                loser = player2;
            }
            else return;
            winner.Status = Status.Online;
            loser.Status = Status.Online;
            EndGame(winner.username);
            winner.Callback.OtherPlayerDisconnected();
        }


        public int calcLoserPoints(string username)
        {
            PlayerColor c = (player1.username == username) ? PlayerColor.Red : PlayerColor.Blue;
            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (board[j, i] == c)
                    {
                        count++;
                    }
                }

            }

            return count * 10;
        }

        public int checkIfAllCollsFilled(string username)
        {
            PlayerColor c = (player1.username == username) ? PlayerColor.Red : PlayerColor.Blue;
            int count = 0;
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 7; j++)
                {
                    if (board[j, i] == c)
                    {
                        count++;
                        break;
                    }
                }

            }

            return count == 7 ? 100 : 0;

        }

        public override bool Equals(object obj) // im not sure if this is correct
        {
            var other = obj as GameBoard;
            return other.game.GameId == this.game.GameId;
        }
    }
}
