using AutoMapper;
using BusinessLayer.Abstract;
using DataAccessLayer.Concrete;
using DtoLayer.CustomerDtos;
using EntityLayer;
using Microsoft.AspNetCore.Mvc;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using WebUI.Models;

namespace WebUI.Controllers
{
    public class CustomerController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICustomerService _customerService;
        private readonly IMapper _mapper;

        public CustomerController(IHttpClientFactory httpClientFactory, ICustomerService customerService, IMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _customerService = customerService;
            _mapper = mapper;
        }

        public async Task<IActionResult> Index(string firstNameFilter, string lastNameFilter,
        string regionFilter, string emailDomainFilter, DateTime? startDate, DateTime? endDate)
        {
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

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var filteredCustomers = FilterCustomers(customers, firstNameFilter,
                    lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);
                return Json(_mapper.Map<List<ResultCustomerDto>>(filteredCustomers));
            }

            return View(model);
        }

        private IEnumerable<Customer> FilterCustomers(IEnumerable<Customer> customers,
            string firstName, string lastName, string region, string emailDomain,
            DateTime? startDate, DateTime? endDate)
        {
            if (!string.IsNullOrEmpty(firstName))
                customers = customers.Where(c => c.FirstName.StartsWith(firstName,
                    StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(lastName))
                customers = customers.Where(c => c.LastName.StartsWith(lastName,
                    StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(region))
                customers = customers.Where(c => c.Region.StartsWith(region,
                    StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(emailDomain))
            {
                customers = customers.Where(c => c.Email.Split('@').Length > 1 &&
                    c.Email.Split('@')[1].StartsWith(emailDomain,
                    StringComparison.OrdinalIgnoreCase));
            }

            if (startDate.HasValue)
                customers = customers.Where(c => c.RegistrationDate.Date >= startDate.Value.Date);

            if (endDate.HasValue)
                customers = customers.Where(c => c.RegistrationDate.Date <= endDate.Value.Date);

            return customers;
        }


        [HttpGet]
        public IActionResult CreateCustomer()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            if (!ModelState.IsValid)
            {
                return View(createCustomerDto);
            }

            try
            {
                var customer = _mapper.Map<Customer>(createCustomerDto);
                await _customerService.TAddAsync(customer);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Müşteri oluşturulurken hata oluştu: {ex.Message}");
                return View(createCustomerDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            var customer = await _customerService.TGetByIDAsync(id);
            if (customer == null)
            {
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
                return View(updateCustomerDto);
            }

            try
            {
                var existingCustomer = await _customerService.TGetByIDAsync(id);
                if (existingCustomer == null)
                {
                    ModelState.AddModelError("", "Müşteri bulunamadı.");
                    return View(updateCustomerDto);
                }


                _mapper.Map(updateCustomerDto, existingCustomer);


                await _customerService.TUpdateAsync(existingCustomer);
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Müşteri güncellenirken hata oluştu: {ex.Message}");
                return View(updateCustomerDto);
            }
        }


        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                {
                    ModelState.AddModelError("", "Silinmek istenen müşteri bulunamadı.");
                    return RedirectToAction(nameof(Index));
                }

                await _customerService.TDeleteAsync(customer);
                return RedirectToAction("Index", "Customer", new { deleted = "true" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ModelState.AddModelError("", "Kayıt güncellenmiş veya silinmiş olabilir. Lütfen tekrar deneyin.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Müşteri silinirken hata oluştu: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
      
    }
}
