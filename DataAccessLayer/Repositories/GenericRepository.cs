using DataAccessLayer.Abstract;
using DataAccessLayer.Concrete;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class GenericRepository<T> : IGenericDal<T> where T : class
    {
        private readonly CRMContext _crmContext;

        public GenericRepository(CRMContext crmContext)
        {
            _crmContext = crmContext;
        }

        public void Add(T entity)
        {
            _crmContext.Add(entity);
            _crmContext.SaveChanges();
        }

        public void Delete(T entity)
        {
            _crmContext.Remove(entity);
            _crmContext.SaveChanges();
        }


        public T GetbyID(int ID)
        {
            var values= _crmContext.Set<T>().Find(ID);
            return values;
        }

        public List<T> GetListAll()
        {
           var values= _crmContext.Set<T>().ToList();
            return values;
        }

        public void Update(T entity)
        {
            _crmContext.Update(entity);
            _crmContext.SaveChanges();
        }
    }
}
