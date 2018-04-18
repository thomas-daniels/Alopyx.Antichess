using Alopyx.Antichess.Neural;
using ChessDotNet;
using ChessDotNet.Variants.Antichess;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Alopyx.Antichess
{
    public class Engine
    {
        AntichessGame game;
        AntichessNetwork net;
        public AntichessGame Game { get { return game; } }
        List<MoveWithMetadata> valid = new List<MoveWithMetadata>();

        public Engine(string netMatrixPath)
        {
            game = new AntichessGame();

            Matrix init = Matrix.Load(netMatrixPath);
            net = new AntichessNetwork(init);
        }

        public Engine(AntichessGame game, AntichessNetwork net)
        {
            this.game = game;
            this.net = net;
        }

        public Move FindBestMove()
        {
            ProcessValidMoves();
            if (valid.Count == 1)
            {
                return valid[0].Move;
            }
            Move forcedWin = FindForcedWin(2);
            // The forced win depth can be increased at the cost of more computation time. For now it's hardcoded to be 1
            // until I implement some better time management that allows Alopyx to make better use of the time it receives.
            if (forcedWin != null) return forcedWin;

            LookForTraps();

            IEnumerable<Move> avoid = valid.Where(x => x.Trap).Select(x => x.Move);

            return net.Query(game, game.WhoseTurn, avoid);
        }

        Move FindForcedWin(int depth)
        {
            foreach (MoveWithMetadata move in valid)
            {
                if (move.AllowsXOptions > depth) continue;
                if (move.AllowsXOptions == 0)
                {
                    return null;
                }
                bool allOptionsWork = true;
                foreach (Move option in move.Options)
                {
                    AntichessGame gameCopy = new AntichessGame(game.GetFen());
                    gameCopy.ApplyMove(move.Move, true);
                    gameCopy.ApplyMove(option, true);
                    if (gameCopy.IsWinner(gameCopy.WhoseTurn))
                    {
                        continue;
                    }
                    Engine engineCopy = new Engine(gameCopy, net);
                    engineCopy.ProcessValidMoves();
                    if (engineCopy.FindForcedWin(depth) == null)
                    {
                        allOptionsWork = false;
                        break;
                    }
                }
                if (allOptionsWork)
                {
                    return move.Move;
                }
            }
            return null;
        }

        void ProcessValidMoves()
        {
            valid.Clear();
            ReadOnlyCollection<Move> validMoves = game.GetValidMoves(game.WhoseTurn);
            foreach (Move move in validMoves)
            {
                AntichessGame copy = new AntichessGame(game.GetFen());
                copy.ApplyMove(move, true);

                DetailedMove detailed = copy.Moves[copy.Moves.Count - 1];
                ReadOnlyCollection<Move> validOptions = copy.GetValidMoves(copy.WhoseTurn);
                int optionsCount = validOptions.Count;

                string[] newFenParts = copy.GetFen().Split(' ');
                newFenParts[1] = newFenParts[1] == "w" ? "b" : "w";
                newFenParts[3] = "-";
                AntichessGame copy2 = new AntichessGame(string.Join(" ", newFenParts));
                int mobilityScore = copy2.GetValidMoves(copy2.WhoseTurn).Count;

                bool trap = false;
                valid.Add(new MoveWithMetadata(move, detailed, validOptions, optionsCount, mobilityScore, trap));
            }
        }

        void LookForTraps()
        {
            foreach (MoveWithMetadata move in valid)
            {
                AntichessGame copy = new AntichessGame(game.GetFen());
                copy.ApplyMove(move.Move, true);
                Engine engineCopy = new Engine(copy, net);
                engineCopy.ProcessValidMoves();
                bool trap = engineCopy.FindForcedWin(1) != null;
                move.Trap = trap;
            }
        }
    }
}
