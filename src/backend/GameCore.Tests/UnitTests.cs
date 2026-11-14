using GameCore.WebAPI;
using GameCore.WebAPI.Security;
using GameCore.WebAPI.Validation;

namespace GameCore.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Hash_ThenVerify_RoundTrips()
    {
        var hash = PasswordHasher.Hash("GameCore123!", 1_000);

        Assert.True(PasswordHasher.Verify("GameCore123!", hash));
        Assert.False(PasswordHasher.Verify("gamecore123!", hash));
    }

    [Fact]
    public void Verify_SeedAdminHash_FromSecuritySql()
    {
        const string seedHash = "100000.pcnAsewbMeQ9i+iZnkMy2A==.pcnTHGRSHKHoYsfBv0CFvgZVnJr2+lwj1cPG8ooeS+k=";

        Assert.True(PasswordHasher.Verify("GameCore123!", seedHash));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("100000.@@@.@@@")]
    [InlineData("0.AAAA.AAAA")]
    public void Verify_MalformedHash_ReturnsFalseInsteadOfThrowing(string? encoded)
    {
        Assert.False(PasswordHasher.Verify("GameCore123!", encoded));
    }
}

public sealed class RequestValidatorTests
{
    [Fact]
    public void Sale_WithRepeatedGame_IsRejected()
    {
        var errors = RequestValidator.ValidateSale(new SaleCreateRequest(1, null, [new(5, 1), new(5, 2)]));

        Assert.True(errors.ContainsKey("items"));
    }

    [Fact]
    public void Sale_WithNullItemEntry_IsRejectedWithoutThrowing()
    {
        var errors = RequestValidator.ValidateSale(new SaleCreateRequest(1, null, [null!]));

        Assert.True(errors.ContainsKey("items"));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(19.99, false)]
    [InlineData(-0.01, true)]
    [InlineData(10.555, true)]
    public void Game_Price(decimal price, bool rejected)
    {
        var errors = RequestValidator.ValidateGame(new GameWriteRequest("Juego", null, null, price, null, [], []));

        Assert.Equal(rejected, errors.ContainsKey("unitPrice"));
    }

    [Fact]
    public void Game_TitleLongerThanColumn_IsRejected()
    {
        var errors = RequestValidator.ValidateGame(new GameWriteRequest(new string('x', 181), null, null, 1, null, null, null));

        Assert.True(errors.ContainsKey("title"));
    }

    [Theory]
    [InlineData("ana@example.test", false)]
    [InlineData("  ANA.Torres+demo@Example.Test ", false)]
    [InlineData("ana@", true)]
    [InlineData("ana example@test.local", true)]
    public void Customer_Email(string email, bool rejected)
    {
        var errors = RequestValidator.ValidateCustomer(new CustomerWriteRequest("Ana", "Torres", null, email));

        Assert.Equal(rejected, errors.ContainsKey("email"));
    }

    [Fact]
    public void NormalizeEmail_TrimsLowercasesAndNullsBlank()
    {
        Assert.Equal("ana@example.test", RequestValidator.NormalizeEmail("  Ana@Example.TEST "));
        Assert.Null(RequestValidator.NormalizeEmail("   "));
    }

    [Fact]
    public void Distribution_DefaultDate_IsRejected()
    {
        var errors = RequestValidator.ValidateDistribution(new DistributionWriteRequest(1, 1, default, 1));

        Assert.True(errors.ContainsKey("distributionDate"));
    }
}
