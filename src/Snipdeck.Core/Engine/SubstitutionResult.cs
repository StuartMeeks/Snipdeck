namespace Snipdeck.Core.Engine
{
    public sealed record SubstitutionResult(string Text, IReadOnlyList<string> UnresolvedTokens)
    {
        public bool IsFullyResolved => UnresolvedTokens.Count == 0;
    }
}
