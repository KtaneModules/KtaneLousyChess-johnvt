using Engines;
using System;
using System.Collections.Generic;
using System.Text;

namespace Assets
{
    public enum Player { White = -1, Black = 1, None = 0 }

    public enum NextAction { None, WhiteFrom, WhiteTo, BlackFrom, BlackTo }

    public enum Capture { Yes, No, Only }

    public class Game
    {
        // Let's make each pawn uniquely identifiable, for easier UpdateDisplay
        public const string STARTING_BOARD = "kqbnr56789..........01234RNBQK";

        // For promoted pawns, we'll create a bunch of hidden queens that we can enable when needed,
        // ASCII -15, so 0123456789 becomes !"#$%&'()*

        public StringBuilder Board = new StringBuilder(STARTING_BOARD);
        public List<string> Boards = new List<string>();
        public Player Turn = Player.White;
        public Player Winner = Player.None;
        public Engine WhiteEngine;
        public Engine BlackEngine;
        public Move Move = new Move();
        public List<Move> Moves = new List<Move>();
        public string BombNumber;
        public int CurrentMove;

        public NextAction NextAction = NextAction.WhiteFrom;

        public void Run()
        {
            Boards.Add(Board.ToString());

            // Take turns until game is finished
            while (true)
            {
                Move move = (Turn == Player.White)
                    ? WhiteEngine.Move(this)
                    : BlackEngine.Move(this);

                // No valid moves left, other player wins
                if (move == null)
                {
                    Winner = (Turn == Player.White) ? Player.Black : Player.White;
                    break;
                }

                // Valid move
                Moves.Add(move);

                // King captured? Game over
                if (Piece.IsBlackKing(Board[move.To]))
                    Winner = Player.White;
                else if (Piece.IsWhiteKing(Board[move.To]))
                    Winner = Player.Black;

                Board[move.To] = Board[move.From];
                Board[move.From] = '.';

                // Is the game finished?
                if (Winner != Player.None)
                {
                    Boards.Add(Board.ToString());
                    break;
                }

                // Pawn promotion
                if (GetXy(move.To)[1] == 0 && Piece.IsWhitePawn(Board[move.To]))
                    Board[move.To] = (char)(Board[move.To] - 15);
                else if (GetXy(move.To)[1] == 5 && Piece.IsBlackPawn(Board[move.To]))
                    Board[move.To] = (char)(Board[move.To] - 15);

                Boards.Add(Board.ToString());

                // Draw by 40 moves by each side
                if (Turn == Player.Black && CurrentMove == 79)
                    break;

                Turn = (Turn == Player.White) ? Player.Black : Player.White;
                CurrentMove++;
            }
        }

        public int GetDistance(int from, int to)
        {
            var fromXy = GetXy(from);
            var toXy = GetXy(to);
            return Math.Abs(fromXy[0] - toXy[0]) + Math.Abs(fromXy[1] - toXy[1]);
        }

        public IEnumerable<Move> GetValidMoves(Player player)
        {
            var moves = new List<Move>();

            // Scan all squares
            for (int i = 0; i < Board.Length; i++)
            {
                char piece = Board[i];

                // If it's empty, or from the enemy, continue
                if (Piece.IsNoPiece(piece)) continue;
                if (Piece.GetColor(piece) != Turn) continue;

                int x = i % 5;
                int y = i / 5;
                switch (Piece.GetType(piece))
                {
                    case Piece.Type.King:
                        moves.AddRange(ScanAllDirections(x, y, 0, 1, true));
                        moves.AddRange(ScanAllDirections(x, y, 1, 1, true));
                        break;
                    case Piece.Type.Queen:
                        moves.AddRange(ScanAllDirections(x, y, 0, 1));
                        moves.AddRange(ScanAllDirections(x, y, 1, 1));
                        break;
                    case Piece.Type.Rook:
                        moves.AddRange(ScanAllDirections(x, y, 0, 1));
                        break;
                    case Piece.Type.Bishop:
                        // Extra MinitChess bishop move:
                        moves.AddRange(ScanAllDirections(x, y, 0, 1, true, Capture.No));
                        moves.AddRange(ScanAllDirections(x, y, 1, 1));
                        break;
                    case Piece.Type.Knight:
                        moves.AddRange(ScanAllDirections(x, y, 1, 2, true));
                        moves.AddRange(ScanAllDirections(x, y, -1, 2, true));
                        break;
                    case Piece.Type.Pawn:
                        moves.AddRange(Scan(x, y, -1, (int)Piece.GetColor(piece), true, Capture.Only));
                        moves.AddRange(Scan(x, y, 1, (int)Piece.GetColor(piece), true, Capture.Only));
                        moves.AddRange(Scan(x, y, 0, (int)Piece.GetColor(piece), true, Capture.No));
                        break;
                }
            }

            return moves;
        }

