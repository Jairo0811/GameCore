using System.Net;
using System.Net.Http.Json;
using GameCore.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace GameCore.Tests;

public sealed class CustomerTests : ApiTestBase
{
    [Fact]
    public async Task UpdateCustomer_WithBlankNames_Returns400AndKeepsData()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/customers/{ids.CustomerWithEmailId}", new { firstName = "  ", lastName = (string?)null, phone = (string?)null, email = (string?)null });

        var problem = await AssertProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Contains("firstName", Errors(problem));
        Assert.Contains("lastName", Errors(problem));
        Assert.Equal("Ana", await Factory.QueryAsync(db => db.Customers.Where(x => x.CustomerId == ids.CustomerWithEmailId).Select(x => x.FirstName).SingleAsync()));
    }

    [Fact]
    public async Task CreateCustomer_WithDuplicateEmail_Returns409()
    {
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new { firstName = "Otra", lastName = "Ana", phone = (string?)null, email = "ANA@test.local" });

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateCustomer_KeepingOwnEmail_Succeeds_ButTakingAnotherEmail_Returns409()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var same = await client.PutAsJsonAsync($"/api/customers/{ids.CustomerWithEmailId}", new { firstName = "Ana", lastName = "Torres", phone = (string?)null, email = "ana@test.local" });
        var taken = await client.PutAsJsonAsync($"/api/customers/{ids.CustomerWithoutEmailId}", new { firstName = "Carlos", lastName = "Méndez", phone = (string?)null, email = "ana@test.local" });

        Assert.Equal(HttpStatusCode.NoContent, same.StatusCode);
        await AssertProblemAsync(taken, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task SeveralCustomersWithoutEmail_AreAllowed()
    {
        var client = await ManagerAsync();

        var first = await client.PostAsJsonAsync("/api/customers", new { firstName = "Sin", lastName = "Correo", phone = (string?)null, email = "" });
        var second = await client.PostAsJsonAsync("/api/customers", new { firstName = "Tampoco", lastName = "Correo", phone = (string?)null, email = (string?)null });

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
        Assert.Equal(3, await Factory.QueryAsync(db => db.Customers.CountAsync(x => x.Email == null)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@b")]
    [InlineData("josé@test.local")]
    public async Task CreateCustomer_WithInvalidEmail_Returns400(string email)
    {
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/customers", new { firstName = "Correo", lastName = "Inválido", phone = (string?)null, email });

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCustomer_Unknown_Returns404()
    {
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync("/api/customers/9999", new { firstName = "A", lastName = "B", phone = (string?)null, email = (string?)null });

        await AssertProblemAsync(response, HttpStatusCode.NotFound);
    }
}

public sealed class EmployeeTests : ApiTestBase
{
    private static object Employee(int branchId, int jobPositionId, string? email = null, bool isActive = true, string firstName = "Nuevo") => new
    {
        branchId, jobPositionId, firstName, lastName = "Empleado", phone = (string?)null, email, addressLine = (string?)null, isActive
    };

    [Fact]
    public async Task CreateEmployee_Inactive_IsStoredAsInactive()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/employees", Employee(ids.BranchId, ids.JobPositionId, isActive: false));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await ReadJsonAsync(response)).GetProperty("employeeId").GetInt32();
        Assert.False(await Factory.QueryAsync(db => db.Employees.Where(x => x.EmployeeId == id).Select(x => x.IsActive).SingleAsync()));
    }

    [Theory]
    [InlineData("branch")]
    [InlineData("position")]
    public async Task CreateEmployee_WithUnknownReference_Returns400NotA500(string reference)
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var payload = reference == "branch" ? Employee(9999, ids.JobPositionId) : Employee(ids.BranchId, 9999);
        var response = await client.PostAsJsonAsync("/api/employees", payload);

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateEmployee_WithUnknownBranch_Returns400()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PutAsJsonAsync($"/api/employees/{ids.ActiveEmployeeId}", Employee(9999, ids.JobPositionId, firstName: "Valeria"));

        await AssertProblemAsync(response, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateEmployee_WithDuplicateEmail_Returns409()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var response = await client.PostAsJsonAsync("/api/employees", Employee(ids.BranchId, ids.JobPositionId, email: "valeria@test.local"));

        await AssertProblemAsync(response, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateEmployee_CanDeactivateAndReactivate()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var off = await client.PutAsJsonAsync($"/api/employees/{ids.ActiveEmployeeId}", Employee(ids.BranchId, ids.JobPositionId, "valeria@test.local", isActive: false, firstName: "Valeria"));
        Assert.Equal(HttpStatusCode.NoContent, off.StatusCode);
        Assert.False(await Factory.QueryAsync(db => db.Employees.Where(x => x.EmployeeId == ids.ActiveEmployeeId).Select(x => x.IsActive).SingleAsync()));

        var on = await client.PutAsJsonAsync($"/api/employees/{ids.ActiveEmployeeId}", Employee(ids.BranchId, ids.JobPositionId, "valeria@test.local", isActive: true, firstName: "Valeria"));
        Assert.Equal(HttpStatusCode.NoContent, on.StatusCode);
        Assert.True(await Factory.QueryAsync(db => db.Employees.Where(x => x.EmployeeId == ids.ActiveEmployeeId).Select(x => x.IsActive).SingleAsync()));
    }

    [Fact]
    public async Task SeveralEmployeesWithoutEmail_AreAllowed()
    {
        var ids = await SeedIds.LoadAsync(Factory);
        var client = await ManagerAsync();

        var first = await client.PostAsJsonAsync("/api/employees", Employee(ids.BranchId, ids.JobPositionId, firstName: "Uno"));
        var second = await client.PostAsJsonAsync("/api/employees", Employee(ids.BranchId, ids.JobPositionId, firstName: "Dos"));

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }
}
