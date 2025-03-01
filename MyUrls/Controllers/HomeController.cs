using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyUrls.Context;
using MyUrls.Models;
using System.Diagnostics;

namespace MyUrls.Controllers
{
	public class HomeController : Controller
	{
		private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
		{
			_logger = logger;
            _context = context;
        }

		private static List<UrlModel> urlList = new List<UrlModel>();

        public async Task<IActionResult> Index()
        {
            var urlList = await _context.Urls.ToListAsync();
            return View(urlList);
        }

        [HttpPost]
        public async Task<IActionResult> AddUrl(string url, string description, EnvironmentType environment, string tag)
        {
            if (!string.IsNullOrEmpty(url))
            {
                var newUrl = new UrlModel
                {
                    Url = url,
                    Description = description,
                    Environment = environment,
                    Tag = tag 
                };
                _context.Urls.Add(newUrl);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }


        public async Task<IActionResult> DeleteUrl(int id)
        {
            var url = await _context.Urls.FindAsync(id);
            if (url != null)
            {
                _context.Urls.Remove(url);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Index");
        }



        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
		public IActionResult Error()
		{
			return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
		}
	}
}