        private IEnumerable<Move> ScanAllDirections(int x, int y, int dx, int dy, bool lastStep = false, Capture capture = Capture.Yes)
        {
            var moves = new List<Move>();
            for (var i = 0; i < 4; i++)
            {
                moves.AddRange(Scan(x, y, dx, dy, lastStep, capture));

                // Exchange dx with dy and negate dy
                var temp = dx;
                dx = dy;
                dy = -temp;
            }

            return moves;
        }

        private IEnumerable<Move> Scan(int x, int y, int dx, int dy, bool lastStep, Capture capture = Capture.Yes)
        {
            var x0 = x;
            var y0 = y;
            var color = Piece.GetColor(PieceAt(x, y));
            var moves = new List<Move>();
            while (true)
            {

                // Take a step
                x += dx;
                y += dy;

                // Out of bounds
                if (x < 0 || x > 4 || y < 0 || y > 5)
                {
                    break;
                }

                var piece = PieceAt(x, y);

                // There is a piece
                if (Piece.IsPiece(piece))
                {
                    // Own piece
                    if (Piece.GetColor(piece) == color)
                    {
                        break;
                    }

                    // Other piece but we may not capture
                    if (capture == Capture.No)
                    {
                        break;
                    }

                    // We may capture, the piece cannot jump over it
                    lastStep = true;
                }

                // No piece, but we may only capture
                else if (capture == Capture.Only)
                {
                    break;
                }

                // Add move
                moves.Add(new Move() { From = GetIndex(x0, y0), To = GetIndex(x, y) });

                // Stop if it's the last step
                if (lastStep) break;
            }

            return moves;
        }

        public int GetIndex(int x, int y) { return x + y * 5; }

        public char PieceAt(int x, int y) { return Board[x + y * 5]; }

        public int[] GetXy(int i) { return new int[] { i % 5, i / 5 }; }

        public string SquareName(int i) {
            return ((char)((int)'a' + i % 5)).ToString() + (6 - i / 5).ToString();
        }
    }

    class Piece
    {
        public enum Type { None, King, Queen, Rook, Bishop, Knight, Pawn }
        public static bool IsWhite(char c) { return "01234RNBQ!\"#$%K".Contains(c.ToString()); }
        public static bool IsBlack(char c) { return "kq&'()*bnr56789".Contains(c.ToString()); }
        public static bool IsNoPiece(char c) { return c == '.'; }
        public static bool IsPiece(char c) { return c != '.'; }
        public static Player GetColor(char c) { return IsWhite(c) ? Player.White : (IsBlack(c) ? Player.Black : Player.None); }
        public static bool IsKing(char c) { return "Kk".Contains(c.ToString()); }
        public static bool IsQueen(char c) { return "Qq!\"#$%&'()*".Contains(c.ToString()); }
        public static bool IsRook(char c) { return "Rr".Contains(c.ToString()); }
        public static bool IsBishop(char c) { return "Bb".Contains(c.ToString()); }
        public static bool IsKnight(char c) { return "Nn".Contains(c.ToString()); }
        public static bool IsPawn(char c) { return "0123456789".Contains(c.ToString()); }
        public static bool IsWhiteKing(char c) { return c == 'K'; }
        public static bool IsBlackKing(char c) { return c == 'k'; }
        public static bool IsWhiteQueen(char c) { return "Q!\"#$%".Contains(c.ToString()); }
        public static bool IsBlackQueen(char c) { return "q&'()*".Contains(c.ToString()); }
        public static bool IsWhiteRook(char c) { return c == 'R'; }
        public static bool IsBlackRook(char c) { return c == 'r'; }
        public static bool IsWhiteBishop(char c) { return c == 'B'; }
        public static bool IsBlackBishop(char c) { return c == 'b'; }
        public static bool IsWhiteKnight(char c) { return c == 'N'; }
        public static bool IsBlackKnight(char c) { return c == 'n'; }
        public static bool IsWhitePawn(char c) { return "01234".Contains(c.ToString()); }
        public static bool IsBlackPawn(char c) { return "56789".Contains(c.ToString()); }
        public static Type GetType(char c)
        {
            if ("Kk".Contains(c.ToString())) return Type.King;
            if ("Qq!\"#$%&'()*".Contains(c.ToString())) return Type.Queen;
            if ("Rr".Contains(c.ToString())) return Type.Rook;
            if ("Bb".Contains(c.ToString())) return Type.Bishop;
            if ("Nn".Contains(c.ToString())) return Type.Knight;
            if ("0123456789".Contains(c.ToString())) return Type.Pawn;
            return Type.None;
        }
        public static char GetOtherColor(char c)
        {
            if (IsPawn(c)) return '\0';
            if (char.ToLower(c) == c) return char.ToUpper(c);
            return char.ToLower(c);
        }
    }

    public class Move
    {
        public int From;
        public int To;

        public string GetName()
        {
            return ((char)((int)'a' + From % 5)).ToString()
                + (6 - From / 5).ToString()
                + "-"
                + ((char)((int)'a' + To % 5)).ToString()
                + (6 - To / 5).ToString();
        }
    }
}