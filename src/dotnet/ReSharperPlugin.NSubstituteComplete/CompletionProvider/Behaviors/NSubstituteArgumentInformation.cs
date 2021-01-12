using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors
{
    public class NSubstituteArgumentInformation : TextualInfo
    {
        public IType Type { get; }
        public string ArgSuffix { get; }
        public char TypeFirstLetter { get; }

        public NSubstituteArgumentInformation([NotNull] string text, [NotNull] string identity, IType type, string argSuffix, char typeFirstLetter)
            : base(text, identity)
        {
            ArgSuffix = argSuffix;
            TypeFirstLetter = typeFirstLetter;
            Type = type;
        }
    }
}
