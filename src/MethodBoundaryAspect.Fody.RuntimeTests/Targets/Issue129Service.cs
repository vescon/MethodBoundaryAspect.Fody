using MethodBoundaryAspect.Fody.RuntimeTests.Aspects;
using System.Threading.Tasks;

namespace MethodBoundaryAspect.Fody.RuntimeTests.Targets
{
    public class Issue129Place
    {
        public string Id { get; set; }
    }

    public class Issue129Dto
    {
        public string Id { get; set; }
    }

    public interface IIssue129Dal
    {
        Task<Issue129Place> FindPlace(string id, bool fallback);
        Task<Issue129Place> SavePlace(string id);
    }

    public class Issue129Dal : IIssue129Dal
    {
        private readonly bool _found;

        public Issue129Dal(bool found)
        {
            _found = found;
        }

        public async Task<Issue129Place> FindPlace(string id, bool fallback)
        {
            await Task.Yield();
            return _found && fallback ? new Issue129Place { Id = id } : null;
        }

        public async Task<Issue129Place> SavePlace(string id)
        {
            await Task.Yield();
            return new Issue129Place { Id = id };
        }
    }

    public abstract class Issue129ServiceBase
    {
    }

    /// <summary>
    /// Reproduction of https://github.com/vescon/MethodBoundaryAspect.Fody/issues/129
    /// </summary>
    [EmptyMethodBoundaryAspect]
    public class Issue129Service : Issue129ServiceBase
    {
        public IIssue129Dal Dal { get; set; }

        public Issue129Service(IIssue129Dal dal)
        {
            Dal = dal;
        }

        public async Task<Issue129Dto> GetPlaceDetails(string placeId)
        {
            var place = await Dal.FindPlace(placeId, false) ?? await Dal.FindPlace(placeId, true);
            var dto = new Issue129Dto();

            if (place != null)
            {
                dto = new Issue129Dto { Id = place.Id };
                return dto;
            }

            var saved = await Dal.SavePlace(placeId);
            if (saved != null)
            {
                dto = new Issue129Dto { Id = saved.Id };
                return dto;
            }

            dto = place == null ? null : new Issue129Dto();

            return dto;
        }
    }
}
