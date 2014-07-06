using System.Text;
using System.Linq;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    /// <summary>
    /// Appliable when all characters are pseude-graphic characters.
    /// </summary>
    public class Cp866ToWin1251ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("cp866");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("windows-1251");

        private static readonly char[] AssumeChars = new[]
            {
                '\u00AE',
                '\u00A3',
                '\u00AB',
                '\u00BB',
                '\u00AF',
                '\u00A4'
            };

        public Cp866ToWin1251ConversionRule()
        {
        }

        public Cp866ToWin1251ConversionRule(params BaseTreeRule[] children) : base(children)
        {
        }

        public override Encoding GetSourceEncoding()
        {
            return SourceEncoding;
        }

        public override Encoding GetTargetEncoding()
        {
            return TargetEncoding;
        }

        protected override bool IsRuleAppliableImpl(string input)
        {
            bool hasAssumeChar = false;
            foreach (var ch in input)
            {
                if (AssumeChars.Contains(ch))
                {
                    hasAssumeChar = true;
                    break;
                }
            }

            return hasAssumeChar;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}