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
    public class CategoryRepository : Repository<Category>, ICategoryRepository 
    {
        private readonly ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db){ 
            _db = db;
        }

        public void UpdateCategory(Category category)
        {
            _db.Categories.Update(category); 
        }
    }
}
