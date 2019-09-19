using Assets;
using System.Collections.Generic;
using System.Linq;

namespace Goals
{
    public abstract class Goal
    {
        abstract public List<Move> Filter(List<Move> moves, Game game);
    }

    public class CaptureKing : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move => Piece.IsKing(game.Board[move.To])).ToList();
        }
    }

    public class MirrorLastMove : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            if (game.Moves.Count == 0) return new List<Move>();

            var prevMove = game.Moves[game.Moves.Count - 1];
            var movePiece = Piece.GetType(game.Board[prevMove.To]);
            var prevMoveFromXy = game.GetXy(prevMove.From);
            var prevMoveToXy = game.GetXy(prevMove.To);
            var moveFromIndex = game.GetIndex(4 - prevMoveFromXy[0], 5 - prevMoveFromXy[1]);
            var moveToPos = game.GetIndex(4 - prevMoveToXy[0], 5 - prevMoveToXy[1]);

            return moves.Where(move =>
            {
                return Piece.GetType(game.Board[move.From]) == movePiece
                && move.From == moveFromIndex
                && move.To == moveToPos;
            }).ToList();
        }
    }

    public class MoveCloserToEnemySetup : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move =>
            {
                var piece = game.Board[move.From];
                int enemySquare;
                if (Piece.IsPawn(piece))
                {
                    enemySquare = game.GetIndex(
                        game.GetXy(move.From)[0],
                        Piece.IsBlack(piece) ? 4 : 1
                        );
                }
                else
                {
                    enemySquare = Game.STARTING_BOARD.IndexOf(Piece.GetOtherColor(piece));
                }

                var oldDistance = game.GetDistance(move.From, enemySquare);
                var newDistance = game.GetDistance(move.To, enemySquare);
                return newDistance < oldDistance;
            }).ToList();
        }
    }

    public class MoveCloserToKing : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            var enemyKing = (game.Turn == Player.White) ? 'k' : 'K';
            var enemyKingSquare = game.Board.ToString().IndexOf(enemyKing);
            return moves.Where(move =>
            {
                var oldDistance = game.GetDistance(move.From, enemyKingSquare);
                var newDistance = game.GetDistance(move.To, enemyKingSquare);
                return newDistance < oldDistance;
            }).ToList();
        }
    }

    public class MoveFromBlackToWhiteTile : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move =>
            {
                return (move.From % 2 == 1) && (move.To % 2 == 0);
            }).ToList();
        }
    }

    public class MoveFromWhiteToBlackTile : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move =>
            {
                return (move.From % 2 == 0) && (move.To % 2 == 1);
            }).ToList();
        }
    }

    public class MoveSamePiece : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            if (game.Moves.Count == 0) return new List<Move>();

            var prevMove = game.Moves[game.Moves.Count - 1];
            var prevPiece = Piece.GetType(game.Board[prevMove.To]);

            return moves.Where(move =>
            {
                return Piece.GetType(game.Board[move.From]) == prevPiece;
            }).ToList();
        }
    }

    public class StayOnBlackTile : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move =>
            {
                return (move.From % 2 == 1) && (move.To % 2 == 1);
            }).ToList();
        }
    }

    public class StayOnWhiteTile : Goal
    {
        public override List<Move> Filter(List<Move> moves, Game game)
        {
            return moves.Where(move =>
            {
                return (move.From % 2 == 0) && (move.To % 2 == 0);
            }).ToList();
        }
    }
}
