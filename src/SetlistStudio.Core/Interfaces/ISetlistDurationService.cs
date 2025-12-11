using System.Threading.Tasks;
using SetlistStudio.Core.Entities;
using SetlistStudio.Core.Models;

namespace SetlistStudio.Core.Interfaces;

public interface ISetlistDurationService
{
    Task<SetlistDurationResult?> CalculateDurationAsync(int setlistId, string userId);
}
