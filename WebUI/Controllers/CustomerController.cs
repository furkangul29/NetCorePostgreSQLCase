using AutoMapper;
using BusinessLayer.Abstract;
using DataAccessLayer.Concrete;
using DtoLayer.CustomerDtos;
using EntityLayer;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebUI.Models;

namespace WebUI.Controllers
{

    
    public class CustomerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;
        private readonly ILogger<CustomerController> _logger;
        //UserManager<ApplicationUser> _userManager;

        
        public CustomerController(IHttpClientFactory httpClientFactory, ICustomerService customerService, IMapper mapper, ILogger<CustomerController> logger /*UserManager<ApplicationUser> userManager*/)
        {
            _httpClientFactory = httpClientFactory;
            _customerService = customerService;
            _mapper = mapper;
            _logger = logger;
            //_userManager = userManager;
        }
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "customers.filter")]
        public async Task<IActionResult> Index(string firstNameFilter, string lastNameFilter, string regionFilter, string emailDomainFilter, DateTime? startDate, DateTime? endDate)
        {

            //var user = await _userManager.GetUserAsync(User);
            _logger.LogWarning("Müşteri listesi sayfasına {KullanıcıAdı} tarafından erişildi.", User?.Identity.Name ?? "Anonim");

            var customers = await _customerService.TGetListAllAsync();
            var model = new FilteredCustomerViewModel
            {
                Customers = _mapper.Map<List<ResultCustomerDto>>(customers),
                FirstNameFilter = firstNameFilter,
                LastNameFilter = lastNameFilter,
                RegionFilter = regionFilter,
                EmailFilter = emailDomainFilter,
                StartDate = startDate,
                EndDate = endDate
            };

            _logger.LogWarning("Uygulanan filtreler: Ad: {Ad}, Soyad: {Soyad}, Bölge: {Bölge}, E-posta: {Eposta}, Başlangıç Tarihi: {BaslangicTarihi}, Bitiş Tarihi: {BitisTarihi}",
                firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var filteredCustomers = FilterCustomers(customers, firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);
                _logger.LogWarning("{KullanıcıAdı} tarafından filtre uygulanmış AJAX isteği yapıldı.", User.Identity?.Name ?? "Anonim");
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

            _logger.LogWarning("Müşteriler şu filtrelere göre listelendi: Ad: {Ad}, Soyad: {Soyad}, Bölge: {Bölge}, Eposta Alan Adı: {EpostaAlanAdi}, Başlangıç Tarihi: {BaslangicTarihi}, Bitiş Tarihi: {BitisTarihi}",
                firstName, lastName, region, emailDomain, startDate, endDate);

            return customers;
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            _logger.LogWarning("Yeni müşteri oluşturma sayfası {KullanıcıAdı} tarafından açıldı.", User.Identity?.Name ?? "Anonim");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogCritical("Müşteri oluşturulurken geçersiz model durumu. Kullanıcı: {KullanıcıAdı}", User.Identity?.Name ?? "Anonim");
                return View(createCustomerDto);
            }

            try
            {
                var customer = _mapper.Map<Customer>(createCustomerDto);
                await _customerService.TAddAsync(customer);
                _logger.LogWarning("{KullanıcıAdı} tarafından yeni bir müşteri başarıyla oluşturuldu.", User.Identity?.Name ?? "Anonim");
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{KullanıcıAdı} tarafından müşteri oluşturulurken hata meydana geldi: {HataMesajı}", User.Identity?.Name ?? "Anonim", ex.Message);
                ModelState.AddModelError("", $"Müşteri oluşturulurken hata oluştu: {ex.Message}");
                return View(createCustomerDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            _logger.LogWarning("{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşterinin güncelleme sayfası açıldı.", id, User.Identity?.Name ?? "Anonim");

            var customer = await _customerService.TGetByIDAsync(id);
            if (customer == null)
            {
                _logger.LogCritical("ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {KullanıcıAdı}", id, User.Identity?.Name ?? "Anonim");
                return NotFound($"Müşteri bulunamadı: ID {id}");
            }

            var updateCustomerDto = _mapper.Map<UpdateCustomerDto>(customer);
            return View(updateCustomerDto);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerDto updateCustomerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogCritical("Müşteri güncellenirken geçersiz model durumu. Kullanıcı: {KullanıcıAdı}", User.Identity?.Name ?? "Anonim");
                return View(updateCustomerDto);
            }

            try
            {
                var existingCustomer = await _customerService.TGetByIDAsync(id);
                if (existingCustomer == null)
                {
                    _logger.LogCritical("Güncellenmek istenen ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {KullanıcıAdı}", id, User.Identity?.Name ?? "Anonim");
                    ModelState.AddModelError("", "Müşteri bulunamadı.");
                    return View(updateCustomerDto);
                }

                _mapper.Map(updateCustomerDto, existingCustomer);
                await _customerService.TUpdateAsync(existingCustomer);
                _logger.LogWarning("{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşteri başarıyla güncellendi.", User.Identity?.Name ?? "Anonim", id);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşteri güncellenirken hata meydana geldi: {HataMesajı}", id, User.Identity?.Name ?? "Anonim", ex.Message);
                ModelState.AddModelError("", $"Müşteri güncellenirken hata oluştu: {ex.Message}");
                return View(updateCustomerDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                _logger.LogWarning("ID'si {MusteriId} olan müşteri için silme isteği {KullanıcıAdı} tarafından gönderildi.", id, User.Identity?.Name ?? "Anonim");

                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                {
                    _logger.LogCritical("Silinmek istenen ID'si {MusteriId} olan müşteri bulunamadı. Kullanıcı: {KullanıcıAdı}", id, User.Identity?.Name ?? "Anonim");
                    ModelState.AddModelError("", "Silinmek istenen müşteri bulunamadı.");
                    return RedirectToAction(nameof(Index));
                }

                await _customerService.TDeleteAsync(customer);
                _logger.LogWarning("{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşteri başarıyla silindi.", id, User.Identity?.Name ?? "Anonim");
                return RedirectToAction("Index", "Customer", new { deleted = "true" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogCritical(ex, "{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşteri silinirken eşzamanlılık hatası oluştu.", id, User.Identity?.Name ?? "Anonim");
                ModelState.AddModelError("", "Kayıt güncellenmiş veya silinmiş olabilir. Lütfen tekrar deneyin.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{KullanıcıAdı} tarafından ID'si {MusteriId} olan müşteri silinirken hata oluştu: {HataMesajı}", id, User.Identity?.Name ?? "Anonim", ex.Message);
                ModelState.AddModelError("", $"Müşteri silinirken hata oluştu: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}