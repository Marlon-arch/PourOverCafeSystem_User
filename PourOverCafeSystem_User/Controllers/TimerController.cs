using Microsoft.AspNetCore.Mvc;
using PourOverCafeSystem_User.Database;
using PourOverCafeSystem_User.ViewModels;
using System.Linq;

namespace PourOverCafeSystem_User.Controllers
{
    public class TimerController : Controller
    {
        private readonly PourOverCoffeeDbContext _context;

        public TimerController(PourOverCoffeeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Countdown(int reservationId)
        {
            var timer = _context.Timers.FirstOrDefault(t => t.ReservationId == reservationId);

            var model = new CountdownViewModel
            {
                ReservationId = reservationId,
                EndTime = timer?.EndTime ?? DateTime.MinValue
            };

            return View(model);
        }

        [HttpGet]
        public IActionResult Status(int reservationId)
        {
            var timer = _context.Timers.FirstOrDefault(t => t.ReservationId == reservationId);
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
            var payment = _context.Payments
                .Where(p => p.ReservationId == reservationId)
                .OrderByDescending(p => p.PaymentId)
                .FirstOrDefault();

            return Json(new
            {
                isDisapproved = payment?.PaymentStatus == "Cancelled" && !string.IsNullOrEmpty(payment.Remarks),
                remarks = payment?.Remarks,
                cancelled = reservation?.ReservationStatus == "Cancelled" && string.IsNullOrEmpty(payment?.Remarks),
                expired = reservation?.ReservationStatus == "Expired",
                arrived = timer?.Arrived == true,
                approved = reservation?.ReservationStatus == "Approved",
                endTime = timer?.EndTime
            });
        }

        [HttpGet]
        public IActionResult Expire(int reservationId)
        {
            var reservation = _context.Reservations.FirstOrDefault(r => r.ReservationId == reservationId);
            if (reservation != null)
            {
                reservation.ReservationStatus = "Expired";

                var payment = _context.Payments.FirstOrDefault(p => p.ReservationId == reservationId);
                if (payment != null) payment.PaymentStatus = "Cancelled";

                if (reservation.TableId.HasValue)
                {
                    var table = _context.CafeTables.FirstOrDefault(t => t.TableId == reservation.TableId);
                    if (table != null) table.Status = "Available";
                }

                _context.SaveChanges();
            }
            return Ok();
        }
    }
}
