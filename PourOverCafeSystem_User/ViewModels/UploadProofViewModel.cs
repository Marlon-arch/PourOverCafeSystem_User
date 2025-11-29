namespace PourOverCafeSystem_User.ViewModels
{
    public class UploadProofViewModel
    {
        public int ReservationId { get; set; }
        public string GuestName { get; set; }
        public int GuestCount { get; set; }
        public int TableId { get; set; }
        public string GcashNumber { get; set; }
        public string ReceiptNumber { get; set; }
        public string OrderText { get; set; }
    }
}
