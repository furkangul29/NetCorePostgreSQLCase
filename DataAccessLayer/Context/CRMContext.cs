using EntityLayer;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccessLayer.Context
{
    public class CRMContext:DbContext
    {
        public CRMContext(DbContextOptions<CRMContext> options) : base(options) { }
        public DbSet<Customer> Customers { get; set; }
        public DbSet<User> Users { get; set; }

       
    }
    
}
