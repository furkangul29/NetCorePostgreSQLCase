using Microsoft.AspNetCore.Mvc;


namespace WebUI.ViewComponents.UILayoutViewComponents
{
    public class _UILayoutHeaderComponentPartial : ViewComponent
    {
        public async Task< IViewComponentResult> InvokeAsync()
        {
     
            return View();
        }
    }
}
