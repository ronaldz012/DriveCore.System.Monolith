namespace Module.Auth.Application.UseCases.Branches.SetBranchCompleteness;

public class SetBranchCompletenessRequest
{
    /// <summary>
    /// Fecha desde la cual la información de la branch está completa.
    /// Operación sin retorno: una vez fijada no se puede revertir a transición.
    /// </summary>
    public DateTime CompleteSince { get; set; }
}
