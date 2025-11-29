using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PourOverCafeSystem_User.Database;

namespace PourOverCafeSystem_User.Controllers
{
    public class ReservationController : Controller
    {
        private readonly PourOverCoffeeDbContext _context;

        public ReservationController(PourOverCoffeeDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> SelectTable(int? numberOfGuests)
        {
            if (numberOfGuests == null || numberOfGuests < 1 || numberOfGuests > 6)
            {
                TempData["Error"] = "Please enter a valid number of guests (1 to 6).";
                return RedirectToAction("Index", "Home");
            }

            var tables = await _context.CafeTables.ToListAsync();

            ViewBag.NumberOfGuests = numberOfGuests;

            return View(tables);
        }

        [HttpGet]
        public async Task<IActionResult> ReservationForm(int tableId, int guestCount)
        {
            var table = await _context.CafeTables.FindAsync(tableId);
            if (table == null || table.Status != "Available")
            {
                TempData["Error"] = "Selected table is no longer available.";
                return RedirectToAction("SelectTable", new { numberOfGuests = guestCount });
            }

            ViewBag.GuestCount = guestCount;
            ViewBag.Table = table;

            ViewBag.GuestName = TempData["GuestName"]?.ToString() ?? "";
            ViewBag.ContactNumber = TempData["ContactNumber"]?.ToString() ?? "";
            ViewBag.OrderText = TempData["OrderText"]?.ToString() ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ReservationForm(int tableId, int guestCount, string guestName, string contactNumber, string? orderText)
        {
            var table = await _context.CafeTables.FindAsync(tableId);
            if (table == null || table.Status != "Available")
            {
                TempData["Error"] = "Selected table is no longer available.";
                return RedirectToAction("SelectTable", new { numberOfGuests = guestCount });
            }

            var reservation = new Reservation
            {
                TableId = tableId,
                GuestName = guestName,
                ContactNumber = contactNumber,
                NumberOfGuests = guestCount,
                OrderText = orderText,
                ReservationStatus = "Pending",
                ReservationDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila"))
            };

            TempData["GuestName"] = guestName;
            TempData["ContactNumber"] = contactNumber;
            TempData["OrderText"] = orderText;


            _context.Reservations.Add(reservation);
            await _context.SaveChangesAsync();

            return RedirectToAction("UploadProof", "Payment", new { reservationId = reservation.ReservationId });
        }
    }
}
