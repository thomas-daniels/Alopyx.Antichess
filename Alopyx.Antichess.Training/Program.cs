using Alopyx.Antichess.Neural;
using ChessDotNet;
using ChessDotNet.Variants.Antichess;
using System.Collections.ObjectModel;
using System.IO;

namespace Alopyx.Antichess.Training
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "fics")
            {
                TrainFICSPgn(args[1], args[2]);
            }
        }

        static void TrainFICSPgn(string path, string matrixOutputPath)
        {
            StreamReader sr = new StreamReader(path);
            string line;

            AntichessNetwork net = new AntichessNetwork();

            bool skipThisGame = false;
            string result = null;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (!line.StartsWith("["))
                {
                    if (!skipThisGame)
                    {
                        bool white = result == "1-0";
                        PgnReader<AntichessGame> reader = new PgnReader<AntichessGame>();
                        reader.ReadPgnFromString(line);
                        ReadOnlyCollection<DetailedMove> moves = reader.Game.Moves;

                        AntichessGame replay = new AntichessGame();
                        foreach (DetailedMove dm in moves)
                        {
                            if ((dm.Player == Player.White) == white && replay.GetValidMoves(replay.WhoseTurn).Count > 1)
                            {
                                net.Train(replay.GetBoard(), replay.WhoseTurn, new Move(dm.OriginalPosition, dm.NewPosition, dm.Player, dm.Promotion));
                            }
                            replay.ApplyMove(new Move(dm.OriginalPosition, dm.NewPosition, dm.Player, dm.Promotion), true);
                        }
                    }
                    skipThisGame = false;
                    result = null;
                }
                else
                {
                    if (line.Contains("IsComp") && line.Contains("Yes"))
                    {
                        skipThisGame = true;
                    }
                    else if (line.StartsWith("[Result"))
                    {
                        result = line.Split('"')[1];
                        if (result == "1/2-1/2") skipThisGame = true;
                    }
                    else if (line.StartsWith("[Variant"))
                    {
                        if (!line.Contains("suicide")) skipThisGame = true;
                    }
                }
            }
            sr.Close();

            net.GetMatrix().Save(matrixOutputPath);
        }
    }
}
