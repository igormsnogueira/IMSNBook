using IMSNBook.DataAccess.Data;
using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IMSNBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SD.Role_Admin")]
    public class CategoryController : Controller
    {
        private readonly IUnityOfWork _unityOfWork; //variable to hold the _unityOfWork
        public CategoryController(IUnityOfWork unityOfWork) //the Dependency Injection will provide an implementation of IUnityOfWork here
        {
            _unityOfWork = unityOfWork;
        }
        public IActionResult Index()
        {
            var categoriesList = _unityOfWork.Category.GetAll(); //using the method we setup in Category Repository to GetAll elements for this entity, but through the unity of work class
            return View(categoriesList);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString()) // business logic : the category name cannot be equal do the display order
            {
                ModelState.AddModelError("name", "The DisplayOrder cannot exactly match the Name.");//creating a custom error for the property Name. the parameter for the property name needs to be lowercase
            }

            //if (obj.Name != null && obj.Name.ToLower() == "test") //other business logic, name cannot be “test”
            //{
            //    ModelState.AddModelError("", "The Name cannot be \"test\"."); //this is a general error that is not specific to a property (we wrote “” on first argument) , so it will be displayed only by a asp-validation-summary div 
            //}

            if (ModelState.IsValid)//if no errors
            {
                _unityOfWork.Category.Add(obj);
                _unityOfWork.Save();
                TempData["success"] = "Category created successfully!";
                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) //if no id provided or if it is 0 , we return the default NotFound page
            {
                return NotFound();
            }

            //Category? categoryFromDb = _db.Categories.Find(id);
            Category? categoryFromDb = _unityOfWork.Category.Get(c => c.Id == id); //using the method we created to get an element, we need to pass the query logic to find the element as lambda expression as defined in the method implementation
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Category obj)
        {
            if (ModelState.IsValid)//if no errors
            {
                _unityOfWork.Category.UpdateCategory(obj);
                _unityOfWork.Save();
                TempData["success"] = "Category updated successfully!";
                return RedirectToAction("Index");
            }
            return View();
        }

        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            //Category? categoryFromDb = _db.Categories.Find(id); //finding the object
            Category? categoryFromDb = _unityOfWork.Category.Get((c) => c.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);//return the view providing the category found
        }


        [HttpPost, ActionName("Delete")]
        public IActionResult DeletePOST(int? id)
        {
            Category? categoryFromDb = _unityOfWork.Category.Get((c) => c.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }

            _unityOfWork.Category.Remove(categoryFromDb);
            _unityOfWork.Save();
            TempData["success"] = "Category deleted successfully!";
            return RedirectToAction("Index");
        }
    }
}
