using IMSNBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMSNBook.DataAccess.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category> //It will already contain all the methods from the IRepository, with the type defined as Category
    {
        void UpdateCategory(Category category); //Adding a method to update category data, because we know this entity will use it in this way. The reason why we did not have an update method in IRepository and ICategory as depending on the entity it changes the way it is done or the need for it
    }
}
