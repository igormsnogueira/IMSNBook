using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using IMSNBook.Models.ViewModels;
using IMSNBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace IMSNBookWeb.Areas.Customer.Controllers
{
    [Area("customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnityOfWork _unityOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnityOfWork unityOfWork)
        {
            _unityOfWork = unityOfWork;
        }
        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new() {
                ShoppingCartList = _unityOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties:"Product"),
                OrderHeader = new()
            };

            foreach(ShoppingCart shoppingCart in ShoppingCartVM.ShoppingCartList)
            {
                shoppingCart.Price = GetPriceBasedOnQuantity(shoppingCart);
                ShoppingCartVM.OrderHeader.OrderTotal += shoppingCart.Price * shoppingCart.Count;
            }
            return View(ShoppingCartVM);
        }

        public IActionResult Summary()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            ShoppingCartVM = new()
            {
                ShoppingCartList = _unityOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
                OrderHeader = new()
            };

            ShoppingCartVM.OrderHeader.ApplicationUser = _unityOfWork.ApplicationUser.Get(u=>u.Id ==  userId);

            //set defaults to the user data register from db
            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
            ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
            ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
            ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

            foreach (ShoppingCart shoppingCart in ShoppingCartVM.ShoppingCartList)
            {
                shoppingCart.Price = GetPriceBasedOnQuantity(shoppingCart);
                ShoppingCartVM.OrderHeader.OrderTotal += shoppingCart.Price * shoppingCart.Count;
            }
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        public IActionResult SummaryPost()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            //properties that are not sent on the post request, we need to retrieve manually, even having the ShoppingCartVM property of the class bind to the form
            ShoppingCartVM.ShoppingCartList = _unityOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
            ShoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = userId;
            //We cannot populate a navigation property linked to the OrderHeader here, otherwise when saving OrderHeader to db, Entity framework will try to create a record for the navigation properties associated to it into their corresponding tables, and we do not want a new ApplicationUser
            // ShoppingCartVM.OrderHeader.ApplicationUser = _unityOfWork.ApplicationUser.Get(u => u.Id == userId);
            //Correct way, creating a new user object separately
           
            ApplicationUser applicationUser = _unityOfWork.ApplicationUser.Get(u => u.Id == userId);

            foreach (ShoppingCart shoppingCart in ShoppingCartVM.ShoppingCartList)
            {
                shoppingCart.Price = GetPriceBasedOnQuantity(shoppingCart);
                ShoppingCartVM.OrderHeader.OrderTotal += shoppingCart.Price * shoppingCart.Count;
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)//regular costumer account
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else //company user
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }

            _unityOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unityOfWork.Save(); //as we saved, it will autmatically retireved the saved id to the ShoppinCartVM.OrderHeader.Id

            foreach (var cart in ShoppingCartVM.ShoppingCartList)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                    Price = cart.Price,
                    Count = cart.Count
                };

                _unityOfWork.OrderDetail.Add(orderDetail);
                _unityOfWork.Save();
            }

            if (applicationUser.CompanyId.GetValueOrDefault() == 0) {//regular costumer account
                var domain = "https://localhost:7220/"; //temporarily we need our app domain
                var options = new Stripe.Checkout.SessionCreateOptions
                {
                    SuccessUrl = domain+ $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}", //success endpoint in our app where user will be redirected after paying for the purchase with stripe. We will send it with the OrderHeader id of this order, so we can get it in the action method responsible for the order success view
                    CancelUrl = domain+ "customer/cart/index", //if user cancel payment, it goes to shppping cart index page
                    LineItems = new List<SessionLineItemOptions>(), //creating a list that will hold all the items being purchased
                    Mode = "payment",
                };

                foreach(var item in ShoppingCartVM.ShoppingCartList) //iterate over the items in shopping cart to create a stripe item object for each
                {
                    var sessionLineItem = new SessionLineItemOptions //creating the stripe item object with the class SessionLineItemOptions of the stripe package
                    {
                        PriceData = new SessionLineItemPriceDataOptions //setting up the price details like cost, currency and some product info using the SessionLineItemPriceDataOptions class of the stripe package
                        {
                            UnitAmount = (long)(item.Price * 100), //price for a single item, it needs to be on long format and we need to multiply by 100, an item that costs $22.50 need to have the value here 22500
                            Currency = "usd", //currency
                            ProductData = new SessionLineItemPriceDataProductDataOptions //setting up product details like name, image, etc with this class
                            {
                                Name=item.Product.Title
                            }
                        },
                        Quantity = item.Count //quantity of items being purchased, stripe will automatically calculate the final price based on the quantity
                    };
                    options.LineItems.Add(sessionLineItem);
                }

                var service = new SessionService(); 
                Session session = service.Create(options); //creating a new stripe session service with the options we set up
                _unityOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);//storing the generated session id generated by stripe. Ps: the payment intent id will be null for now, and will only be generated after the payment succeed on the stripe payment page, so we need to update this record on the confirmation page
                _unityOfWork.Save();

                Response.Headers.Add("Location", session.Url); //getting the url from stripe where the user will make the payment, and adding it to the response header "location"

                return new StatusCodeResult(303);//303 means we are redirecting user to a new  url after we received a post request. This method will return this status code, and redirect user to the url set in the response header "Location"
            }



            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id}); //redirect to order confirmation page, and provide the order id to the method that shows the view
        }

        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unityOfWork.OrderHeader.Get(u => u.Id == id, includeProperties: "ApplicationUser");
            if(orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment) //in this case we know it is a regular customer order, and not a company order
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId); //loading the stripe session with the session id we stored when it was created, before redirecting user to the payment page, so, we can get valuable info from the session like if the payment succeed or not

                if(session.PaymentStatus.ToLower() == "paid") //user paid, so payment is successfull. Values of PaymentStatus can be "paid", "unpaid" or "no_payment_required"
                {
                    _unityOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId); //now the payment succeeded so we can store the generated PaymentIntentId
                    _unityOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved); //updating order status and payment status for the order
                    _unityOfWork.Save();
                }

                List<ShoppingCart> shoppingCarts = _unityOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();

                _unityOfWork.ShoppingCart.RemoveRange(shoppingCarts); //clearing the shopping cart for this user
                _unityOfWork.Save();
            }
            return View(id);
        }

        public IActionResult Plus(int cartId)
        {
            var cartFromDb = _unityOfWork.ShoppingCart.Get(u => u.Id == cartId);
            cartFromDb.Count++;
            _unityOfWork.ShoppingCart.UpdateShoppingCart(cartFromDb);
            _unityOfWork.Save();


            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cartFromDb = _unityOfWork.ShoppingCart.Get(u => u.Id == cartId);
            if (cartFromDb.Count <= 1) //removing last item, so we need to delete it, as there is no reason for having an shopping item with quantity 0 in the db
            {
                _unityOfWork.ShoppingCart.Remove(cartFromDb);
            }
            else
            {
                cartFromDb.Count--;
                _unityOfWork.ShoppingCart.UpdateShoppingCart(cartFromDb);
            }
            _unityOfWork.Save();


            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cartFromDb = _unityOfWork.ShoppingCart.Get(u => u.Id == cartId);
            _unityOfWork.ShoppingCart.Remove(cartFromDb);
            _unityOfWork.Save();


            return RedirectToAction(nameof(Index));
        }

        private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
        {
            if(shoppingCart.Count <= 50)
            {
                return shoppingCart.Product.Price;
            }

            if (shoppingCart.Count <= 100)
            {
                return shoppingCart.Product.Price50;
            }

            return shoppingCart.Product.Price100;
        }
    }
}
