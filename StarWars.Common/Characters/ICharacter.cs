using System.Collections.Generic;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace StarWars.Characters
{
    /// <summary>
    /// A character in the Star Wars universe.
    /// </summary>
    [InterfaceType(Name = "Character")]
    public interface ICharacter : ISearchResult
    {
        /// <summary>
        /// The ids of the character's friends.
        /// </summary>
        //NOTE: Updated to use v11 method...
        [UsePaging(type: typeof(InterfaceType<ICharacter>))]
        IReadOnlyList<int> Friends { get; }

        /// <summary>
        /// The episodes the character appears in.
        /// </summary>
        IReadOnlyList<Episode> AppearsIn { get; }

        /// <summary>
        /// The height of the character.
        /// </summary>
        [UseConvertUnit]
        double Height { get; }
    }
}
