﻿using System;
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

    /// <summary>
    /// this class handles all games
    /// i
    /// </summary>
    class GameBoard
    {
        public Game game { get; }
        private PlayerColor[,] board;
        public PlayerInfo player1 { get; }
        public PlayerInfo player2 { get; }
        private bool turnPlayer1;

        /// <summary>
        /// creates game of given players in database and in host
        /// </summary>
        /// <param name="player1"></param>
        /// <param name="player2"></param>
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


        /// <summary>
        /// gets where the disk inserted and player who made the move and verifies if its a win, draw, wrong move, or correct move
        /// </summary>
        /// <param name="player"></param>
        /// <param name="col"></param>
        /// <returns></returns>
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



        /// <summary>
        /// cauclates the points of each the players updates the database according the winner or draw
        /// </summary>
        /// <param name="player"></param>
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
            else // there is a winner!
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


        /// <summary>
        /// checks if board is filled by colors red and yellow only
        /// </summary>
        /// <returns></returns>
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

            var pc = board[col, row];


            var dig_left_up = AdvInDirection(pc, col, row, -1, -1);
            var dig_left_down = AdvInDirection(pc, col, row, 1, 1);

            var dig_left = dig_left_up + dig_left_down - 1;

            var dig_right_up = AdvInDirection(pc, col, row, -1, 1);
            var dig_right_down = AdvInDirection(pc, col, row, 1, -1);

            var dig_right = dig_right_up + dig_right_down - 1;

            var horz_left = AdvInDirection(pc, col, row, -1, 0);
            var horz_right = AdvInDirection(pc, col, row, 1, 0);

            var horz = horz_left + horz_right - 1;

            var down = AdvInDirection(pc, col, row, 0, -1);

            return ((dig_left >= 4) || (dig_right >= 4) || (horz >= 4) || (down >= 4));
        }



        /// <summary>
        /// recursion function to check given direction and count if shows same color given count
        /// </summary>
        /// <param name="pc"></param>
        /// <param name="col"></param>
        /// <param name="row"></param>
        /// <param name="count"></param>
        /// <param name="directionX"></param>
        /// <param name="directionY"></param>
        /// <returns></returns>
        private int AdvInDirection(PlayerColor pc, int col, int row, int directionCol, int directionRow)
        {
            if (col < 0 || col > 6 || row < 0 || row > 5)
                return 0;
            if (board[col, row] != pc)
                return 0;

            return 1 + AdvInDirection(pc, col + directionCol, row + directionRow, directionCol, directionRow);
        }


        /// <summary>
        /// given username return true if player is in the game 
        /// else returns false
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public bool CheckIfPlayerInGame(string username)
        {
            return player1.username == username || player2.username == username;
        }

        /// <summary>
        /// sets the username as loser and ends the game
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        public PlayerInfo PlayerDisconnected(string username)
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
            else
                return null;
            winner.Status = Status.Online;
            loser.Status = Status.Online;
            EndGame(winner.username);
            return winner;
        }


        /// <summary>
        /// caucaltes losers points
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
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


        /// <summary>
        /// checks if all columns are filled by given username
        /// and returns 100 if true 
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
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

        public override bool Equals(object obj)
        {
            var other = obj as GameBoard;
            return other.game.GameId == this.game.GameId;
        }
    }
}
