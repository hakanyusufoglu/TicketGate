using TicketGate.Booking.Domain.Enums;

namespace TicketGate.Booking.Domain.Entities;

/// <summary>
/// Bilet varligi. Durum gecisleri Available, Reserved, Confirmed ve Cancelled sirasiyla yalnizca bu siniftaki metodlar uzerinden yapilir.
/// xmin PostgreSQL sistem kolonu ile optimistic concurrency korumasi saglanir.
/// State machine mantigi domain icinde kapsullenir; handler'lar alanlari dogrudan degistiremez.
/// </summary>
public sealed class Ticket
{
    private Ticket()
    {
        Seat = string.Empty;
    }

    private Ticket(Guid eventId, string seat, decimal price)
    {
        Id = Guid.NewGuid();
        EventId = eventId;
        Seat = seat;
        Price = price;
        Status = TicketStatus.Available;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid EventId { get; private set; }

    public string Seat { get; private set; }

    public decimal Price { get; private set; }

    public TicketStatus Status { get; private set; }

    public Guid? LockedByUserId { get; private set; }

    public DateTime? LockedAt { get; private set; }

    public Guid? BookedByUserId { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Yeni bileti Available durumunda olusturur.
    /// State machine baslangici domain factory ile merkezi hale getirilir.
    /// </summary>
    public static Ticket Create(Guid eventId, string seat, decimal price)
    {
        return new Ticket(eventId, seat, price);
    }

    /// <summary>
    /// Bileti rezerve eder. Available durumundan Reserved durumuna gecer.
    /// Yalnizca Redis lock basariyla alindiktan sonra handler tarafindan cagrilmalidir.
    /// </summary>
    public void Reserve(Guid userId)
    {
        if (Status != TicketStatus.Available)
        {
            return;
        }

        Status = TicketStatus.Reserved;
        LockedByUserId = userId;
        LockedAt = DateTime.UtcNow;
        UpdatedAt = LockedAt.Value;
    }

    /// <summary>
    /// Odeme tamamlandiginda bileti onaylar. Reserved durumundan Confirmed durumuna gecer.
    /// Redis lock sahibi kontrolu handler'da yapilir; domain yalnizca state gecisini kapsuller.
    /// </summary>
    public void Confirm(Guid userId)
    {
        if (Status != TicketStatus.Reserved)
        {
            return;
        }

        Status = TicketStatus.Confirmed;
        BookedByUserId = userId;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Kilidi kaldirir ve bileti tekrar satisa acar. Reserved durumundan Available durumuna gecer.
    /// TTL expire veya kullanici vazgecmesi durumunda TicketLockExpiredWorker tarafindan cagrilir.
    /// </summary>
    public void Release()
    {
        if (Status != TicketStatus.Reserved)
        {
            return;
        }

        Status = TicketStatus.Available;
        LockedByUserId = null;
        LockedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Iade sonrasi bileti tekrar satisa acar. Confirmed durumundan Available durumuna gecer.
    /// PaymentRefunded event'i gelince Booking handler'i tarafindan cagrilir.
    /// Release() metodundan farki Reserved degil Confirmed durumundan gelmesidir.
    /// </summary>
    public void ReleaseAfterRefund()
    {
        if (Status != TicketStatus.Confirmed)
        {
            return;
        }

        Status = TicketStatus.Available;
        BookedByUserId = null;
        LockedByUserId = null;
        LockedAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Organizator veya admin iptalinde bileti iptal eder. Confirmed durumundan Cancelled durumuna gecer.
    /// Bu akis iade degildir; bilet tekrar satisa acilmaz ve odeme iadesi ayri yurutulur.
    /// </summary>
    public void Cancel()
    {
        if (Status != TicketStatus.Confirmed)
        {
            return;
        }

        Status = TicketStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;
    }
}
