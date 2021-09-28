using HotChocolate.Types;

namespace StarWars.Characters
{
    [InterfaceType(Name = "SearchResult")]
    public interface ISearchResult
    {
        /// <summary>
        /// The unique identifier for the character.
        /// </summary>
        int Id { get; }

        /// <summary>
        /// The name of the character.
        /// </summary>
        string Name { get; }
    }
}
