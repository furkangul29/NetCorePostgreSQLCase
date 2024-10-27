using DtoLayer.CustomerDtos;

namespace WebUI.Models
{
    public class FilteredCustomerViewModel
    {
        public List<ResultCustomerDto> Customers { get; set; }
        public string FirstNameFilter { get; set; }
        public string LastNameFilter { get; set; }
        public string RegionFilter { get; set; }
        public string EmailFilter { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool HasFilterPermission { get; set; }

    }
}
