using AutoMapper;
using BusinessLayer.Abstract;
using DataAccessLayer.Concrete;
using DtoLayer.CustomerDtos;
using EntityLayer;
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

        public CustomerController(IHttpClientFactory httpClientFactory, ICustomerService customerService, IMapper mapper, ILogger<CustomerController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _customerService = customerService;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string firstNameFilter, string lastNameFilter, string regionFilter, string emailDomainFilter, DateTime? startDate, DateTime? endDate)
        {
            _logger.LogInformation("Customer list page accessed by user {UserId}", User.Identity?.Name ?? "Anonymous");

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

            _logger.LogInformation("Filters applied: FirstName: {FirstName}, LastName: {LastName}, Region: {Region}, Email: {Email}, StartDate: {StartDate}, EndDate: {EndDate}",
                firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                var filteredCustomers = FilterCustomers(customers, firstNameFilter, lastNameFilter, regionFilter, emailDomainFilter, startDate, endDate);
                _logger.LogInformation("AJAX request made by user {UserId} with applied filters", User.Identity?.Name ?? "Anonymous");
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

            _logger.LogInformation("Customers filtered with FirstName: {FirstName}, LastName: {LastName}, Region: {Region}, EmailDomain: {EmailDomain}, StartDate: {StartDate}, EndDate: {EndDate}",
                firstName, lastName, region, emailDomain, startDate, endDate);

            return customers;
        }

        [HttpGet]
        public IActionResult CreateCustomer()
        {
            _logger.LogInformation("Create customer page accessed by user {UserId}", User.Identity?.Name ?? "Anonymous");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer(CreateCustomerDto createCustomerDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for creating a customer by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View(createCustomerDto);
            }

            try
            {
                var customer = _mapper.Map<Customer>(createCustomerDto);
                await _customerService.TAddAsync(customer);
                _logger.LogInformation("Customer created successfully by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating customer by user {UserId}", User.Identity?.Name ?? "Anonymous");
                ModelState.AddModelError("", $"Müşteri oluşturulurken hata oluştu: {ex.Message}");
                return View(createCustomerDto);
            }
        }

        [HttpGet]
        public async Task<IActionResult> UpdateCustomer(int id)
        {
            _logger.LogInformation("Update customer page accessed for ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");

            var customer = await _customerService.TGetByIDAsync(id);
            if (customer == null)
            {
                _logger.LogWarning("Customer not found for ID {CustomerId} accessed by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
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
                _logger.LogWarning("Invalid model state for updating a customer by user {UserId}", User.Identity?.Name ?? "Anonymous");
                return View(updateCustomerDto);
            }

            try
            {
                var existingCustomer = await _customerService.TGetByIDAsync(id);
                if (existingCustomer == null)
                {
                    _logger.LogWarning("Customer not found while attempting to update ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                    ModelState.AddModelError("", "Müşteri bulunamadı.");
                    return View(updateCustomerDto);
                }

                _mapper.Map(updateCustomerDto, existingCustomer);
                await _customerService.TUpdateAsync(existingCustomer);
                _logger.LogInformation("Customer updated successfully for ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                return RedirectToAction("Index", "Customer");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating customer ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                ModelState.AddModelError("", $"Müşteri güncellenirken hata oluştu: {ex.Message}");
                return View(updateCustomerDto);
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            try
            {
                _logger.LogInformation("Delete request for customer ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");

                var customer = await _customerService.TGetByIDAsync(id);
                if (customer == null)
                {
                    _logger.LogWarning("Customer not found for deletion attempt ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                    ModelState.AddModelError("", "Silinmek istenen müşteri bulunamadı.");
                    return RedirectToAction(nameof(Index));
                }

                await _customerService.TDeleteAsync(customer);
                _logger.LogInformation("Customer deleted successfully ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                return RedirectToAction("Index", "Customer", new { deleted = "true" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogWarning(ex, "Concurrency error while deleting customer ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                ModelState.AddModelError("", "Kayıt güncellenmiş veya silinmiş olabilir. Lütfen tekrar deneyin.");
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting customer ID {CustomerId} by user {UserId}", id, User.Identity?.Name ?? "Anonymous");
                ModelState.AddModelError("", $"Müşteri silinirken hata oluştu: {ex.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
    }
}
