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
    public class UserManager : IUserService
    {
        private readonly IUserDal _userDal;

        public UserManager(IUserDal userDal)
        {
            _userDal = userDal;
        }

        public void TAdd(User entity)
        {
            _userDal.Add(entity);
        }

        public void TDelete(User entity)
        {
            _userDal.Delete(entity);
        }

        public User TGetbyID(int ID)
        {
           var values= _userDal.GetbyID(ID);
            return values;
        }

        public List<User> TGetListAll()
        {
            var values = _userDal.GetListAll();
            return values;
        }

        public void TUpdate(User entity)
        {
           _userDal.Update(entity);
        }
    }
}
