using FluentAssertions;
using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.Domain.Services;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Lottery;
using Fortuno.DTO.NAuth;
using Fortuno.DTO.ProxyPay;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;
using Moq;

namespace Fortuno.Tests.Domain.Services;

public class LotteryServiceTests
{
    private readonly Mock<ILotteryRepository<Lottery>> _lotteryRepo = new();
    private readonly Mock<ILotteryImageRepository<LotteryImage>> _imageRepo = new();
    private readonly Mock<IRaffleRepository<Raffle>> _raffleRepo = new();
    private readonly Mock<IRaffleAwardRepository<RaffleAward>> _awardRepo = new();
    private readonly Mock<ITicketRepository<Ticket>> _ticketRepo = new();
    private readonly Mock<ISlugService> _slug = new();
    private readonly Mock<IStoreOwnershipGuard> _ownership = new();
    private readonly Mock<INumberCompositionService> _numbers = new();
    private readonly Mock<IProxyPayAppService> _proxyPay = new();
    private readonly Mock<INAuthAppService> _nauth = new();

    public LotteryServiceTests()
    {
        // Default: ProxyPay devolve uma Store com clientId válido (via EnsureMyStoreAsync).
        // Mantém GetStoreAsync default para fluxos que ainda usam (Publish backfill).
        var defaultStore = new ProxyPayStoreInfo
        {
            StoreId = 10,
            OwnerUserId = 42,
            Name = "QA Store",
            ClientId = "client-qa"
        };
        _proxyPay.Setup(p => p.EnsureMyStoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(defaultStore);
        _proxyPay.Setup(p => p.GetStoreAsync(It.IsAny<long>()))
            .ReturnsAsync((long sid) => new ProxyPayStoreInfo
            {
                StoreId = sid,
                OwnerUserId = 42,
                Name = "QA Store",
                ClientId = "client-qa"
            });
        _proxyPay.Setup(p => p.GetMyStoreAsync()).ReturnsAsync(defaultStore);

        _nauth.Setup(n => n.GetCurrentAsync()).ReturnsAsync(new NAuthUserInfo
        {
            UserId = 42,
            Name = "QA Owner",
            Email = "qa@example.com",
            DocumentId = "12345678900",
            Phone = "11999999999"
        });
    }

    private LotteryService CreateSut() => new(
        _lotteryRepo.Object,
        _imageRepo.Object,
        _raffleRepo.Object,
        _awardRepo.Object,
        _ticketRepo.Object,
        _slug.Object,
        _ownership.Object,
        _numbers.Object,
        _proxyPay.Object,
        _nauth.Object);

    private static LotteryInsertInfo ValidInsert() => new()
    {
        Name = "Rifa QA",
        DescriptionMd = "desc",
        RulesMd = "rules",
        PrivacyPolicyMd = "privacy",
        TicketPrice = 10m,
        TotalPrizeValue = 1000m,
        TicketMin = 1,
        TicketMax = 0,
        TicketNumIni = 1,
        TicketNumEnd = 100,
        NumberType = NumberTypeDto.Int64,
        NumberValueMin = 1,
        NumberValueMax = 100,
        ReferralPercent = 0f
    };

    // ---------- Create ----------
    [Fact]
    public async Task CreateAsync_ShouldReturnDraftWithSlug_ResolvingStoreFromAuthenticatedUser()
    {
        _slug.Setup(s => s.GenerateUniqueSlugAsync("Rifa QA")).ReturnsAsync("rifa-qa");
        _lotteryRepo.Setup(r => r.InsertAsync(It.IsAny<Lottery>()))
            .ReturnsAsync((Lottery l) => { l.LotteryId = 77; return l; });

        var sut = CreateSut();
        var info = await sut.CreateAsync(42, ValidInsert());

        info.LotteryId.Should().Be(77);
        info.Slug.Should().Be("rifa-qa");
        info.Status.Should().Be(LotteryStatusDto.Draft);
        info.StoreId.Should().Be(10); // StoreId vem do EnsureMyStoreAsync, não do DTO
        info.StoreClientId.Should().Be("client-qa");

        _proxyPay.Verify(p => p.EnsureMyStoreAsync("QA Owner", "qa@example.com"), Times.Once);
        _lotteryRepo.Verify(r => r.InsertAsync(
            It.Is<Lottery>(l => l.Status == LotteryStatus.Draft && l.StoreId == 10)), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenNAuthUserMissing()
    {
        _nauth.Setup(n => n.GetCurrentAsync()).ReturnsAsync((NAuthUserInfo?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, ValidInsert());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*NAuth*");
    }

    [Fact]
    public async Task CreateAsync_ShouldThrow_WhenUserHasNoEmailOrName()
    {
        _nauth.Setup(n => n.GetCurrentAsync()).ReturnsAsync(new NAuthUserInfo
        {
            UserId = 42, Name = "", Email = ""
        });

        var sut = CreateSut();
        Func<Task> act = () => sut.CreateAsync(42, ValidInsert());

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nome ou e-mail*");
    }

    // ---------- Publish ----------
    [Fact]
    public async Task PublishAsync_ShouldTransitionDraftToOpen_WhenAllPrerequisitesMet()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft, NumberType = NumberType.Int64 };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _imageRepo.Setup(i => i.CountByLotteryAsync(1)).ReturnsAsync(1);
        _raffleRepo.Setup(r => r.CountByLotteryAsync(1)).ReturnsAsync(1);
        _raffleRepo.Setup(r => r.ListByLotteryAsync(1, null)).ReturnsAsync(new List<Raffle> { new() { RaffleId = 5 } });
        _awardRepo.Setup(a => a.CountByRaffleAsync(5)).ReturnsAsync(2);
        _lotteryRepo.Setup(r => r.UpdateAsync(It.IsAny<Lottery>())).ReturnsAsync((Lottery l) => l);

        var sut = CreateSut();
        var info = await sut.PublishAsync(42, 1);

        info.Status.Should().Be(LotteryStatusDto.Open);
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenNotDraft()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Cancelled };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var sut = CreateSut();
        Func<Task> act = () => sut.PublishAsync(42, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Draft*");
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenMissingImagesOrRaffles()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft, NumberType = NumberType.Int64 };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _imageRepo.Setup(i => i.CountByLotteryAsync(1)).ReturnsAsync(0);
        _raffleRepo.Setup(r => r.CountByLotteryAsync(1)).ReturnsAsync(0);
        _raffleRepo.Setup(r => r.ListByLotteryAsync(1, null)).ReturnsAsync(new List<Raffle>());

        var sut = CreateSut();
        Func<Task> act = () => sut.PublishAsync(42, 1);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Requisitos de publicação*");
    }

    [Fact]
    public async Task PublishAsync_ShouldThrow_WhenLotteryNotFound()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lottery?)null);

        var sut = CreateSut();
        Func<Task> act = () => sut.PublishAsync(42, 99);

        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ---------- Close ----------
    [Fact]
    public async Task CloseAsync_ShouldTransitionOpenToClosed()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Open };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _lotteryRepo.Setup(r => r.UpdateAsync(It.IsAny<Lottery>())).ReturnsAsync((Lottery l) => l);

        var sut = CreateSut();
        var info = await sut.CloseAsync(42, 1);

        info.Status.Should().Be(LotteryStatusDto.Closed);
    }

    [Fact]
    public async Task CloseAsync_ShouldThrow_WhenNotOpen()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var sut = CreateSut();
        Func<Task> act = () => sut.CloseAsync(42, 1);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    // ---------- Cancel ----------
    [Fact]
    public async Task CancelAsync_ShouldMarkActiveTicketsAsPendingRefund()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _ticketRepo.Setup(t => t.ListByLotteryAsync(1)).ReturnsAsync(new List<Ticket>
        {
            new() { TicketId = 10, RefundState = TicketRefundState.None },
            new() { TicketId = 11, RefundState = TicketRefundState.Refunded }
        });
        _lotteryRepo.Setup(r => r.UpdateAsync(It.IsAny<Lottery>())).ReturnsAsync((Lottery l) => l);

        var sut = CreateSut();
        var info = await sut.CancelAsync(42, 1, new LotteryCancelRequest
        {
            Reason = "Motivo QA com mais de vinte caracteres para passar a validação."
        });

        info.Status.Should().Be(LotteryStatusDto.Cancelled);
        _ticketRepo.Verify(t => t.MarkRefundStateAsync(
            It.Is<IEnumerable<long>>(ids => ids.Contains(10) && !ids.Contains(11)),
            (int)TicketRefundState.PendingRefund),
            Times.Once);
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenReasonTooShort()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var sut = CreateSut();
        Func<Task> act = () => sut.CancelAsync(42, 1, new LotteryCancelRequest { Reason = "curto" });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Motivo*");
    }

    [Fact]
    public async Task CancelAsync_ShouldThrow_WhenAlreadyCancelled()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Cancelled };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var sut = CreateSut();
        Func<Task> act = () => sut.CancelAsync(42, 1, new LotteryCancelRequest
        {
            Reason = "motivo valido com vinte ou mais caracteres para nao falhar na validacao"
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já está cancelada*");
    }

    // ---------- Update ----------
    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenNotDraft()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Open };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);

        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(42, 1, new LotteryUpdateInfo { Name = "x" });

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldApplyChanges_WhenDraft()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft, Name = "old", Slug = "old" };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _lotteryRepo.Setup(r => r.SlugExistsAsync("new-slug")).ReturnsAsync(false);
        _lotteryRepo.Setup(r => r.UpdateAsync(It.IsAny<Lottery>())).ReturnsAsync((Lottery l) => l);

        var sut = CreateSut();
        var info = await sut.UpdateAsync(42, 1, new LotteryUpdateInfo
        {
            Name = "new",
            DescriptionMd = "d",
            RulesMd = "r",
            PrivacyPolicyMd = "p",
            Slug = "new-slug",
            TicketPrice = 1,
            TotalPrizeValue = 1,
            NumberType = NumberTypeDto.Int64
        });

        info.Name.Should().Be("new");
        info.Slug.Should().Be("new-slug");
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrow_WhenSlugTaken()
    {
        var entity = new Lottery { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft, Name = "old", Slug = "old" };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _lotteryRepo.Setup(r => r.SlugExistsAsync("taken")).ReturnsAsync(true);

        var sut = CreateSut();
        Func<Task> act = () => sut.UpdateAsync(42, 1, new LotteryUpdateInfo
        {
            Name = "x",
            Slug = "taken",
            NumberType = NumberTypeDto.Int64
        });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*já em uso*");
    }

    // ---------- Queries ----------
    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lottery?)null);
        var sut = CreateSut();

        (await sut.GetByIdAsync(99)).Should().BeNull();
    }

    [Fact]
    public async Task GetBySlugAsync_ShouldMapEntity_WhenFound()
    {
        _lotteryRepo.Setup(r => r.GetBySlugAsync("rifa-qa"))
            .ReturnsAsync(new Lottery { LotteryId = 1, Slug = "rifa-qa", Status = LotteryStatus.Open });
        var sut = CreateSut();

        var result = await sut.GetBySlugAsync("rifa-qa");
        result.Should().NotBeNull();
        result!.Slug.Should().Be("rifa-qa");
    }

    [Fact]
    public async Task ListByStoreAsync_ShouldMapAll()
    {
        _lotteryRepo.Setup(r => r.ListByStoreAsync(1))
            .ReturnsAsync(new List<Lottery>
            {
                new() { LotteryId = 1, StoreId = 1, Status = LotteryStatus.Draft },
                new() { LotteryId = 2, StoreId = 1, Status = LotteryStatus.Open }
            });

        var sut = CreateSut();
        var list = await sut.ListByStoreAsync(1);

        list.Should().HaveCount(2);
    }

    [Fact]
    public async Task CalculatePossibilitiesAsync_ShouldUseNumberCompositionService()
    {
        var entity = new Lottery { LotteryId = 1, NumberType = NumberType.Int64, NumberValueMin = 0, NumberValueMax = 9 };
        _lotteryRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(entity);
        _numbers.Setup(n => n.CountPossibilities(NumberType.Int64, 0, 9)).Returns(10);

        var sut = CreateSut();
        var result = await sut.CalculatePossibilitiesAsync(1);

        result.Should().Be(10);
    }

    [Fact]
    public async Task CalculatePossibilitiesAsync_ShouldThrow_WhenLotteryMissing()
    {
        _lotteryRepo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Lottery?)null);
        var sut = CreateSut();

        Func<Task> act = () => sut.CalculatePossibilitiesAsync(99);
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }
}
