using System.Collections.Generic;
using JetBrains.Annotations;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure.AspectLookupItems.Info;
using JetBrains.ReSharper.Psi;

namespace ReSharperPlugin.NSubstituteComplete.CompletionProvider.Behaviors;

public class NSubstituteArgumentsInformation([NotNull] string text, [NotNull] string identity, IEnumerable<IType> types, string argSuffix)
    : TextualInfo(text, identity)
{
    public IEnumerable<IType> Types { get; } = types;
    public string ArgSuffix { get; } = argSuffix;
}
