using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace cpcx.Infrastructure;

public class DisableControllerConvention(Type controllerType) : IApplicationModelConvention
{
    public void Apply(ApplicationModel application)
    {
        var controller = application.Controllers
            .FirstOrDefault(c => c.ControllerType == controllerType);
        if (controller is not null)
            application.Controllers.Remove(controller);
    }
}
