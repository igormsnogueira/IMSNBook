using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMSNBook.DataAccess.Repository.IRepository
{
    public interface IUnityOfWork
    {
        //set one attribute/variable for each entity/model repository you have. Ex: CategoryRepository, ProductRepository, etc.
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        ICompanyRepository Company { get; }
        IShoppingCartRepository ShoppingCart { get; }
        IApplicationUserRepository ApplicationUser { get; }
        public IOrderHeaderRepository OrderHeader { get; }
        public IOrderDetailRepository OrderDetail { get; }
        void Save(); //method to commit/save/apply all db operations to the database
    }
}
