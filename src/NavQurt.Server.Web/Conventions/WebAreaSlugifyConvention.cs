using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace NavQurt.Server.Web.Conventions
{
    public class WebAreaSlugifyConvention : IControllerModelConvention
    {
        private readonly IOutboundParameterTransformer _transformer;

        public WebAreaSlugifyConvention(IOutboundParameterTransformer transformer)
        {
            _transformer = transformer;
        }

        public void Apply(ControllerModel controller)
        {
            var areaName = controller.Attributes
                .OfType<Microsoft.AspNetCore.Mvc.AreaAttribute>()
                .FirstOrDefault()?.RouteValue;

            if (string.Equals(areaName, "Web", StringComparison.OrdinalIgnoreCase))
            {
                controller.ControllerName = _transformer.TransformOutbound(controller.ControllerName);
            }
        }
    }
}
