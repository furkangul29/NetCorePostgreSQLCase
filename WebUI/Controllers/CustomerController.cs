using AutoMapper;
using BusinessLayer.Abstract;
using DtoLayer.CustomerDtos;
using EntityLayer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using WebUI.Models;

namespace WebUI.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme, Policy = "customers.read")]
    public class CustomerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerController> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CustomerController(IHttpClientFactory httpClientFactory, ICustomerService customerService, IMapper mapper, ILogger<CustomerController> logger, IHttpContextAccessor httpContextAccessor)
        {
            _httpClientFactory = httpClientFactory;
            _customerService = customerService;
            _mapper = mapper;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpGet]
        public async Task<IActionResult> Index(string firstNameFilter, string lastNameFilter, string regionFilter, string emailDomainFilter, DateTime? startDate, DateTime? endDate)
        {
            
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            ViewBag.Username = userName;

            _logger.LogWarning("Müşteri listesi sayfasına {user} tarafından erişildi.", userName);

            var userScopes = User.Claims
                            .Where(c => c.Type == "scope" || c.Type == ClaimTypes.Role)
                            .Select(c => c.Value)
                            .ToList();
            ViewBag.HasFilterPermission = userScopes.Contains("customers.filter");

            var customers = await _customerService.TGetListAllAsync();

           


            var model = new FilteredCustomerViewModel
            {
                Customers = _mapper.Map<List<ResultCustomerDto>>(customers),
                FirstNameFilter = firstNameFilter,
                LastNameFilter = lastNameFilter,
                RegionFilter = regionFilter,
                EmailFilter = emailDomainFilter,
                StartDate = startDate,
                EndDate = endDate,
                HasFilterPermission = ViewBag.HasFilterPermission 
            };

           

            _logger.LogWarning("Uygulanan filtreler: Ad: {Ad}, Soyad: {Soyad}, Bölge: {Bolge}, E-posta: {Eposta}, Başlangıç Tarihi: {BaslangicTarihi}, Bitiş Tarihi: {BitisTarihi} - İşlem Yapan: {user}",
                firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate, userName);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var filteredCustomers = FilterCustomers(customers, firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);
                _logger.LogWarning("{user} tarafından filtre uygulanmış AJAX isteği yapıldı.", userName);
                return Json(_mapper.Map<List<ResultCustomerDto>>(filteredCustomers));
            }

            return View(model);
        }

        private IEnumerable<Customer> FilterCustomers(IEnumerable<Customer> customers, string firstName, string lastName, string region, string emailDomain, DateTime? startDate, DateTime? endDate)
        {
            if (!string.IsNullOrEmpty(firstName))
                customers = customers.Where(c => c.FirstName.StartsWith(firstName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(lastName))
                customers = customers.Where(c => c.LastName.StartsWith(lastName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(region))
                customers = customers.Where(c => c.Region.StartsWith(region, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(emailDomain))
            {
                customers = customers.Where(c => c.Email.Split('@').Length > 1 &&
                    c.Email.Split('@')[1].StartsWith(emailDomain, StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
                customers = customers.Where(c => c.RegistrationDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                customers = customers.Where(c => c.RegistrationDate.Date <= endDate.Value.Date);

            _logger.LogWarning("Müşteriler şu filtrelere göre listelendi: Ad: {Ad}, Soyad: {Soyad}, Bölge: {Bolge}, E-posta Alan Adı: {EpostaAlanAdi}, Başlangıç Tarihi: {BaslangicTarihi}, Bitiş Tarihi: {BitisTarihi} - İşlem Yapan: {user}",
                firstName, lastName, region, emailDomain, startDate, endDate, _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "Anonim");

            return customers;
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogWarning("Yeni müşteri oluşturma sayfası {user} tarafından açıldı.", userName);
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (!ModelState.IsValid)
            {
                _logger.LogCritical("Müşteri oluşturulurken geçersiz model durumu. Kullanıcı: {user}", userName);
                return View(createCustomerDto);
            }

            try
            {
                var customer = _mapper.Map<Customer>(createCustomerDto);
                await _customerService.TAddAsync(customer);
                _logger.LogWarning("{user} tarafından yeni bir müşteri başarıyla oluşturuldu.", userName);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{user} tarafından müşteri oluşturulurken hata meydana geldi: {HataMesaji}", userName, ex.Message);
                ModelState.AddModelError("", $"Müşteri oluşturulurken hata oluştu: {ex.Message}");
                return View(createCustomerDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;
            _logger.LogWarning("{user} tarafından ID'si {MusteriId} olan müşterinin güncelleme sayfası açıldı.", userName, id);

            var customer = await _customerService.TGetByIDAsync(id);
            if (customer == null)
            {
                _logger.LogCritical("ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {user}", id, userName);
                return NotFound($"Müşteri bulunamadı: ID {id}");
            }

            var updateCustomerDto = _mapper.Map<UpdateCustomerDto>(customer);
            return View(updateCustomerDto);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto updateCustomerDto)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            if (!ModelState.IsValid)
            {
                _logger.LogCritical("Müşteri güncellenirken geçersiz model durumu. Kullanıcı: {user}", userName);
                return View(updateCustomerDto);
            }

            try
            {
                var existingCustomer = await _customerService.TGetByIDAsync(id);
                if (existingCustomer == null)
                {
                    _logger.LogCritical("Güncellenmek istenen ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {user}", id, userName);
                    ModelState.AddModelError("", "Müşteri bulunamadı.");
                    return View(updateCustomerDto);
                }

                _mapper.Map(updateCustomerDto, existingCustomer);
                await _customerService.TUpdateAsync(existingCustomer);
                _logger.LogWarning("{user} tarafından ID'si {MusteriId} olan müşteri başarıyla güncellendi.", userName, id);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{user} tarafından ID'si {MusteriId} olan müşteri güncellenirken hata meydana geldi: {HataMesaji}", userName, id, ex.Message);
                ModelState.AddModelError("", $"Müşteri güncellenirken hata oluştu: {ex.Message}");
                return View(updateCustomerDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var userName = User.FindFirst(ClaimTypes.Name)?.Value;

            try
            {
                _logger.LogWarning("ID'si {MusteriId} olan müşteri için silme isteği {user} tarafından gönderildi.", id, userName);

                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                {
                    _logger.LogCritical("Silinmek istenen ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {user}", id, userName);
                    return NotFound("Müşteri bulunamadı.");
                }

                await _customerService.TDeleteAsync(customer);
                _logger.LogWarning("{user} tarafından ID'si {MusteriId} olan müşteri başarıyla silindi.", userName, id);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{user} tarafından ID'si {MusteriId} olan müşteri silinirken hata meydana geldi: {HataMesaji}", userName, id, ex.Message);
                return BadRequest($"Müşteri silinirken hata oluştu: {ex.Message}");
            }
        }
    }
}
