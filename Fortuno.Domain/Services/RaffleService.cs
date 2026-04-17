using Fortuno.Domain.Enums;
using Fortuno.Domain.Interfaces;
using Fortuno.Domain.Models;
using Fortuno.DTO.Enums;
using Fortuno.DTO.Raffle;
using Fortuno.DTO.RaffleWinner;
using Fortuno.Infra.Interfaces.AppServices;
using Fortuno.Infra.Interfaces.Repository;

namespace Fortuno.Domain.Services;

public class RaffleService : IRaffleService
{
    private readonly IRaffleRepository<Raffle> _raffleRepo;
    private readonly IRaffleAwardRepository<RaffleAward> _awardRepo;
    private readonly IRaffleWinnerRepository<RaffleWinner> _winnerRepo;
    private readonly ITicketRepository<Ticket> _ticketRepo;
    private readonly ILotteryRepository<Lottery> _lotteryRepo;
    private readonly IStoreOwnershipGuard _ownership;
    private readonly INAuthAppService _nauth;

    public RaffleService(
        IRaffleRepository<Raffle> raffleRepo,
        IRaffleAwardRepository<RaffleAward> awardRepo,
        IRaffleWinnerRepository<RaffleWinner> winnerRepo,
        ITicketRepository<Ticket> ticketRepo,
        ILotteryRepository<Lottery> lotteryRepo,
        IStoreOwnershipGuard ownership,
        INAuthAppService nauth)
    {
        _raffleRepo = raffleRepo;
        _awardRepo = awardRepo;
        _winnerRepo = winnerRepo;
        _ticketRepo = ticketRepo;
        _lotteryRepo = lotteryRepo;
        _ownership = ownership;
        _nauth = nauth;
    }

