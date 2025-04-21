using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace IMSNBook.DataAccess.Repository.IRepository
{
    //defining a generic repository interface
    public interface IRepository<T> where T: class //generic class T, so it can be any model like Category, Movie , etc ..., and specifying that T must be type class and not a data type like int,string etc.
    {
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null); //defining method to get all elements from a entity/data table, IEnumerable is a generic type for any collection like List<>, Array, Dictionary. The includeProperties is a list of foreign properties linked to the current model to load them all in the same query for each record
        T Get(Expression<Func<T,bool>> filter, string? includeProperties = null, bool tracked = false); //defining method to get a specific element, instead of providing id, to be more flexible, we are going to provide the LINQ function used to find the item, so it can be like (obj)=> obj.id == 5. And the includeProperties is the same logic explained on the GetAll above
        void Add(T entity); //Add the element provided to the data/table
        //void Update(T entity);//Update the element provided to the data/table, as many times we just update some parts of the element, and it depends on the entity, some people prefer to not include the update and call the update directly from the business logic/controller method, or including a method to it in a specific repository interface
        void Remove(T entity);//Remove the element provided from the data/table
        void RemoveRange(IEnumerable<T> entities); //Remove all elements provided from the data/table. Can be a list of elements for example.

    }
}
