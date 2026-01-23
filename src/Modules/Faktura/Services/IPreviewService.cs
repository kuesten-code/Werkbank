using Kuestencode.Core.Models;
using Kuestencode.Faktura.Models;

namespace Kuestencode.Faktura.Services;

public interface IPreviewService
{
    Invoice GenerateSampleInvoice(Company company);
}
