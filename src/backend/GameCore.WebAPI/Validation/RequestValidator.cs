using System.Text.RegularExpressions;

namespace GameCore.WebAPI.Validation;

/// <summary>
/// Shape validation for write requests (required values, lengths, ranges, duplicates).
/// Checks that need the database (foreign keys, uniqueness) live in the endpoints.
/// Limits mirror the column definitions in <c>database/schema.sql</c>.
/// </summary>
public static partial class RequestValidator
{
    public const decimal MaxUnitPrice = 9_999_999_999.99m; // decimal(12,2)
    public const int MaxSaleQuantity = 10_000;
    public static readonly DateOnly MinDate = new(1950, 1, 1);

    public static Dictionary<string, string[]> ValidateLogin(LoginRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;
        if (string.IsNullOrWhiteSpace(request.Email)) errors.Add("email", "El correo es obligatorio.");
        if (string.IsNullOrEmpty(request.Password)) errors.Add("password", "La contraseña es obligatoria.");
        return errors.Result;
    }

    public static Dictionary<string, string[]> ValidateGame(GameWriteRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;

        RequiredText(errors, "title", "El título", request.Title, 180);
        if (request.UnitPrice < 0 || request.UnitPrice > MaxUnitPrice)
            errors.Add("unitPrice", "El precio debe estar entre 0 y 9,999,999,999.99.");
        else if (decimal.Round(request.UnitPrice, 2) != request.UnitPrice)
            errors.Add("unitPrice", "El precio admite como máximo dos decimales.");
        if (request.ReleaseDate is { } date && (date < MinDate || date > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(10)))
            errors.Add("releaseDate", "La fecha de lanzamiento no es válida.");
        if (request.AgeRatingId is <= 0)
            errors.Add("ageRatingId", "La clasificación indicada no es válida.");
        Ids(errors, "genreIds", "géneros", request.GenreIds);
        Ids(errors, "platformIds", "plataformas", request.PlatformIds);
        return errors.Result;
    }

    public static Dictionary<string, string[]> ValidateCustomer(CustomerWriteRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;

        RequiredText(errors, "firstName", "El nombre", request.FirstName, 80);
        RequiredText(errors, "lastName", "El apellido", request.LastName, 80);
        Phone(errors, request.Phone);
        Email(errors, request.Email);
        return errors.Result;
    }

    public static Dictionary<string, string[]> ValidateEmployee(EmployeeWriteRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;

        RequiredText(errors, "firstName", "El nombre", request.FirstName, 80);
        RequiredText(errors, "lastName", "El apellido", request.LastName, 80);
        if (request.BranchId <= 0) errors.Add("branchId", "La sucursal es obligatoria.");
        if (request.JobPositionId <= 0) errors.Add("jobPositionId", "El puesto es obligatorio.");
        Phone(errors, request.Phone);
        Email(errors, request.Email);
        if (Normalize(request.AddressLine) is { Length: > 250 })
            errors.Add("addressLine", "La dirección admite como máximo 250 caracteres.");
        return errors.Result;
    }

    public static Dictionary<string, string[]> ValidateSale(SaleCreateRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;

        if (request.CustomerId <= 0) errors.Add("customerId", "El cliente es obligatorio.");
        if (request.EmployeeId is <= 0) errors.Add("employeeId", "El empleado indicado no es válido.");

        if (request.Items is null || request.Items.Length == 0)
            return errors.Add("items", "La venta debe incluir al menos un videojuego.").Result;

        if (request.Items.Any(item => item is null || item.GameId <= 0))
            errors.Add("items", "Cada línea debe indicar un videojuego válido.");
        if (request.Items.Any(item => item is not null && (item.Quantity <= 0 || item.Quantity > MaxSaleQuantity)))
            errors.Add("items", $"La cantidad de cada línea debe estar entre 1 y {MaxSaleQuantity:N0}.");
        if (request.Items.Where(item => item is not null).GroupBy(item => item.GameId).Any(group => group.Count() > 1))
            errors.Add("items", "Un mismo videojuego no puede repetirse en la venta; ajusta la cantidad de esa línea.");
        return errors.Result;
    }

    public static Dictionary<string, string[]> ValidateDistribution(DistributionWriteRequest? request)
    {
        var errors = new ErrorBag();
        if (request is null) return errors.Add("body", "El cuerpo de la solicitud es obligatorio.").Result;

        if (request.GameId <= 0) errors.Add("gameId", "El videojuego es obligatorio.");
        if (request.CountryId <= 0) errors.Add("countryId", "El país es obligatorio.");
        if (request.DistributionDate < MinDate || request.DistributionDate > DateOnly.FromDateTime(DateTime.UtcNow).AddYears(10))
            errors.Add("distributionDate", "La fecha de distribución no es válida.");
        if (request.Units < 0) errors.Add("units", "Las unidades no pueden ser negativas.");
        return errors.Result;
    }

    /// <summary>Trims a value and converts empty/whitespace to null.</summary>
    public static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static string? NormalizeEmail(string? value) => Normalize(value)?.ToLowerInvariant();

    private static void RequiredText(ErrorBag errors, string field, string label, string? value, int maxLength)
    {
        var normalized = Normalize(value);
        if (normalized is null) errors.Add(field, $"{label} es obligatorio.");
        else if (normalized.Length > maxLength) errors.Add(field, $"{label} admite como máximo {maxLength} caracteres.");
    }

    private static void Phone(ErrorBag errors, string? value)
    {
        var phone = Normalize(value);
        if (phone is not null && !PhonePattern().IsMatch(phone))
            errors.Add("phone", "El teléfono solo admite dígitos, espacios y los caracteres + - ( ) . (máximo 20).");
    }

    private static void Email(ErrorBag errors, string? value)
    {
        var email = Normalize(value);
        if (email is null) return;
        if (email.Length > 150 || !EmailPattern().IsMatch(email))
            errors.Add("email", "El correo no tiene un formato válido.");
    }

    private static void Ids(ErrorBag errors, string field, string label, int[]? ids)
    {
        if (ids is null) return;
        if (ids.Any(id => id <= 0)) errors.Add(field, $"La lista de {label} contiene identificadores inválidos.");
        if (ids.Length != ids.Distinct().Count()) errors.Add(field, $"La lista de {label} contiene identificadores repetidos.");
    }

    // ASCII only: the Email columns are varchar(150).
    [GeneratedRegex(@"^[A-Za-z0-9._%+'\-]+@[A-Za-z0-9\-]+(\.[A-Za-z0-9\-]+)*\.[A-Za-z]{2,}$")]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"^[0-9+()\-. ]{1,20}$")]
    private static partial Regex PhonePattern();

    private sealed class ErrorBag
    {
        private readonly Dictionary<string, List<string>> _errors = new(StringComparer.Ordinal);

        public ErrorBag Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var list)) _errors[field] = list = [];
            if (!list.Contains(message)) list.Add(message);
            return this;
        }

        public Dictionary<string, string[]> Result => _errors.ToDictionary(x => x.Key, x => x.Value.ToArray());
    }
}
