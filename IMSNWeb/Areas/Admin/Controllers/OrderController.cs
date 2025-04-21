using IMSNBook.DataAccess.Repository.IRepository;
using IMSNBook.Models;
using IMSNBook.Models.ViewModels;
using IMSNBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace IMSNBookWeb.Areas.Admin.Controllers
{
    [Area("admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnityOfWork _unityOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnityOfWork unityOfWork)
        {
            _unityOfWork = unityOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Details(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader=_unityOfWork.OrderHeader.Get(u=>u.Id == orderId, includeProperties: "ApplicationUser"),
                OrderDetail= _unityOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId == orderId, includeProperties: "Product")
            };
            return View(OrderVM);
        }
        [HttpPost]
        [Authorize(Roles =SD.Role_Admin+","+SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _unityOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeader.TrackingNumber))
            {
                orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            _unityOfWork.OrderHeader.Update(orderHeaderFromDb);
            _unityOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details),new {orderId = orderHeaderFromDb.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _unityOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unityOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id});
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _unityOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;

            if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
            }

            _unityOfWork.OrderHeader.Update(orderHeader);
            _unityOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _unityOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);

            if (orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions { Reason=RefundReasons.RequestedByCustomer, PaymentIntent=orderHeader.PaymentIntentId };
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unityOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unityOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            _unityOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
        }

        [HttpPost]
        [ActionName("Details")]
        public IActionResult Details_PAY_NOW()
        {
            OrderVM.OrderHeader = _unityOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unityOfWork.OrderDetail.GetAll(u => u.OrderHeaderId == OrderVM.OrderHeader.Id, includeProperties: "Product");

            var domain = "https://localhost:7220/"; //temporarily we need our app domain
            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}", //success endpoint in our app where user will be redirected after paying for the purchase with stripe. We will send it with the OrderHeader id of this order, so we can get it in the action method responsible for the order success view
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}", //if user cancel payment, it goes to this url page
                LineItems = new List<SessionLineItemOptions>(), //creating a list that will hold all the items being purchased
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetail) //iterate over the items in OrderDetail from db to create a stripe item object for each
            {
                var sessionLineItem = new SessionLineItemOptions //creating the stripe item object with the class SessionLineItemOptions of the stripe package
                {
                    PriceData = new SessionLineItemPriceDataOptions //setting up the price details like cost, currency and some product info using the SessionLineItemPriceDataOptions class of the stripe package
                    {
                        UnitAmount = (long)(item.Price * 100), //price for a single item, it needs to be on long format and we need to multiply by 100, an item that costs $22.50 need to have the value here 22500
                        Currency = "usd", //currency
                        ProductData = new SessionLineItemPriceDataProductDataOptions //setting up product details like name, image, etc with this class
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count //quantity of items being purchased, stripe will automatically calculate the final price based on the quantity
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options); //creating a new stripe session service with the options we set up
            _unityOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);//storing the generated session id generated by stripe. Ps: the payment intent id will be null for now, and will only be generated after the payment succeed on the stripe payment page, so we need to update this record on the confirmation page
            _unityOfWork.Save();

            Response.Headers.Add("Location", session.Url); //getting the url from stripe where the user will make the payment, and adding it to the response header "location"

            return new StatusCodeResult(303);//303 means we are redirecting user to a new  url after we received a post request. This method will return this status code, and redirect user to the url set in the response header "Location"
        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {
            OrderHeader orderHeader = _unityOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment) //in this case we know it is a company order
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId); //loading the stripe session with the session id we stored when it was created, before redirecting user to the payment page, so, we can get valuable info from the session like if the payment succeed or not

                if (session.PaymentStatus.ToLower() == "paid") //user paid, so payment is successfull. Values of PaymentStatus can be "paid", "unpaid" or "no_payment_required"
                {
                    _unityOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId); //now the payment succeeded so we can store the generated PaymentIntentId
                    _unityOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved); //updating payment status for the order
                    _unityOfWork.Save();
                }

             
            }
            return View(orderHeaderId);
        }

        #region APICALLS
        [HttpGet]
        public IActionResult GetAll(string status) //will be accessed through /admin/order/getall
        {
            IEnumerable<OrderHeader> ordersList;

            if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                ordersList = _unityOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                ordersList = _unityOfWork.OrderHeader.GetAll(u=>u.ApplicationUserId == userId, includeProperties: "ApplicationUser").ToList();
            }

                switch (status)
                {
                    case "pending":
                        ordersList = ordersList.Where(o => o.PaymentStatus == SD.PaymentStatusDelayedPayment || o.PaymentStatus == SD.PaymentStatusPending);
                        break;
                    case "inprocess":
                        ordersList = ordersList.Where(o => o.OrderStatus == SD.StatusInProcess);
                        break;
                    case "completed":
                        ordersList = ordersList.Where(o => o.OrderStatus == SD.StatusShipped);
                        break;
                    case "approved":
                        ordersList = ordersList.Where(o => o.OrderStatus == SD.StatusApproved);
                        break;
                    default:
                        break;
                }
            return Json(new { data = ordersList }); //convert the data to json format and return
        }
        #endregion
    }
}
