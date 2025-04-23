namespace Talepreter.Contracts.Orleans.Grains
{
    [GenerateSerializer]
    public class ChapterPagePair
    {
        [Id(0)]
        public int? Chapter { get; init; }
        [Id(1)]
        public int? Page { get; init; }

        public ChapterPagePair Clone() => new() { Chapter = Chapter, Page = Page };
    }
}
