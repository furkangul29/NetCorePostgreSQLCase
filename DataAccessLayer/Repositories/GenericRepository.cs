using DataAccessLayer.Abstract;
using DataAccessLayer.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataAccessLayer.Repositories
{
    public class GenericRepository<T> : IGenericDal<T> where T : class
    {
        private readonly CRMContext _crmContext;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(CRMContext crmContext)
        {
            _crmContext = crmContext;
            _dbSet = crmContext.Set<T>(); // DbSet'i al
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity); // Varlığı asenkron olarak ekle
            await _crmContext.SaveChangesAsync(); // Değişiklikleri kaydet
        }

        public async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity); // Varlığı sil
            await _crmContext.SaveChangesAsync(); // Değişiklikleri kaydet
        }

        public async Task<T> GetByIDAsync(int id)
        {
            return await _dbSet.FindAsync(id); // ID'ye göre varlığı bul
        }

        public async Task<List<T>> GetListAllAsync()
        {
            return await _dbSet.ToListAsync(); // Tüm varlıkları listele
        }

        public async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity); // Varlığı güncelle
            await _crmContext.SaveChangesAsync(); // Değişiklikleri kaydet
        }
    }
}
