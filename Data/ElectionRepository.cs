using System;
using System.Collections.Generic;

namespace TwilioDapperVoting.Data
{
    public class ElectionRepository
    {
        private static Dictionary<string, int> emojiTally = new Dictionary<string, int>();

        public void Vote(string emoji)
        {
            if (emojiTally.ContainsKey(emoji))
            {
                emojiTally[emoji]++;
            }
            else
            {
                emojiTally[emoji] = 1;
            }
        }

        public Dictionary<string, int> GetEmojiTally(){
            return emojiTally;
        }
    }
}