using Fortuno.Domain.Models;
using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public class UserReferrerRepository : Repository<UserReferrer>, IUserReferrerRepository<UserReferrer>
{
    public UserReferrerRepository(FortunoContext context) : base(context) { }

    public async Task<UserReferrer?> GetByUserIdAsync(long userId)
        => await _context.UserReferrers.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId);

    public async Task<UserReferrer?> GetByCodeAsync(string referralCode)
        => await _context.UserReferrers.AsNoTracking().FirstOrDefaultAsync(x => x.ReferralCode == referralCode);

    public async Task<bool> CodeExistsAsync(string referralCode)
        => await _context.UserReferrers.AsNoTracking().AnyAsync(x => x.ReferralCode == referralCode);
}
