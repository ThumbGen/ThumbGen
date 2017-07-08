using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThumbGen
{
    public class LetterPairSimilarity
    {
        public static double CompareStrings(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return 0.0;
            }
            List<string> pairs1 = wordLetterPairs(str1.ToUpperInvariant());
            List<string> pairs2 = wordLetterPairs(str2.ToUpperInvariant());
            int intersection = 0;
            int union = pairs1.Count + pairs2.Count;
            for (int i = 0; i < pairs1.Count; i++)
            {
                Object pair1 = pairs1[i];
                for (int j = 0; j < pairs2.Count; j++)
                {
                    Object pair2 = pairs2[j];
                    if (pair1.Equals(pair2))
                    {
                        intersection++;
                        pairs2.RemoveAt(j);
                        break;
                    }
                }
            }
            return (2.0 * intersection) / union;
        }

        private static List<string> wordLetterPairs(string str)
        {
            List<string> allPairs = new List<string>();
            // Tokenize the string and put the tokens/words into an array 
            String[] words = str.Split('s');
            // For each word
            for (int w = 0; w < words.Length; w++)
            {
                // Find the pairs of characters
                string[] pairsInWord = letterPairs(words[w]);
                if (pairsInWord != null)
                {
                    for (int p = 0; p < pairsInWord.Length; p++)
                    {
                        allPairs.Add(pairsInWord[p]);
                    }
                }
            }
            return allPairs;
        }

        private static String[] letterPairs(string str)
        {
            if (str.Length > 0)
            {
                int numPairs = str.Length - 1;
                String[] pairs = new String[numPairs];
                for (int i = 0; i < numPairs; i++)
                {
                    pairs[i] = str.Substring(i, 2);
                }
                return pairs;
            }
            else
            {
                return null;
            }
        }
    }
}
