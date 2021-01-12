using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors
{
    public class NSubstituteArgumentsInformation : TextualInfo
    {
        public IEnumerable<IType> Types { get; }
        public string ArgSuffix { get; }

        public NSubstituteArgumentsInformation([NotNull] string text, [NotNull] string identity, IEnumerable<IType> types, string argSuffix)
            : base(text, identity)
        {
            ArgSuffix = argSuffix;
            Types = types;
        }
    }
}
