using BusinessLayer.Abstract;
using EntityLayer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

[ApiController]
[Route("api/customers/filter")]
public class CustomerFiltersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomerFiltersController> _logger;

    public CustomerFiltersController(ICustomerService customerService, ILogger<CustomerFiltersController> logger)
    {
        _customerService = customerService;
        _logger = logger;
    }

    [HttpGet("name/{name}")]
    public ActionResult<IEnumerable<Customer>> FilterByName(string name)
    {
        try
        {
            var customers = _customerService.TGetListAll()
                .Where(c => (c.FirstName + " " + c.LastName)
                .StartsWith(name, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!customers.Any())
                return NotFound("Girilen isim kriterine uygun kayıt bulunamadı.");

            return Ok(new { TotalCount = customers.Count, Data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "İsme göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("region/{region}")]
    public ActionResult<IEnumerable<Customer>> FilterByRegion(string region)
    {
        try
        {
            var customers = _customerService.TGetListAll()
                .Where(c => c.Region.StartsWith(region, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!customers.Any())
                return NotFound("Girilen bölge kriterine uygun kayıt bulunamadı.");

            return Ok(new { TotalCount = customers.Count, Data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bölgeye göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("date-range")]
    public ActionResult<IEnumerable<Customer>> FilterByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            var customers = _customerService.TGetListAll()
                .Where(c => c.RegistrationDate >= startDate && c.RegistrationDate <= endDate)
                .ToList();

            return Ok(new { TotalCount = customers.Count, Data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tarih aralığına göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("email/{domain}")]
    public ActionResult<IEnumerable<Customer>> FilterByEmailDomain(string domain)
    {
        try
        {
            var customers = _customerService.TGetListAll()
           .Where(c => c.Email.Split('@')[0].StartsWith(domain, StringComparison.OrdinalIgnoreCase))
                .ToList();

            return Ok(new { TotalCount = customers.Count, Data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email domaine göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("registration-month/{monthInput}")]
    public ActionResult<IEnumerable<Customer>> FilterByRegistrationMonth(string monthInput)
    {
        try
        {
            if (int.TryParse(monthInput, out int month) && month >= 1 && month <= 12)
            {
                var customers = _customerService.TGetListAll()
                    .Where(c => c.RegistrationDate.Month == month)
                    .ToList();

                if (!customers.Any())
                    return NotFound($"{month}. aya ait kayıt bulunamadı.");

                return Ok(new { TotalCount = customers.Count, Data = customers });
            }
            else
            {
                var turkishMonths = new Dictionary<string, int>
            {
                {"ocak", 1}, {"şubat", 2}, {"mart", 3},
                {"nisan", 4}, {"mayıs", 5}, {"haziran", 6},
                {"temmuz", 7}, {"ağustos", 8}, {"eylül", 9},
                {"ekim", 10}, {"kasım", 11}, {"aralık", 12}
            };

                var matchingMonths = turkishMonths
                    .Where(m => m.Key.StartsWith(monthInput.ToLower()))
                    .Select(m => m.Value)
                    .ToList();

                if (!matchingMonths.Any())
                    return BadRequest("Geçerli bir ay bulunamadı.");

                var customers = _customerService.TGetListAll()
                    .Where(c => matchingMonths.Contains(c.RegistrationDate.Month))
                    .ToList();

                if (!customers.Any())
                    return NotFound($"{monthInput} ile başlayan aylara ait kayıt bulunamadı.");

                return Ok(new { TotalCount = customers.Count, Data = customers });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kayıt ayına göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    [HttpGet("recent/{days}")]
    public ActionResult<IEnumerable<Customer>> FilterByRecentRegistration(int days)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-days);
            var customers = _customerService.TGetListAll()
                .Where(c => c.RegistrationDate >= cutoffDate)
                .ToList();

            return Ok(new { TotalCount = customers.Count, Data = customers });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Son kayıtlara göre filtreleme hatası");
            return StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}