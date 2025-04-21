using IMSNBook.DataAccess.Data;
using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using IMSNBook.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Diagnostics;

namespace IMSNBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "SD.Role_Admin")]
    public class ProductController : Controller
    {
        private readonly IUnityOfWork _unityOfWork; //variable to hold the _unityOfWork
        private readonly IWebHostEnvironment _webHostEnvironment; //variable to access static files
        public ProductController(IUnityOfWork unityOfWork, IWebHostEnvironment webHostEnvironment) //the Dependency Injection will provide an implementation of IUnityOfWork here, and one for the webHostEnvironment
        {
            _unityOfWork = unityOfWork;
            _webHostEnvironment = webHostEnvironment;
        }
        public IActionResult Index()
        {
            var productsList = _unityOfWork.Product.GetAll(includeProperties: "Category"); //using the method we setup in Product Repository to GetAll elements for this entity, but through the unity of work class
            return View(productsList);
        }

        public IActionResult Upsert(int? id)
        {
            IEnumerable<SelectListItem> categoryList = _unityOfWork.Category.GetAll().Select((p) =>
                new SelectListItem
                {
                    Text = p.Name,
                    Value = p.Id.ToString()
                }
            );

            ProductVM productVM = new()
            {
                Product = new Product(), //passing an empty product with default values, so we can set them in the Create view
                CategoryList = categoryList
            };

            if (id == null|| id == 0) //create page
            {
                return View(productVM);
            }
            else {//update page
                productVM.Product = _unityOfWork.Product.Get(p => p.Id == id);
                return View(productVM);
            }
        }

        [HttpPost]
        public IActionResult Upsert(ProductVM productVM, IFormFile? file)
        {
            if (ModelState.IsValid)//if no errors
            {
                string wwwRootPath = _webHostEnvironment.WebRootPath;//WebRootPath is the wwwroot folder
                if(file != null)
                {
                    string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName); //using Guid to generate a random string to use as file name, and getting the extension from the file received in the request, to keep the same extension
                    string productPath = Path.Combine(wwwRootPath, @"images\products"); //get the full path  where we store the image, joining the wwwRootPath with the images/product path that is locate in it. We use @ before the string to make it literal so \ wont be considered a scape character. Also we are using backslash \ because it is windows environment, otherwise we would use /

                    if (!string.IsNullOrEmpty(productVM.Product.ImageUrl)) //for cases of updating data, if the ImageUrl is already set (not null nor empty), there is an image already saved, so we delete it first
                    {
                        var oldImagePath = Path.Combine(wwwRootPath,productVM.Product.ImageUrl.TrimStart('\\')); //as the image path is being saved with the \ at the beginning, we need to trim/remove it from the start, because the wwwRootPath already includes it, 
                        if (System.IO.File.Exists(oldImagePath)) { //check if this file exists in the project
                            System.IO.File.Delete(oldImagePath); //deleting the old file
                        }
                    }

                    using (var fileStream = new FileStream(Path.Combine(productPath, fileName), FileMode.Create))//create a stream to deal with files and providing the path and name of the file together as its full address, and as we are adding/creating a file, we pass the mode FileMode.Create
                    {
                        file.CopyTo(fileStream); //saving the file to the filestream/provided path including the name
                    }
                    productVM.Product.ImageUrl = @"\images\products\"+fileName; //setting the path to the url in the Model property to store in the db, we just need to store the path from within the wwwroot, and the filename, not before it. Because we just need this piece to display it in the html <img> src.
                }

                if (productVM.Product.Id == 0) {
                    _unityOfWork.Product.Add(productVM.Product);
                    TempData["success"] = "Product created successfully!";
                }
                else
                {
                    _unityOfWork.Product.Update(productVM.Product);
                    TempData["success"] = "Product updated successfully!";
                }
                _unityOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {
                productVM.CategoryList = _unityOfWork.Category.GetAll().Select((p) =>
                        new SelectListItem
                        {
                            Text = p.Name,
                            Value = p.Id.ToString()
                        }
                );

                return View(productVM);
            }
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) //if no id provided or if it is 0 , we return the default NotFound page
            {
                return NotFound();
            }

            //Product? productFromDb = _db.Products.Find(id);
            Product? productFromDb = _unityOfWork.Product.Get(c => c.Id == id); //using the method we created to get an element, we need to pass the query logic to find the element as lambda expression as defined in the method implementation
            if (productFromDb == null)
            {
                return NotFound();
            }
            return View(productFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Product obj)
        {
            if (ModelState.IsValid)//if no errors
            {
                _unityOfWork.Product.Update(obj);
                _unityOfWork.Save();
                TempData["success"] = "Product updated successfully!";
                return RedirectToAction("Index");
            }
            return View();
        }

        //defining a region for api calls is totally optional, regions are just to organize code for ourselves, but it is a good practice
        #region APICALLS
        [HttpGet]
        public IActionResult GetAll() //will be accessed through /admin/product/getall
        {
            var productsList = _unityOfWork.Product.GetAll(includeProperties: "Category");
            return Json(new { data = productsList }); //convert the data to json format and return
        }

        [HttpDelete]
        public IActionResult Delete(int? id) //will be accessed through /admin/product/delete/{id}
        {
            Product? productFromDb = _unityOfWork.Product.Get((c) => c.Id == id);
            if (productFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            string productPath = Path.Combine(_webHostEnvironment.WebRootPath, @"images\products");

            if (!string.IsNullOrEmpty(productFromDb.ImageUrl)) 
            {
                var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, productFromDb.ImageUrl.TrimStart('\\')); 
                if (System.IO.File.Exists(oldImagePath))
                { 
                    System.IO.File.Delete(oldImagePath); 
                }
            }

            _unityOfWork.Product.Remove(productFromDb);
            _unityOfWork.Save();

            return Json(new { success = true, message = "Product deleted successfully!" });
        }
        #endregion
    }
}
