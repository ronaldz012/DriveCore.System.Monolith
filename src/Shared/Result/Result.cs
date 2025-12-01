using System;

namespace Shared.Result;
/// <summary>
/// Representa el resultado de una operación que puede tener éxito o fallar.
/// Implementa el patrón Railway Oriented Programming (ROP).
/// </summary>
/// <typeparam name="TValue">Tipo del valor cuando la operación es exitosa</typeparam>
public readonly record struct Result<TValue>
{
    /// <summary>
    /// Valor cuando la operación fue exitosa. Será null si falló.
    /// </summary>
    public TValue? Value { get; }

    /// <summary>
    /// Información del error cuando la operación falló. Será null si fue exitosa.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Indica si la operación fue exitosa (true) o falló (false).
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Constructor privado para crear un resultado exitoso.
    /// Solo se usa internamente a través de la conversión implícita.
    /// </summary>
    /// <param name="value">Valor de retorno exitoso</param>
    private Result(TValue value)
    {
        Value = value;
        IsSuccess = true;
        Error = null;
    }

    /// <summary>
    /// Constructor privado para crear un resultado fallido.
    /// Solo se usa internamente a través de la conversión implícita.
    /// </summary>
    /// <param name="error">Información del error</param>
    private Result(Error error)
    {
        Error = error;
        IsSuccess = false;
        Value = default; // null para tipos referencia, 0 para int, etc.
    }

    /// <summary>
    /// Permite convertir automáticamente un valor al tipo Result exitoso.
    /// Ejemplo: return menuId; (int) se convierte a Result{int} exitoso
    /// </summary>
    public static implicit operator Result<TValue>(TValue value) => new(value);

    /// <summary>
    /// Permite convertir automáticamente un Error al tipo Result fallido.
    /// Ejemplo: return new Error("CODE", "msg"); se convierte a Result{T} fallido
    /// </summary>
    public static implicit operator Result<TValue>(Error error) => new(error);
}

/// <summary>
/// Representa un error con un código identificador y un mensaje descriptivo.
/// </summary>
/// <param name="Code">Código único del error (ejemplo: "VALIDATION_ERROR", "NOT_FOUND")</param>
/// <param name="Message">Mensaje descriptivo del error para el usuario o logs</param>
public record Error(string Code, string Message);