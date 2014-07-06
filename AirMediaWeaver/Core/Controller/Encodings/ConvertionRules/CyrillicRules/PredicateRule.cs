using System.Text;
using AirMedia.Core.Utils;


namespace AirMedia.Core.Controller.Encodings.ConvertionRules.CyrillicRules
{
    public class PredicateRule : BaseTreeRule
    {
        private readonly Predicate<string> _branchingPredicate; 

        public PredicateRule(Predicate<string> branchingPredicate, 
            params BaseTreeRule[] children) : base(children)
        {
            _branchingPredicate = branchingPredicate;
        }

        public override Encoding GetSourceEncoding()
        {
            return null;
        }

        public override Encoding GetTargetEncoding()
        {
            return null;
        }

        protected override bool IsRuleAppliableImpl(string input)
        {
            return false;
        }

        protected override string ApplyConversionImpl(string input)
        {
            return null;
        }

        protected override BaseTreeRule[] FindAppliableChildRulesImpl(string input)
        {
            if (_branchingPredicate.Apply(input) == false)
                return new BaseTreeRule[0];

            return base.FindAppliableChildRulesImpl(input);
        }
    }
}