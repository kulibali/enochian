﻿using System;
using System.Collections.Generic;
using Verophyle.Regexp;
using Verophyle.Regexp.InputSet;
using Verophyle.Regexp.Node;

namespace Enochian.Text
{
    public class Encoding : Configurable, IFileReference
    {
        public static Encoding Default { get; } = new Encoding { Name = "Default" };

        public string Name { get; internal set; }
        public string RelativePath { get; internal set; }

        public FeatureSet Features { get; internal set; }

        public IList<EncodingPattern> Patterns { get; } = new List<EncodingPattern>();

        public override IEnumerable<IConfigurable> Children => Patterns;

        public override IConfigurable Configure(IDictionary<string, object> config)
        {
            base.Configure(config);

            var patterns = config.GetChildren("patterns", this);
            if (patterns != null)
            {
                try
                {
                    foreach (var pattern in patterns)
                    {
                        Patterns.Add(new EncodingPattern(Features, pattern));
                    }
                }
                catch (Exception e)
                {
                    AddError("patterns needs to be a list of pattern configs: {0}", e.Message);
                }
            }
            return this;
        }
    }

    public class EncodingPattern : Configurable
    {
        public EncodingPattern(FeatureSet features, IDictionary<string, object> config)
        {
            Features = features;
            Configure(config);
        }

        public FeatureSet Features { get; }

        public string Input { get; private set; }
        public string Output { get; private set; }

        public string FeatureSpec { get; private set; }

        public bool IsReplacement => Input != null && !Input.Contains("_");

        public double[] Vector { get; private set; }

        public override IConfigurable Configure(IDictionary<string, object> config)
        {
            base.Configure(config);

            Input = config.Get<string>("input", this);
            if (string.IsNullOrWhiteSpace(Input))
            {
                AddError("empty text template");
            }

            Output = config.Get<string>("output", this);

            var features = config.GetList<string>("features", this);
            if (features != null)
            {
                FeatureSpec = string.Join(", ", features);

                if (Features != null)
                {
                    var errors = new List<string>();
                    Vector = Features.GetFeatureVector(features, errors);
                    foreach (var error in errors)
                        AddError("error in feature spec for '{0}': {1}", Input, error);
                }
                else
                {
                    AddError("null feature set for '{0}'", Input);
                }
            }
            else
            {
                AddError("invalid feature spec (needs to be a list of strings)");
            }

            return this;
        }

        public DeterministicAutomaton<char, UnicodeCategoryMatcher> GetRegexp()
        {
            if (string.IsNullOrEmpty(Input))
                return new DeterministicAutomaton<char, UnicodeCategoryMatcher>();

            int pos = 0;
            Node<char> seq = null;
            foreach (var ch in Input)
            {
                Node<char> leaf = ch == '_'
                    ? new Dot<char>(new DotSet<char>(), ref pos)
                    : new Leaf<char>(new CharSet(ch), ref pos);
                seq = seq != null ? new Seq<char>(seq, leaf) : leaf;
            }
            Node<char> end = new End<char>(ref pos);
            seq = seq != null ? new Seq<char>(seq, end) : end;

            return new DeterministicAutomaton<char, UnicodeCategoryMatcher>(seq);
        }
    }
}