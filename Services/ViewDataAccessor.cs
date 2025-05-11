using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace SkillSwap.Services
{
    public interface IViewDataAccessor
    {
        ViewDataDictionary ViewData { get; }
    }

    public class ViewDataAccessor : IViewDataAccessor
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ViewDataAccessor(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public ViewDataDictionary ViewData
        {
            get
            {
                // In Razor Pages, ActionContext doesn't contain ViewData directly
                // We need to check if the context contains a PageContext or a ViewContext
                var httpContext = _httpContextAccessor.HttpContext;
                
                if (httpContext?.Items["PageContext"] is PageContext pageContext)
                {
                    return pageContext.ViewData;
                }
                
                if (httpContext?.Items["ViewContext"] is ViewContext viewContext)
                {
                    return viewContext.ViewData;
                }
                
                // Create a default empty ViewDataDictionary if we can't find one
                return new ViewDataDictionary(
                    new EmptyModelMetadataProvider(),
                    new ModelStateDictionary());
            }
        }
    }
}

