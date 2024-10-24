using BusinessLayer.Abstract;
using DataAccessLayer.Abstract;
using EntityLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Concrete
{
    public class CustomerManager : ICustomerService
    {
        private readonly ICustomerDal _customerDal;

        public CustomerManager(ICustomerDal customerDal)
        {
            _customerDal = customerDal;
        }

        public Task TAddAsync(Customer entity)
        {
           var values = _customerDal.AddAsync(entity);
            return values;
        }

        public Task TDeleteAsync(Customer entity)
        {
            var values = _customerDal.DeleteAsync(entity);
            return values;
        }

        public Task<Customer> TGetByIDAsync(int id)
        {
            var values = _customerDal.GetByIDAsync(id);
            return values;
        }

        public Task<List<Customer>> TGetListAllAsync()
        {
            var values = _customerDal.GetListAllAsync();
            return values;
        }

        public Task TUpdateAsync(Customer entity)
        {
            var values = _customerDal.UpdateAsync(entity);
            return values;
        }
    }
}
