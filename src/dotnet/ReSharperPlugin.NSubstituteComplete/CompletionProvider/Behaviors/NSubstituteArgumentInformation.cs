using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

public class NSubstituteArgumentInformation([NotNull] string text, [NotNull] string identity, IType type, string argSuffix, char typeFirstLetter)
    : TextualInfo(text, identity)
{
    public IType Type { get; } = type;
    public string ArgSuffix { get; } = argSuffix;
    public char TypeFirstLetter { get; } = typeFirstLetter;

    [CanBeNull]
    public string TypeName { get; set; }
}
