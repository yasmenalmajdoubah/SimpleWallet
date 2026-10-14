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
    public async Task<ActionResult<WalletHomeResponse>> Get(CancellationToken cancellationToken)
    {
        var home = await _wallets.GetHomeAsync(CurrentUserId(), cancellationToken);
        return home is null
            ? NotFound(new { message = "المحفظة غير موجودة." })
            : Ok(home);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<PagedResponse<TransactionResponse>>> Transactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _wallets.GetTransactionsAsync(CurrentUserId(), page, pageSize, cancellationToken));
    }

    [HttpGet("bills")]
    public ActionResult<IReadOnlyList<BillResponse>> Bills() => Ok(_wallets.Bills());

    [HttpGet("requests")]
    public async Task<ActionResult<IReadOnlyList<MoneyRequestResponse>>> Requests(CancellationToken cancellationToken)
    {
        return Ok(await _wallets.GetRequestsAsync(CurrentUserId(), cancellationToken));
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

    [HttpPut("salary")]
    public async Task<ActionResult<SalaryStateResponse>> SetSalary(SalaryRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.SetSalaryAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("salary/collect")]
    public async Task<ActionResult<MoneyMovementResponse>> CollectSalary(CancellationToken cancellationToken)
    {
        return Ok(await _wallets.CollectSalaryAsync(CurrentUserId(), cancellationToken));
    }

    [HttpPost("move")]
    public async Task<ActionResult<MoneyMovementResponse>> Move(MoveMoneyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.MoveAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("card/freeze")]
    public async Task<ActionResult<CardResponse>> Freeze(FreezeCardRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.SetCardFrozenAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("card/pay")]
    public async Task<ActionResult<MoneyMovementResponse>> PayWithCard(CardPaymentRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.PayWithCardAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("bills/pay")]
    public async Task<ActionResult<MoneyMovementResponse>> PayBill(PayBillRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.PayBillAsync(CurrentUserId(), request, cancellationToken));
    }

    [HttpPost("requests")]
    public async Task<IActionResult> CreateRequest(CreateMoneyRequest request, CancellationToken cancellationToken)
    {
        await _wallets.CreateRequestAsync(CurrentUserId(), request, cancellationToken);
        return Ok(new { message = "انبعت الطلب." });
    }

    [HttpPost("requests/{id:guid}/accept")]
    public async Task<ActionResult<MoneyMovementResponse>> Accept(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _wallets.AcceptRequestAsync(CurrentUserId(), id, cancellationToken));
    }

    [HttpPost("requests/{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
    {
        await _wallets.RejectRequestAsync(CurrentUserId(), id, cancellationToken);
        return Ok(new { message = "انرفض الطلب." });
    }

    [HttpPost("requests/{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        await _wallets.CancelRequestAsync(CurrentUserId(), id, cancellationToken);
        return Ok(new { message = "انلغى الطلب." });
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.Parse(value!);
    }
}
