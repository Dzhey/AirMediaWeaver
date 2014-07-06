
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    public class OutputIsBrokenString : Predicate<string>
    {
        private const float TriggerThreshold = .7f;

        public override bool Apply(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            int omitCharCount = 0;
            int questionMarkCount = 0;
            foreach (var ch in input)
            {
                if (ch == '?')
                    questionMarkCount++;
                else if (char.IsDigit(ch) || char.IsWhiteSpace(ch))
                    omitCharCount++;
            }

            int denom = input.Length - omitCharCount;

            if (denom == 0)
                return false;

            float threshold = (float)questionMarkCount/denom;

            return threshold >= TriggerThreshold;
        }
    }
}