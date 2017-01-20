﻿using System;
using System.Collections.Generic;
using System.Linq;
using Enochian.Text;

namespace Enochian.Flow.Steps
{
    public class SampleText : FlowStep<TextChunk, TextChunk>
    {
        public SampleText(IFlowResources resources)
            : base(resources)
        {
        }

        public FeatureSet Features { get; private set; }

        public IList<string> Tokens { get; set; }

        public static readonly char[] WHITESPACE = new[] { ' ', '\t', '\n', '\r' };

        public override IConfigurable Configure(dynamic config)
        {
            base.Configure((object)config);

            if (Resources != null)
            {
                Features = Resources.FeatureSets.FirstOrDefault(fs => fs.Name == config.Features);
                if (Features == null)
                    AddError("invalid features name '{0}'", config.Features);
            }
            else
            {
                AddError("no resources specified for SampleText");
            }

            string text = Convert.ToString(config.Text);
            if (!string.IsNullOrWhiteSpace(text))
                Tokens = text.Split(WHITESPACE, StringSplitOptions.RemoveEmptyEntries);

            return this;
        }

        internal override IEnumerable<object> GetOutputs()
        {
            if (Tokens == null)
                yield break;

            foreach (var token in Tokens)
            {
                yield return new TextChunk
                {
                    Lines = new[]
                    {
                        new Interline
                        {
                            Encoding = Encoding.Default,
                            Segments = new[] { new Segment { Text = token } },
                        },
                    },
                };
            }
        }
    }
}
