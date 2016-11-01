using ChessDotNet;
using System.Collections.ObjectModel;

namespace Alopyx.Antichess
{
    public class MoveWithMetadata
    {
        public Move Move { get; set; }
        public DetailedMove Detailed { get; set; }
        public ReadOnlyCollection<Move> Options { get; set; }
        public int AllowsXOptions { get; set; }
        public int MobilityScore { get; set; }
        public double Score
        {
            get
            {
                return AllowsXOptions + (1 - MobilityScore / 100f);
            }
        }

        public MoveWithMetadata(Move move, DetailedMove detailed, ReadOnlyCollection<Move> options, int optionsAllowed, int mobilityScore)
        {
            Move = move;
            Detailed = detailed;
            Options = options;
            AllowsXOptions = optionsAllowed;
            MobilityScore = mobilityScore;
        }
    }
}
