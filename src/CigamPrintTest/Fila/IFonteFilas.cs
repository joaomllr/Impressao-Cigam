using System.Collections.Generic;

namespace CigamPrintTest.Fila
{
    /// <summary>
    /// Abstração para consulta de impressoras e jobs do spooler. Permite mock em testes unitários.
    /// </summary>
    public interface IFonteFilas
    {
        IEnumerable<InfoImpressora> ObterImpressoras();
        IEnumerable<InfoJob> ObterTodosJobs();
        InfoImpressora ObterStatusFila(string nomeFila);
    }
}
