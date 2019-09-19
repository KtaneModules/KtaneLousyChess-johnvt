using Assets;
using Goals;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Engines
{
    public abstract class Engine
    {
        public abstract char Letter { get; }
        public abstract string Name { get; }
        public abstract Goal[] Goals { get; }
        public Player Color;
        public int Seed;
        public string Random;

        public static List<Engine> GetAllRandom()
        {
            List<Engine> engines = new List<Engine>()
            {
                new DarkSquaresAreLava(),
                new TheKingMustDie(),
                new LetsSwitchSides(),
                new MirrorMirror(),
                new LightSquaresAreLava()
            };
            return engines.Shuffle();
        }

        public void Init(Player color, int seed, string bombNumber)
        {
            Color = color;
            Seed = seed;
            var prev = seed;
            var random = new StringBuilder();
            random.Append(seed);
            for (var i = 0; i < 40; i++)
            {
                var next = (prev + int.Parse(bombNumber[i % bombNumber.Length].ToString())) % 10;
                random.Append(next);
                prev = next;
            }
            Random = random.ToString();
        }

        public Move Move(Game game)
        {
            var validMoves = game.GetValidMoves(Color);

            // No valid moves left??
            if (validMoves.Count() == 0) return null;

            // Try each goal
            foreach (Goal goal in Goals)
            {
                var moves = goal.Filter(validMoves.ToList(), game);
                if (moves.Count > 0)
                {
                    return GetRandomMove(moves, game);
                }
            }

            // No goal can be met? Pick any valid move.
            return GetRandomMove(validMoves.ToList(), game);
        }

        private Move GetRandomMove(List<Move> moves, Game game)
        {
            var rnd = Random[game.CurrentMove / 2 + 1];
            var prevRnd = Random[game.CurrentMove / 2];

            // Sort moves by square name, asc if prev=even, desc if prev=odd
            if (prevRnd % 2 == 0)
                moves.Sort((a, b) => string.Compare(a.GetName(), b.GetName()));
            else
                moves.Sort((a, b) => string.Compare(b.GetName(), a.GetName()));

            int index = (rnd - '0') % moves.Count;

            /*Debug.Log("Move " + (game.CurrentMove / 2 + 1)
                + " for " + game.Turn
                + ", rnd=" + rnd
                + ", prevRnd=" + prevRnd
                + ", possible moves: " + string.Join(", ", moves.ConvertAll(m => m.GetName()).ToArray())
                + ", move=" + moves[index].GetName()
                );*/

            return moves[index];
        }
    }

    class DarkSquaresAreLava : Engine
    {
        public override char Letter
        {
            get { return 'D'; }
        }

        public override string Name
        {
            get { return "Dark squares are lava"; }
        }

        public override Goal[] Goals
        {
            get
            {
                return new Goal[]
                {
                    new CaptureKing(),
                    new MoveFromBlackToWhiteTile(),
                    new StayOnWhiteTile()
                };
            }
        }
    }

    class TheKingMustDie : Engine
    {
        public override char Letter
        {
            get { return 'K'; }
        }

        public override string Name
        {
            get { return "The king must die"; }
        }

        public override Goal[] Goals
        {
            get
            {
                return new Goal[]
                {
                    new CaptureKing(),
                    new MoveCloserToKing()
                };
            }
        }
    }

    class MirrorMirror : Engine
    {
        public override char Letter
        {
            get { return 'M'; }
        }

        public override string Name
        {
            get { return "Mirror, mirror"; }
        }

        public override Goal[] Goals
        {
            get
            {
                return new Goal[]
                {
                    new CaptureKing(),
                    new MirrorLastMove(),
                    new MoveSamePiece()
                };
            }
        }
    }

    class LetsSwitchSides : Engine
    {
        public override char Letter
        {
            get { return 'S'; }
        }

        public override string Name
        {
            get { return "Let’s switch sides"; }
        }

        public override Goal[] Goals
        {
            get
            {
                return new Goal[]
                {
                    new CaptureKing(),
                    new MoveCloserToEnemySetup()
                };
            }
        }
    }

    class LightSquaresAreLava : Engine
    {
        public override char Letter
        {
            get { return 'L'; }
        }

        public override string Name
        {
            get { return "Light squares are lava"; }
        }

        public override Goal[] Goals
        {
            get
            {
                return new Goal[]
                {
                    new CaptureKing(),
                    new MoveFromWhiteToBlackTile(),
                    new StayOnBlackTile()
                };
            }
        }
    }
}
