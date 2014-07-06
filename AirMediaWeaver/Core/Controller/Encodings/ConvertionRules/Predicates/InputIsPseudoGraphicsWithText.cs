using System.Linq;
using AirMedia.Core.Utils;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates
{
    /// <summary>
    /// Appliable when input has at least 40% of pseudo-graphic characters and at least one alphabetic character.
    /// </summary>
    public class InputIsPseudoGraphicsWithText : Predicate<string>
    {
        private const float TriggerThreshold = .4f;

        public override bool Apply(string input)
        {
            if (input.Length == 0)
                return false;

            int pseudoGraphicCharCount = 0;
            bool hasAlphabeticChar = false;
            foreach (var ch in input)
            {
                if (UnicodeCodepages.PseudographicsCodepage.Contains(ch))
                    pseudoGraphicCharCount++;

                if (!hasAlphabeticChar && UnicodeCodepages.AlphabeticCyrillicChars.Contains(ch))
                    hasAlphabeticChar = true;
            }

            if (hasAlphabeticChar == false) return false;

            float threshold = ((float)pseudoGraphicCharCount) / input.Length;

            return threshold >= TriggerThreshold;
        }
    }
}