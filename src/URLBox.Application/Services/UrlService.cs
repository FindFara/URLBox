using URLBox.Application.ViewModel;
using URLBox.Domain.Entities;
using URLBox.Domain.Enums;
using URLBox.Domain.Interfaces;

namespace URLBox.Application.Services
{
    public class UrlService
    {
        private readonly IUrlRepository _repository;

        public UrlService(IUrlRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<UrlViewModel>> GetUrlsAsync()
        {
           var items = await _repository.GetAllAsync();

            return items.Select(i => new UrlViewModel
            {
                Description = i.Description,
                Environment = i.Environment,
                Id = i.Id,
                Order = i.Order,
                Tag = i.Tag,
                UrlValue = i.UrlValue,
            }).ToList();
        }

        public async Task AddUrlAsync(string urlValue, string description, EnvironmentType environment, string tag, int order = 0)
        {
            var url = new Url
            {
                UrlValue = urlValue,
                Description = description,
                Environment = environment,
                Tag = tag,
                Order = order
            };
            await _repository.AddAsync(url);
        }
        public Task DeleteUrlAsync(int id) => _repository.DeleteAsync(id);
    }
}