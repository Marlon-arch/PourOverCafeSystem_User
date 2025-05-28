using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PourOverCafeSystem_User.Database;
using Microsoft.EntityFrameworkCore;

public class PaymentController : Controller
{
    private readonly PourOverCoffeeDbContext _context;

    public PaymentController(PourOverCoffeeDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public IActionResult UploadProof(int reservationId)
    {
        var payment = new Payment { ReservationId = reservationId };

        var reservation = _context.Reservations
            .Include(r => r.Table)
            .FirstOrDefault(r => r.ReservationId == reservationId);

        ViewBag.GuestCount = reservation?.NumberOfGuests ?? 1;
        ViewBag.TableId = reservation?.TableId;

        ViewBag.GcashNumber = TempData["GcashNumber"]?.ToString() ?? "";
        ViewBag.ReceiptNumber = TempData["ReceiptNumber"]?.ToString() ?? "";

        return View(payment);
    }

    [HttpPost]
    public async Task<IActionResult> UploadProof(Payment payment, IFormFile ScreenshotPath)
    {
        if (ScreenshotPath != null && ScreenshotPath.Length > 0)
        {
            var uploads = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images/uploads");
            if (!Directory.Exists(uploads)) Directory.CreateDirectory(uploads);

            var fileName = $"Reservation_{payment.ReservationId}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(ScreenshotPath.FileName)}";
            var filePath = Path.Combine(uploads, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ScreenshotPath.CopyToAsync(stream);
            }

            payment.ScreenshotPath = "/images/uploads/" + fileName;
            payment.PaymentStatus = "Pending";

            _context.Payments.Add(payment);

            var reservation = await _context.Reservations.FirstOrDefaultAsync(r => r.ReservationId == payment.ReservationId);
            if (reservation != null && reservation.TableId.HasValue)
            {
                var table = await _context.CafeTables.FirstOrDefaultAsync(t => t.TableId == reservation.TableId);
                if (table != null)
                {
                    table.Status = "Reserved";
                }
            }

            await _context.SaveChangesAsync();

            // Call Admin API to broadcast SignalR message
            using (var http = new HttpClient())
            {
                http.BaseAddress = new Uri("https://localhost:7176"); // Admin project URL
                await http.PostAsync("/api/broadcast-refresh", null);
            }

            return RedirectToAction("Countdown", "Timer", new { reservationId = payment.ReservationId });
        }

        TempData["Error"] = "Please upload a valid image file.";
        return View(payment);
    }
    
    [HttpPost]
    public IActionResult StoreTempData([FromBody] GcashTempData data)
    {
        TempData["GcashNumber"] = data.GcashNumber;
        TempData["ReceiptNumber"] = data.ReceiptNumber;
        return Ok();
    }

    public class GcashTempData
    {
        public string GcashNumber { get; set; }
        public string ReceiptNumber { get; set; }
    }
}
