using IMSNBook.DataAccess.Data;
using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IMSNBook.DataAccess.Repository
{
    public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository //as we already have defined the logic for the common method required in the IRepository within Repository class, we can simply extend this class as well as the IShoppingCartRepository that requires these method plus the new ones specific for a Category entity
    {
        private readonly ApplicationDbContext _db;
        public ShoppingCartRepository(ApplicationDbContext db) : base(db){ //as we are extending Repository, we need to pass the db to the constructor of this base class as well, because its controller requires it
            _db = db;
        }

        public void UpdateShoppingCart(ShoppingCart shoppingCart)
        {
            _db.ShoppingCarts.Update(shoppingCart); //simply updating the category object with the whole new one passed  via parameter
        }
    }
}
