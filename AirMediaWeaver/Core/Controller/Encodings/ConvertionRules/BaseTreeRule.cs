using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AirMedia.Core.Controller.Encodings.ConvertionRules.Predicates;

namespace AirMedia.Core.Controller.Encodings.ConvertionRules
{
    public abstract class BaseTreeRule
    {
        private readonly List<BaseTreeRule> _children;
        private readonly Utils.Predicate<string> _outputIsBrokenString;

        protected BaseTreeRule()
        {
            _children = new List<BaseTreeRule>();
            _outputIsBrokenString = new OutputIsBrokenString();
        }

        protected BaseTreeRule(params BaseTreeRule[] children) : this()
        {
            foreach (var child in children)
            {
                AddChildRule(child);
            }
        }

        public void AddChildRule(BaseTreeRule child)
        {
            if (_children.Contains(child))
                throw new ArgumentException("Such a child rule is already added");

            _children.Add(child);
        }

        public bool IsRuleApplicable(string input)
        {
            if (input == null) return true;

            if (IsRuleAppliableImpl(input))
                return true;

            var appliableChildRules = FindAppliableChildRulesImpl(input);
            if (appliableChildRules != null && appliableChildRules.Length > 0)
                return true;

            return false;
        }

        public BaseTreeRule[] FindAppliableChildRules(string input)
        {
            if (input == null) return null;

            return FindAppliableChildRulesImpl(input);
        }

        public abstract Encoding GetSourceEncoding();
        public abstract Encoding GetTargetEncoding();

        public string ApplyConversion(string input, Predicate<string> confirmPredicate = null)
        {
            if (input == null) return null;

            if (IsRuleAppliableImpl(input))
            {
                var result = ApplyConversionImpl(input);

                if (ValidateResult(result, confirmPredicate))
                    return result;
            }

            var converterChildren = FindAppliableChildRulesImpl(input);

            if (converterChildren == null || converterChildren.Length == 0)
                return null;

            foreach (var converterChild in converterChildren)
            {
                var result = converterChild.ApplyConversion(input, confirmPredicate);

                if (ValidateResult(result, confirmPredicate))
                    return result;
            }

            return input;
        }

        private bool ValidateResult(string result, Predicate<string> confirmPredicate)
        {
            return _outputIsBrokenString.Apply(result) == false &&
                   (confirmPredicate == null || confirmPredicate(result));
        }

        protected abstract bool IsRuleAppliableImpl(string input);
        protected abstract string ApplyConversionImpl(string input);

        protected virtual BaseTreeRule[] FindAppliableChildRulesImpl(string input)
        {
            return _children.Where(child => child.IsRuleApplicable(input))
                            .ToArray();
        }
    }
}