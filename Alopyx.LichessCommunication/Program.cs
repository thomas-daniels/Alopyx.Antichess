using Alopyx.Antichess;
using ChessDotNet;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Alopyx.LichessCommunication
{
    class Program
    {
        static void Main()
        {
            ServicePointManager.DefaultConnectionLimit = 20; // TODO: cleaner way to avoid requests getting throttled
            Console.WriteLine("Username: ");
            string username = Console.ReadLine();
            Console.WriteLine("Authentication token: ");
            string authenticationToken = Console.ReadLine();
            Console.Clear();

            using (HttpWebResponse resp = SendRequest("/api/stream/event", "GET", authenticationToken))
            using (Stream stream = resp.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    Console.WriteLine(line);

                    JObject lichessEvent = JsonConvert.DeserializeObject<JObject>(line);
                    string type = lichessEvent.GetValue("type").ToObject<string>();
                    if (type == "challenge")
                    {
                        JToken challenge = lichessEvent.GetValue("challenge");
                        bool rated = challenge.Value<bool>("rated");
                        string variant = challenge.Value<JToken>("variant").Value<string>("key");
                        string tc = challenge.Value<JToken>("timeControl").Value<string>("type");
                        string id = challenge.Value<string>("id");
                        if (!rated && variant == "antichess" && tc == "clock")
                        {
                            SendRequest("/challenge/" + id + "/accept", "POST", authenticationToken)?.Dispose();
                        }
                        else
                        {
                            SendRequest("/challenge/" + id + "/decline", "POST", authenticationToken)?.Dispose();
                        }
                    }
                    else if (type == "gameStart")
                    {
                        string game = lichessEvent.GetValue("game").Value<string>("id");
                        new Thread(() =>
                        {
                            Engine engine = new Engine();
                            using (HttpWebResponse gameResp = SendRequest("/bot/game/stream/" + game, "GET", authenticationToken))
                            {
                                if (gameResp == null) return;
                                using (Stream gameStream = gameResp.GetResponseStream())
                                {
                                    using (StreamReader gameReader = new StreamReader(gameStream))
                                    {
                                        string gameEventStr;

                                        bool isWhite = false;
                                        bool first = true;
                                        while ((gameEventStr = gameReader.ReadLine()) != null)
                                        {
                                            if (string.IsNullOrWhiteSpace(gameEventStr)) continue;

                                            Console.WriteLine($"[game {game}] {gameEventStr}");
                                            JObject gameEvent = JsonConvert.DeserializeObject<JObject>(gameEventStr);

                                            int moveCount = -1;

                                            if (gameEvent.ContainsKey("id"))
                                            {
                                                isWhite = gameEvent.GetValue("white").Value<string>("id") != null && gameEvent.GetValue("white").Value<string>("id").ToLowerInvariant() == username.ToLowerInvariant();
                                                Console.WriteLine($"[game {game}] isWhite {isWhite}");

                                                gameEvent = (JObject)gameEvent.GetValue("state");
                                            }
                                            if (gameEvent.ContainsKey("moves"))
                                            {

                                                string[] moves = gameEvent.GetValue("moves").ToObject<string>().Split(' ');
                                                if (first && moves[0] != "")
                                                {
                                                    foreach (string move in moves)
                                                    {
                                                        Move m = new Move(move.Substring(0, 2), move.Substring(2, 2), engine.Game.WhoseTurn, move.Length == 4 ? (char?)null : move.Last());
                                                        engine.Game.ApplyMove(m, true);
                                                    }
                                                }

                                                if (moves.Length > moveCount && ((moves[0] == "" && isWhite) || (moves[0] != "" && moves.Length % 2 == (isWhite ? 0 : 1))))
                                                {
                                                    moveCount = moves.Length;

                                                    string lastMove = moves.Last();
                                                    if (moves[0] != "" && !first)
                                                    {
                                                        Move m = new Move(lastMove.Substring(0, 2), lastMove.Substring(2, 2), isWhite ? Player.Black : Player.White, lastMove.Length == 4 ? (char?)null : lastMove.Last());
                                                        engine.Game.ApplyMove(m, true);
                                                    }
                                                    Move best = engine.FindBestMove();
                                                    if (best == null) break;
                                                    engine.Game.ApplyMove(best, true);

                                                    string moveToSend = best.OriginalPosition.ToString().ToLowerInvariant() +
                                                        best.NewPosition.ToString().ToLowerInvariant() +
                                                        (!best.Promotion.HasValue ? "" : best.Promotion.Value.ToString().ToLowerInvariant());
                                                    Console.WriteLine($"[game {game}] sending move {moveToSend}");
                                                    HttpWebResponse r = SendRequest("/bot/game/" + game + "/move/" + moveToSend, "POST", authenticationToken);
                                                    if (r != null) { r.Dispose(); }
                                                    else { break; } // Happens if Alopyx times out because it had to think for too long, then sends the move to the server when it found it and the game is already over.
                                                    Console.WriteLine($"[game {game}] move sent");
                                                }

                                                first = false;
                                            }
                                        }
                                    }
                                }
                            }
                            Console.WriteLine($"[game {game}] response and stream disposed");
                        }).Start();
                    }
                    else
                    {
                        Console.WriteLine($"Unknown type {type}.");
                    }
                }
            }
        }

        static HttpWebResponse SendRequest(string url, string method, string authorization)
        {
            HttpWebRequest hwr = WebRequest.CreateHttp(new Uri(new Uri("https://lichess.org/"), url));
            hwr.Headers[HttpRequestHeader.Authorization] = "Bearer " + authorization;
            hwr.Method = method;
            try
            {
                return (HttpWebResponse)hwr.GetResponse();
            }
            catch (WebException e)
            {
                Console.WriteLine("WebException caught: " + e.Message);
                return null;
            }
        }
    }
}
