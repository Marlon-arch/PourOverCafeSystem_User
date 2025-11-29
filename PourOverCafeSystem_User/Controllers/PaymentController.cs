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
    public async Task<IActionResult> UploadProof(Payment payment, IFormFile screenshot, [FromServices] CloudinaryService cloudinary)
    {
        try
        {
            if (screenshot != null && screenshot.Length > 0)
            {
                Console.WriteLine("Screenshot received: " + screenshot.FileName);
                var imageUrl = await cloudinary.UploadImageAsync(screenshot);
                Console.WriteLine("Uploaded URL: " + imageUrl);
                payment.ScreenshotPath = imageUrl;
            }
            else
            {
                Console.WriteLine("Screenshot is null or empty.");
                TempData["Error"] = "Please upload a valid image file.";
                return RedirectToAction("UploadProof", new { reservationId = payment.ReservationId });
            }

            payment.PaymentStatus = "Pending";
            _context.Payments.Add(payment);

            // Update table status to Reserved
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
            try
            {
                using (var http = new HttpClient())
                {
                    http.BaseAddress = new Uri("http://mjbondoc-001-site2.anytempurl.com/"); // Admin project URL
                    await http.PostAsync("/api/broadcast-refresh", null);
                }
            }
            catch (Exception signalEx)
            {
                Console.WriteLine("SignalR broadcast failed: " + signalEx.Message);
            }

            return RedirectToAction("Countdown", "Timer", new { reservationId = payment.ReservationId });
        }
        catch (Exception ex)
        {
            Console.WriteLine("UPLOAD ERROR: " + ex.Message);
            TempData["Error"] = "Upload failed. Try again.";
            return RedirectToAction("UploadProof", new { reservationId = payment.ReservationId });
        }
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
