namespace GameCore.WebAPI.Errors;

/// <summary>
/// RFC 7807 responses with user-facing (Spanish) messages, consumed by the frontend.
/// </summary>
public static class ApiProblems
{
    public static IResult Validation(IDictionary<string, string[]> errors) =>
        Results.ValidationProblem(errors, title: "La solicitud contiene datos inválidos.");

    public static IResult Validation(string field, string message) =>
        Validation(new Dictionary<string, string[]> { [field] = [message] });

    public static IResult NotFound(string message) =>
        Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Recurso no encontrado.", detail: message);

    public static IResult Conflict(string message) =>
        Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Conflicto con datos existentes.", detail: message);
}
