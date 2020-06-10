using System;
using System.Collections.Concurrent;
using System.IO;

namespace Cinema.Model.Dictionary
{
    public class DictionaryReceiveMessage
    {
        public readonly ConcurrentDictionary<string, string> _messages;

        public DictionaryReceiveMessage()
        {
            _messages = new ConcurrentDictionary<string, string>();
            using (var reader = new StreamReader(@"/home/scalfi/csharp/Cinema/cinema.csv"))
            {
                string line;

                while (( line = reader.ReadLine()) != null)
                {
                    string [] coluns = line.Split(",");
                    _messages.TryAdd(coluns[0],coluns[1]);
                }
            }
        }
    }
}