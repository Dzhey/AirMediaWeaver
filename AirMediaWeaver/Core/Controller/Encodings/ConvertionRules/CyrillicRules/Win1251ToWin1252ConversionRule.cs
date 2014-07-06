using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    /// <summary>
    /// Appliable when all characters are pseude-graphic characters.
    /// </summary>
    public class Win1251ToWin1252ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("windows-1251");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("windows-1252");

        private readonly AllInputCharsIsLowercase _ruleChecker;


        public Win1251ToWin1252ConversionRule()
        {
            _ruleChecker = new AllInputCharsIsLowercase(); ;
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
            return _ruleChecker.Apply(input);
        }

        protected override string ApplyConversionImpl(string input)
        {
            var text = SourceEncoding.GetBytes(input);

            return TargetEncoding.GetString(text);
        }

    }
}