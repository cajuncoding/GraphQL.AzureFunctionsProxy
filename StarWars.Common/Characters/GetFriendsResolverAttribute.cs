using System.Linq;
using System.Reflection;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using StarWars.Repositories;

namespace StarWars.Characters
{
    public sealed class GetFriendsResolverAttribute
        : ObjectFieldDescriptorAttribute
    {
        public override void OnConfigure(
            IDescriptorContext context,
            IObjectFieldDescriptor descriptor,
            MemberInfo member)
        {
            descriptor.Resolve(ctx =>
            {
                ICharacter character = ctx.Parent<ICharacter>();
                ICharacterRepository repository = ctx.Service<ICharacterRepository>();
                return repository.GetCharacters(character.Friends.ToArray());
            });
        }
    }
}