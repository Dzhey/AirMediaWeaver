using System.Linq;
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class AllInputIsNotRussianChars : Predicate<string>
    {
        public override bool Apply(string input)
        {
            foreach (var ch in input)
            {
                if (UnicodeCodepages.RussianAlphabeticChars.Contains(ch) == false)
                    return false;
            }

            return true;
        }
    }
}