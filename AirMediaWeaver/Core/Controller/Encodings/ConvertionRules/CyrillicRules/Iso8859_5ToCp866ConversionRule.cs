using System.Linq;
using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Iso8859_5ToCp866ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("iso-8859-5");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("cp866");

        private readonly InputIsPseudoGraphicsWithText _checkerRule;

        public Iso8859_5ToCp866ConversionRule()
        {
            _checkerRule = new InputIsPseudoGraphicsWithText();
        }

        public Iso8859_5ToCp866ConversionRule(BaseTreeRule child) : base(child)
        {
            _checkerRule = new InputIsPseudoGraphicsWithText();
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
            if (_checkerRule.Apply(input) == false)
                return false;

            bool hasBlackSquares = false;
            foreach (var ch in input)
            {
                if (UnicodeCodepages.BlackSquareSeudographicsChars.Contains(ch))
                {
                    hasBlackSquares = true;
                    break;
                }
            }

            return hasBlackSquares;
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}