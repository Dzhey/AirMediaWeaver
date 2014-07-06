using System.Linq;
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class AtLeastHalfUmlauts : Predicate<string>
    {
        private const float TriggerThreshold = .5f;

        public override bool Apply(string input)
        {
            int umlautCharsCount = 0;
            int digitCount = 0;

            foreach (var ch in input)
            {
                if (UnicodeCodepages.UmlautcChars.Contains(ch))
                    umlautCharsCount++;
                else if (char.IsDigit(ch))
                    digitCount++;
            }

            int denom = input.Length - digitCount;

            if (denom == 0)
                return umlautCharsCount > 0;

            float threshold = (float)umlautCharsCount / denom;

            return threshold >= TriggerThreshold;
        }
    }
}