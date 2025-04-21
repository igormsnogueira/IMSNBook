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
    public class CompanyController : Controller
    {
        private readonly IUnityOfWork _unityOfWork; //variable to hold the _unityOfWork
        public CompanyController(IUnityOfWork unityOfWork) 
        {
            _unityOfWork = unityOfWork;
        }
        public IActionResult Index()
        {
            var companiesList = _unityOfWork.Company.GetAll(); //using the method we setup in Company Repository to GetAll elements for this entity, but through the unity of work class
            return View(companiesList);
        }

        public IActionResult Upsert(int? id)
        {
            Company company = new Company();

            if (id == null|| id == 0) //create page
            {
                return View(company);
            }
            else {//update page
                company = _unityOfWork.Company.Get(p => p.Id == id);
                return View(company);
            }
        }

        [HttpPost]
        public IActionResult Upsert(Company company)
        {
            if (ModelState.IsValid)//if no errors
            {

                if (company.Id == 0) {
                    _unityOfWork.Company.Add(company);
                    TempData["success"] = "Company created successfully!";
                }
                else
                {
                    _unityOfWork.Company.Update(company);
                    TempData["success"] = "Company updated successfully!";
                }
                _unityOfWork.Save();
                return RedirectToAction("Index");
            }
            else
            {
                return View(company);
            }
        }

        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0) //if no id provided or if it is 0 , we return the default NotFound page
            {
                return NotFound();
            }

            Company? companyFromDb = _unityOfWork.Company.Get(c => c.Id == id); //using the method we created to get an element, we need to pass the query logic to find the element as lambda expression as defined in the method implementation
            if (companyFromDb == null)
            {
                return NotFound();
            }
            return View(companyFromDb);
        }

        [HttpPost]
        public IActionResult Edit(Company obj)
        {
            if (ModelState.IsValid)//if no errors
            {
                _unityOfWork.Company.Update(obj);
                _unityOfWork.Save();
                TempData["success"] = "Company updated successfully!";
                return RedirectToAction("Index");
            }
            return View();
        }

        //defining a region for api calls is totally optional, regions are just to organize code for ourselves, but it is a good practice
        #region APICALLS
        [HttpGet]
        public IActionResult GetAll() //will be accessed through /admin/company/getall
        {
            var companiesList = _unityOfWork.Company.GetAll().ToList();
            return Json(new { data = companiesList }); //convert the data to json format and return
        }

        [HttpDelete]
        public IActionResult Delete(int? id) //will be accessed through /admin/company/delete/{id}
        {
            Company? companyFromDb = _unityOfWork.Company.Get((c) => c.Id == id);
            if (companyFromDb == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _unityOfWork.Company.Remove(companyFromDb);
            _unityOfWork.Save();

            return Json(new { success = true, message = "Company deleted successfully!" });
        }
        #endregion
    }
}
