using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleWallet.Application.Wallets;

namespace SimpleWallet.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/wallet")]
public class WalletController : ControllerBase
{
    private readonly WalletService _wallets;

    public WalletController(WalletService wallets)
    {
        _wallets = wallets;
    }

    [HttpGet]
    public async Task<ActionResult<WalletBalanceResponse>> Get(CancellationToken cancellationToken)
    {
        var balance = await _wallets.GetBalanceAsync(CurrentUserId(), cancellationToken);
        return balance is null
            ? NotFound(new { message = "المحفظة غير موجودة." })
            : Ok(balance);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> Transactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _wallets.GetTransactionsAsync(CurrentUserId(), page, pageSize, cancellationToken));
    }

    [HttpPost("deposit")]
    public async Task<ActionResult<MoneyMovementResponse>> Deposit(DepositRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.DepositAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("transfer")]
    public async Task<ActionResult<MoneyMovementResponse>> Transfer(TransferRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.TransferAsync(CurrentUserId(), request, cancellationToken));
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}
