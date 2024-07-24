using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonDecoder
{
    public class LazyJToken
    {
        public JToken Token { get; }
        public string Path { get; }
        public long StartPosition { get; }
        public long EndPosition { get; }

        public LazyJToken(JToken token, string path)
        {
            Token = token;
            Path = path;
        }

        public LazyJToken(JToken token, long startPosition, long endPosition)
        {
            Token = token;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }
    }
}
