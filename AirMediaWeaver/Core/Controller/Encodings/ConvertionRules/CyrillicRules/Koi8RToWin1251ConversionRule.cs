using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class Koi8RToWin1251ConversionRule : BaseTreeRule
    {
        private static readonly Encoding SourceEncoding = Encoding.GetEncoding("koi8-r");
        private static readonly Encoding TargetEncoding = Encoding.GetEncoding("windows-1251");

        private readonly AllInputCharsIsUppercase _ruleChecker;


        public Koi8RToWin1251ConversionRule()
        {
            _ruleChecker = new AllInputCharsIsUppercase(); ;
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