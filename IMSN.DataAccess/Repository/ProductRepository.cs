using IMSNBook.DataAccess.Data;
using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMSNBook.DataAccess.Repository
{
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        private readonly ApplicationDbContext _db;
        public ProductRepository(ApplicationDbContext db) : base(db) {
            _db = db;
        }
        public void Update(Product product)
        {
            var productFromDb = _db.Products.FirstOrDefault(x => x.Id == product.Id);
            if (productFromDb != null)
            {
                productFromDb.CategoryId = product.CategoryId;
                productFromDb.Author = product.Author;
                productFromDb.Title = product.Title;
                productFromDb.Description = product.Description;
                productFromDb.ISBN = product.ISBN;
                productFromDb.ListPrice = product.ListPrice;
                productFromDb.Price = product.Price;
                productFromDb.Price100 = product.Price100;
                productFromDb.Price50 = product.Price50;
                if (product.ImageUrl != null) //we just update ImageUrl if the user provides a new image (image url not null)
                {
                    productFromDb.ImageUrl = product.ImageUrl;
                }
                //As we got the reference from the db object in the var productFromDb, we o not need to call a .Update() here, we already got the reference and manually updated it. You just need to save it now to commit the change, which is done in the unit of work class method
            }
        }
    }
}
