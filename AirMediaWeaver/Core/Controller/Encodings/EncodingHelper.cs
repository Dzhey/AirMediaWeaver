using System.Linq;
using AirMedia.Core.Controller.Encodings.ConvertionRules;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings
{
    public class EncodingHelper
    {
        private const string AssumedTextCharacters = "abcdefghijklmnopqrstuvwyz" +
                                                     "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
                                                     "àáâãäå¸æçèêëìíîïðñòóôõö÷øùúûüýþÿ" +
                                                     "ÀÁÂÃÄÅ¨ÆÇÈÊËÌÀÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞß";

        private static readonly AllInputCharsAreDigits AllInputCharsAreDigits = new AllInputCharsAreDigits();

        public static bool CheckIsMalformedText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            foreach (var ch in text)
            {
                if (AssumedTextCharacters.Contains(ch))
                    return false;
            }

            if (AllInputCharsAreDigits.Apply(text))
                return false;

            return true;
        }

        public static BaseTreeRule[] FindTextConverters(string input)
        {
            return CyrillicConverter.FindApplicableConversionRules(input);
        } 

        public static bool TryConvertText(string input, out string output)
        {
            return CyrillicConverter.TryConvert(input, out output, s => !CheckIsMalformedText(s));
        }
    }
}