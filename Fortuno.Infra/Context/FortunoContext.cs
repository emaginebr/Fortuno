using Fortuno.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Context;

public class FortunoContext : DbContext
{
    public FortunoContext(DbContextOptions<FortunoContext> options) : base(options) { }

    public DbSet<Lottery> Lotteries => Set<Lottery>();
    public DbSet<LotteryImage> LotteryImages => Set<LotteryImage>();
    public DbSet<LotteryCombo> LotteryCombos => Set<LotteryCombo>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Raffle> Raffles => Set<Raffle>();
    public DbSet<RaffleAward> RaffleAwards => Set<RaffleAward>();
    public DbSet<RaffleWinner> RaffleWinners => Set<RaffleWinner>();
    public DbSet<UserReferrer> UserReferrers => Set<UserReferrer>();
    public DbSet<InvoiceReferrer> InvoiceReferrers => Set<InvoiceReferrer>();
    public DbSet<RefundLog> RefundLogs => Set<RefundLog>();
    public DbSet<NumberReservation> NumberReservations => Set<NumberReservation>();
    public DbSet<TicketOrder> TicketOrders => Set<TicketOrder>();
    public DbSet<TicketOrderNumber> TicketOrderNumbers => Set<TicketOrderNumber>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureLottery(modelBuilder);
        ConfigureLotteryImage(modelBuilder);
        ConfigureLotteryCombo(modelBuilder);
        ConfigureTicket(modelBuilder);
        ConfigureRaffle(modelBuilder);
        ConfigureRaffleAward(modelBuilder);
        ConfigureRaffleWinner(modelBuilder);
        ConfigureUserReferrer(modelBuilder);
        ConfigureInvoiceReferrer(modelBuilder);
        ConfigureRefundLog(modelBuilder);
        ConfigureNumberReservation(modelBuilder);
        ConfigureTicketOrder(modelBuilder);
        ConfigureTicketOrderNumber(modelBuilder);
    }

    private static void ConfigureLottery(ModelBuilder mb)
    {
        mb.Entity<Lottery>(e =>
        {
            e.ToTable("fortuna_lotteries");
            e.HasKey(x => x.LotteryId).HasName("fortuna_lotteries_pkey");
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").UseIdentityAlwaysColumn();
            e.Property(x => x.StoreId).HasColumnName("store_id").IsRequired();
            e.Property(x => x.StoreClientId).HasColumnName("store_client_id").HasColumnType("varchar(64)");
            e.Property(x => x.EditionNumber).HasColumnName("edition_number").HasDefaultValue(1).IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(160)").IsRequired();
            e.Property(x => x.Slug).HasColumnName("slug").HasColumnType("varchar(200)").IsRequired();
            e.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("fortuna_lotteries_slug_uq");
            e.Property(x => x.DescriptionMd).HasColumnName("description_md").HasColumnType("text").IsRequired();
            e.Property(x => x.RulesMd).HasColumnName("rules_md").HasColumnType("text").IsRequired();
            e.Property(x => x.PrivacyPolicyMd).HasColumnName("privacy_policy_md").HasColumnType("text").IsRequired();
            e.Property(x => x.TicketPrice).HasColumnName("ticket_price").HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.TotalPrizeValue).HasColumnName("total_prize_value").HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.TicketMin).HasColumnName("ticket_min").HasDefaultValue(0);
            e.Property(x => x.TicketMax).HasColumnName("ticket_max").HasDefaultValue(0);
            e.Property(x => x.TicketNumIni).HasColumnName("ticket_num_ini").HasDefaultValue(1L);
            e.Property(x => x.TicketNumEnd).HasColumnName("ticket_num_end").HasDefaultValue(0L);
            e.Property(x => x.NumberType).HasColumnName("number_type").HasConversion<int>().HasDefaultValue(Fortuno.Domain.Enums.NumberType.Int64);
            e.Property(x => x.NumberValueMin).HasColumnName("number_value_min").IsRequired();
            e.Property(x => x.NumberValueMax).HasColumnName("number_value_max").IsRequired();
            e.Property(x => x.ReferralPercent).HasColumnName("referral_percent").HasDefaultValue(0f);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(Fortuno.Domain.Enums.LotteryStatus.Draft);
            e.Property(x => x.CancelReason).HasColumnName("cancel_reason").HasColumnType("varchar(1000)");
            e.Property(x => x.CancelledByUserId).HasColumnName("cancelled_by_user_id");
            e.Property(x => x.CancelledAt).HasColumnName("cancelled_at").HasColumnType("timestamp without time zone");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            e.HasIndex(x => x.Status).HasDatabaseName("fortuna_lotteries_status_ix");
        });
    }

    private static void ConfigureLotteryImage(ModelBuilder mb)
    {
        mb.Entity<LotteryImage>(e =>
        {
            e.ToTable("fortuna_lottery_images");
            e.HasKey(x => x.LotteryImageId).HasName("fortuna_lottery_images_pkey");
            e.Property(x => x.LotteryImageId).HasColumnName("lottery_image_id").UseIdentityAlwaysColumn();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.ImageUrl).HasColumnName("image_url").HasColumnType("varchar(500)").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasColumnType("varchar(260)");
            e.Property(x => x.DisplayOrder).HasColumnName("display_order").HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Lottery)
             .WithMany(l => l.Images)
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_lottery_image")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureLotteryCombo(ModelBuilder mb)
    {
        mb.Entity<LotteryCombo>(e =>
        {
            e.ToTable("fortuna_lottery_combos");
            e.HasKey(x => x.LotteryComboId).HasName("fortuna_lottery_combos_pkey");
            e.Property(x => x.LotteryComboId).HasColumnName("lottery_combo_id").UseIdentityAlwaysColumn();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(120)").IsRequired();
            e.Property(x => x.DiscountValue).HasColumnName("discount_value").HasDefaultValue(0f);
            e.Property(x => x.DiscountLabel).HasColumnName("discount_label").HasColumnType("varchar(80)").IsRequired();
            e.Property(x => x.QuantityStart).HasColumnName("quantity_start").HasDefaultValue(0);
            e.Property(x => x.QuantityEnd).HasColumnName("quantity_end").HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Lottery)
             .WithMany(l => l.Combos)
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_lottery_combo")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTicket(ModelBuilder mb)
    {
        mb.Entity<Ticket>(e =>
        {
            e.ToTable("fortuna_tickets");
            e.HasKey(x => x.TicketId).HasName("fortuna_tickets_pkey");
            e.Property(x => x.TicketId).HasColumnName("ticket_id").UseIdentityAlwaysColumn();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
            e.Property(x => x.TicketNumber).HasColumnName("ticket_number").IsRequired();
            e.Property(x => x.TicketValue).HasColumnName("ticket_value").HasColumnType("varchar(64)").IsRequired().HasDefaultValue("");
            e.Property(x => x.RefundState).HasColumnName("refund_state").HasConversion<int>().HasDefaultValue(Fortuno.Domain.Enums.TicketRefundState.None);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Lottery)
             .WithMany(l => l.Tickets)
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_ticket")
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.LotteryId, x.TicketNumber }).IsUnique().HasDatabaseName("fortuna_tickets_lottery_number_uq");
            e.HasIndex(x => new { x.LotteryId, x.RefundState }).HasDatabaseName("fortuna_tickets_lottery_refund_ix");
            e.HasIndex(x => new { x.UserId, x.CreatedAt }).HasDatabaseName("fortuna_tickets_user_created_ix");
            e.HasIndex(x => x.InvoiceId).HasDatabaseName("fortuna_tickets_invoice_ix");
            e.HasIndex(x => new { x.LotteryId, x.TicketValue }).HasDatabaseName("fortuna_tickets_lottery_value_ix");
        });
    }

    private static void ConfigureRaffle(ModelBuilder mb)
    {
        mb.Entity<Raffle>(e =>
        {
            e.ToTable("fortuna_raffles");
            e.HasKey(x => x.RaffleId).HasName("fortuna_raffles_pkey");
            e.Property(x => x.RaffleId).HasColumnName("raffle_id").UseIdentityAlwaysColumn();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.Name).HasColumnName("name").HasColumnType("varchar(160)").IsRequired();
            e.Property(x => x.DescriptionMd).HasColumnName("description_md").HasColumnType("text");
            e.Property(x => x.RaffleDatetime).HasColumnName("raffle_datetime").HasColumnType("timestamp without time zone").IsRequired();
            e.Property(x => x.VideoUrl).HasColumnName("video_url").HasColumnType("varchar(500)");
            e.Property(x => x.IncludePreviousWinners).HasColumnName("include_previous_winners").HasDefaultValue(false);
            e.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(Fortuno.Domain.Enums.RaffleStatus.Open);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Lottery)
             .WithMany(l => l.Raffles)
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_raffle")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRaffleAward(ModelBuilder mb)
    {
        mb.Entity<RaffleAward>(e =>
        {
            e.ToTable("fortuna_raffle_awards");
            e.HasKey(x => x.RaffleAwardId).HasName("fortuna_raffle_awards_pkey");
            e.Property(x => x.RaffleAwardId).HasColumnName("raffle_award_id").UseIdentityAlwaysColumn();
            e.Property(x => x.RaffleId).HasColumnName("raffle_id").IsRequired();
            e.Property(x => x.Position).HasColumnName("position").IsRequired();
            e.Property(x => x.Description).HasColumnName("description").HasColumnType("varchar(300)").IsRequired();

            e.HasOne(x => x.Raffle)
             .WithMany(r => r.Awards)
             .HasForeignKey(x => x.RaffleId)
             .HasConstraintName("fk_raffle_raffle_award")
             .OnDelete(DeleteBehavior.Cascade);

            e.HasIndex(x => new { x.RaffleId, x.Position }).IsUnique().HasDatabaseName("fortuna_raffle_awards_raffle_position_uq");
        });
    }

    private static void ConfigureRaffleWinner(ModelBuilder mb)
    {
        mb.Entity<RaffleWinner>(e =>
        {
            e.ToTable("fortuna_raffle_winners");
            e.HasKey(x => x.RaffleWinnerId).HasName("fortuna_raffle_winners_pkey");
            e.Property(x => x.RaffleWinnerId).HasColumnName("raffle_winner_id").UseIdentityAlwaysColumn();
            e.Property(x => x.RaffleId).HasColumnName("raffle_id").IsRequired();
            e.Property(x => x.RaffleAwardId).HasColumnName("raffle_award_id").IsRequired();
            e.Property(x => x.TicketId).HasColumnName("ticket_id");
            e.Property(x => x.UserId).HasColumnName("user_id");
            e.Property(x => x.Position).HasColumnName("position").IsRequired();
            e.Property(x => x.PrizeText).HasColumnName("prize_text").HasColumnType("varchar(300)").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Raffle)
             .WithMany(r => r.Winners)
             .HasForeignKey(x => x.RaffleId)
             .HasConstraintName("fk_raffle_raffle_winner")
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.RaffleAward)
             .WithMany()
             .HasForeignKey(x => x.RaffleAwardId)
             .HasConstraintName("fk_raffle_award_raffle_winner")
             .OnDelete(DeleteBehavior.ClientSetNull);

            e.HasOne(x => x.Ticket)
             .WithMany()
             .HasForeignKey(x => x.TicketId)
             .HasConstraintName("fk_ticket_raffle_winner")
             .OnDelete(DeleteBehavior.ClientSetNull);

            e.HasIndex(x => new { x.RaffleId, x.RaffleAwardId }).IsUnique().HasDatabaseName("fortuna_raffle_winners_raffle_award_uq");
        });
    }

    private static void ConfigureUserReferrer(ModelBuilder mb)
    {
        mb.Entity<UserReferrer>(e =>
        {
            e.ToTable("fortuna_user_referrers");
            e.HasKey(x => x.UserReferrerId).HasName("fortuna_user_referrers_pkey");
            e.Property(x => x.UserReferrerId).HasColumnName("user_referrer_id").UseIdentityAlwaysColumn();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.ReferralCode).HasColumnName("referral_code").HasColumnType("varchar(8)").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasIndex(x => x.UserId).IsUnique().HasDatabaseName("fortuna_user_referrers_user_uq");
            e.HasIndex(x => x.ReferralCode).IsUnique().HasDatabaseName("fortuna_user_referrers_code_uq");
        });
    }

    private static void ConfigureInvoiceReferrer(ModelBuilder mb)
    {
        mb.Entity<InvoiceReferrer>(e =>
        {
            e.ToTable("fortuna_invoice_referrers");
            e.HasKey(x => x.InvoiceReferrerId).HasName("fortuna_invoice_referrers_pkey");
            e.Property(x => x.InvoiceReferrerId).HasColumnName("invoice_referrer_id").UseIdentityAlwaysColumn();
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
            e.Property(x => x.ReferrerUserId).HasColumnName("referrer_user_id").IsRequired();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.ReferralPercentAtPurchase).HasColumnName("referral_percent_at_purchase").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasIndex(x => x.InvoiceId).IsUnique().HasDatabaseName("fortuna_invoice_referrers_invoice_uq");
            e.HasIndex(x => new { x.ReferrerUserId, x.LotteryId }).HasDatabaseName("fortuna_invoice_referrers_referrer_lottery_ix");

            e.HasOne<Lottery>()
             .WithMany()
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_invoice_referrer")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureRefundLog(ModelBuilder mb)
    {
        mb.Entity<RefundLog>(e =>
        {
            e.ToTable("fortuna_refund_logs");
            e.HasKey(x => x.RefundLogId).HasName("fortuna_refund_logs_pkey");
            e.Property(x => x.RefundLogId).HasColumnName("refund_log_id").UseIdentityAlwaysColumn();
            e.Property(x => x.TicketId).HasColumnName("ticket_id").IsRequired();
            e.Property(x => x.ExecutedByUserId).HasColumnName("executed_by_user_id").IsRequired();
            e.Property(x => x.ReferenceValue).HasColumnName("reference_value").HasColumnType("numeric(12,2)").IsRequired();
            e.Property(x => x.ExternalReference).HasColumnName("external_reference").HasColumnType("varchar(160)");
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasOne(x => x.Ticket)
             .WithMany()
             .HasForeignKey(x => x.TicketId)
             .HasConstraintName("fk_ticket_refund_log")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNumberReservation(ModelBuilder mb)
    {
        mb.Entity<NumberReservation>(e =>
        {
            e.ToTable("fortuna_number_reservations");
            e.HasKey(x => x.ReservationId).HasName("fortuna_number_reservations_pkey");
            e.Property(x => x.ReservationId).HasColumnName("reservation_id").UseIdentityAlwaysColumn();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id");
            e.Property(x => x.TicketNumber).HasColumnName("ticket_number").IsRequired();
            e.Property(x => x.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamp without time zone").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasIndex(x => new { x.LotteryId, x.ExpiresAt }).HasDatabaseName("fortuna_number_reservations_lottery_expires_ix");
            e.HasIndex(x => new { x.UserId, x.LotteryId }).HasDatabaseName("fortuna_number_reservations_user_lottery_ix");

            e.HasOne<Lottery>()
             .WithMany()
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_lottery_number_reservation")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTicketOrder(ModelBuilder mb)
    {
        mb.Entity<TicketOrder>(e =>
        {
            e.ToTable("fortuna_ticket_orders");
            e.HasKey(x => x.TicketOrderId).HasName("fortuna_ticket_orders_pkey");
            e.Property(x => x.TicketOrderId).HasColumnName("ticket_order_id").UseIdentityAlwaysColumn();
            e.Property(x => x.InvoiceId).HasColumnName("invoice_id").IsRequired();
            e.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasColumnType("varchar(40)").IsRequired();
            e.Property(x => x.UserId).HasColumnName("user_id").IsRequired();
            e.Property(x => x.LotteryId).HasColumnName("lottery_id").IsRequired();
            e.Property(x => x.Quantity).HasColumnName("quantity").IsRequired();
            e.Property(x => x.Mode).HasColumnName("mode").HasConversion<int>().IsRequired();
            e.Property(x => x.ReferralCode).HasColumnName("referral_code").HasColumnType("varchar(8)");
            e.Property(x => x.ReferralPercentAtPurchase).HasColumnName("referral_percent_at_purchase").HasDefaultValue(0f);
            e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasColumnType("numeric(14,2)").IsRequired();
            e.Property(x => x.BrCode).HasColumnName("br_code").HasColumnType("varchar(2000)");
            e.Property(x => x.BrCodeBase64).HasColumnName("br_code_base64").HasColumnType("text");
            e.Property(x => x.ExpiredAt).HasColumnName("expired_at").HasColumnType("timestamp without time zone").IsRequired();
            e.Property(x => x.Status).HasColumnName("status").HasConversion<int>().HasDefaultValue(Fortuno.Domain.Enums.TicketOrderStatus.Pending);
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");
            e.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasIndex(x => x.InvoiceId).IsUnique().HasDatabaseName("ix_ticket_orders_invoice_id");
            e.HasIndex(x => x.UserId).HasDatabaseName("ix_ticket_orders_user_id");
            e.HasIndex(x => x.LotteryId).HasDatabaseName("ix_ticket_orders_lottery_id");

            e.HasOne(x => x.Lottery)
             .WithMany()
             .HasForeignKey(x => x.LotteryId)
             .HasConstraintName("fk_ticket_order_lottery")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureTicketOrderNumber(ModelBuilder mb)
    {
        mb.Entity<TicketOrderNumber>(e =>
        {
            e.ToTable("fortuna_ticket_order_numbers");
            e.HasKey(x => x.TicketOrderNumberId).HasName("fortuna_ticket_order_numbers_pkey");
            e.Property(x => x.TicketOrderNumberId).HasColumnName("ticket_order_number_id").UseIdentityAlwaysColumn();
            e.Property(x => x.TicketOrderId).HasColumnName("ticket_order_id").IsRequired();
            e.Property(x => x.TicketNumber).HasColumnName("ticket_number").IsRequired();
            e.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone").HasDefaultValueSql("now()");

            e.HasIndex(x => x.TicketOrderId).HasDatabaseName("ix_ticket_order_numbers_order_id");
            e.HasIndex(x => new { x.TicketOrderId, x.TicketNumber }).IsUnique().HasDatabaseName("ix_ticket_order_numbers_order_number_uq");

            e.HasOne(x => x.Order)
             .WithMany(o => o.Numbers)
             .HasForeignKey(x => x.TicketOrderId)
             .HasConstraintName("fk_ticket_order_number_order")
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