    public async Task<RaffleInfo> CreateAsync(long currentUserId, RaffleInsertInfo dto)
    {
        var lottery = await _lotteryRepo.GetByIdAsync(dto.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {dto.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Criação permitida apenas em Lottery Draft.");

        var entity = new Raffle
        {
            LotteryId = dto.LotteryId,
            Name = dto.Name,
            DescriptionMd = dto.DescriptionMd,
            RaffleDatetime = dto.RaffleDatetime,
            VideoUrl = dto.VideoUrl,
            IncludePreviousWinners = dto.IncludePreviousWinners,
            Status = RaffleStatus.Open
        };
        var saved = await _raffleRepo.InsertAsync(entity);
        return MapToDto(saved);
    }

    public async Task<List<RaffleInfo>> ListByLotteryAsync(long lotteryId)
    {
        var list = await _raffleRepo.ListByLotteryAsync(lotteryId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<RaffleInfo?> GetByIdAsync(long raffleId)
    {
        var entity = await _raffleRepo.GetByIdAsync(raffleId);
        return entity is null ? null : MapToDto(entity);
    }

    public async Task<List<RaffleWinnerPreviewRow>> PreviewWinnersAsync(long currentUserId, RaffleWinnersPreviewRequest request)
    {
        var (raffle, lottery, awards) = await LoadRaffleContextAsync(currentUserId, request.RaffleId);

        if (raffle.Status != RaffleStatus.Open)
            throw new InvalidOperationException("Prévia permitida apenas em Raffle Open.");

        var excludedTicketIds = new HashSet<long>();
        if (!raffle.IncludePreviousWinners)
        {
            var ids = await _winnerRepo.ListTicketIdsAlreadyWonInLotteryAsync(lottery.LotteryId);
            foreach (var id in ids) excludedTicketIds.Add(id);
        }

        var existingWinners = await _winnerRepo.ListByRaffleAsync(raffle.RaffleId);
        var takenAwardIds = new HashSet<long>(existingWinners.Select(w => w.RaffleAwardId));
        var openAwards = awards.Where(a => !takenAwardIds.Contains(a.RaffleAwardId)).OrderBy(a => a.Position).ToList();

        if (openAwards.Count == 0)
            throw new InvalidOperationException("Todos os awards deste Raffle já possuem ganhadores.");

        var numbers = request.WinningNumbers ?? new();
        if (numbers.Count == 0)
            throw new InvalidOperationException("Nenhum número informado na prévia.");

        var rows = new List<RaffleWinnerPreviewRow>();
        var usedTicketIdsInThisPreview = new HashSet<long>();

        for (int i = 0; i < numbers.Count && i < openAwards.Count; i++)
        {
            var award = openAwards[i];
            var number = numbers[i];

            var row = new RaffleWinnerPreviewRow
            {
                Position = award.Position,
                AwardId = award.RaffleAwardId,
                PrizeText = award.Description,
                Number = number
            };

            var ticket = await _ticketRepo.GetByLotteryAndNumberAsync(lottery.LotteryId, number);
            if (ticket is null)
            {
                row.NotFound = true;
                row.Note = "Número não corresponde a nenhum ticket vendido.";
            }
            else if (excludedTicketIds.Contains(ticket.TicketId))
            {
                row.ExcludedByFlag = true;
                row.TicketId = ticket.TicketId;
                row.UserId = ticket.UserId;
                row.Note = "Ticket excluído: flag IncludePreviousWinners=false e ticket já venceu em Raffle anterior.";
            }
            else if (!usedTicketIdsInThisPreview.Add(ticket.TicketId))
            {
                row.TicketId = ticket.TicketId;
                row.UserId = ticket.UserId;
                row.NotFound = false;
                row.Note = "Ticket duplicado nesta mesma prévia — cada posição deste Raffle exige ticket distinto.";
            }
            else
            {
                row.TicketId = ticket.TicketId;
                row.UserId = ticket.UserId;
                var user = await _nauth.GetByIdAsync(ticket.UserId);
                row.UserName = user?.Name;
                row.UserCpfMasked = MaskCpf(user?.DocumentId);
            }

            rows.Add(row);
        }

        return rows;
    }

    public async Task<List<RaffleWinnerInfo>> ConfirmWinnersAsync(long currentUserId, RaffleWinnersPreviewRequest request)
    {
        var (raffle, lottery, _) = await LoadRaffleContextAsync(currentUserId, request.RaffleId);

        if (raffle.Status != RaffleStatus.Open)
            throw new InvalidOperationException("Confirmação permitida apenas em Raffle Open.");

        var preview = await PreviewWinnersAsync(currentUserId, request);

        var erros = new List<string>();
        foreach (var row in preview)
        {
            if (row.ExcludedByFlag)
                erros.Add($"Posição {row.Position}: ticket excluído pelo flag IncludePreviousWinners.");
            else if (row.Note?.StartsWith("Ticket duplicado") == true)
                erros.Add($"Posição {row.Position}: {row.Note}");
        }
        if (erros.Count > 0)
            throw new InvalidOperationException("Prévia contém erros: " + string.Join(" | ", erros));

        var winners = preview.Select(row => new RaffleWinner
        {
            RaffleId = raffle.RaffleId,
            RaffleAwardId = row.AwardId,
            TicketId = row.TicketId,
            UserId = row.UserId,
            Position = row.Position,
            PrizeText = row.PrizeText
        }).ToList();

        var saved = await _winnerRepo.InsertBatchAsync(winners);

        var infos = new List<RaffleWinnerInfo>();
        foreach (var w in saved)
        {
            string? userName = null;
            long? ticketNumber = null;
            if (w.UserId.HasValue)
            {
                var user = await _nauth.GetByIdAsync(w.UserId.Value);
                userName = user?.Name;
            }
            if (w.TicketId.HasValue)
            {
                var t = await _ticketRepo.GetByIdAsync(w.TicketId.Value);
                ticketNumber = t?.TicketNumber;
            }
            infos.Add(new RaffleWinnerInfo
            {
                RaffleWinnerId = w.RaffleWinnerId,
                RaffleId = w.RaffleId,
                RaffleAwardId = w.RaffleAwardId,
                Position = w.Position,
                PrizeText = w.PrizeText,
                TicketId = w.TicketId,
                UserId = w.UserId,
                TicketNumber = ticketNumber,
                UserName = userName,
                CreatedAt = w.CreatedAt
            });
        }
        return infos;
    }

    public async Task<RaffleInfo> CloseAsync(long currentUserId, long raffleId)
    {
        var (raffle, _, _) = await LoadRaffleContextAsync(currentUserId, raffleId);

        if (raffle.Status != RaffleStatus.Open)
            throw new InvalidOperationException("Somente Raffle Open pode ser fechado.");

        raffle.Status = RaffleStatus.Closed;
        raffle.UpdatedAt = DateTime.UtcNow;
        var saved = await _raffleRepo.UpdateAsync(raffle);
        return MapToDto(saved);
    }

    public async Task<RaffleInfo> UpdateAsync(long currentUserId, long raffleId, RaffleUpdateInfo dto)
    {
        var (raffle, lottery, _) = await LoadRaffleContextAsync(currentUserId, raffleId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Edição permitida apenas em Lottery Draft.");

        raffle.Name = dto.Name;
        raffle.DescriptionMd = dto.DescriptionMd;
        raffle.RaffleDatetime = dto.RaffleDatetime;
        raffle.VideoUrl = dto.VideoUrl;
        raffle.IncludePreviousWinners = dto.IncludePreviousWinners;
        raffle.UpdatedAt = DateTime.UtcNow;
        var saved = await _raffleRepo.UpdateAsync(raffle);
        return MapToDto(saved);
    }

    public async Task DeleteAsync(long currentUserId, long raffleId)
    {
        var (raffle, lottery, _) = await LoadRaffleContextAsync(currentUserId, raffleId);

        if (lottery.Status != LotteryStatus.Draft)
            throw new InvalidOperationException("Exclusão permitida apenas em Lottery Draft.");

        await _raffleRepo.DeleteAsync(raffleId);
    }

    public async Task<RaffleInfo> CancelAsync(long currentUserId, long raffleId, RaffleCancelRequest request)
    {
        var (raffle, lottery, awards) = await LoadRaffleContextAsync(currentUserId, raffleId);

        if (raffle.Status != RaffleStatus.Open)
            throw new InvalidOperationException("Somente Raffle Open pode ser cancelado.");

        // Verifica se há tickets vendidos na Lottery
        var ticketsSold = await _ticketRepo.CountSoldAsync(lottery.LotteryId);

        if (ticketsSold > 0)
        {
            // Exige redistribuição dos awards órfãos para outro Raffle Open da mesma Lottery
            var otherOpen = (await _raffleRepo.ListByLotteryAsync(lottery.LotteryId))
                .Where(r => r.RaffleId != raffleId && r.Status == RaffleStatus.Open)
                .Select(r => r.RaffleId).ToHashSet();
            if (otherOpen.Count == 0)
                throw new InvalidOperationException("Nenhum Raffle Open remanescente para receber a redistribuição. Cancele a Lottery inteira (FR-042c).");

            var awardIds = awards.Select(a => a.RaffleAwardId).ToHashSet();
            var redistributions = request?.Redistributions ?? new();
            var mapped = redistributions.Where(r => awardIds.Contains(r.RaffleAwardId)).ToList();
            if (mapped.Count != awardIds.Count)
                throw new InvalidOperationException("Todos os RaffleAwards do Raffle a cancelar devem ser redistribuídos para um Raffle Open remanescente.");
            if (mapped.Any(m => !otherOpen.Contains(m.TargetRaffleId)))
                throw new InvalidOperationException("targetRaffleId deve apontar para um Raffle Open remanescente da mesma Lottery.");

            foreach (var m in mapped)
                await _awardRepo.ReassignToRaffleAsync(m.RaffleAwardId, m.TargetRaffleId);
        }

        raffle.Status = RaffleStatus.Cancelled;
        raffle.UpdatedAt = DateTime.UtcNow;
        var saved = await _raffleRepo.UpdateAsync(raffle);
        return MapToDto(saved);
    }

    // -------- helpers --------

    private async Task<(Raffle raffle, Lottery lottery, List<RaffleAward> awards)> LoadRaffleContextAsync(long currentUserId, long raffleId)
    {
        var raffle = await _raffleRepo.GetByIdAsync(raffleId)
            ?? throw new KeyNotFoundException($"Raffle {raffleId} não encontrado.");
        var lottery = await _lotteryRepo.GetByIdAsync(raffle.LotteryId)
            ?? throw new KeyNotFoundException($"Lottery {raffle.LotteryId} não encontrada.");

        await _ownership.EnsureOwnershipAsync(lottery.StoreId, currentUserId);

        var awards = await _awardRepo.ListByRaffleAsync(raffleId);
        return (raffle, lottery, awards);
    }

    private static string? MaskCpf(string? cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf)) return null;
        var digits = new string(cpf.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) return "***";
        return $"***.{digits.Substring(3, 3)}.{digits.Substring(6, 3)}-**";
    }

    private static RaffleInfo MapToDto(Raffle e) => new()
    {
        RaffleId = e.RaffleId,
        LotteryId = e.LotteryId,
        Name = e.Name,
        DescriptionMd = e.DescriptionMd,
        RaffleDatetime = e.RaffleDatetime,
        VideoUrl = e.VideoUrl,
        IncludePreviousWinners = e.IncludePreviousWinners,
        Status = (RaffleStatusDto)e.Status,
        CreatedAt = e.CreatedAt,
        UpdatedAt = e.UpdatedAt
    };
}
