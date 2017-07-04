using ChessDotNet;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Alopyx.Antichess.Neural
{
    public class AntichessNetwork
    {
        Network net;

        const int LAYER_IN = 2 + 64 * 12;
        const int LAYER_OUT = 64 + 64 + 5;
        const double LEARNING_RATE = 0.1;

        Dictionary<char, int> offsetInput = new Dictionary<char, int>()
        {
            { 'P', 0 },
            { 'N', 1 },
            { 'B', 2 },
            { 'R', 3 },
            { 'Q', 4 },
            { 'K', 5 },
            { 'p', 6 },
            { 'n', 7 },
            { 'b', 8 },
            { 'r', 9 },
            { 'q', 10 },
            { 'k', 11 }
        };

        Dictionary<char, int> promotionOffsetOutput = new Dictionary<char, int>()
        {
            { 'N', 0 },
            { 'B', 1 },
            { 'R', 2 },
            { 'Q', 3 },
            { 'K', 4 }
        };

        public AntichessNetwork()
        {
            net = new Network();
            Layer l = new Layer(LAYER_IN, LAYER_OUT, 0.1);
            /*
             * 2 + 64*12: whose turn (2) + squares with pieces (64 * 12) (P, N, B, R, Q, K, p, n, b, r, q, k)
             * 64 + 64 + 5: from (64) + to (64) + possible promoton (5) */
            net.AddLayer(l);
        }

        public AntichessNetwork(Matrix init)
        {
            net = new Network();
            Layer l = new Layer(LAYER_IN, LAYER_OUT, 0.1, init);
            net.AddLayer(l);
        }

        double[] BoardToInputs(Piece[][] board, Player whoseTurn)
        {
            double[] inputList = new double[LAYER_IN];
            for (int i = 0; i < inputList.Length; i++) inputList[i] = 0.01;
            inputList[whoseTurn == Player.White ? 0 : 1] = 0.99;

            List<Piece> flatBoard = board.SelectMany(x => x).ToList();
            for (int i = 2; i < inputList.Length; i += 12)
            {
                if (flatBoard[(i - 2) / 12] != null)
                {
                    inputList[i + offsetInput[flatBoard[(i - 2) / 12].GetFenCharacter()]] = 0.99;
                }
            }
            return inputList;
        }

        int SquareToOffset(Position square)
        {
            return (square.Rank - 1) * 8 + (int)square.File;
        }

        public void Train(Piece[][] board, Player whoseTurn, Move move)
        {
            double[] inputList = BoardToInputs(board, whoseTurn);

            double[] targetList = new double[LAYER_OUT];
            for (int i = 0; i < targetList.Length; i++) targetList[i] = 0.01;
            targetList[SquareToOffset(move.OriginalPosition)] = 0.99;
            targetList[SquareToOffset(move.NewPosition) + 64] = 0.99;
            if (move.Promotion.HasValue)
            {
                targetList[64 + 64 + promotionOffsetOutput[char.ToUpperInvariant(move.Promotion.Value)]] = 0.99;
            }

            net.Train(inputList, targetList);
        }

        public Move Query(ChessGame game, Player whoseTurn, IEnumerable<Move> avoid)
        {
            ReadOnlyCollection<Move> allValidMoves = game.GetValidMoves(whoseTurn);
            if (allValidMoves.Count == 1) return allValidMoves.Single();
            if (allValidMoves.Count == 0) return null;
            
            Matrix output = net.Query(BoardToInputs(game.GetBoard(), whoseTurn));

            int bestPromotionOffset = 0;
            double bestPromotion = output.Get(64 + 64 + 1, 1);
            for (int i = 1; i < 5; i++)
            {
                double currPromotion = output.Get(64 + 64 + 1 + i, 1);
                if (currPromotion > bestPromotion)
                {
                    bestPromotionOffset = i;
                    bestPromotion = currPromotion;
                }
            }
            IEnumerable<Move> nonTraps = allValidMoves.Except(avoid);
            if (!nonTraps.Any()) nonTraps = allValidMoves;
            IEnumerable<Move> movesWithSinglePromotionOption = nonTraps.Where(x => !x.Promotion.HasValue || promotionOffsetOutput[x.Promotion.Value] == bestPromotionOffset);
            IEnumerable<Tuple<Move, double>> sortedMovesWithWeight = movesWithSinglePromotionOption.Select(
                x => new Tuple<Move, double>(
                    x,
                    output.Get(1 + SquareToOffset(x.OriginalPosition), 1) + output.Get(1 + 64 + SquareToOffset(x.NewPosition), 1)
                )
            ).OrderByDescending(x => x.Item2);
            return sortedMovesWithWeight.First().Item1;
        }

        public Matrix GetMatrix()
        {
            return net.GetLayer(0).Weights;
        }
    }
}
