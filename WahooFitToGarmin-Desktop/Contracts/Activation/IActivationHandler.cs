using System.Threading.Tasks;

namespace WahooFitToGarmin_Desktop.Contracts.Activation
{
    public interface IActivationHandler
    {
        bool CanHandle();

        Task HandleAsync();
    }
}
